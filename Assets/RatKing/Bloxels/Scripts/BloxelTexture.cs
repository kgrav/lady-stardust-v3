using UnityEngine;
using System.Collections.Generic;

namespace RatKing.Bloxels {

	[CreateAssetMenu(fileName = "BloxelTexture", menuName = "Bloxels/BloxelTexture")]
	public class BloxelTexture : ScriptableObject {
		public string ID = ""; // TODO: [Serializable] string ID {get; private set;} ??
#if UNITY_EDITOR
		public Texture2D texture = null;
		public Texture2D[] texturesSecondary = null;
#else
		[System.NonSerialized] public Texture2D texture = null;
		[System.NonSerialized] public Texture2D[] texturesSecondary = null;
#endif
		public int countX = 1;
		public int countY = 1;
		public int maxPixelWidth = 0;
		public int maxPixelHeight = 0;
		public float transparency = 0f;
		public float noiseStrength = 0f;
		public float noiseScale = 1f;
		public int texAtlasIdx = 0;
		public int flags = 1;
		public int neighbourFlags = 1;
		public BloxelTexture[] composition = new BloxelTexture[4]; // top, sides, bottom, inner
		public bool generateCollider = true;
		public string[] tags = new string[0];
		public Base.DynamicVariables variables = new Base.DynamicVariables();
		// set by Unity Editor only:
		public string Shelf = ""; // all textures inside a shelf get displayed at once in the editor
		public Rect rect;
		public Vector2[] pos; // TODO need to be serialized?
		public Vector2[] size; // TODO need to be serialized? // TODO can be one only?
		[System.NonSerialized] public TextureWithDims tempProcessedTex;
		[System.NonSerialized] public int tempIndexInAtlas = -1;

		//

		static readonly int[] compositeMap = { 0, 1, 1, 2, 1, 1, 3 };
		public BloxelTexture GetComposite(int dir) {
			if (composition == null) { return this; }
			var tex = composition[compositeMap[dir]];
			return tex == null ? this : tex;
		}

		public bool HasTag(string tag) {
			foreach (var t in tags) { if (t == tag) { return true; } }
			return false;
		}

		public bool ChangeShelfByPath(string path) {
			if (ID == "$MISSING" || string.IsNullOrWhiteSpace(path)) { Shelf = ""; return false; }
			path = System.IO.Path.GetDirectoryName(path);
			var a = path.LastIndexOf("/");
			var b = path.LastIndexOf("\\");
			var newShelf = path.Substring((a > b ? a : b) + 1);
			if (Shelf == newShelf) { return false; }
			Shelf = newShelf;
			return true;
		}
	}
}
 