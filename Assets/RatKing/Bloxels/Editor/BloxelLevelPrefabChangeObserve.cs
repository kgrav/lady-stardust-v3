using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RatKing.Bloxels {

	[InitializeOnLoad]
	public class BloxelLevelPrefabChangeObserve {
		static BloxelLevelPrefabChangeObserve() {
			PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;
			PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;
		}

		static void PrefabInstanceUpdated(GameObject go) {
			var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
			//Debug.Log("prefab instance updated: " + go + " " + go.scene.name + " " + PrefabUtility.IsPartOfPrefabInstance(go) + " " + prefab.scene.name);
			if (!Application.isPlaying && prefab != null) {
				foreach (var bl in go.GetComponentsInChildren<BloxelLevel>(true)) {
					bl.PrefabInstanceUpdated(prefab);
				}
			}
		}
	}

}
