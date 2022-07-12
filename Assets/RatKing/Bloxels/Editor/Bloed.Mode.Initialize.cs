using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RatKing.Bloxels;

namespace RatKing {

	public partial class Bloed : EditorWindow {
		int chunkSize = -1;
		[SerializeField] bool foldBloxelTypes;
		[SerializeField] bool foldDisplaySettings;
		[SerializeField] bool foldOtherSettings;

		void OnGUI_Initalize(Color guiNormalColor) {

			GUILayout.BeginVertical("box");

			GUILayout.Label("Create New BloxelLevel");

			GUI.skin = null;
			levelSettings = (BloxelLevelSettings)EditorGUILayout.ObjectField("Settings", levelSettings, typeof(BloxelLevelSettings), true);
			GUI.skin = Bloed.skin;

			if (chunkSize < 0) { chunkSize = (int)defaultChunkSize.num; }
			chunkSize = EditorGUILayout.IntField("Chunk Size", chunkSize);
			chunkSize = Mathf.Clamp(chunkSize, 4, 64);

			if (GUILayout.Button("Initialize!")) {
				if (levelSettings == null) {
					Debug.LogError("Settings needed for the new level!");
				}
				else {
					BloxelUtility.CreateLevel(levelSettings, chunkSize);
					MakeSureItsInited();
					MarkSceneDirty();
				}
			}

			GUILayout.EndVertical();

			GUILayout.Space(5f);

			foldBloxelTypes = EditorGUILayout.BeginFoldoutHeaderGroup(foldBloxelTypes, new GUIContent("Bloxel Types/Textures Utilities"));

			if (foldBloxelTypes) {

				if (GUILayout.Button("Update ALL Types & Textures", GUILayout.Height(32f))) {
					curMarked.Clear();
					BloxelUtility.Init(true, false);
					curTypeShelf = ProjectSettings.TypeShelves[0];
					curTextureShelf = ProjectSettings.TextureShelves[0];
					if (BloxelLevel.CurrentSettings != null) { BloxelLevel.CurrentSettings.ResetStandardBloxels(); }
					GUILayout.EndHorizontal();
					foreach (var j in Resources.FindObjectsOfTypeAll<BloxelJoist>()) { if (j.gameObject.scene.IsValid()) { j.UpdateMesh(); } }
					return;
				}

				if (GUILayout.Button("Update Textures Only", GUILayout.Height(32f))) {
					curMarked.Clear();
					BloxelUtility.Init(true, true);
					curTextureShelf = ProjectSettings.TextureShelves[0];
					if (BloxelLevel.CurrentSettings != null) { BloxelLevel.CurrentSettings.ResetStandardBloxels(); }
					GUILayout.EndHorizontal();
					foreach (var j in Resources.FindObjectsOfTypeAll<BloxelJoist>()) { if (j.gameObject.scene.IsValid()) { j.UpdateMesh(); } }
					return;
				}

				if (GUILayout.Button("Recreate ALL Existing Meshes\nin ALL Scenes & Prefabs", GUILayout.Height(42f))) {
					if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) {
						Debug.LogWarning("Save current scene before iterating over all scenes!");
						return;
					}
					var lastScenePath = "";
					var ai = new ByronMayne.AssetIterator() { iterationType = ByronMayne.AssetIterator.IterationType.All };
					void SaveScene(GameObject go) {
						if (go.scene.path != lastScenePath) {
							lastScenePath = go.scene.path;
							if (!go.scene.path.EndsWith(".prefab")) { EditorSceneManager.SaveScene(go.scene); }
						}
					}
					ai.onBehaviourVisited += c => {
						if (PrefabUtility.IsPartOfPrefabInstance(c)) { return; }
						if (c is BloxelLevel bl) {
							foreach (var chunk in bl.Chunks) {
								if (chunk.Value.MeshesExist) { RecreateChunkMeshes(bl); SaveScene(bl.gameObject); break; }
							}
						}
						else if (c is BloxelJoist bj) {
							if (bj.mf.sharedMesh != null) { bj.UpdateMesh(); SaveScene(bj.gameObject); }
						}
					};
					ai.Start();
					return;
				}
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

			EditorGUIUtility.labelWidth = 150f;

			GUILayout.Space(5f);
			
			foldDisplaySettings = EditorGUILayout.BeginFoldoutHeaderGroup(foldDisplaySettings, new GUIContent("Display Settings"));

			if (foldDisplaySettings) {

				bool ChangeColor(ref ColorSetting set) {
					var newCol = EditorGUILayout.ColorField(set.display, set.col);
					if (newCol != set.col) {
						set.col = newCol;
						set.Save();
						return true;
					}
					return false;
				}

				GUI.skin = null;
				ChangeColor(ref chunkBoundsColor);
				ChangeColor(ref gridColor);
				ChangeColor(ref colorMarkerCube);
				ChangeColor(ref colorMarkerSolid);
				ChangeColor(ref colorMarkerAir);
				ChangeColor(ref colorMarkerTexer);
				ChangeColor(ref colorMarkerPicking);
				ChangeColor(ref colorCursorCube);
				if (ChangeColor(ref colorCursor)) {
					if (cursorHoverMarker != null && cursorHoverMarker.cursorMaterial != null) {
						cursorHoverMarker.cursorMaterial.color = colorCursor.col;
					}
				}
				GUI.skin = Bloed.skin;
				if (GUILayout.Button("Reset Colors")) {
					chunkBoundsColor.Reset();
					gridColor.Reset();
					colorMarkerCube.Reset();
					colorMarkerSolid.Reset();
					colorMarkerAir.Reset();
					colorMarkerTexer.Reset();
					colorMarkerPicking.Reset();
					colorCursorCube.Reset();
					colorCursor.Reset();
				}
			}
			
			EditorGUILayout.EndFoldoutHeaderGroup();

			GUILayout.Space(5f);
			
			foldOtherSettings = EditorGUILayout.BeginFoldoutHeaderGroup(foldOtherSettings, new GUIContent("Other Settings"));

			if (foldOtherSettings) {

				bool ChangeNumber(ref NumberSetting set) {
					var newNum = set.isInt ? EditorGUILayout.IntField(set.display, (int)set.num) : EditorGUILayout.FloatField(set.display, set.num);
					if (newNum != set.num) {
						set.num = newNum;
						set.Save();
						return true;
					}
					return false;
				}

				ChangeNumber(ref defaultChunkSize);
				ChangeNumber(ref pickMaxDistanceNormal);
				ChangeNumber(ref pickMaxDistanceGrid);
				ChangeNumber(ref gridExtents);

				if (GUILayout.Button("Reset Other")) {
					defaultChunkSize.Reset();
					pickMaxDistanceNormal.Reset();
					pickMaxDistanceGrid.Reset();
					gridExtents.Reset();
				}
			}
			
			EditorGUILayout.EndFoldoutHeaderGroup();

			// TODO
			//if (GUILayout.Button("Move Cursor to 0,0,0")) {
			//	var view = SceneView.currentDrawingSceneView;
			//	if (view != null) {
			//		view.pivot = new Vector3(0.5f, 0.5f, 0.5f);
			//	}
			//}
		}
	}

}