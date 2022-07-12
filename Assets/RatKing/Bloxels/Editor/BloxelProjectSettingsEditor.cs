using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RatKing.Bloxels {
	
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BloxelProjectSettings))]
	public class BloxelProjectSettingsEditor : Editor {
		SerializedProperty propTexAtlases;
		bool showInternals;

		//

		void OnEnable() {
			propTexAtlases = serializedObject.FindProperty("texAtlases");
			showInternals = EditorPrefs.GetBool("BLOED_SHOW_INTERNALS");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			EditorGUILayout.PropertyField(propTexAtlases);
			
			EditorGUILayout.Space();
			
			var newShowInternals = GUILayout.Toggle(showInternals, "Show Debug Info/Settings");
			if (newShowInternals != showInternals) {
				EditorPrefs.SetBool("BLOED_SHOW_INTERNALS", newShowInternals);
				showInternals = newShowInternals;
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}

}
