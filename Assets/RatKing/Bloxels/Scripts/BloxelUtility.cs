using System.Collections;
using System.Collections.Generic;
using RatKing.Base;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
#endif

// TODO: unstatify

namespace RatKing.Bloxels {

	[System.Serializable]
	public struct TextureWithDims {
		public Texture2D tex;
		public float w, h;
		public TextureWithDims(Texture2D tex, float w, float h) { this.tex = tex; this.w = w; this.h = h; }
		public bool Is(float w, float h, Texture2D tex) { return this.w > 0 && this.h > 0 && this.tex != null && this.w == w && this.h == h && this.tex == tex; }
		//
		public static TextureWithDims none = new TextureWithDims(null, 0, 0);
	}

	public class SavedState {
		public Position3BloxelChunkDictionary chunks;
		public Bounds bounds;
		public Transform chunkParent;
		public int changedBloxelsCount;
	}

	public static class BloxelUtility {
		public static BloxelLevel CurLevel => BloxelLevel.Current;
		public static BloxelLevelSettings CurLevelSettings => CurLevel != null ? CurLevel.Settings : null;
		static BloxelProjectSettings projectSettings = null;
		public static BloxelProjectSettings ProjectSettings => (projectSettings != null) ? projectSettings : (projectSettings = Resources.Load<BloxelProjectSettings>("Settings/ProjectSettings"));

		public static bool IsInited { get; private set; }
		// current stuff:
		static Dictionary<string, SavedState> savedStates = new Dictionary<string, SavedState>(); // TODO remove?
		//
		public enum FaceDir { Top, Back, Left, Bottom, Front, Right }
		//public enum FaceDirMask { Top = 1, Back = 2, Left = 4, Bottom = 8, Front = 16, Right = 32 }
		public static readonly Vector3[] faceDirVectors = new[] { Vector3.up, Vector3.back, Vector3.left, Vector3.down, Vector3.forward, Vector3.right };
		public static readonly Vector3[] faceDirVectors2 = new[] { Vector3.down, Vector3.back, Vector3.left, Vector3.up, Vector3.forward, Vector3.right };
		public static readonly Position3[] faceDirPositions = new[] { Position3.up, Position3.back, Position3.left, Position3.down, Position3.forward, Position3.right };
		public static readonly Dictionary<Position3, int> faceDirIndexByPosition = new Dictionary<Position3, int>(6) { { Position3.up, 0 }, { Position3.back, 1 }, { Position3.left, 2 }, { Position3.down, 3 }, { Position3.forward, 4 }, { Position3.right, 5 } };

		//

		public static void CreateLevel(BloxelLevelSettings settings, int chunkSize) {
			//if (CurLevel != null || (CurLevel = GameObject.FindObjectOfType<BloxelLevel>()) != null) {
  			//	Debug.LogWarning("Level already exists!");
			//	return;
			//}
			var lgo = new GameObject("BLOXELS [" + chunkSize + "]");
#if UNITY_EDITOR
			StageUtility.PlaceGameObjectInCurrentStage(lgo);
#endif
			BloxelLevel.SetCurrent(lgo.AddComponent<BloxelLevel>());
			//lgo.hideFlags = HideFlags.NotEditable;
			CurLevel.Init(settings, ProjectSettings, chunkSize);
		}

		// when Bloxels was a MonoBehaviour, this was the Start()
		public static void Init(bool forceRecreatingEverything, bool texturesOnly) {
			if (!texturesOnly) { ProjectSettings.PrepareTemplates(); } // TODO
			ProjectSettings.PrepareTextures(true); // TODO: new that i have to reset everytime

			if (CurLevel != null) { CurLevel.UpdateListsOfUIDs(); }

#if UNITY_EDITOR
			//Bloed.MarkSceneDirty();
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif

			IsInited = true;
		}

