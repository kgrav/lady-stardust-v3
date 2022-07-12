// from: https://gist.github.com/ByronMayne/abf5f29ae65f6bda460485bfa2e2551c

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ByronMayne {

	public delegate void VistedDelegate<T>(T instance);

	public interface IProgressHandler {
		void ClearProgressBar();
		void UpdateProgress(int stepCount, string stepName, string message, int totalSteps, float progress);
	}

	public class AssetIterator : IProgressHandler {
		public class AbortIteration : Exception { }

		[Flags]
		public enum IterationType {
			None = 0,
			Prefabs = 1 << 1,
			Scenes = 1 << 2,
			All = Prefabs | Scenes
		}

		private IterationType m_IterationType;
		private IProgressHandler m_ProgressHandler;
		private List<Behaviour> m_CachedBehaviours;
		private int m_TotalSteps = 0;
		private int m_CurrentStep = 0;


		public VistedDelegate<GameObject> onGameObjectVisited;
		public VistedDelegate<Component> onBehaviourVisited;
		public VistedDelegate<SerializedProperty> onPropertyVisited;

		public IterationType iterationType {
			get { return m_IterationType; }
			set { m_IterationType = value; }
		}

		/// <summary>
		/// Gets or sets the progress handler. By default this instance handles drawing
		/// it's own progress bar. If this is set to null no progress bar is shown. To
		/// revert to default set it back to this instance. 'this.progressHandler = this'. To 
		/// support escaping throw an <see cref="AbortIteration"/> to cancel iteration and
		/// cause no errors to be logged.
		/// </summary>
		public IProgressHandler progressHandler {
			get { return m_ProgressHandler; }
			set { m_ProgressHandler = value; }
		}

		public void Start() {
			if (m_IterationType == IterationType.None) {
				Debug.LogWarning("iterationType is currently set to None so there is no point running this script. Returning early.");
				return;
			}

			if (onGameObjectVisited == null &&
				onBehaviourVisited == null &&
				onPropertyVisited == null) {
				Debug.LogWarning("No callbacks were subscribed for the Asset Iterator there is no point of running the script.");
				return;
			}

			//if (onSceneVisited == null && (m_IterationType & IterationType.Scenes) != IterationType.Scenes) {
			//	Debug.LogWarning("You subscribed to the onSceneVistedDelegate however the IterationType was set to prefabs only. This callback will not be invoked.");
			//}

			// Setup
			m_CachedBehaviours = new List<Behaviour>();

			if ((m_IterationType & IterationType.Prefabs) == IterationType.Prefabs)
				m_TotalSteps++;
			if ((m_IterationType & IterationType.Scenes) == IterationType.Scenes)
				m_TotalSteps++;

			try {
				if ((m_IterationType & IterationType.Prefabs) == IterationType.Prefabs) {
					m_CurrentStep++;
					IteratePrefabs();
				}

				if ((m_IterationType & IterationType.Scenes) == IterationType.Scenes) {
					m_CurrentStep++;
					IterateScenes();
				}
			}
			catch (AbortIteration) {
				// Nothing to do here they just quit.
			}
			catch (Exception e) {
				// We have an exception that so we want to log it
				Debug.LogException(e);
			}
			finally {
				if (m_ProgressHandler != null) {
					m_ProgressHandler.ClearProgressBar();
				}
			}
		}

		// changes by RatKing
		private void IteratePrefabs() {
			AssetDatabase.StartAssetEditing();
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
			var length = prefabGuids.Length;

			for (int i = 0; i < length; i++) {
				var progress = ((float)i / length);
				var assetpath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
				var root = PrefabUtility.LoadPrefabContents(assetpath);
				IterateHierarchy(root.transform, root.transform, false);
				PrefabUtility.SaveAsPrefabAsset(root, assetpath);
				PrefabUtility.UnloadPrefabContents(root);

				if (m_ProgressHandler != null) {
					m_ProgressHandler.UpdateProgress(m_CurrentStep, "Iterating Prefabs", root.name, m_TotalSteps, progress);
				}
			}
			AssetDatabase.StopAssetEditing();
		}

		// changes by RatKing
		// via https://forum.unity.com/threads/solved-load-scene-without-being-in-build-settings-and-without-using-assetbundle.745472/
		private void IterateScenes() {
			var startingScene = SceneManager.GetActiveScene();
			if (startingScene.isDirty) {
				Debug.LogWarning("Save current scene before iterating over all scenes!");
				return;
			}
			var startPath = startingScene.path;
			var scenes = AssetDatabase.FindAssets("t:SceneAsset", new string[] { "Assets" });
			int length = scenes.Length;
			// Loop over every scene
			for (int i = 0; i < length; i++) {
				float progress = ((float)i / length);
				// Open the scene 
				Scene openedScene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(scenes[i]), OpenSceneMode.Single);
				// Update our progress bar
				if (m_ProgressHandler != null) {
					m_ProgressHandler.UpdateProgress(m_CurrentStep, "Iterating Scenes", openedScene.name, m_TotalSteps, progress);
				}
				// Grab all our root objects
				GameObject[] roots = openedScene.GetRootGameObjects();
				// Loop over them all and get their transforms
				for (int x = 0; x < roots.Length; x++) {
					Transform root = roots[x].transform;
					IterateHierarchy(root, root, true);
				}
			}
			if (!string.IsNullOrWhiteSpace(startPath)) {
				EditorSceneManager.OpenScene(startPath, OpenSceneMode.Single);
			}
		}

		private void IterateHierarchy(Transform root, Transform transform, bool isInScene) {
			// Callback: GameObject
			if (onGameObjectVisited != null) {
				onGameObjectVisited(transform.gameObject);
			}

			m_CachedBehaviours.Clear();

			transform.GetComponents(m_CachedBehaviours);

			// Callback: MonoBehaviour
			foreach (var behaviour in m_CachedBehaviours) {
				if (behaviour == null) {
					// If a component is missing this case will be true.
					continue;
				}

				if (onBehaviourVisited != null) {
					onBehaviourVisited(behaviour);
				}

				if (onPropertyVisited != null) {
					SerializedObject serializedObject = new SerializedObject(behaviour);
					SerializedProperty iterator = serializedObject.GetIterator();

					while (iterator.Next(true)) {
						onPropertyVisited(iterator);
					}
				}
			}

			for (int i = 0; i < transform.childCount; i++) {
				IterateHierarchy(root, transform.GetChild(i), isInScene);
			}
		}

		void IProgressHandler.ClearProgressBar() {
			EditorUtility.ClearProgressBar();
		}

		void IProgressHandler.UpdateProgress(int stepCount, string stepName, string message, int totalSteps, float progress) {
			if (EditorUtility.DisplayCancelableProgressBar(string.Format("{0} {1}/{2}", stepName, stepCount, totalSteps), message, progress)) {
				throw new AbortIteration();
			}
		}
	}

}