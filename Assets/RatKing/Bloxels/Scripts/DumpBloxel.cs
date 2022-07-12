using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RatKing.Bloxels {

	// used to get a list of bloxels from Bloxels
	public struct DumpBloxel {
		public readonly Base.Position3 pos;
		//public readonly int templateIndex;
		public readonly BloxelTemplate template;
		public readonly int textureIndex;
		//
		public DumpBloxel(int x, int y, int z, BloxelTemplate template, int textureIndex) {
			pos = new Base.Position3(x, y, z);
			this.template = template;
			this.textureIndex = textureIndex;
		}
		public DumpBloxel(Base.Position3 pos, BloxelTemplate template, int textureIndex) {
			this.pos = pos;
			this.template = template;
			this.textureIndex = textureIndex;
		}
		public DumpBloxel(Base.Position3 pos, Bloxel bloxel) {
			this.pos = pos;
			template = bloxel.template;
			textureIndex = bloxel.textureIdx;
		}
	}

	public struct DumpBloxelSide {
		public readonly Base.Position3 chunkPos;
		public int[] relativePos;
		public SideExtraData[] sideData;
		//
		public DumpBloxelSide(int x, int y, int z, int count) {
			chunkPos = new Base.Position3(x, y, z);
			relativePos = new int[count];
			sideData = new SideExtraData[count];
		}
		public DumpBloxelSide(Base.Position3 chunkPos, int count) {
			this.chunkPos = chunkPos;
			relativePos = new int[count];
			sideData = new SideExtraData[count];
		}
		public void Set(int i, int relativePos, SideExtraData sideData) {
			this.relativePos[i] = relativePos;
			this.sideData[i] = sideData;
		}
	}

}