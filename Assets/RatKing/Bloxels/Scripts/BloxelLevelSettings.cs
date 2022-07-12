using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RatKing.Bloxels {

	[CreateAssetMenu(fileName = "BloxelLevelSettings", menuName = "Bloxels/BloxelLevelSettings")]
	[DefaultExecutionOrder(-10000)]
	public class BloxelLevelSettings : ScriptableObject {

		[System.Serializable]
		public struct StandardBloxel {
			public BloxelTexture texture;
			public bool isAir;
		}

		public enum StandardMethodType {
			SingleBloxel,
			SecondIsFloor,
			Custom
		}

		[Header("Bloxels")]
		[SerializeField] StandardBloxel[] standardBloxels = { new StandardBloxel() { texture = null, isAir = true } };
		public StandardBloxel[] StandardBloxels => standardBloxels;
		[SerializeField] StandardMethodType standardMethodType = StandardMethodType.SingleBloxel;
		public StandardMethodType StandardTypeMethod => standardMethodType;
#if UNITY_EDITOR
		[SerializeField] UnityEditor.MonoScript customMethodProvider = default;
#endif
		[Header("Chunks")]
		[SerializeField, Layer] int chunkLayer = 0;
		public int ChunkLayer => chunkLayer;
		public int ChunkLayerMask => 1 << chunkLayer;
		[SerializeField, TagSelector] string chunkTag = "";
		public string ChunkTag => string.IsNullOrWhiteSpace(chunkTag) ? "Untagged" : chunkTag;
#if UNITY_EDITOR
		[SerializeField] StaticEditorFlags chunkEditorFlags = (StaticEditorFlags)(-1);
		public StaticEditorFlags ChunkEditorFlags => chunkEditorFlags;
#endif
		[SerializeField] ShadowCastingMode shadowCastingMode = ShadowCastingMode.TwoSided;
		public ShadowCastingMode ShadowCastingMode => shadowCastingMode;
		//
		[SerializeField, HideInInspector] string customMethodProviderTypeName = default;
		//
		Bloxel[] standardBloxelsPrepared = null;
		System.Func<Base.Position3, int> standardMethod = null;

#if UNITY_EDITOR
		void OnValidate() {
			customMethodProviderTypeName = null;
			if (customMethodProvider != null) {
				var type = customMethodProvider.GetClass();
				if (type != null) {
					if (type.GetInterface(nameof(IBloxelCustomStandardMethodProvider)) != null) { customMethodProviderTypeName = type.FullName; }
					else { Debug.LogWarning("Custom Method Provider '" + customMethodProvider.name + "' should implement IBloxelCustomStandardMethodProvider!"); }
				}
				else {
					Debug.LogWarning("Custom Method Provider has no type!");
				}
			}
			if (standardBloxels == null || standardBloxels.Length == 0) {
				standardBloxels = new[] { new StandardBloxel() { texture = null, isAir = true } };
			}
		}
#endif

		//

		public Bloxel GetStandardBloxel(Base.Position3 pos) {
			return standardBloxelsPrepared[standardMethod(pos)];
		}

		public void ResetStandardBloxels() {
			var ps = BloxelUtility.ProjectSettings;
			if (ps == null || ps.TextureIndicesByID == null) {
				Debug.LogWarning("could not set standard bloxels");
				return;
			}

			standardBloxelsPrepared = new Bloxel[standardBloxels.Length];
			for (int i = 0; i < standardBloxels.Length; ++i) {
				var sb = standardBloxels[i];
				standardBloxelsPrepared[i] = new Bloxel(
					sb.isAir ? ps.AirTemplate : ps.BoxTemplate,
					sb.texture != null ? ps.TextureIndicesByID[sb.texture.ID] : 0);
			}

			switch (standardMethodType) {
				case StandardMethodType.SingleBloxel:
					standardMethod = pos => 0;
					break;
				case StandardMethodType.SecondIsFloor:
					standardMethod = pos => pos.y < 1 ? 1 : 0;
					break;
				case StandardMethodType.Custom:
					var type = customMethodProviderTypeName != null ? System.Type.GetType(customMethodProviderTypeName) : null;
					if (type == null) {
						Debug.LogWarning("CustomStandardMethodProvider not defined!");
						standardMethod = pos => 0;
						return;
					}
					if (type.GetInterface(nameof(IBloxelCustomStandardMethodProvider)) == null) {
						Debug.LogError("CustomStandardMethodProvider is wrong type! (" + customMethodProviderTypeName + ")");
						standardMethod = pos => 0;
						return;
					}
					var bsc = System.Activator.CreateInstance(type) as IBloxelCustomStandardMethodProvider;
					if (bsc.StandardBloxelsNeeded > standardBloxels.Length) {
						Debug.LogError("Wrong amount of Standard Bloxels for CustomStandardMethodProvider!");
						standardMethod = pos => 0;
						return;
					}
					standardMethod = bsc.CustomStandardMethod;
					break;
			}
		}
	}

}