using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RatKing {

	[System.Serializable]
	public class BMDArray : SerializableDictionary.Storage<Bloxels.BloxelMeshData[]> {
	}

	[System.Serializable]
	public class StringBMDArrayDictionary : SerializableDictionary<string, Bloxels.BloxelMeshData[], BMDArray> {
		public StringBMDArrayDictionary() : base() { }
		public StringBMDArrayDictionary(int capacity) : base(capacity) { }
	}

	//
	
	[System.Serializable]
	public class Position3BloxelChunkDictionary : SerializableDictionary<Base.Position3, Bloxels.BloxelChunk> {
		public Position3BloxelChunkDictionary() : base() { }
		public Position3BloxelChunkDictionary(int capacity) : base(capacity) { }
	}

}