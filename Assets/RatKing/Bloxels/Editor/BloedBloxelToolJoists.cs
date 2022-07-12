using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RatKing.Bloxels;
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEditor;

namespace RatKing {

	public class BloedBloxelToolJoists : BloedBloxelTool {
		public bool AllowsPivotPlaceMode { get { return false; } set { } }
		public bool AllowsGridMode { get { return false; } set { } }
		public bool AllowsWireCubes { get { return false; } set { } }
		public bool AllowsChangingSelectedTemplate { get { return false; } set { } }
		public bool AllowsChangingSelectedTexture { get { return true; } set { } }
		public Bloed.OverwritePickInner OverwritePickInner { get { return Bloed.OverwritePickInner.No; } set { } }
		public bool RepaintSceneOnSubBloxelMouseDrag { get { return true; } set { } }

		//

		readonly float[] quantizeFactors = new[] { 0.125f, 0.25f, 0.5f };

		int quantizeFactor = 1;
		bool addCollider = true;
		bool addCaps = false; // TODO
		bool rotateTexture = false;
		bool randomizeTexOffset = true; // TODO
		bool fitToCeiling = true;
		float noise = 0f;
		bool randomizeNoiseSeed = true;
		float standardHeight = 2f;
		float width = 0.25f;
		float length = 0.25f;

		//

		public void OnClick(Bloed bloed, Event evt) {
			if (bloed == null || BloxelLevel.Current == null) { return; }

			var click = !evt.alt && evt.type == EventType.MouseDown;

			if (click && evt.button == 0 && !evt.control) {
				if (GetHit(evt, bloed, out var point, out var normal)) {
					point = Bloed.Vec3InvTrPos(point);
					point = Base.Position3.RoundedVector(point / quantizeFactors[quantizeFactor]).ToVector(quantizeFactors[quantizeFactor]);
					point = Bloed.Vec3TrPos(point);
					CreateJoist(bloed, point, normal, bloed.CurBloxTex);
					evt.Use();
				}
			}
		}

		void CreateJoist(Bloed bloed, Vector3 pos, Vector3 normal, BloxelTexture tex) {
			BloxelJoist joist = null;
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			var parent = BloxelLevel.Current;
			if (parent == null) { return; }
			var h = standardHeight;
			var ftc = fitToCeiling;
			var rt = rotateTexture;
			var ac = addCollider;
			var n = noise;
			var rns = randomizeNoiseSeed;
			var w = width;
			var l = length;
			var c = addCaps;
			var rto = randomizeTexOffset;
			BloedUndoRedo.AddAction(() => {
				//Selection.activeGameObject = null;
				var go = new GameObject("Joist");
				go.transform.SetParent(parent.transform);
				GameObjectUtility.SetStaticEditorFlags(go, BloxelUtility.CurLevelSettings.ChunkEditorFlags);
				joist = go.AddComponent<BloxelJoist>();
				joist.transform.position = pos;
				joist.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
				joist.transform.localScale = Vector3.one;
				joist.SetSize(w, l, false);
				joist.SetTexRotate(rt, false);
				joist.SetTexture(tex, false);
				joist.SetCaps(c, false);
				if (n > 0f) { joist.SetNoise(n, rns ? Random.Range(0, 640000) : 0, false); }
				if (ftc && BloxelJoistEditor.Fit(joist)) { }
				else { joist.SetHeight(h, false); }
				joist.SetParent(parent);
				if (rto && tex != null) { joist.SetTexOffset(Random.Range(0, tex.countX), Random.Range(0, tex.countY)); }
				joist.UpdateMesh();
				joist.SetCollider(ac);
				if (prefabStage == null) { Bloed.MarkSceneDirty(true); }
#if UNITY_2020_1_OR_NEWER
				else { PrefabUtility.SaveAsPrefabAsset(joist.transform.root.gameObject, prefabStage.assetPath); }
#else
				else { PrefabUtility.SaveAsPrefabAsset(joist.transform.root.gameObject, prefabStage.prefabAssetPath); }
#endif
				return true;
			},
			() => {
				var parentGO = joist.transform.root.gameObject;
				if (joist != null) { GameObject.DestroyImmediate(joist.gameObject); }
				if (prefabStage == null) { Bloed.MarkSceneDirty(true); }
#if UNITY_2020_1_OR_NEWER
				else { PrefabUtility.SaveAsPrefabAsset(parentGO, prefabStage.assetPath); }
#else
				else { PrefabUtility.SaveAsPrefabAsset(parentGO, prefabStage.prefabAssetPath); }
#endif
			});
		}

