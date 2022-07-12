using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RatKing.Bloxels {
	
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BloxelTexture))]
	public class BloxelTextureEditor : Editor {
		//BloxelTexture vt;
		SerializedProperty propID;
		SerializedProperty propTexture;
		SerializedProperty propTexturesSecondary;
		SerializedProperty propCountX;
		SerializedProperty propCountY;
		SerializedProperty propMaxPixelWidth;
		SerializedProperty propMaxPixelHeight;
		SerializedProperty propTransparency;
		SerializedProperty propNoiseStrength;
		SerializedProperty propNoiseScale;
		SerializedProperty propTexAtlasIdx;
		SerializedProperty propFlags;
		SerializedProperty propNeighbourFlags;
		SerializedProperty propComposition;
		SerializedProperty propCollider;
		SerializedProperty propTags;
		SerializedProperty propVariables;
		//SerializedProperty propDestroyStrength;
		//SerializedProperty propDestroyed;
		//
		readonly float previewTexWidth = 150f;
		readonly float previewTexDist = 4f;
		//
		static Rect previewRect;
		static GUIStyle previewStyle; // readonly

		static readonly string[] compositionNames = { "Top", "Sides", "Bottom", "Inner" };

		//

		void OnEnable() {
			propID = serializedObject.FindProperty(nameof(BloxelTexture.ID));
			propTexture = serializedObject.FindProperty(nameof(BloxelTexture.texture));
			propTexturesSecondary = serializedObject.FindProperty(nameof(BloxelTexture.texturesSecondary));
			propCountX = serializedObject.FindProperty(nameof(BloxelTexture.countX));
			propCountY = serializedObject.FindProperty(nameof(BloxelTexture.countY));
			propMaxPixelWidth = serializedObject.FindProperty(nameof(BloxelTexture.maxPixelWidth));
			propMaxPixelHeight = serializedObject.FindProperty(nameof(BloxelTexture.maxPixelHeight));
			propTransparency = serializedObject.FindProperty(nameof(BloxelTexture.transparency));
			propNoiseStrength = serializedObject.FindProperty(nameof(BloxelTexture.noiseStrength));
			propNoiseScale = serializedObject.FindProperty(nameof(BloxelTexture.noiseScale));
			propTexAtlasIdx = serializedObject.FindProperty(nameof(BloxelTexture.texAtlasIdx));
			propFlags = serializedObject.FindProperty(nameof(BloxelTexture.flags));
			propNeighbourFlags = serializedObject.FindProperty(nameof(BloxelTexture.neighbourFlags));
			propComposition = serializedObject.FindProperty(nameof(BloxelTexture.composition));
			propCollider = serializedObject.FindProperty(nameof(BloxelTexture.generateCollider));
			propTags = serializedObject.FindProperty(nameof(BloxelTexture.tags));
			propVariables = serializedObject.FindProperty(nameof(BloxelTexture.variables));
			//propDestroyStrength = serializedObject.FindProperty(nameof(BloxelTexture.destroyStrength));
			//propDestroyed = serializedObject.FindProperty(nameof(BloxelTexture.destroyed));
		}

		public override void OnInspectorGUI() {
			var headerStyle = FolderToolEditor.GetHeaderStyle();
			var previewTex = Texture2D.whiteTexture;
			if (previewStyle == null) { previewStyle = new GUIStyle { normal = new GUIStyleState { background = previewTex } }; }

			var color = GUI.color;
			var labelWidth = EditorGUIUtility.labelWidth;

			if (BloxelUtility.ProjectSettings == null || serializedObject.targetObject == BloxelUtility.ProjectSettings.MissingTexture) {
				GUI.color = Color.red;
				GUILayout.Label("Do not edit the $MISSING BloxelTexture!");
				GUI.color = color;
			}
			else {
				EditorGUILayout.PropertyField(propID);
				if (!serializedObject.isEditingMultipleObjects) { // shelf
					var bt = (BloxelTexture)serializedObject.targetObject;
					bt.ChangeShelfByPath(AssetDatabase.GetAssetPath(bt));
					var shelf = bt.Shelf.Split(Bloed.shelfNameSeparator);
					EditorGUILayout.LabelField("Shelf", shelf[shelf.Length - 1]);
				}

				// texture atlas index
				var texAtlases = new string[BloxelUtility.ProjectSettings.TexAtlases.Length];
				propTexAtlasIdx.intValue = (int)Mathf.Repeat(propTexAtlasIdx.intValue, texAtlases.Length);
				for (int i = 0; i < texAtlases.Length; ++i) { texAtlases[i] = BloxelUtility.ProjectSettings.TexAtlases[i].ID; }
				propTexAtlasIdx.intValue = EditorGUILayout.Popup("Tex Atlas", propTexAtlasIdx.intValue, texAtlases);
				var curTexAtlas = BloxelUtility.ProjectSettings.TexAtlases[propTexAtlasIdx.intValue];

				// flags
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Flags", GUILayout.Width(EditorGUIUtility.labelWidth));
				for (int i = 0; i < 8; ++i) {
					var b = (propFlags.intValue & (1 << i)) != 0;
					var nb = GUILayout.Toggle(b, "", GUILayout.Width(13), GUILayout.ExpandWidth(false));
					if (nb != b && b) { propFlags.intValue &= ~(1 << i); }
					else if (nb != b && nb) { propFlags.intValue |= (1 << i); }
				}
				EditorGUILayout.LabelField("", GUILayout.Width(10), GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Neighbours", GUILayout.Width(EditorGUIUtility.labelWidth));
				for (int i = 0; i < 8; ++i) {
					var b = (propNeighbourFlags.intValue & (1 << i)) != 0;
					var nb = GUILayout.Toggle(b, "", GUILayout.Width(13), GUILayout.ExpandWidth(false));
					if (nb != b && b) { propNeighbourFlags.intValue &= ~(1 << i); }
					else if (nb != b && nb) { propNeighbourFlags.intValue |= (1 << i); }
				}
				EditorGUILayout.LabelField("", GUILayout.Width(10), GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal();
				
				EditorGUILayout.PropertyField(propCollider, true);

				EditorGUILayout.Slider(propTransparency, 0f, 1f);
				EditorGUILayout.Slider(propNoiseStrength, 0f, 2f);
				if (propNoiseStrength.floatValue > 0f) {
					EditorGUILayout.PropertyField(propNoiseScale);
				}
				else {
					propNoiseScale.floatValue = 1f;
				}
				EditorGUILayout.ObjectField(propTexture, new GUIContent("Base Texture"));
				
				if (!serializedObject.isEditingMultipleObjects && curTexAtlas.Properties.Length > 1) {
					propTexturesSecondary.arraySize = curTexAtlas.Properties.Length - 1;
					for (int i = 1; i < curTexAtlas.Properties.Length; ++i) {
						var propTexSec = propTexturesSecondary.GetArrayElementAtIndex(i - 1);
						EditorGUILayout.ObjectField(propTexSec, new GUIContent(curTexAtlas.Properties[i]));
					}
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("New Pixel Size: ", GUILayout.Width(100f));
				EditorGUILayout.PropertyField(propMaxPixelWidth, GUIContent.none);
				EditorGUILayout.PropertyField(propMaxPixelHeight, GUIContent.none);
				GUILayout.EndHorizontal();
			}

			if (serializedObject.isEditingMultipleObjects) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Block Count: ", GUILayout.Width(100f));
				EditorGUILayout.PropertyField(propCountX, GUIContent.none);
				EditorGUILayout.PropertyField(propCountY, GUIContent.none);
				GUILayout.EndHorizontal();
			}
			else {
				GUI.changed = true;

				var bt = (BloxelTexture)serializedObject.targetObject;
				if (bt.texture != null) {
					GUI.color = Color.green;
					GUILayout.Label("Original tex size: " + bt.texture.width + "x" + bt.texture.height);
					GUI.color = Color.cyan;
					var p = BloxelProjectSettings.TEXTURE_PADDING * 2;
					var w = propMaxPixelWidth.intValue <= 0 ? bt.texture.width : propMaxPixelWidth.intValue;
					var h = propMaxPixelHeight.intValue <= 0 ? bt.texture.width : propMaxPixelHeight.intValue;
					GUILayout.Label("Tex size in atlas: " + (w + p) + "x" + (h+ p));
					GUI.color = color;
					GUILayout.Space(6f);
				}
				else {
					previewRect = new Rect();
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("Block Count: ", GUILayout.Width(100f));
				var newCountX = EditorGUILayout.IntField(bt.countX);
				if (bt.countX != newCountX) { bt.countX = propCountX.intValue = Mathf.Max(1, newCountX); EditorUtility.SetDirty(bt); }
				var newCountY = EditorGUILayout.IntField(bt.countY);
				if (bt.countY != newCountY) { bt.countY = propCountY.intValue = Mathf.Max(1, newCountY); EditorUtility.SetDirty(bt); }
				GUILayout.EndHorizontal();

				if (bt.texture != null) {
					try { bt.texture.GetPixel(0, 0); }
					catch {
						GUI.color = Color.yellow;
						GUILayout.Space(6f);
						GUILayout.Label("Texture not set to Read/Write enabled!");
						GUI.color = color;
					}
				}

				EditorGUILayout.Space();

				var previewTexHeight = previewTexWidth * (bt.texture == null ? 1f : (bt.texture.height / (float)bt.texture.width));

				if (Event.current.type == EventType.Repaint) {
					var r = GUILayoutUtility.GetLastRect();
					previewRect.width = previewTexWidth + previewTexDist * (1 + newCountX);
					previewRect.height = previewTexHeight + previewTexDist * (1 + newCountY);
					previewRect.x = r.x + (EditorGUIUtility.currentViewWidth - previewRect.width) * 0.5f;
					previewRect.y = r.yMax;
				}
				var backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = Color.magenta;
				GUILayout.BeginArea(previewRect, GUIContent.none, previewStyle);

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
				float picH = previewTexHeight / bt.countY;
				float picW = previewTexWidth / bt.countX;
				GUI.backgroundColor = backgroundColor;
				GUI.color = new Color(1f, 1f, 1f, 1f - bt.transparency);
				for (int j = 0; j < bt.countY; j++) {
					GUILayout.Space(previewTexDist);
					GUILayout.BeginHorizontal();
					for (int i = 0; i < bt.countX; ++i) {
						GUILayout.Space(previewTexDist);
						var r = GUILayoutUtility.GetRect(picW, picH, GUILayout.Width(picW), GUILayout.Height(picH)); r.x -= 1;
						if (bt.texture != null) {
							GUI.DrawTextureWithTexCoords(r, bt.texture, new Rect(i / (float)bt.countX, 1f - ((j + 1) / (float)bt.countY), 1f / bt.countX, 1f / bt.countY), false);
						}
					}
					GUILayout.EndHorizontal();
				}
				GUI.color = Color.white;
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.EndArea();

				GUILayout.Space(previewRect.height);

				//if (vt.texture != null && vt.atlas != null) {
				//	GUI.color = Color.cyan;
				//	GUILayout.Space(6f);
				//	GUILayout.Label("Rectangle in Tex Atlas:");
				//	GUILayout.Label("  > " + vt.rect);
				//	GUILayout.Label("  > pos: " + Mathf.FloorToInt(vt.rect.x * vt.atlas.width) + "/" + Mathf.FloorToInt(vt.rect.y * vt.atlas.height) + " -- size: " + Mathf.FloorToInt(vt.rect.width * vt.atlas.width) + "/" + Mathf.FloorToInt(vt.rect.height * vt.atlas.height));
				//	GUI.color = color;
				//	vt.atlas = (Texture)EditorGUILayout.ObjectField("<ATLAS>", vt.atlas, typeof(Texture), false);
				//	GUI.color = Color.red;
				//	if (GUILayout.Button("Update Tex Atlas")) {
				//		Bloxels.UpdateTextureAtlas();
				//	}
				//	GUI.color = color;
				//}
				GUILayout.Space(12f);

				if (GUI.changed) { EditorUtility.SetDirty(bt); }
			}

			//EditorGUILayout.PropertyField(propIsDestroyable);
			//if (propIsDestroyable.boolValue) { EditorGUILayout.PropertyField(propDestroyed); }

			//EditorGUILayout.PropertyField(propComposition, true);
			EditorGUILayout.LabelField("<b>Composition:</b>", headerStyle);
			EditorGUIUtility.labelWidth = 100f;
			if (propComposition.arraySize != 4) { propComposition.arraySize = 4; }
			for (int i = 0; i < 4; ++i) {
				if (propComposition.GetArrayElementAtIndex(i).objectReferenceValue == target) { propComposition.GetArrayElementAtIndex(i).objectReferenceValue = null; }
				GUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(propComposition.GetArrayElementAtIndex(i), new GUIContent("    " + compositionNames[i]));
				if (GUILayout.Button("X", GUILayout.Width(25f))) { propComposition.GetArrayElementAtIndex(i).objectReferenceValue = null; }
				GUILayout.EndHorizontal();
			}
			EditorGUIUtility.labelWidth = labelWidth;

			EditorGUILayout.PropertyField(propTags, true);
			
			EditorGUILayout.PropertyField(propVariables, true);

			//EditorGUILayout.PropertyField(propDestroyStrength);
			//if (propDestroyStrength.floatValue >= 0f) { EditorGUILayout.PropertyField(propDestroyed); }
			
			if (serializedObject.hasModifiedProperties) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

}