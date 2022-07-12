using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;
using UnityEditor;

namespace RatKing.Bloxels {
	
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BloxelLevel))]
	public class BloxelLevelEditor : Editor {
		SerializedProperty recreateOnStart;
		SerializedProperty settings;

		void OnEnable() {
			recreateOnStart = serializedObject.FindProperty("recreateOnStart");
			settings = serializedObject.FindProperty("settings");
		}

		public override void OnInspectorGUI() {

			EditorGUILayout.PropertyField(recreateOnStart);
			EditorGUILayout.PropertyField(settings);

			if (target is BloxelLevel bl && bl.ProjectSettings != null && bl.ProjectSettings.ShowInternals) {
				var color = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("Internals (set in ProjectSettings):");
				GUI.color = color;
				EditorGUILayout.PropertyField(serializedObject.FindProperty("TemplateUIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("TextureUIDs"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("Joists"));
			}

			EditorGUILayout.Space(5f);
			
			// make the meshes unique if gameobject was unpacked
			if (PrefabStageUtility.GetCurrentPrefabStage() == null) { // we're not inside the prefab mode!
				foreach (var t in serializedObject.targetObjects) {
					var lvl = t as BloxelLevel;
					if (lvl.Chunks != null && lvl.Chunks.Count > 0 && !PrefabUtility.IsPartOfPrefabAsset(lvl.gameObject) && !PrefabUtility.IsPartOfPrefabInstance(lvl.gameObject)) {
						var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(lvl.gameObject));
						var hasMesh = false;
						foreach (var sa in subAssets) { if (sa is Mesh) { hasMesh = true; break; } }
						if (hasMesh) {
							lvl.MakeMeshesUnique(true);
						}
					}
				}
			}

			if (!serializedObject.isEditingMultipleObjects) {
				var lvl = serializedObject.targetObject as BloxelLevel;

				if (BloxelLevel.Current == null || BloxelLevel.Current.gameObject.scene != lvl.gameObject.scene) {
					BloxelLevel.SetCurrent(lvl);
					return;
				}

				//foreach (var c in lvl.Chunks) {
				//	GUILayout.Label(c.Key + " .. " + (c.Value != null ? c.Value.name : "NONE"));
				//}

				if (lvl != null && !lvl.IsCurrent) {
					var color = GUI.color;
					GUI.color = Color.green;
					if (GUILayout.Button("Set Active", GUILayout.Height(40f))) { 
						lvl.gameObject.SetActive(true);
						BloxelLevel.SetCurrent(lvl);
						Bloed.MarkSceneDirty();
					}
					GUI.color = color;
					serializedObject.ApplyModifiedProperties();
				}
				if (lvl != null && lvl.Chunks != null && lvl.Chunks.Count > 0 && PrefabUtility.IsPartOfPrefabInstance(lvl.gameObject)) {
					if (GUILayout.Button("Unpack Prefab Instance", GUILayout.Height(30f))) {
						var root = PrefabUtility.GetOutermostPrefabInstanceRoot(lvl.gameObject);
						PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
						lvl.MakeMeshesUnique(true);
					}
				}
			}

			if (serializedObject.hasModifiedProperties) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

}
