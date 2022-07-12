using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RatKing.Bloxels;

namespace RatKing {

	public partial class Bloed : EditorWindow {

		static GUIStyle styleBtnDropDown = null; // not well created in own GUISkin!
		[SerializeField] int textureButtonSize = 0;
		static float contextWidth = -1f;

		void OnGUI_Build(Color guiNormalColor) {
			if (BloxelLevel.Current == null) { return; }

			if (styleBtnDropDown == null) {
				styleBtnDropDown = new GUIStyle(skin.GetStyle("button"));
				styleBtnDropDown.alignment = TextAnchor.MiddleLeft;
			}
			if (contextWidth < 1f) { contextWidth = EditorGUIUtility.currentViewWidth; }

			// forbid editing prefab instances!
			if (PrefabUtility.IsPartOfPrefabInstance(BloxelLevel.Current) && !BloxelUtility.TryGetPrefabStage(out var prefabStage)) {
				GUILayout.BeginVertical("box");
				GUI.color = Color.red;
				GUILayout.Label("Unfortunately it's not allowed to edit a prefab instance directly. Either unpack the prefab or open the prefab editor.", wrappedStyle);
				GUI.color = guiNormalColor;
				if (GUILayout.Button("Open Prefab", GUILayout.Height(30f))) {
					var prefab = PrefabUtility.GetCorrespondingObjectFromSource(BloxelLevel.Current);
					AssetDatabase.OpenAsset(prefab);
				}
				if (GUILayout.Button("Unpack Prefab (no undo!)")) {
					var root = PrefabUtility.GetOutermostPrefabInstanceRoot(BloxelLevel.Current.gameObject);
					PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
				}
				GUILayout.EndVertical();
				return;
			}
			
			pickCurrentLevelOnly = GUILayout.Toggle(pickCurrentLevelOnly, "Pick Currently Active Level Only");

			GUILayout.BeginVertical("box");

			// show grid yes or no
			GUILayout.BeginHorizontal();
			for (int m = 0; m < 3; ++m) {
				var gm = (GridMode) m;
				GUI.color = (gridVisible && gridMode == gm) ? Color.red : guiNormalColor;
				if (GUILayout.Button("Grid " + gm)) {
					if (!gridVisible) { gridMode = gm; ShowGrid(true); }
					else if (gridVisible && gridMode == gm) { ShowGrid(false); }
					else { gridMode = gm; }
					SceneView.RepaintAll();
				}
				GUI.color = Color.white;
			}
			GUILayout.EndHorizontal();

			if (lockCursorToGrid != GUILayout.Toggle(lockCursorToGrid, "Lock Cursor To Grid")) {
				lockCursorToGrid = !lockCursorToGrid;
				SceneView.RepaintAll();
			}

			GUILayout.EndVertical();

			GUILayout.Space(10f);

			var currentViewWidth = EditorGUIUtility.currentViewWidth;
			
			// choose template:

			if (ProjectSettings.Types != null) {

				// the type shelves	

				var sn = curTypeShelf.ToUpper().Split(shelfNameSeparator);
				if (EditorGUILayout.DropdownButton(new GUIContent("<color=yellow><b>Types:</b></color> " + sn[sn.Length - 1]), FocusType.Passive, styleBtnDropDown, GUILayout.Height(30f))) {
					var menu = new GenericMenu();
					menu.DropDown(GUILayoutUtility.GetLastRect());
					foreach (var shelf in ProjectSettings.TypeShelves) {
						sn = shelf.Split(shelfNameSeparator);
						menu.AddItem(new GUIContent(sn[sn.Length - 1]), curTypeShelf == shelf, s => curTypeShelf = (string)s, shelf);
					}
					menu.ShowAsContext();
				}
				// scroll through the shelves
				else if (Event.current.type == EventType.ScrollWheel && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
					var idx = Mathf.Clamp(ProjectSettings.TypeShelves.IndexOf(curTypeShelf), 0, ProjectSettings.TypeShelves.Count);
					idx = (idx + (Event.current.delta.y > 0 ? 1 : -1) + ProjectSettings.TypeShelves.Count) % ProjectSettings.TypeShelves.Count;
					curTypeShelf = ProjectSettings.TypeShelves[idx];
					Event.current.Use();
					Repaint();
					GUILayout.EndScrollView();
					return;
				}

				if (!ProjectSettings.TypeShelves.Contains(curTypeShelf)) {
					curTypeShelf = ProjectSettings.TypeShelves[0];
				}

				// the templates

				int c = Mathf.Max(1, Mathf.FloorToInt(currentViewWidth / 125f));
				int ii = 0;
				var rect = new Rect();

				void TypeButton(BloxelTemplate tmp) {
					if (tmp == null) { return; }
					if ((ii++ % c) == 0) {
						rect = GUILayoutUtility.GetRect(GUIContent.none, "button", GUILayout.Height(26f));
						GUILayout.BeginHorizontal();
					}
					var isChosen = curBloxTmp != null && curBloxTmp.ID == tmp.ID;
					if (isChosen) { GUI.color = new Color(0f, 1f, 0f, 1f); }
					var display = (tmp.Type != null && !string.IsNullOrWhiteSpace(tmp.Type.shortDesc)) ? tmp.Type.shortDesc : tmp.ID;
					if (GUI.Button(new Rect(rect.x + ((ii - 1) % c) * rect.width / c, rect.y, rect.width / c, 30f), display)) {
						curBloxTmp = tmp.Type != null ? tmp.Type.Templates[0] : tmp;
						SceneView.RepaintAll();
					}
					if (isChosen) { GUI.color = guiNormalColor; }

					if ((ii % c) == 0) { GUILayout.EndHorizontal(); }
				}

				TypeButton(ProjectSettings.AirTemplate);
				TypeButton(ProjectSettings.BoxTemplate);

				for (int i = 0, tc = ProjectSettings.Types.Length; i < tc; ++i) {
					var type = ProjectSettings.Types[i];
					if (type.Shelf != curTypeShelf) { continue; }
					if (type.Templates == null || type.Templates.Count == 0) { continue; } // this template wasn't initialized yet
					TypeButton(type.Templates[0]);
				}
				if ((ii % c) != 0) {
					GUILayout.EndHorizontal();
				}
			}

			// choose texture:

			if (ProjectSettings.Textures != null) {

				GUILayout.Space(10f);

				// the texture shelves

				var sn = curTextureShelf.ToUpper().Split(shelfNameSeparator);
				if (EditorGUILayout.DropdownButton(new GUIContent("<color=yellow><b>Textures:</b></color> " + sn[sn.Length - 1]), FocusType.Passive, styleBtnDropDown, GUILayout.Height(30f))) {
					var menu = new GenericMenu();
					menu.DropDown(GUILayoutUtility.GetLastRect());
					foreach (var shelf in ProjectSettings.TextureShelves) {
						sn = shelf.Split(shelfNameSeparator);
						menu.AddItem(new GUIContent(sn[sn.Length - 1]), curTextureShelf == shelf, s => curTextureShelf = (string)s, shelf);
					}
					menu.ShowAsContext();
				}
				// scroll through the shelves
				else if (Event.current.type == EventType.ScrollWheel && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
					var idx = Mathf.Clamp(ProjectSettings.TextureShelves.IndexOf(curTextureShelf), 0, ProjectSettings.TextureShelves.Count);
					idx = (idx + (Event.current.delta.y > 0 ? 1 : -1) + ProjectSettings.TextureShelves.Count) % ProjectSettings.TextureShelves.Count;
					curTextureShelf = ProjectSettings.TextureShelves[idx];
					Event.current.Use();
					Repaint();
					GUILayout.EndScrollView();
					return;
				}

				if (!ProjectSettings.TextureShelves.Contains(curTextureShelf)) {
					curTextureShelf = ProjectSettings.TextureShelves[0];
				}

				// the textures
				var size = 80f + textureButtonSize * 10f;
				int c = Mathf.FloorToInt(contextWidth / size);
				var space = (contextWidth - c * size) * 0.5f;
				var rect = new Rect();
				int ii = 0;

				bool TextureButton(int idx) {
					if (c == 0) { return false; }
					if ((ii++ % c) == 0) {
						rect = EditorGUILayout.GetControlRect(false, size, "button"); 
						GUILayout.BeginHorizontal();
					}
					var r = new Rect(rect.x + ((ii - 1) % c) * size + space, rect.y, size, size);
					var isChosen = curBloxTexIdx == idx;
					if (isChosen) { GUI.color = new Color(0f, 1f, 0f, 1f); }
					if (GUI.Button(r, GUIContent.none)) {
						curBloxTexIdx = idx;
						SceneView.RepaintAll();
					}
					else if (Event.current.type == EventType.ScrollWheel && Event.current.shift && r.Contains(Event.current.mousePosition)) {
						textureButtonSize = Mathf.Clamp(textureButtonSize + (Event.current.delta.y > 0 ? -1 : 1), -3, 5); // make button size smaller or bigger
						Event.current.Use();
						Repaint();
						GUILayout.EndHorizontal();
						GUILayout.EndScrollView();
						return false;
					}
					if (isChosen) { GUI.color = guiNormalColor; }
					var t = ProjectSettings.Textures[idx];
					var tc = t.composition;
					if (tc[0] != null || tc[1] != null || tc[2] != null || tc[3] != null) {
						var pad = new Vector2(10f, 10f);
						var sh = r.size * 0.5f;
						GUI.DrawTexture(new Rect(r.position + new Vector2(6f, 6f), sh - pad), (tc[0] != null ? tc[0] : t).texture);
						GUI.DrawTexture(new Rect(r.position + new Vector2(4f, 6f) + r.size.WithY(0f) * 0.5f, sh - pad), (tc[1] != null ? tc[1] : t).texture);
						GUI.DrawTexture(new Rect(r.position + new Vector2(6f, 4f) + r.size.WithX(0f) * 0.5f, sh - pad), (tc[2] != null ? tc[2] : t).texture);
						GUI.DrawTexture(new Rect(r.position + new Vector2(4f, 4f) + sh, sh - pad), (tc[3] != null ? tc[3] : t).texture);
					}
					else {
						GUI.DrawTexture(new Rect(r.position + new Vector2(8f, 8f), r.size - new Vector2(16f, 16f)), t.texture);
					}

					if ((ii % c) == 0) {
						GUILayout.EndHorizontal();
					}
					return true;
				}

				if (!TextureButton(1)) { return; }; // test 1
				for (int i = 2, tc = ProjectSettings.Textures.Count; i < tc; ++i) {
					if (ProjectSettings.Textures[i].Shelf == curTextureShelf) { if (!TextureButton(i)) { return; }; }
				}
				if ((ii % c) != 0) {
					GUILayout.EndHorizontal();
				}

				var testRect = EditorGUILayout.GetControlRect(true, 0.1f, "button");
				if (Event.current.type == EventType.Repaint) {
					contextWidth = testRect.width;
				}
			}
		}
	}

}