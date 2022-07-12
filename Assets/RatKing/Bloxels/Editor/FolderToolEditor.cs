using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RatKing.Bloxels {

	public class FolderToolEditor {
		public static GUIStyle headerStyle;

		public static GUIStyle GetHeaderStyle() {
			if (headerStyle == null) {
				headerStyle = new GUIStyle(GUI.skin.GetStyle("label")) { richText = true };
			}
			return headerStyle;
		}

		//

		// [MenuItem("Window/Bloxels/Fill Assets Shelves")]
		public static void CorrectShelves() {
			var resVTy = Resources.LoadAll<BloxelType>("BloxelTypes");
			foreach (var r in resVTy) {
				if (r.ChangeShelfByPath(AssetDatabase.GetAssetPath(r))) {
					Debug.Log("changed " + r.ID + " -> " + r.Shelf);
					EditorUtility.SetDirty(r);
				}
			}
			var resVTx = Resources.LoadAll<BloxelTexture>("BloxelTextures");
			foreach (var r in resVTx) {
				if (r.ChangeShelfByPath(AssetDatabase.GetAssetPath(r))) {
					Debug.Log("changed " + r.ID + " -> " + r.Shelf);
					EditorUtility.SetDirty(r);
				}
			}
			AssetDatabase.SaveAssets();
		}
	}

}