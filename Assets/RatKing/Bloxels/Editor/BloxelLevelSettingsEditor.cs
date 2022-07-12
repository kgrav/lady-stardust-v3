using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RatKing.Bloxels {
	
	[CustomEditor(typeof(BloxelLevelSettings))]
	public class BloxelLevelSettingsEditor : Editor {
		SerializedProperty propStandardBloxels;
		SerializedProperty propStandardMethodType;
		SerializedProperty propCustomMethodProvider;
		SerializedProperty propChunkLayer;
		SerializedProperty propChunkTag;
		SerializedProperty propChunkEditorFlags;
		SerializedProperty propShadowCastingMode;

		//

		void OnEnable() {
			propStandardBloxels = serializedObject.FindProperty("standardBloxels");
			propStandardMethodType = serializedObject.FindProperty("standardMethodType");
			propCustomMethodProvider = serializedObject.FindProperty("customMethodProvider");
			
			propChunkLayer = serializedObject.FindProperty("chunkLayer");
			propChunkTag = serializedObject.FindProperty("chunkTag");
			propChunkEditorFlags = serializedObject.FindProperty("chunkEditorFlags");
			propShadowCastingMode = serializedObject.FindProperty("shadowCastingMode");
		}
		public override void OnInspectorGUI() {
			var headerStyle = FolderToolEditor.GetHeaderStyle();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(propStandardBloxels);
			EditorGUILayout.PropertyField(propStandardMethodType);
			if (!serializedObject.isEditingMultipleObjects && (BloxelLevelSettings.StandardMethodType)propStandardMethodType.enumValueIndex == BloxelLevelSettings.StandardMethodType.Custom) {
				EditorGUILayout.PropertyField(propCustomMethodProvider);
			}
			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
				var bls = (BloxelLevelSettings)serializedObject.targetObject;
				bls.ResetStandardBloxels();
			}

			if (!serializedObject.isEditingMultipleObjects) {
				var customMethodProvider = propCustomMethodProvider.objectReferenceValue as MonoScript;
				if (customMethodProvider != null) {
					var type = customMethodProvider.GetClass();
					if (type != null) {
						if (type.GetInterface(nameof(IBloxelCustomStandardMethodProvider)) == null) {
							EditorGUILayout.HelpBox("Custom Method Provider '" + customMethodProvider.name + "' should implement IBloxelCustomStandardMethodProvider!", MessageType.Error);
						}
					}
					else {
						EditorGUILayout.HelpBox("Custom Method Provider has no type!", MessageType.Error);
					}
				}
			}

			EditorGUILayout.PropertyField(propChunkLayer);
			EditorGUILayout.PropertyField(propChunkTag);
			EditorGUILayout.PropertyField(propChunkEditorFlags);
			EditorGUILayout.PropertyField(propShadowCastingMode);
			
			if (serializedObject.hasModifiedProperties) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

}
