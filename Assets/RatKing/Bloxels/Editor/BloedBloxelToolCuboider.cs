using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RatKing.Bloxels;

namespace RatKing {

	public class BloedBloxelToolCuboider : BloedBloxelTool {
		public bool AllowsPivotPlaceMode { get { return true; } set { } }
		public bool AllowsGridMode { get { return true; } set { } }
		public bool AllowsWireCubes { get { return true; } set { } }
		public bool AllowsChangingSelectedTemplate { get { return false; } set { } }
		public bool AllowsChangingSelectedTexture { get { return false; } set { } }
		public Bloed.OverwritePickInner OverwritePickInner { get { return Bloed.OverwritePickInner.UseCurTemplate; } set { } }
		public bool RepaintSceneOnSubBloxelMouseDrag { get { return drawBox; } set { } }

		//

		bool showPosAndSize = true;
		bool hasBox;
		int newBoxSize = 5;
		Base.Position3 start;
		Base.Position3 end;
		bool hollow = false;
		int wallThickness = 1;
		bool texturePadding = false;
		bool drawBox = false;
		bool centerSelection = true;
		bool negativeSelection = false;
		bool moveSelectionWithGrid = true;

		//

		public void OnClick(Bloed bloed, Event evt) {
			if (bloed == null) { return; }

			var mouseDown = !evt.alt && evt.type == EventType.MouseDown;
			var mouseUp = evt.type == EventType.MouseUp;

			if (mouseDown && evt.button == 0 && evt.control) {
				drawBox = true;
				end = start = bloed.CurPos.Value;
				hasBox = true;
				evt.Use();
			}
			else if (mouseDown && evt.button == 0 && !evt.control && bloed.CurPos != null) {
				Base.Position3 diff = end - start, half = diff / 2;
				if (bloed.IsGridVisible) {
					if (bloed.gridMode == Bloed.GridMode.X) { half.x = 0; if (!hasBox) { diff.x = newBoxSize - 1; } }
					if (bloed.gridMode == Bloed.GridMode.Y) { half.y = 0; if (!hasBox) { diff.y = newBoxSize - 1; } }
					if (bloed.gridMode == Bloed.GridMode.Z) { half.z = 0; if (!hasBox) { diff.z = newBoxSize - 1; } }
				}
				start = bloed.CurPos.Value - (centerSelection ? half : Base.Position3.zero);
				end = bloed.CurPos.Value + diff - (centerSelection ? half : Base.Position3.zero);
				evt.Use();
			}

			if (drawBox) {
				end = bloed.CurPos.Value;
				if (bloed.IsGridVisible) {
					if (bloed.gridMode == Bloed.GridMode.X) { end.x = start.x + (negativeSelection ? -1 : 1) * (newBoxSize - 1); }
					if (bloed.gridMode == Bloed.GridMode.Y) { end.y = start.y + (negativeSelection ? -1 : 1) * (newBoxSize - 1); }
					if (bloed.gridMode == Bloed.GridMode.Z) { end.z = start.z + (negativeSelection ? -1 : 1) * (newBoxSize - 1); }
				}
			}

			if (mouseUp && evt.button == 0) {
				drawBox = false;
				if (start.x > end.x) { var t = start.x; start.x = end.x; end.x = t; }
				if (start.y > end.y) { var t = start.y; start.y = end.y; end.y = t; }
				if (start.z > end.z) { var t = start.z; start.z = end.z; end.z = t; }
			}
		}

