using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace RatKing.Bloxels {

	[InitializeOnLoad, DefaultExecutionOrder(-100000)]
	public class HierarchyExtended {
		//static GUIStyle labelStyle;
		static Texture2D texWhite;

		//

		static HierarchyExtended() {
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemCallback;
		}

		~HierarchyExtended() {
			EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemCallback;
		}

		static void HierarchyWindowItemCallback(int instanceID, Rect selectionRect) {
			//if (labelStyle == null) {
			//	labelStyle = GUI.skin.GetStyle("label");
			//	labelStyle.fontStyle = FontStyle.Bold;
			//}
			if (texWhite == null) {
				texWhite = new Texture2D(1, 1); texWhite.SetPixel(0, 0, Color.white);
				texWhite.Apply();
			}
            GameObject target = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (target != null && target.TryGetComponent<BloxelLevel>(out var bl)) {
				var colSav = GUI.color;
				var colSavBack = GUI.backgroundColor;
				selectionRect.x -= 2f;
				selectionRect.y += 1f;
				GUI.color = GUI.backgroundColor = bl.IsCurrent ? Color.green.WithAlpha(0.3f) : Color.yellow.WithAlpha(0.2f);
				GUI.DrawTexture(selectionRect, texWhite);
				GUI.color = GUI.backgroundColor = bl.IsCurrent ? Color.green.WithAlpha(0.8f) : Color.yellow.WithAlpha(0.5f);
				GUI.DrawTexture(new Rect(selectionRect.x, selectionRect.y + 1f, selectionRect.width, 1f), texWhite);
				GUI.DrawTexture(new Rect(selectionRect.x, selectionRect.y + selectionRect.height - 2f, selectionRect.width, 1f), texWhite);
				GUI.DrawTexture(new Rect(selectionRect.x, selectionRect.y + 1f, 1f, selectionRect.height - 2f), texWhite);
				//GUI.color = GUI.backgroundColor = Color.black;
				//GUI.Label(selectionRect, target.name, labelStyle);
				GUI.color = colSav;
				GUI.backgroundColor = colSavBack;
			}
		}		
	}

}