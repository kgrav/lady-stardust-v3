using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RatKing.Bloxels {

	[CreateAssetMenu(fileName = "AtlasSettings", menuName = "Bloxels/TextureAtlasSettings")]
	public class TextureAtlasSettings : ScriptableObject {
		[SerializeField] string id = "";
		public string ID => id;
		[SerializeField] Material material = null;
		public Material Material => material;
		[SerializeField] string[] properties = new[] { "_MainTex" };
		public string[] Properties => properties;
#if UNITY_EDITOR
		[SerializeField] TextureImporterCompression atlasCompression = TextureImporterCompression.CompressedHQ;
		public TextureImporterCompression AtlasCompression => atlasCompression;
#endif
		[SerializeField] FilterMode atlasFilterMode = FilterMode.Point;
		public FilterMode AtlasFilterMode => atlasFilterMode;
		[HideInInspector] [SerializeField] List<BloxelTexture> textures = new List<BloxelTexture>();
		public List<BloxelTexture> Textures => textures;

		//

		void OnValidate() {
			if (properties.Length == 0) {
				properties = new[] { "_MainTex" };
			}
		}
	}

}
