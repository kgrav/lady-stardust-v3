using UnityEngine;
using System.Collections.Generic;

namespace RatKing.Bloxels {
	
	[CreateAssetMenu(fileName = "BloxelType", menuName = "Bloxels/BloxelType")]
	public class BloxelType : ScriptableObject {

		[System.Serializable]
		public class InnerDirHandling {
			public enum Type { None, BoxMap, KeepUVs }
			public Type type = Type.BoxMap;
			public bool dirRotate;
			public bool hasPreferredDir;
			public BloxelUtility.FaceDir preferredDir;
			public float preferredDirAddAngle; // this angle gets added to the preferred inner dir
		}
		//
		public string ID; // TODO all these publics should be properties
		public string shortDesc;
		public Mesh mesh;
		public BloxelType colliderType;
		public int flag = 1;
		public int neighborFlags = 1;
		public int possibleRotations = 1; // bit field
		// inner uv direction handling:
		public InnerDirHandling[] innerDirHandlings = new InnerDirHandling[1];
		// set by Unity Editor only:
		// all types inside a shelf get displayed at once in the editor:
		public string Shelf = "";
		// only used during the game:
		public List<BloxelTemplate> Templates; // TODO should be privately set???

		//

		public bool IsRotationPossible(int d, int r) {
			return (possibleRotations & (1 << (d * 4 + r))) != 0;
		}

		public bool IsRotationPossible(int b) {
			return (possibleRotations & (1 << b)) != 0;
		}

		//
		
		public bool ChangeShelfByPath(string path) {
			if (string.IsNullOrWhiteSpace(path)) { Shelf = ""; return false; }
			path = System.IO.Path.GetDirectoryName(path);
			var a = path.LastIndexOf("/");
			var b = path.LastIndexOf("\\");
			var newShelf = path.Substring((a > b ? a : b) + 1);
			if (Shelf == newShelf) { return false; }
			Shelf = newShelf;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
			return true;
		}
	}
}