		void ChangeBloxels(Bloed bloed, System.Func<Bloxel, Base.Position3, Bloxel> method) {
			var level = BloxelLevel.Current;
			if (!hasBox || bloed == null || level == null) { return; }

			var diff = end - start;

			var count = Mathf.Abs(diff.x * diff.y * diff.z);
			var targetBloxels = new Dictionary<Base.Position3, Bloxel>(count);
			var sourceBloxels = new Dictionary<Base.Position3, Bloxel>(count);
			var paddingBloxels = new HashSet<Base.Position3>();

			var half = diff / 2;
			var wt = wallThickness;
			for (int z = 0; z <= diff.z; ++z) {
				for (int y = 0; y <= diff.y; ++y) {
					for (int x = 0; x <= diff.x; ++x) {
						if (hollow && !(y < wt || y > diff.y - wt || x < wt || x > diff.x - wt || z < wt || z > diff.z - wt)) {
							continue;
						}
						var pos = start + new Base.Position3(x, y, z);
						var oldBloxel = level.GetBloxel(pos);
						targetBloxels[pos] = oldBloxel;
						sourceBloxels[pos] = method(oldBloxel, pos);
						if (texturePadding) {
							foreach (var n in BloxelUtility.faceDirPositions) { paddingBloxels.Add(pos + n); }
						}
					}
				}
			}

			if (texturePadding) {
				foreach (var b in targetBloxels) { paddingBloxels.Remove(b.Key); }
				paddingBloxels.RemoveWhere(pos => {
					var oldBloxel = level.GetBloxel(pos);
					if (oldBloxel.IsAir) { return true; }
					var newTexIdx = method(oldBloxel, pos).textureIdx;
					if (newTexIdx <= 0) { if (bloed.CurBloxTexIdxOfLevel <= 0) { return false; } newTexIdx = bloed.CurBloxTexIdxOfLevel; }
					if (oldBloxel.textureIdx == newTexIdx) { return true; }
					return false;
				});
				// the following bloxels change their texture only, not their type
				foreach (var pos in paddingBloxels) {
					var oldBloxel = level.GetBloxel(pos);
					targetBloxels[pos] = oldBloxel;
					var newTexIdx = method(oldBloxel, pos).textureIdx;
					if (newTexIdx <= 0) { newTexIdx = bloed.CurBloxTexIdxOfLevel; }
					sourceBloxels[pos] = new Bloxel(oldBloxel.template, newTexIdx);
				}
			}

			BloedUndoRedo.AddAction(() => {
				Selection.activeGameObject = null;
				level.UpdateSeveralChunksStart();
				foreach (var s in sourceBloxels) {
					level.ChangeBloxel(s.Key, s.Value.template, s.Value.textureIdx);
				}
				level.UpdateSeveralChunksEnd();
				Bloed.MarkSceneDirty();
				return true;
			},
			() => {
				if (level == null) { level = BloxelLevel.Current; if (level == null) { Debug.LogWarning("Undo stack got corrupted!"); return; } }
				Selection.activeGameObject = null;
				level.UpdateSeveralChunksStart();
				foreach (var t in targetBloxels) {
					level.ChangeBloxel(t.Key, t.Value.template, t.Value.textureIdx);
				}
				level.UpdateSeveralChunksEnd();
				Bloed.MarkSceneDirty();
			});
		}

		//

		public void OnSceneGUI(Bloed bloed, SceneView view) {
			EditorGUIUtility.labelWidth = 100f;
			GUILayout.Label("Tool: CUBOIDER");
			
			if (bloed.IsGridVisible) {
				var gridPos3 = Base.Position3.FlooredVector(Bloed.Vec3InvTrPos(view.pivot));
				var diff = end - start;
				switch (bloed.gridMode) {
					case Bloed.GridMode.X: if (negativeSelection && !drawBox) { gridPos3.x -= diff.x; } start.x = gridPos3.x; end.x = start.x + diff.x; break;
					case Bloed.GridMode.Y: if (negativeSelection && !drawBox) { gridPos3.y -= diff.y; } start.y = gridPos3.y; end.y = start.y + diff.y; break;
					case Bloed.GridMode.Z: if (negativeSelection && !drawBox) { gridPos3.z -= diff.z; } start.z = gridPos3.z; end.z = start.z + diff.z; break;
				}
			}

			var helpText = "<i> • Ctrl+Drag - Create box";
			if (bloed.IsGridVisible) {
				var diff = end - start;
				switch (bloed.gridMode) {
					case Bloed.GridMode.X: helpText += "\n • Tab - Switch Y and Z sides"; break;
					case Bloed.GridMode.Y: helpText += "\n • Tab - Switch X and Z sides"; break;
					case Bloed.GridMode.Z: helpText += "\n • Tab - Switch X and Y sides"; break;
				}
			}
			else {
				helpText += "\n • Press G to activate grid!";
			}
			GUILayout.Label(helpText + "</i>");
			
			var h = GUILayout.Height(20f);

			if (!hollow) {
				hollow = GUILayout.Toggle(hollow, "Fill Hollow");
			}
			else {
				GUILayout.BeginHorizontal();
				hollow = GUILayout.Toggle(hollow, "Fill Hollow - Thick", h);
				if (GUILayout.Button("-", GUILayout.Width(20f), h) && wallThickness > 1) { wallThickness -= 1; }
				wallThickness = Mathf.Max(1, EditorGUILayout.IntField(wallThickness, GUILayout.Width(20f), h));
				if (GUILayout.Button("+", GUILayout.Width(20f), h)) { wallThickness += 1; }
				GUILayout.EndHorizontal();
			}

			texturePadding = GUILayout.Toggle(texturePadding, "Texture Padding");

			GUILayout.Space(5f);
			
			EditorGUIUtility.labelWidth = 65f;

			if (bloed.IsGridVisible) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("New Box Height", h);
				if (GUILayout.Button("-", GUILayout.Width(20f), h)) { newBoxSize -= 1; }
				newBoxSize = Mathf.Max(1, EditorGUILayout.DelayedIntField(newBoxSize, GUILayout.Width(40f), h));
				if (GUILayout.Button("+", GUILayout.Width(20f), h)) { newBoxSize += 1; }
				GUILayout.EndHorizontal();
				
				negativeSelection = GUILayout.Toggle(negativeSelection, "Negative Height");
			}