		/// <summary>
		/// Helper method to rotate a Mesh
		/// </summary>
		/// <param name="mesh">The mesh to rotate (around origin)</param>
		/// <param name="d">Direction 0-5</param>
		/// <param name="r">Rotation 0-3</param>
		/// <returns></returns>
		public static Mesh CreateRotatedMesh(Mesh mesh, int d, int r) {
			d %= 6;
			r %= 4;
			if (d == 0 && r == 0) { return mesh; }
			var vertices = mesh.vertices;
			var normals = mesh.normals;
			var count = mesh.vertexCount;
			if (d != 0) {
				var quat =
					d == 1 ? Quaternion.Euler(180f, 0f, 0f) :
					d == 2 ? Quaternion.Euler(0f, 0f, -90f) :
					d == 3 ? Quaternion.Euler(0f, 0f, 90f) :
					d == 4 ? Quaternion.Euler(90f, 0f, 0f) :
					d == 5 ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
				for (int i = 0; i < count; ++i) {
					vertices[i] = quat * vertices[i];
					normals[i] = quat * normals[i];
				}
			}
			if (r != 0) {
				var quat = Quaternion.Euler(0f, r * 90f, 0f);
				for (int i = 0; i < count; ++i) {
					vertices[i] = quat * vertices[i];
					normals[i] = quat * normals[i];
				}
			}
			var newMesh = Mesh.Instantiate(mesh);
			newMesh.vertices = vertices;
			newMesh.normals = normals;
			return newMesh;
		}

		public static Quaternion GetRotation(int d, int r) {
			d %= 6;
			r %= 4;
			if (d == 0 && r == 0) { return Quaternion.identity; }
			return Quaternion.Euler(0f, r * 90f, 0f) * (
							d == 1 ? Quaternion.Euler(180f, 0f, 0f) :
							d == 2 ? Quaternion.Euler(0f, 0f, -90f) :
							d == 3 ? Quaternion.Euler(0f, 0f, 90f) :
							d == 4 ? Quaternion.Euler(90f, 0f, 0f) :
							d == 5 ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity);
		}

		public static FaceDir GetRotatedDir(FaceDir faceDir, int d, int r) {
			d %= 6;
			r %= 4;
			if (d + r == 0) { return faceDir; }
			var vec = Quaternion.Euler(0f, r * 90f, 0f) * faceDirVectors[(int)faceDir]; // the vector to rotate
			var quat =
				d == 1 ? Quaternion.Euler(180f, 0f, 0f) :
				d == 2 ? Quaternion.Euler(0f, 0f, -90f) :
				d == 3 ? Quaternion.Euler(0f, 0f, 90f) :
				d == 4 ? Quaternion.Euler(90f, 0f, 0f) :
				d == 5 ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
			vec = quat * vec; // the vector to rotate
			return (FaceDir)faceDirIndexByPosition[Position3.RoundedVector(vec)]; // new Position3(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z))];
		}

		// TODO still needed? I have TemplatesByUID again ... in BloxelSettings
		public static BloxelTemplate FindTemplateByUID(string UID) {
			if (UID == "AIR") { return ProjectSettings.AirTemplate; }
			if (UID == "CUBE") { return ProjectSettings.BoxTemplate; }
			foreach (var typ in ProjectSettings.Types) {
				foreach (var t in typ.Templates) {
					if (t.UID == UID) { return t; }
				}
			}
			return ProjectSettings.MissingTemplate;
		}

		// TODO
		public static void OnGameEnded() {
			for (var iter = savedStates.GetEnumerator(); iter.MoveNext();) {
				var ss = iter.Current.Value;
				if (ss.chunkParent != null) { GameObject.Destroy(ss.chunkParent.gameObject); }
			}
			savedStates.Clear();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// HELPERS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// public helper methods

		public static Vector3 GetWorldPos(Position3 pos) {
			return pos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);
		}

#if UNITY_EDITOR
		public static bool TryGetPrefabStage(out PrefabStage stage) {
			stage = PrefabStageUtility.GetCurrentPrefabStage();
			return stage != null;
		}

		public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask) {
			PhysicsScene scene;
			if (TryGetPrefabStage(out var prefStage)) { scene = prefStage.scene.GetPhysicsScene(); }
			else { scene = BloxelLevel.Current.gameObject.scene.GetPhysicsScene(); }
			return scene.Raycast(ray.origin, ray.direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
		}
#else
		public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask) {
			var scene = BloxelLevel.Current.gameObject.scene.GetPhysicsScene();
			return scene.Raycast(ray.origin, ray.direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
		}
#endif
	}

}