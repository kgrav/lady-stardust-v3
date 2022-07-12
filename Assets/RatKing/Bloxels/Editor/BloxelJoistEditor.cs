using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RatKing.Bloxels;

namespace RatKing {

	// TODO undo/redo

	[CanEditMultipleObjects]
	[CustomEditor(typeof(BloxelJoist))]
	public class BloxelJoistEditor : Editor {
		SerializedProperty createOnStart;
		SerializedProperty caps;
		SerializedProperty texRotate;
		SerializedProperty height;
		SerializedProperty width;
		SerializedProperty length;
		SerializedProperty texOffsetX;
		SerializedProperty texOffsetY;
		SerializedProperty noise;
		SerializedProperty noiseSeed;
		SerializedProperty texture;

		//

		void OnEnable() {
			createOnStart = serializedObject.FindProperty("createOnAwake");
			caps = serializedObject.FindProperty("caps");
			texRotate = serializedObject.FindProperty("texRotate");
			height = serializedObject.FindProperty("height");
			width = serializedObject.FindProperty("width");
			length = serializedObject.FindProperty("length");
			texOffsetX = serializedObject.FindProperty("texOffsetX");
			texOffsetY = serializedObject.FindProperty("texOffsetY");
			noise = serializedObject.FindProperty("noise");
			noiseSeed = serializedObject.FindProperty("noiseSeed");
			texture = serializedObject.FindProperty("texture");

			// TODO foreach (var to in serializedObject.targetObjects) {
			// TODO 	var joist = to as BloxelJoist;
			// TODO 	if (!joist.TryGetComponent<MeshFilter>(out var mf)) { continue; }
			// TODO 	if (mf.sharedMesh == null) {
			// TODO 		joist.UpdateMesh();
			// TODO 	}
			// TODO 	else if (!Application.isPlaying && mf.sharedMesh.name != "Joist " + joist.GetInstanceID().ToString()) {
			// TODO 		mf.sharedMesh = Instantiate(mf.sharedMesh);
			// TODO 		mf.sharedMesh.name = "Joist " + joist.GetInstanceID().ToString();
			// TODO 	}
			// TODO }
		}

		static readonly string[] options = new[] {
			"Add Collider", "Remove Collider",
			"Fit To Floor", "Fit To Ceiling",
			"Place On Floor", "Place On Ceiling"
		};

		public override void OnInspectorGUI() {
			bool updateMesh = false;

			EditorGUILayout.PropertyField(createOnStart);
			EditorGUILayout.PropertyField(caps);
			EditorGUILayout.PropertyField(texRotate);
			EditorGUILayout.PropertyField(height);
			EditorGUILayout.PropertyField(width);
			EditorGUILayout.PropertyField(length);
			EditorGUILayout.PropertyField(texOffsetX);
			EditorGUILayout.PropertyField(texOffsetY);
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(noise);
			noise.floatValue = GUILayout.HorizontalSlider(noise.floatValue, 0f, 0.5f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(noiseSeed);
			if (GUILayout.Button("RND")) {
				foreach (var to in serializedObject.targetObjects) {
					var joist = to as BloxelJoist;
					joist.SetNoiseSeed(Random.Range(0, 640000));
					EditorUtility.SetDirty(joist);
					updateMesh = true;
				}
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(texture);
			//if (GUI.changed) { EditorUtility.SetDirty(vt); }

			var selection = GUILayout.SelectionGrid(-1, options, 2);

			switch (selection) {
				case 0: // ADD COLLIDER
					foreach (var to in serializedObject.targetObjects) {
						var joist = to as BloxelJoist;
						joist.SetCollider(true);
						EditorUtility.SetDirty(joist);
					}
					break;
				case 1: // REMOVE COLLIDER
					foreach (var to in serializedObject.targetObjects) {
						var joist = to as BloxelJoist;
						joist.SetCollider(false);
						EditorUtility.SetDirty(joist);
					}
					break;

				case 2: // FIT TOWARDS FLOOR
					foreach (var to in serializedObject.targetObjects) {
						var joist = to as BloxelJoist;
						if (Fit(joist, true)) { updateMesh = true; }
					}
					break;
				case 3: // FIT TOWARDS CEILING
					foreach (var to in serializedObject.targetObjects) {
						var joist = to as BloxelJoist;
						if (Fit(joist)) { updateMesh = true; }
					}
					break;

				case 4: // PLACE ON FLOOR
					foreach (var to in serializedObject.targetObjects) {
						Place(to as BloxelJoist);
					}
					break;
				case 5: // PLACE ON CEILING
					foreach (var to in serializedObject.targetObjects) {
						var joist = to as BloxelJoist;
						Place(to as BloxelJoist, true);
					}
					break;
			}

			if (updateMesh || serializedObject.hasModifiedProperties) {
				serializedObject.ApplyModifiedProperties();
				foreach (var to in serializedObject.targetObjects) {
					((BloxelJoist)to).UpdateMesh();
				}
			}
		}

		public static void Place(BloxelJoist joist, bool ceiling = false) {
			float sign = ceiling ? 1f : -1f;
			float offsetFactor = ceiling ? 1f : 0f;
			var up = joist.transform.up * sign;
			var offset = offsetFactor * joist.Height;
			var origin = joist.transform.position + up * (offset - 0.1f) * joist.transform.lossyScale.y;
			var ray = new Ray(origin, up);
			if (Bloed.RaycastSlow(ray, out var hit, BloxelUtility.CurLevelSettings.ChunkLayerMask | LayerMask.GetMask("Default"))) {
				joist.transform.position = hit.point - (up * offset) * joist.transform.lossyScale.y;
			}
		}

		public static bool Fit(BloxelJoist joist, bool floor = false) {
			float sign = floor ? -1f : 1f;
			float factor = floor ? 0f : 1f;
			var origin = joist.transform.position + joist.transform.up * (joist.Height * factor - sign * 0.1f) * joist.transform.lossyScale.y;
			var ray = new Ray(origin, sign * joist.transform.up);
			if (Bloed.RaycastSlow(ray, out var hit, BloxelUtility.CurLevelSettings.ChunkLayerMask | LayerMask.GetMask("Default"))) {
				joist.transform.position = Vector3.Lerp(hit.point, joist.transform.position, factor);
				joist.SetHeight(joist.Height + (hit.distance / joist.transform.lossyScale.y - 0.1f));
				return true;
			}
			return false;
		}
	}

}