			centerSelection = GUILayout.Toggle(centerSelection, "Center Box");
			moveSelectionWithGrid = GUILayout.Toggle(moveSelectionWithGrid, "Move Box With Grid");

			if (hasBox) {
				ChangeSize(ref start, ref end, showPosAndSize);

				GUILayout.BeginHorizontal();
				if (GUILayout.Button(showPosAndSize ? "Pos/Size" : "Min/Max")) {
					showPosAndSize = !showPosAndSize;
				}
				if (GUILayout.Button("Clear Selection")) {
					hasBox = drawBox = false;
					start = end = Base.Position3.zero;
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5f);
				
				if (GUILayout.Button("Fill (<color=#66ff66>" + bloed.CurBloxTmp.ID + "</color>/<color=#66ff66>" + bloed.CurBloxTex.ID + "</color>)")) {
					var bloxel = new Bloxel(bloed.CurBloxTmp, bloed.CurBloxTexIdxOfLevel);
					ChangeBloxels(bloed, (o, p) => bloxel);
				}
				if (GUILayout.Button("Fill With Air")) {
					var air = new Bloxel(bloed.ProjectSettings.AirTemplate, 0);
					ChangeBloxels(bloed, (o, p) => air);
				}
				if (BloxelLevel.Current.Settings.StandardTypeMethod != BloxelLevelSettings.StandardMethodType.SingleBloxel || !BloxelLevel.Current.Settings.StandardBloxels[0].isAir) {
					if (GUILayout.Button("Fill With Default Bloxels")) {
						ChangeBloxels(bloed, (o, p) => BloxelLevel.Current.Settings.GetStandardBloxel(p));
					}
				}
				if (GUILayout.Button("Texturize (<color=#66ff66>" + bloed.CurBloxTex.ID + "</color>)")) {
					ChangeBloxels(bloed, (o, p) => new Bloxel(o.template, bloed.CurBloxTexIdxOfLevel));
				}

				// TODO negative grid
			}
		}

