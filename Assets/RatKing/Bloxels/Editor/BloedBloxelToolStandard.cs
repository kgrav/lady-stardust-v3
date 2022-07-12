using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RatKing.Bloxels;

namespace RatKing {

	public class BloedBloxelToolStandard : BloedBloxelTool {
		public bool AllowsPivotPlaceMode { get { return true; } set { } }
		public bool AllowsGridMode { get { return true; } set { } }
		public bool AllowsWireCubes { get { return true; } set { } }
		public bool AllowsChangingSelectedTemplate { get { return true; } set { } }
		public bool AllowsChangingSelectedTexture { get { return true; } set { } }
		public Bloed.OverwritePickInner OverwritePickInner { get { return Bloed.OverwritePickInner.No; } set { } }
		public bool RepaintSceneOnSubBloxelMouseDrag { get { return false; } set { } }

		//

		bool centered = true;
		Base.Position3 size = Base.Position3.one;
		bool texPadding = false;
		bool changeTexOnly = false;
		bool hollow = false;

		//

		public void OnClick(Bloed bloed, Event evt) {
			if (bloed == null) { return; }
			
			var beType = evt.type == EventType.MouseDown;
			if (!beType && (bloed.IsGridVisible || changeTexOnly)) {
				beType = evt.type == EventType.MouseDrag;
			}

			var click = !evt.alt && beType;

			if (click && evt.button == 0 && !evt.control && bloed.CurBloxTmp != null) {
				ChangeBloxels(bloed, bloed.CurPos.Value, bloed.CurBloxTmp, bloed.CurBloxTexIdxOfLevel);
			}
		}

		void ChangeBloxels(Bloed bloed, Base.Position3 pos, BloxelTemplate tmp, int texIdx) {
			var level = BloxelLevel.Current;
			var bloxelsTextureOnly = new Dictionary<Base.Position3, Bloxel>(size.x * size.y * size.z);
			var bloxelsWithTemplate = new Dictionary<Base.Position3, Bloxel>(size.x * size.y * size.z);
			
			GetMinMax(bloed, out var min, out var max);

			// the sides
			if (texPadding) {
				for (int x = -min.x; x < max.x; ++x) {
					for (int y = -min.y; y < max.y; ++y) {
						var p = pos + new Base.Position3(x, y, -min.z - 1);
						var bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
						//
						p = pos + new Base.Position3(x, y, max.z);
						bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
					}
				}
				for (int y = -min.y; y < max.y; ++y) {
					for (int z = -min.z; z < max.z; ++z) {
						var p = pos + new Base.Position3(-min.x - 1, y, z);
						var bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
						//
						p = pos + new Base.Position3(max.x, y, z);
						bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
					}
				}
				for (int x = -min.x; x < max.x; ++x) {
					for (int z = -min.z; z < max.z; ++z) {
						var p = pos + new Base.Position3(x, -min.y - 1, z);
						var bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
						//
						p = pos + new Base.Position3(x, max.y, z);
						bloxel = level.GetBloxel(p);
						if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
					}
				}
			}

			// the main block in the middle			
			for (int y = -min.y; y < max.y; ++y) {
				for (int x = -min.x; x < max.x; ++x) {
					for (int z = -min.z; z < max.z; ++z) {
						if (hollow && !(y == -min.y || y == max.y - 1 || x == -min.x || x == max.x - 1 || z == -min.z || z == max.z - 1)) {
							continue;
						}
						var p = pos + new Base.Position3(x, y, z);
						var bloxel = level.GetBloxel(p);
						if (changeTexOnly) {
							if (!bloxel.IsAir && bloxel.textureIdx != texIdx) { bloxelsTextureOnly[p] = bloxel; }
						}
						else if (!bloxel.Is(tmp, texIdx)) {
							bloxelsWithTemplate[p] = bloxel;
						}
					}
				}
			}
		
			BloedUndoRedo.AddAction(() => {
				Selection.activeGameObject = null;
				if (bloxelsWithTemplate.Count == 0 && bloxelsTextureOnly.Count == 0) { return false; }
				level.UpdateSeveralChunksStart();
				foreach (var b in bloxelsWithTemplate) { level.ChangeBloxel(b.Key, tmp, texIdx); }
				foreach (var b in bloxelsTextureOnly) { level.ChangeBloxel(b.Key, b.Value.template, texIdx); }
				level.UpdateSeveralChunksEnd();
				Bloed.MarkSceneDirty();
				return true;
			},
			() => {
				if (level == null) { level = BloxelLevel.Current; if (level == null) { Debug.LogWarning("Undo stack got corrupted!"); return; } }
				Selection.activeGameObject = null;
				level.UpdateSeveralChunksStart();
				foreach (var b in bloxelsTextureOnly) { level.ChangeBloxel(b.Key, b.Value.template, b.Value.textureIdx); }
				foreach (var b in bloxelsWithTemplate) { level.ChangeBloxel(b.Key, b.Value.template, b.Value.textureIdx); }
				level.UpdateSeveralChunksEnd();
				Bloed.MarkSceneDirty();
			});
		}

