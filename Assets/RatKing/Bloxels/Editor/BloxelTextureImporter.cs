using UnityEngine;
using UnityEditor;

namespace RatKing.Bloxels {

	public class BloxelTextureImporter : AssetPostprocessor {

		// http://answers.unity3d.com/questions/382545/changing-texture-import-settings-during-runtime.html
		void OnPreprocessTexture() {
			var ap = assetPath.ToLower();
			var importer = assetImporter as TextureImporter;

			if (ap.Contains("bloxel") && ap.Contains("generated") && ap.Contains("atlas")) {
				var projectSettings = Resources.Load<BloxelProjectSettings>("Settings/ProjectSettings");
				if (projectSettings != null) {
					TextureAtlasSettings settings = null;
					foreach (var tas in projectSettings.TexAtlases) {
						if (assetPath.Contains(tas.ID)) { settings = tas; break; }
					}
					//Debug.Log(asset.name + " has settings " + settings);
					importer.sRGBTexture = true;
					importer.textureType = ap.Contains("normal") || ap.Contains("bump") ? TextureImporterType.NormalMap : TextureImporterType.Default;
					if (settings != null) {
						importer.textureCompression = settings.AtlasCompression;
						importer.filterMode = settings.AtlasFilterMode;
					}
					importer.wrapMode = TextureWrapMode.Clamp;
				}
				else {
					return;
				}
			}
			else if (ap.Contains("bloxel") && ap.Contains("texture")) {
				importer.textureType = TextureImporterType.Default;
#if !UNITY_5_5_OR_NEWER
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
				importer.linearTexture = true;
				importer.generateMipsInLinearSpace = false;
#else
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.sRGBTexture = true;
				//importer.sRGBTexture = false;
#endif
				importer.isReadable = true;
				importer.filterMode = FilterMode.Point;
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.mipmapEnabled = false;
				importer.wrapMode = TextureWrapMode.Repeat;
			}
			else {
				return;
			}

			var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
			if (asset != null) { EditorUtility.SetDirty(asset); }
			//else { importer.textureType = TextureImporterType.Default; }
		}
	}

}