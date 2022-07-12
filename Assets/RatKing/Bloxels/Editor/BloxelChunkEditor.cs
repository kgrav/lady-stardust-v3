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
	[CustomEditor(typeof(BloxelChunk))]
	public class BloxelChunkEditor : Editor {
		public override void OnInspectorGUI() {

			if (target is BloxelChunk bc && bc.lvl != null && bc.lvl.ProjectSettings != null && bc.lvl.ProjectSettings.ShowInternals) {
				var color = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("Internals (set in ProjectSettings):");
				GUI.color = color;
				EditorGUILayout.PropertyField(serializedObject.FindProperty("Pos"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("WorldPos"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("lvl"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changedBloxelsDataPosIndices"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changedBloxelsTemplateIndices"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changedBloxelsTextureIndices"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("changedBloxelsNum"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("textureSideExtraDataByRelPos"));
			}

			if (serializedObject.hasModifiedProperties) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

}
