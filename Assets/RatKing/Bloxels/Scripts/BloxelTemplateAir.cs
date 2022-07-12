using UnityEngine;
using System.Collections;

namespace RatKing.Bloxels {

	public class BloxelTemplateAir : BloxelTemplate {

		public override void Build(BloxelMeshData tmd, int texture, int index, BloxelChunk chunk, ref int vertexCount, Bloxel.BuildMode buildMode) {
			// do nothing
		}

		public override bool HasFullWallSide(int dir) {
			return false;
		}

		public override bool HasAirSide(int dir) {
			return true;
		}

		public override bool IsEmptyTo(BloxelTemplate other, int dir) {
			return true;
		}

		public override void InitSidesDataClipped(int templateCount) {
			// do nothing
		}

		public override void CreateSideBySideData(BloxelTemplate other) {
			// do nothing
		}

		public override void ChangeUVs(Vector2[] uvs, int index, int textureIndex, BloxelChunk chunk, ref int vertexCount) {
			// do nothing
		}

		public override bool HasInnerData() {
			return false;
		}
	}

}