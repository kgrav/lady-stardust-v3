using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RatKing.Bloxels;

namespace RatKing {

	public class BloedBloxelToolCopyAndPaste : BloedBloxelTool {
		public bool AllowsPivotPlaceMode { get { return true; } set { } }
		public bool AllowsGridMode { get { return true; } set { } }
		public bool AllowsWireCubes { get { return true; } set { } }
		public bool AllowsChangingSelectedTemplate { get { return false; } set { } }
		public bool AllowsChangingSelectedTexture { get { return false; } set { } }
		public Bloed.OverwritePickInner OverwritePickInner { get { return pickInner ? Bloed.OverwritePickInner.AlwaysInner : Bloed.OverwritePickInner.AlwaysOuter; } set { } }
		public bool RepaintSceneOnSubBloxelMouseDrag { get { return true; } set { } }

		//

		bool showPosAndSize = false;
		bool pickInner = true;
		//
		bool hasBox;
		Base.Position3 start;
		Base.Position3 end;
		bool drawBox;
		int rotation = 0;
		bool moveSelection = true;
		bool replace = false;
		bool copyAir = true;
		bool overwriteNonAir = true;
		bool centerSelection = false;

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
				ChangeBloxels(bloed, bloed.CurPos.Value);
			}

			if (drawBox) {
				end = bloed.CurPos.Value;
			}