		public void DrawHandles(Bloed bloed) {
			if (!hasBox) { return; }

			var s = start;
			var e = end;
			if (s.x > e.x) { var t = s.x; s.x = e.x; e.x = t; }
			if (s.y > e.y) { var t = s.y; s.y = e.y; e.y = t; }
			if (s.z > e.z) { var t = s.z; s.z = e.z; e.z = t; }

			var handlesColor = Handles.color;

			var col = Color.red;
			Handles.color = col;
			Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
			var startV = s.ToVector();
			var diffV = e.ToVector() - startV;
			var centerV = startV + new Vector3(0.5f, 0.5f, 0.5f) + diffV * 0.5f;
			var sizeV = diffV + Vector3.one * 1.1f;
			
			// the source box
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 0, sizeV.x, sizeV.z, sizeV.y), col.WithAlpha(0.2f), col);
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 3, sizeV.x, sizeV.z, sizeV.y), col.WithAlpha(0.2f), col);
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 1, sizeV.x, sizeV.y, sizeV.z), col.WithAlpha(0.2f), col);
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 4, sizeV.x, sizeV.y, sizeV.z), col.WithAlpha(0.2f), col);
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 2, sizeV.y, sizeV.z, sizeV.x), col.WithAlpha(0.2f), col);
			Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 5, sizeV.y, sizeV.z, sizeV.x), col.WithAlpha(0.2f), col);

			//

			if (!drawBox && !Event.current.control && bloed.CurPos != null) {
				col = Color.yellow;
				Handles.color = col;

				var posV = bloed.CurPos.Value.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);
				if (negativeSelection && bloed.IsGridVisible) {
					if (bloed.gridMode == Bloed.GridMode.X) { posV.x += diffV.x; }
					if (bloed.gridMode == Bloed.GridMode.Y) { posV.y -= diffV.y; }
					if (bloed.gridMode == Bloed.GridMode.Z) { posV.z -= diffV.z; }
				}
				centerV = posV + diffV * 0.5f;
				if (centerSelection) {
					var h = (Base.Position3.RoundedVector(diffV) / 2).ToVector();		
					if (!bloed.IsGridVisible || bloed.gridMode != Bloed.GridMode.X) { centerV.x -= h.x; }
					if (!bloed.IsGridVisible || bloed.gridMode != Bloed.GridMode.Y) { centerV.y -= h.y; }
					if (!bloed.IsGridVisible || bloed.gridMode != Bloed.GridMode.Z) { centerV.z -= h.z; }
				}

				// the target box
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 0, sizeV.x, sizeV.z, sizeV.y), col.WithAlpha(0.1f), col);
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 3, sizeV.x, sizeV.z, sizeV.y), col.WithAlpha(0.1f), col);
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 1, sizeV.x, sizeV.y, sizeV.z), col.WithAlpha(0.1f), col);
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 4, sizeV.x, sizeV.y, sizeV.z), col.WithAlpha(0.1f), col);
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 2, sizeV.y, sizeV.z, sizeV.x), col.WithAlpha(0.1f), col);
				Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(centerV, 5, sizeV.y, sizeV.z, sizeV.x), col.WithAlpha(0.1f), col);
			}

			Handles.matrix = Matrix4x4.identity;
			Handles.color = handlesColor;
		}

		public void Hotkeys(Bloed bloed, Event evt, SceneView view) {
			if (evt.keyCode == KeyCode.Tab && bloed.IsGridVisible) {
				evt.Use();
				var d = end - start;
				var oldD = d;
				switch (bloed.gridMode) {
					case Bloed.GridMode.X: var tx = d.y; d.y = d.z; d.z = tx; break;
					case Bloed.GridMode.Y: var ty = d.x; d.x = d.z; d.z = ty; break;
					case Bloed.GridMode.Z: var tz = d.x; d.x = d.y; d.y = tz; break;
				}
				if (centerSelection) {
					start += oldD / 2;
					start -= d / 2;
				}
				end = start + d;
			}
		}

		public void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker) {
		}

		//

		public static void ChangeSize(ref Base.Position3 start, ref Base.Position3 end, bool posAndSize = false) {
			var s = start;
			var e = end;
			var d = end - start;
			var ws = 15f;
			var wb = GUILayout.Width((Bloed.TOOLS_WIDTH - ws) * 0.1f - 2f); // 4 times
			var wf = GUILayout.Width((Bloed.TOOLS_WIDTH - ws) * 0.2f - 2f - 5f); // 3 times
			var wl = GUILayout.Width((Bloed.TOOLS_WIDTH - ws) * 0.2f - 2f); // 3 times
			var h = GUILayout.Height(20f);

			GUILayout.BeginHorizontal();
			GUILayout.Label("", wl, h);
			GUILayout.Label(posAndSize ? "Pos" : "Min", "label centered");
			GUILayout.Space(ws);
			GUILayout.Label(posAndSize ? "Size" : "Max", "label centered");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("X:", "label centered", wl, h);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { s.x -= 1; e.x -= 1; }
				var posX = EditorGUILayout.DelayedIntField(s.x, wf, h);
				if (posX != s.x) { s.x = posX; e.x = s.x + d.x; }
				if (GUILayout.Button("+", wb, h)) { s.x += 1; e.x += 1; }
			}
			else {
				if (GUILayout.Button("-", wb, h)) { s.x -= 1; }
				s.x = EditorGUILayout.DelayedIntField(s.x, wf, h);
				if (GUILayout.Button("+", wb, h)) { s.x += 1; }
			}
			GUILayout.Space(ws);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { if (d.x > 0) { d.x -= 1; e.x = s.x + d.x; } else { s.x -= 1; e.x -= 1; } }
				var sizeX = EditorGUILayout.DelayedIntField(d.x + 1, wf, h);
				if (sizeX != d.x + 1) { d.x = sizeX - 1; e.x = s.x + d.x; }
				if (GUILayout.Button("+", wb, h)) { d.x += 1; e.x = s.x + d.x; }
			}
			else {
				if (GUILayout.Button("-", wb, h)) { e.x -= 1; }
				e.x = EditorGUILayout.DelayedIntField(e.x, wf, h);
				if (GUILayout.Button("+", wb, h)) { e.x += 1; }
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Y:", "label centered", wl, h);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { s.y -= 1; e.y -= 1; }
				var posY = EditorGUILayout.DelayedIntField(s.y, wf, h);
				if (posY != s.y) { s.y = posY; e.y = s.y + d.y; }
				if (GUILayout.Button("+", wb, h)) { s.y += 1; e.y += 1; }
			}
			else {
				if (GUILayout.Button("-", wb, h)) { s.y -= 1; }
				s.y = EditorGUILayout.DelayedIntField(s.y, wf, h);
				if (GUILayout.Button("+", wb, h)) { s.y += 1; }
			}
			GUILayout.Space(ws);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { if (d.y > 0) { d.y -= 1; e.y = s.y + d.y; } else { s.y -= 1; e.y -= 1; } }
				var sizeY = EditorGUILayout.DelayedIntField(d.y + 1, wf, h);
				if (sizeY != d.y + 1) { d.y = sizeY - 1; e.y = s.y + d.y; }
				if (GUILayout.Button("+", wb, h)) { d.y += 1; e.y = s.y + d.y;}
			}
			else {
				if (GUILayout.Button("-", wb, h)) { e.y -= 1; }
				e.y = EditorGUILayout.DelayedIntField(e.y, wf, h);
				if (GUILayout.Button("+", wb, h)) { e.y += 1; }
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Z:", "label centered", wl, h);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { s.z -= 1; e.z -= 1; }
				var posZ = EditorGUILayout.DelayedIntField(s.z, wf, h);
				if (posZ != s.z) { s.z = posZ; e.z = s.z + d.z; }
				if (GUILayout.Button("+", wb, h)) { s.z += 1; e.z += 1; }
			}
			else {
				if (GUILayout.Button("-", wb, h)) { s.z -= 1; }
				s.z = EditorGUILayout.DelayedIntField(s.z, wf, h);
				if (GUILayout.Button("+", wb, h)) { s.z += 1; }
			}
			GUILayout.Space(ws);
			if (posAndSize) {
				if (GUILayout.Button("-", wb, h)) { if (d.z > 0) { d.z -= 1; e.z = s.z + d.z; } else { s.z -= 1; e.z -= 1; } }
				var sizeZ = EditorGUILayout.DelayedIntField(d.z + 1, wf, h);
				if (sizeZ != d.z + 1) { d.z = sizeZ - 1; e.z = s.z + d.z; }
				if (GUILayout.Button("+", wb, h)) { d.z += 1; e.z = s.z + d.z; }
			}
			else {
				if (GUILayout.Button("-", wb, h)) { e.z -= 1; }
				e.z = EditorGUILayout.DelayedIntField(e.z, wf, h);
				if (GUILayout.Button("+", wb, h)) { e.z += 1; }
			}
			GUILayout.EndHorizontal();

			if (s != start || e != end) {
				if (s.x > e.x) { e.x = s.x + 1; }
				if (s.y > e.y) { e.y = s.y + 1; }
				if (s.z > e.z) { e.z = s.z + 1; }
				start = s;
				end = e;
			}
		}
	}

}