		//

		public void OnSceneGUI(Bloed bloed, SceneView view) {
			EditorGUIUtility.labelWidth = 100f;
			GUILayout.Label("Tool: BLOXER");

			centered = GUILayout.Toggle(centered, "Centered");

			hollow = GUILayout.Toggle(hollow, "Hollow");

			EditorGUIUtility.labelWidth = 20f;
			size.x = EditorGUILayout.IntSlider("X", size.x, 1, 9);
			size.y = EditorGUILayout.IntSlider("Y", size.y, 1, 9);
			size.z = EditorGUILayout.IntSlider("Z", size.z, 1, 9);

			//EditorGUIUtility.labelWidth = 75f;
			//texPadding = EditorGUILayout.IntSlider("Tex Padding", texPadding, 0, 3);
			texPadding = GUILayout.Toggle(texPadding, "Tex Padding");

			changeTexOnly = GUILayout.Toggle(changeTexOnly, "Change Tex Only");
		}

		void GetMinMax(Bloed bloed, out Base.Position3 min, out Base.Position3 max) {
			max = new Base.Position3(Mathf.CeilToInt((size.x + 1) / 2f), Mathf.CeilToInt((size.y + 1) / 2f), Mathf.CeilToInt((size.z + 1) / 2f));
			min = size - max;

			if (!centered && !(Event.current.shift && bloed.IsPressingRMB)) {
				if (!bloed.IsGridVisible) {
					if (bloed.MousecastChunks(Event.current, out var hit, out var _)) {
						var normal = hit.normal * (bloed.CurBloxTmp.IsAir ? 0.01f : -0.01f);
						var fd = (Base.Position3.FlooredVector(hit.point + normal) != bloed.CurPos.Value)
							? (BloxelUtility.faceDirIndexByPosition.TryGetValue(Base.Position3.RoundedVector(hit.normal), out var f) ? f : 6)
							: 6;
						switch (bloed.CurBloxTmp.IsAir ? (fd) : ((fd + 3) % 6)) {
							case 0: min.y = size.y - 1; max.y = 1; break;
							case 1: min.z = 0; max.z = size.z; break;
							case 2: min.x = 0; max.x = size.x; break;
							case 3: min.y = 0; max.y = size.y; break;
							case 4: min.z = size.z - 1; max.z = 1; break;
							case 5: min.x = size.x - 1; max.x = 1; break;
						}
					}
				}
				else {
					var gridPositive = bloed.IsGridPositive();
					switch (bloed.gridMode) {
						case Bloed.GridMode.Y: if (gridPositive) { min.y = 0; max.y = size.y; } else { min.y = size.y - 1; max.y = 1; } break;
						case Bloed.GridMode.Z: if (gridPositive) { min.z = 0; max.z = size.z; } else { min.z = size.z - 1; max.z = 1; } break;
						case Bloed.GridMode.X: if (gridPositive) { min.x = 0; max.x = size.x; } else { min.x = size.x - 1; max.x = 1; } break;
					}
				}
			}
		}
		
		public void DrawHandles(Bloed bloed) {
		}

		public void Hotkeys(Bloed bloed, Event evt, SceneView view) {
		}

		public void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker) {
			var curPosWorld = bloed.CurPos.Value.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);
			var curBloxTmp = bloed.CurBloxTmp;
			var handlesColor = Handles.color;
			var col = curBloxTmp.IsAir ? bloed.colorMarkerAir : bloed.colorMarkerSolid;
			cursorHoverMarker.localScale = Vector3.one * 1.05f;
			cursorHoverMarker.SetMarker(col.col, curBloxTmp.IsAir ? bloed.ProjectSettings.BoxTemplate.mesh : curBloxTmp.mesh, curBloxTmp.Dir, curBloxTmp.Rot);

			// wires

			Handles.color = col.col.WithAlpha(0.4f);
			Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
			GetMinMax(bloed, out var min, out var max);
			for (int y = -min.y; y < max.y; ++y) {
				for (int x = -min.x; x < max.x; ++x) {
					for (int z = -min.z; z < max.z; ++z) {
						if (hollow && !(y == -min.y || y == max.y - 1 || x == -min.x || x == max.x - 1 || z == -min.z || z == max.z - 1)) {
							continue;
						}
						Handles.DrawWireCube(curPosWorld + new Vector3(x, y, z), Vector3.one);
					}
				}
			}
			Handles.matrix = Matrix4x4.identity;
			Handles.color = handlesColor;
		}
	}

}