			if (mouseUp && evt.button == 0) {
				drawBox = false;
				if (start.x > end.x) { var t = start.x; start.x = end.x; end.x = t; }
				if (start.y > end.y) { var t = start.y; start.y = end.y; end.y = t; }
				if (start.z > end.z) { var t = start.z; start.z = end.z; end.z = t; }
			}
		}

		void ChangeBloxels(Bloed bloed, Base.Position3 pos) {
			var level = BloxelLevel.Current;
			if (!hasBox || bloed == null || level == null) { return; }

			var diff = end - start;

			var count = Mathf.Abs(diff.x * diff.y * diff.z);
			if (replace) { count *= 2; }
			var targetBloxels = new Dictionary<Base.Position3, Bloxel>(count);
			var sourceBloxels = new Dictionary<Base.Position3, Bloxel>(count);

			if (replace) {
				var sBlx = new Bloxel(bloed.CurBloxTmp, bloed.CurBloxTexIdxOfLevel);
				for (int z = 0; z <= diff.z; ++z) {
					for (int y = 0; y <= diff.y; ++y) {
						for (int x = 0; x <= diff.x; ++x) {
							var delta = new Base.Position3(x, y, z);
							sourceBloxels[start + delta] = sBlx;
						}
					}
				}
			}

			var half = diff / 2;

			for (int z = 0; z <= diff.z; ++z) {
				for (int y = 0; y <= diff.y; ++y) {
					for (int x = 0; x <= diff.x; ++x) {
						var delta = new Base.Position3(x, y, z);
						var orig = start + delta;
						if (centerSelection) { delta -= half; }
						var sBlx = level.GetBloxel(orig);
						if (replace) { targetBloxels[orig] = sBlx; }
						if (!copyAir && sBlx.IsAir) { continue; }
						if (rotation != 0) {
							int d = sBlx.template.Dir, r = (sBlx.template.Rot + rotation) % 4;
							if (sBlx.template.Type != null) {
								if (sBlx.template.Type.IsRotationPossible(d, r)) {
									sBlx = new Bloxel(sBlx.template.Type.Templates.Find(bt => bt.Dir == d && bt.Rot == r), sBlx.textureIdx);
								}
								else if (r >= 2 && sBlx.template.Type.IsRotationPossible(d, r % 2)) {
									sBlx = new Bloxel(sBlx.template.Type.Templates.Find(bt => bt.Dir == d && bt.Rot == (r % 2)), sBlx.textureIdx);
								}
							}
							//
							delta = Base.Position3.RoundedVector(Base.Math.RotateAround(delta.ToVector(), Vector3.zero, Vector3.up, 90f * rotation));
						}
						var tBlx = level.GetBloxel(pos + delta);
						if (!overwriteNonAir && !tBlx.IsAir) { continue; }
						sourceBloxels[pos + delta] = sBlx;
						targetBloxels[pos + delta] = tBlx;
					}
				}
			}

			var center = pos + half;
			var sourceStart = pos;
			var sourceEnd = pos + diff;
			var targetStart = start;
			var targetEnd = end;
			// center & rotate
			if (centerSelection) {
				sourceStart -= half; sourceEnd -= half;
				sourceStart = Base.Position3.RoundedVector(Base.Math.RotateAround(sourceStart.ToVector(), pos.ToVector(), Vector3.up, rotation * 90f));
				sourceEnd = Base.Position3.RoundedVector(Base.Math.RotateAround(sourceEnd.ToVector(), pos.ToVector(), Vector3.up, rotation * 90f));
			}
			else {
				sourceEnd = Base.Position3.RoundedVector(Base.Math.RotateAround(sourceEnd.ToVector(), sourceStart.ToVector(), Vector3.up, rotation * 90f));
			}

			BloedUndoRedo.AddAction(() => {
				Selection.activeGameObject = null;
				if (moveSelection) { start = sourceStart; end = sourceEnd; }
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
				if (moveSelection) { start = targetStart; end = targetEnd; }
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
			GUILayout.Label("Tool: COPY&PASTER");

			GUILayout.Label("<i> • Ctrl+Drag - Select source\n • 1 or 2 - Pick inner/outer bloxels\n • Tab - Change rotation</i>");

			pickInner = GUILayout.Toggle(pickInner, "Pick Inner Bloxels");

			EditorGUIUtility.labelWidth = 65f;
			rotation = EditorGUILayout.IntSlider("Rotation", rotation, 0, 3);
			centerSelection = GUILayout.Toggle(centerSelection, "Center Target Box");

			moveSelection = GUILayout.Toggle(moveSelection, "Move Selection To Copy");
			replace = GUILayout.Toggle(replace, "Replace Source");
			copyAir = GUILayout.Toggle(copyAir, "Copy Air");
			overwriteNonAir = GUILayout.Toggle(overwriteNonAir, "Overwrite Non-Air");

			if (hasBox) {
				BloedBloxelToolCuboider.ChangeSize(ref start, ref end, showPosAndSize);

				GUILayout.BeginHorizontal();
				if (GUILayout.Button(showPosAndSize ? "Pos/Size" : "Min/Max")) {
					showPosAndSize = !showPosAndSize;
				}
				if (GUILayout.Button("Clear Selection")) {
					hasBox = drawBox = false;
					start = end = Base.Position3.zero;
				}
				GUILayout.EndHorizontal();
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
				centerV = posV + diffV * 0.5f;
				centerV = Base.Math.RotateAround(centerV, posV, Vector3.up, rotation * 90f);
				if (rotation % 2 == 1) { var t = sizeV.x; sizeV.x = sizeV.z; sizeV.z = t; }
				if (centerSelection) {
					diffV = Base.Math.RotateAround(diffV, Vector3.zero, Vector3.up, rotation * 90f);
					centerV -= (Base.Position3.RoundedVector(diffV) / 2).ToVector();
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
			if (evt.keyCode == KeyCode.Alpha1) {
				evt.Use();
				pickInner = !pickInner;
			}
			else if (evt.keyCode == KeyCode.Alpha2) {
				evt.Use();
				pickInner = !pickInner;
			}
			else if (evt.keyCode == KeyCode.Tab) {
				evt.Use();
				rotation = (rotation + (evt.shift ? -1 : 1) + 4) % 4;
				bloed.UnfocusToolbar(view);
			}
		}

		public void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker) {
		}
	}

}