using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RatKing.Base;

namespace RatKing.Bloxels {

	[ExecuteInEditMode]
	public class BloxelChunk : MonoBehaviour {
		public Position3 Pos;
		public Vector3 WorldPos;
		[HideInInspector] public Mesh rm;
		[HideInInspector] public Mesh cm;
		[HideInInspector] public MeshFilter mf;
		[HideInInspector] public MeshRenderer mr;
		[HideInInspector] public MeshCollider mc;
		public BloxelLevel lvl;
		public int[] changedBloxelsDataPosIndices;
		public List<int> changedBloxelsTemplateIndices = new List<int>();
		public List<int> changedBloxelsTextureIndices = new List<int>();
		public int changedBloxelsNum;
		public IntSideExtraDataDictionary textureSideExtraDataByRelPos = new IntSideExtraDataDictionary(); // relPos*6+faceDir -> texture / less used than changedBloxelsDataPosIndices, so it can be Dictionary...
		// [i+0] relPos*6+fadeDir ; [i+1] textureIndex(16) / rotation (3) / uv set (3) / offset (5+5)
		//
		public bool MeshesExist => rm != null; // TODO internal, for creating chunk in steps

		// save bloxel data in a less stupid way

		public BloxelTemplate GetChangedBloxelTemplateAt(int idx) {
#if UNITY_EDITOR
			if (idx < 0 || idx >= changedBloxelsNum) { Debug.LogError("Wrong index for additional bloxel data!"); return default; }
#endif
			if (!lvl.ProjectSettings.TemplatesByUID.TryGetValue(lvl.TemplateUIDs[changedBloxelsTemplateIndices[idx]], out var tmp)) { return lvl.ProjectSettings.MissingTemplate; }
			return tmp;
		}

		public Bloxel GetChangedBloxelAt(int idx) {
#if UNITY_EDITOR
			if (idx < 0 || idx >= changedBloxelsNum) { Debug.LogError("Wrong index for additional bloxel data!"); return default; }
#endif
			//Debug.Log(i + ") " + saveBloxelNames[i] + " " + tbn.ContainsKey(saveBloxelNames[i]) + "/" + tbn.Count);
			var tplIdx = changedBloxelsTemplateIndices[idx];
			if (lvl.TemplateUIDs.Count <= tplIdx || !lvl.ProjectSettings.TemplatesByUID.TryGetValue(lvl.TemplateUIDs[tplIdx], out var tmp)) {
				tmp = lvl.ProjectSettings.MissingTemplate;
			}
			var tex = changedBloxelsTextureIndices[idx] != 0 && lvl.ProjectSettings.TexturesByID.ContainsKey(lvl.TextureUIDs[changedBloxelsTextureIndices[idx]]) ? changedBloxelsTextureIndices[idx] : 0;
			return new Bloxel(tmp, tex);
		}
		
		//
		
		//public override bool Equals(object obj) {
		//	var other = obj as BloxelChunk;
		//	if (other == null) { return false; }
		//	return go == other.go;
		//}
		//public override int GetHashCode() { return base.GetHashCode(); }
		public override string ToString() { return "Chunk at (" + Pos.x + "/" + Pos.y + "/" + Pos.z + ")"; }

		//

		public void AssignMaterials(TextureAtlasSettings[] texAtlases, BloxelMeshData vmd) {
			if (vmd.submeshCount == 0) {
			//	Debug.Log("zero materials for " + go.name);
				return;
			}
			else if (vmd.submeshCount == 1) {
				mr.sharedMaterial = texAtlases[vmd.triangles[0].index].Material;
			}
			else {
				var array = new Material[rm.subMeshCount];
				int i = 0;
				for (var iter = vmd.triangles.GetEnumerator(); iter.MoveNext(); ) {
					if (vmd.GetTriangles(iter.Current.index).Count > 0) {
						array[i] = texAtlases[iter.Current.index].Material;
						++i;
					}
				}
				mr.sharedMaterials = array; // TODO store array?
			}
		}
	}

}