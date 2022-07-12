using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RatKing.Bloxels {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(BloxelType))]
	public class BloxelTypeEditor : Editor {
		static string[] rotationLabel = new[] {
			"Normal",
			"On Head",
			"90° Left",
			"90° Right",
			"90° Forward",
			"90° Back" };
		SerializedProperty propID;
		SerializedProperty propShortDesc;
		SerializedProperty propMesh;
		SerializedProperty propColliderType;
		SerializedProperty propFlag;
		SerializedProperty propNeighborFlags;
		//SerializedProperty propPossibleRotations;
		//SerializedProperty propDestroyStrength;
		//
		//SerializedProperty propInnerDirHandling;
		//SerializedProperty propInnerDirRotate;
		//SerializedProperty propHasPreferredInnerDir;
		//SerializedProperty propPreferredInnerDir;
		//SerializedProperty propPreferredInnerDirAddAngle;

		//

		void OnEnable() {
			//vt = (BloxelType)target;
			propID = serializedObject.FindProperty("ID");
			propShortDesc = serializedObject.FindProperty("shortDesc");
			propMesh = serializedObject.FindProperty("mesh");
			propColliderType = serializedObject.FindProperty("colliderType");
			propFlag = serializedObject.FindProperty("flag");
			propNeighborFlags = serializedObject.FindProperty("neighborFlags");
		}

		public override void OnInspectorGUI() {
			var headerStyle = FolderToolEditor.GetHeaderStyle();

			serializedObject.Update();
			
			EditorGUILayout.DelayedTextField(propID);
			EditorGUILayout.DelayedTextField(propShortDesc);
			if (!serializedObject.isEditingMultipleObjects) { // shelf
				var vt = (BloxelType)serializedObject.targetObject;
				vt.ChangeShelfByPath(AssetDatabase.GetAssetPath(vt));
				var shelf = vt.Shelf.Split(Bloed.shelfNameSeparator);
				EditorGUILayout.LabelField("Shelf", shelf[shelf.Length - 1]);
			}
			EditorGUILayout.ObjectField(propMesh);
			EditorGUILayout.PropertyField(propColliderType);
			EditorGUILayout.PropertyField(propFlag);
			EditorGUILayout.PropertyField(propNeighborFlags);

			EditorGUILayout.Space();
			//EditorGUILayout.PropertyField(propDestroyStrength);

			//EditorGUILayout.PropertyField(propPossibleRotations);

			// get the possible rotations, stored in a bitmask
			if (serializedObject.isEditingMultipleObjects) {
				EditorGUILayout.LabelField("<i><b>Can't edit possible rotations of multiple objects!</b></i>", headerStyle);
			}
			else {
				EditorGUILayout.LabelField("<b>Possible Rotations:</b>", headerStyle);
				GUI.changed = false;
				var vt = (BloxelType)serializedObject.targetObject;

				for (int d = 0, bit = 0; d < 6; ++d) {
					GUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("    " + rotationLabel[d], GUILayout.Width(100), GUILayout.ExpandWidth(false));
					for (int r = 0; r < 4; ++r, ++bit) {
						if (bit == 0) {
							GUILayout.Space(24f);
							if ((vt.possibleRotations & 1) == 0) { vt.possibleRotations |= 1; }
							continue;
						}
						var v = (1 << bit);
						var ob = (v & vt.possibleRotations) != 0;
						var nb = GUILayout.Toggle(ob, "", GUILayout.Width(20), GUILayout.ExpandWidth(false));
						if (ob != nb) {
							vt.possibleRotations = !nb ?
								(vt.possibleRotations & ~v) :
								(vt.possibleRotations | v);
						}
					}
					EditorGUILayout.LabelField("", GUILayout.Width(10), GUILayout.ExpandWidth(true));
					GUILayout.EndHorizontal();
				}

				if (GUI.changed) { EditorUtility.SetDirty(vt); }
			}
			
			EditorGUILayout.Space();
			//EditorGUILayout.PropertyField(propInnerDirHandling, new GUIContent("Inner UV Dir Handling")); // TODO TYPE
			if (serializedObject.isEditingMultipleObjects) {
				//EditorGUILayout.PropertyField(propInnerDirRotate, new GUIContent("--> Rotate With Template"));
				//EditorGUILayout.PropertyField(propHasPreferredInnerDir, new GUIContent("--> Has Pref. Dir"));
				//EditorGUILayout.PropertyField(propPreferredInnerDir, new GUIContent("--> Direction"));
				//EditorGUILayout.DelayedFloatField(propPreferredInnerDirAddAngle, new GUIContent("--> Additional Angle"));
			}
			else {
				GUI.changed = false;
				var vt = (BloxelType)serializedObject.targetObject;
				if (vt.innerDirHandlings.Length == 0) { vt.innerDirHandlings = new BloxelType.InnerDirHandling[0]; }
				EditorGUILayout.LabelField("<b>Inner UV Dir Handlings:</b>", headerStyle);
				for (int i = 0; i < vt.innerDirHandlings.Length; ++i) {
					if (i != 0) { EditorGUILayout.Space(); }
					var idh = vt.innerDirHandlings[i];
					//EditorGUILayout.BeginFadeGroup(1f);
					idh.type = (BloxelType.InnerDirHandling.Type)EditorGUILayout.EnumPopup((i + 1) + ". Type", idh.type);
					if (idh.type != BloxelType.InnerDirHandling.Type.None) {
						idh.dirRotate = EditorGUILayout.Toggle("-> Rot. With Templ.", idh.dirRotate);
						idh.hasPreferredDir = EditorGUILayout.Toggle("-> Has Pref. Dir", idh.hasPreferredDir);
						if (idh.hasPreferredDir) {
							idh.preferredDir = (BloxelUtility.FaceDir)EditorGUILayout.EnumPopup("--> Direction", idh.preferredDir);
							idh.preferredDirAddAngle = Mathf.Clamp(EditorGUILayout.FloatField("--> Add. Angle", idh.preferredDirAddAngle), 1f, 180f);
						}
						else {
							idh.preferredDir = BloxelUtility.FaceDir.Top;
							idh.preferredDirAddAngle = -1f;
						}
					}
					else {
						idh.preferredDir = BloxelUtility.FaceDir.Top;
						idh.preferredDirAddAngle = -1f;
						idh.dirRotate = false;
					}
					//EditorGUILayout.EndFadeGroup();
					GUILayout.BeginHorizontal();
					if (i != 0 && GUILayout.Button("Remove")) {
						ArrayUtility.RemoveAt(ref vt.innerDirHandlings, i);
					}
					if (GUILayout.Button("Add After")) {
						ArrayUtility.Add(ref vt.innerDirHandlings, new BloxelType.InnerDirHandling());
					}
					GUILayout.EndHorizontal();
				}

				if (GUI.changed) { EditorUtility.SetDirty(vt); }
			}

			serializedObject.ApplyModifiedProperties();
		}
	}

}