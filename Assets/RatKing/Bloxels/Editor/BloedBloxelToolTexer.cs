using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RatKing.Bloxels;

namespace RatKing {

	public class BloedBloxelToolTexer : BloedBloxelTool {
		public bool AllowsPivotPlaceMode { get { return changeMode == 1; } set { } }
		public bool AllowsGridMode { get { return changeMode == 1; } set { } }
		public bool AllowsWireCubes { get { return changeMode == 1; } set { } }
		public bool AllowsChangingSelectedTemplate { get { return false; } set { } }
		public bool AllowsChangingSelectedTexture { get { return true; } set { } }
		public Bloed.OverwritePickInner OverwritePickInner { get { return Bloed.OverwritePickInner.No; } set { } }
		public bool RepaintSceneOnSubBloxelMouseDrag { get { return false; } set { } }

		//

		static readonly Vector3[][] sides = new Vector3[][] {
			new[] { new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f) }, // top
			new[] { new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f) }, // back
			new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f) }, // left
			new[] { new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f) }, // bottom
			new[] { new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f) }, // forward
			new[] { new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f) }, // right
		};
		public static Vector3 GetSide(int side, int idx, float scaleX = 1f, float scaleY = 1f, float pad = 1f) {
			var s = sides[side][idx];
			switch (side) {
				default: case 3: return new Vector3(s.x * scaleX, s.y * pad, s.z * scaleY);
				case 1:	 case 4: return new Vector3(s.x * scaleX, s.y * scaleY, s.z * pad);
				case 2:  case 5: return new Vector3(s.x * pad, s.y * scaleX, s.z * scaleY);
			}
		}
		public static Vector3[] GetBloxelSide(Base.Position3 pos, int side, float scale = 1f, float pad = 1f) {
			for (int i = 0; i < 4; ++i) { tempSide[i] = GetSide(side, i, scale, scale, pad) + pos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f); }
			return tempSide;
		}
		public static Vector3[] GetBloxelSide(Vector3 pos, int side, float scaleX, float scaleY, float pad = 1f) {
			for (int i = 0; i < 4; ++i) { tempSide[i] = GetSide(side, i, scaleX, scaleY, pad) + pos; }
			return tempSide;
		}
		static Vector3[] tempSide = new Vector3[4];

		//
		
		static readonly string[] modeNames = new[] { "Normal", "Full", "Reset" };

		//

		int changeMode = 0;
		//bool changeFullBloxel = false;
		bool changeFullBloxelReset = true;
		bool changeTexture = true;
		int rotation = 0;
		bool changeRotationAbsolute = false;
		bool changeUvSet = false;
		//bool dontCreateOnNoChange = true;
		int uvSet = 0;
		int offset = 0;

		//

		public void OnClick(Bloed bloed, Event evt) {
			var curLevel = BloxelLevel.Current;
			if (bloed == null || curLevel == null) { return; }
			var curPos = bloed.CurPos.Value;

			var beType = evt.type == EventType.MouseDown;
			if (!beType && (changeMode == 1 || changeMode == 2 || changeRotationAbsolute || rotation == 0)) {
				beType = evt.type == EventType.MouseDrag;
			}

			var click = !evt.alt && beType;

			if (click && evt.button == 0 && !evt.control) {
				if (changeMode == 0) {
					if (bloed.MousecastChunks(evt, out var hit, out var _)) {
						var fd = (Base.Position3.FlooredVector(Bloed.Vec3InvTrPos(hit.point + hit.normal * 0.001f)) != curPos)
							? (BloxelUtility.faceDirIndexByPosition.TryGetValue(Base.Position3.RoundedVector(Bloed.Vec3InvTrDir(hit.normal)), out var f) ? f : 6)
							: 6;
						ChangeBloxelSide(bloed, curPos, fd, changeTexture, rotation, changeRotationAbsolute, offset, changeUvSet, uvSet);
					}
				}
				else if (changeMode == 1) {
					Selection.activeGameObject = null;
					var bloxel = curLevel.GetBloxel(curPos);
					if (bloxel.textureIdx != bloed.CurBloxTexIdxOfLevel || changeFullBloxelReset) {
						ChangeFullBloxel(bloed, curPos, bloed.CurBloxTexIdxOfLevel, changeFullBloxelReset);
					}
				}
				else if (changeMode == 2) {
					if (bloed.MousecastChunks(evt, out var hit, out var _)) {
						var fd = (Base.Position3.FlooredVector(Bloed.Vec3InvTrPos(hit.point + hit.normal * 0.001f)) != curPos)
							? (BloxelUtility.faceDirIndexByPosition.TryGetValue(Base.Position3.RoundedVector(Bloed.Vec3InvTrDir(hit.normal)), out var f) ? f : 6)
							: 6;
						RemoveBloxelSide(bloed, curPos, fd);
					}
				}
			}
		}

		void ChangeFullBloxel(Bloed bloed, Base.Position3 pos, int texIdx, bool resetSides) {
			var level = BloxelLevel.Current;
			if (bloed == null || level == null) { return; }
			var bloxel = level.GetBloxel(pos);
			var oldSideDatas = new Dictionary<int, SideExtraData>();
			for (int i = 0; i < 7; ++i) {
				if (level.GetBloxelTexSide(pos, i, out var sd)) { oldSideDatas.Add(i, sd); }
			}
			BloedUndoRedo.AddAction(() => {
				Selection.activeGameObject = null;
				bool dirty = false;
				if (resetSides && oldSideDatas.Count > 0 && level.RemoveBloxelTextureSideData(pos)) {
					Bloed.MarkSceneDirty();
					dirty = true;
				}
				if (level.ChangeBloxel(pos, bloxel.template, texIdx)) {
					Bloed.MarkSceneDirty();
					dirty = true;
				}
				return dirty;
			},
			() => {
				if (level == null) { level = BloxelLevel.Current; if (level == null) { Debug.LogWarning("Undo stack got corrupted!"); return; } }
				Selection.activeGameObject = null;
				if (level.ChangeBloxel(pos, bloxel.template, bloxel.textureIdx)) {
					Bloed.MarkSceneDirty();
				}
				if (resetSides && oldSideDatas.Count > 0) {
					foreach (var sd in oldSideDatas) {
						level.ChangeBloxelTextureSide(pos, sd.Key, sd.Value);
					}
					Bloed.MarkSceneDirty();
				}
			});
		}

		void ChangeBloxelSide(Bloed bloed, Base.Position3 pos, int fd, bool changeTexture, int changeRotation, bool changeRotationAbsolute, int offset, bool changeUvSet, int uvSet) {
			var level = BloxelLevel.Current;
			if (bloed == null || level == null) { return; }
			Selection.activeGameObject = null;

			var hadData = level.GetBloxelTexSide(pos, fd, out var oldSD);
			if (oldSD.ti == -1) { oldSD.ti = level.GetBloxel(pos).textureIdx; }
			if (oldSD.r == -1) { oldSD.r = 0; }
			if (oldSD.uv == -1) { oldSD.uv = 0; }
			if (oldSD.o == -1) { oldSD.o = 0; }

			BloedUndoRedo.AddAction(() => {
				var sd = new SideExtraData(
					changeTexture ? bloed.CurBloxTexIdxOfLevel : -1,
					changeRotationAbsolute ? changeRotation : (changeRotation != 0) ? ((changeRotation + oldSD.r) % 4) : -1,
					changeUvSet ? uvSet : -1,
					offset);
				
				if (level.ChangeBloxelTextureSide(pos, fd, sd) == SideExtraData.dontChange) { return false; }
				Bloed.MarkSceneDirty();
				return true;
			},
			() => {
				if (level == null) { level = BloxelLevel.Current; if (level == null) { Debug.LogWarning("Undo stack got corrupted!"); return; } }
				if (!hadData) { level.RemoveBloxelTextureSideData(pos, fd); }
				else { level.ChangeBloxelTextureSide(pos, fd, oldSD); }
				Bloed.MarkSceneDirty();
			});
		}

		void RemoveBloxelSide(Bloed bloed, Base.Position3 pos, int fd) {
			var level = BloxelLevel.Current;
			if (bloed == null || level == null) { return; }
			Selection.activeGameObject = null;

			if (!level.GetBloxelTexSide(pos, fd, out var oldSD)) { return; }
			if (oldSD.ti == -1) { oldSD.ti = level.GetBloxel(pos).textureIdx; }
			if (oldSD.r == -1) { oldSD.r = 0; }
			if (oldSD.uv == -1) { oldSD.uv = 0; }
			if (oldSD.o == -1) { oldSD.o = 0; }

			BloedUndoRedo.AddAction(() => {
				level.RemoveBloxelTextureSideData(pos, fd);
				Bloed.MarkSceneDirty();
				return true;
			},
			() => {
				if (level == null) { level = BloxelLevel.Current; if (level == null) { Debug.LogWarning("Undo stack got corrupted!"); return; } }
				level.ChangeBloxelTextureSide(pos, fd, oldSD);
				Bloed.MarkSceneDirty();
			});
		}

		//

		public void OnSceneGUI(Bloed bloed, SceneView view) {
			EditorGUIUtility.labelWidth = 100f;
			GUILayout.Label("Tool: TEXER");

			changeMode = GUILayout.SelectionGrid(changeMode, modeNames, 3);

			//changeFullBloxel = GUILayout.Toggle(changeFullBloxel, "Full Bloxel Tex Change Mode");

			GUILayout.Space(4f);
			if (changeMode == 0) {
				changeTexture = GUILayout.Toggle(changeTexture, "Change Image (By Palette)");

				GUILayout.Space(4f);
				EditorGUIUtility.labelWidth = 55f;
				rotation = EditorGUILayout.IntSlider("Rotation", rotation, 0, 3);
				changeRotationAbsolute = GUILayout.Toggle(changeRotationAbsolute, "Change Rotation Absolute");

				GUILayout.Space(4f);
				int offX = offset & 31, offY = offset >> 5;
				var tex = bloed.CurBloxTex;
				GUILayout.BeginHorizontal();
				offX = Mathf.Min(31, EditorGUILayout.IntField("Offset X", offX));
				if (tex != null) {
					if (GUILayout.Button("-", GUILayout.Width(20f))) { offX -= 1; }
					if (GUILayout.Button("+", GUILayout.Width(20f))) { offX += 1; }
					offX = (int)Mathf.Repeat(offX, tex.countX);
					GUILayout.Label("/ " + (tex.countX - 1), GUILayout.Width(40f));
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				offY = Mathf.Min(31, EditorGUILayout.IntField("Offset Y", offY));
				if (tex != null) {
					if (GUILayout.Button("-", GUILayout.Width(20f))) { offY -= 1; }
					if (GUILayout.Button("+", GUILayout.Width(20f))) { offY += 1; }
					offY = (int)Mathf.Repeat(offY, tex.countY);
					GUILayout.Label("/ " + (tex.countX - 1), GUILayout.Width(40f));
				}
				GUILayout.EndHorizontal();
				offset = offX + (offY << 5);

				GUILayout.Space(4f);
				changeUvSet = GUILayout.Toggle(changeUvSet, "Change UV Set");
				if (changeUvSet) {
					uvSet = Mathf.Max(0, EditorGUILayout.IntField("UV Set", uvSet));
				}

				//dontCreateOnNoChange = GUILayout.Toggle(dontCreateOnNoChange, "Don't Create On No Change");
			}
			else if (changeMode == 1) {
				changeFullBloxelReset = GUILayout.Toggle(changeFullBloxelReset, "Reset All Side Data");
			}
			else if (changeMode == 2) {
			}
		}
		
		public void DrawHandles(Bloed bloed) {
		}

		public void Hotkeys(Bloed bloed, Event evt, SceneView view) {
			if (evt.keyCode == KeyCode.Tab && changeRotationAbsolute) {
				evt.Use();
				rotation = (rotation + (evt.shift ? -1 : 1) + 4) % 4;
				bloed.UnfocusToolbar(view);
			}
		}

		public void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker) {
			Handles.color = bloed.colorMarkerTexer.col;
			Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
			cursorHoverMarker.curMarkerMesh = null;
			var curPos = bloed.CurPos.Value;
			//var projSettings = bloed.ProjectSettings;
			if (changeMode == 0 || changeMode == 2) {

				if (bloed.MousecastChunks(Event.current, out var hit, out var _)) {
					var fd = (Base.Position3.FlooredVector(Bloed.Vec3InvTrPos(hit.point + hit.normal * 0.001f)) != curPos)
						? (BloxelUtility.faceDirIndexByPosition.TryGetValue(Base.Position3.RoundedVector(Bloed.Vec3InvTrDir(hit.normal)), out var f) ? f : 6)
						: 6;

					Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

					if (fd == 6) {
						for (int s = 0; s < 6; ++s) {
							// draw all 6 sides
							for (int i = 0; i < 4; ++i) { tempSide[i] = sides[s][i] * 0.65f + curPos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f); }
							Handles.DrawSolidRectangleWithOutline(tempSide, Color.white.WithAlpha(0.15f), Color.white);
						}
						//cursorHoverMarker.transform.localScale = Vector3.one * 0.65f;
						//cursorHoverMarker.SetMarker(Color.white, projSettings.BoxTemplate.mesh, 0, 0);
					}
					else {
						//for (int i = 0; i < 4; ++i) { tempSide[i] = sides[fd][i] * 0.85f + curPos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f); }
						Handles.DrawSolidRectangleWithOutline(GetBloxelSide(curPos, fd, 0.85f, 1.01f), Color.white.WithAlpha(0.25f), Color.white);
					}
				}
			}
			else if (changeMode == 1) {
				//cursorHoverMarker.transform.localScale = Vector3.one * 1.05f;
				//cursorHoverMarker.SetMarker(Color.white, projSettings.BoxTemplate.mesh, 0, 0);
				Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
				for (int s = 0; s < 6; ++s) {
					// draw all 6 sides
					for (int i = 0; i < 4; ++i) { tempSide[i] = sides[s][i] * 1.05f + curPos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f); }
					Handles.DrawSolidRectangleWithOutline(tempSide, Color.white.WithAlpha(0.15f), Color.white);
				}
			}
			Handles.matrix = Matrix4x4.identity;
		}
	}

}