		//

		public void OnSceneGUI(Bloed bloed, SceneView view) {
			EditorGUIUtility.labelWidth = 100f;
			GUILayout.Label("Tool: JOISTER");

			// if (Bloed.TryGetPrefabStage(out var _)) {
			// 	GUILayout.Label("Not usable during prefab\nedit mode!");
			// 	return;
			// }

			GUILayout.Label("<i> • Tab - Change rotation\n • Shift - Pick quantized (" + quantizeFactors[quantizeFactor] + ")</i>");

			EditorGUIUtility.labelWidth = 65f;
			quantizeFactor = EditorGUILayout.IntSlider("Quantize", quantizeFactor, 0, quantizeFactors.Length - 1);
			EditorGUIUtility.labelWidth = 100f;
			addCaps = GUILayout.Toggle(addCaps, "Caps");
			addCollider = GUILayout.Toggle(addCollider, "Add Collider");
			rotateTexture = GUILayout.Toggle(rotateTexture, "Rotate Texture");
			fitToCeiling = GUILayout.Toggle(fitToCeiling, "Fit To Ceiling");
			EditorGUIUtility.labelWidth = 65f;
			noise = EditorGUILayout.Slider("Noise", noise, 0f, 0.5f);
			EditorGUIUtility.labelWidth = 100f;
			if (noise > 0f) {
				randomizeNoiseSeed = GUILayout.Toggle(randomizeNoiseSeed, "Randomize Noise Seed");
			}

			standardHeight = EditorGUILayout.FloatField("Standard Height", standardHeight);
			if (standardHeight < 0f) { standardHeight = 0f; }
			EditorGUIUtility.labelWidth = 65f;
			width = Mathf.Round(EditorGUILayout.Slider("Width", width, 0f, 1f) / 0.05f) * 0.05f;
			length = Mathf.Round(EditorGUILayout.Slider("Length", length, 0f, 1f) / 0.05f) * 0.05f;
			EditorGUIUtility.labelWidth = 100f;
		}
		
		public void DrawHandles(Bloed bloed) {
		}

		public void Hotkeys(Bloed bloed, Event evt, SceneView view) {
			if (evt.keyCode == KeyCode.Tab) {
				evt.Use();
				rotateTexture = !rotateTexture;
				bloed.UnfocusToolbar(view);
			}
		}

		public void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker) {
			cursorHoverMarker.curMarkerMesh = null;
			if (GetHit(Event.current, bloed, out var point, out var normal)) {
				if (Event.current.shift) {
					point = Bloed.Vec3InvTrPos(point);
					point = Base.Position3.RoundedVector(point / quantizeFactors[quantizeFactor]).ToVector(quantizeFactors[quantizeFactor]);
					point = Bloed.Vec3TrPos(point);
				}
				Handles.color = bloed.colorMarkerSolid.col;
				Handles.DrawSolidDisc(point + normal * 0.01f, normal, 0.2f);
				Handles.color = bloed.colorMarkerCube.col;
				Handles.DrawWireDisc(point + normal * 0.01f, normal, 0.2f);
			}
		}

		bool GetHit(Event evt, Bloed bloed, out Vector3 point, out Vector3 normal) {
			var result = BloxelUtility.Raycast(HandleUtility.GUIPointToWorldRay(evt.mousePosition), out var hit, 1000f, -1);
			point = hit.point;
			normal = hit.normal;
			if (!result && bloed.CurWorldPos != null) {
				result = true;
				point = bloed.CurWorldPos.Value;
				normal = bloed.CurWorldNormal.Value;
			}
			else {
			}
			if (BloxelUtility.TryGetPrefabStage(out var _)) {
				point = Bloed.Vec3TrPos(point);
			}
			return result;
		}
	}

}
