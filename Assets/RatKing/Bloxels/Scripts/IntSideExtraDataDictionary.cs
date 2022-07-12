using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;


namespace RatKing {

	[System.Serializable]
	public class IntSideExtraDataDictionary : Dictionary<int, Bloxels.SideExtraData>, ISerializationCallbackReceiver {
		[SerializeField] int[] data;

		//

		public IntSideExtraDataDictionary() : base() { }

		public IntSideExtraDataDictionary(int capacity) : base(capacity) { }

		public IntSideExtraDataDictionary(IDictionary<int, Bloxels.SideExtraData> dict) : base(dict.Count) {
			foreach (var kvp in dict) {
				this[kvp.Key] = kvp.Value;
			}
		}

		protected IntSideExtraDataDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

		//

		public void CopyFrom(IDictionary<int, Bloxels.SideExtraData> dict) {
			this.Clear();
			foreach (var kvp in dict) {
				this[kvp.Key] = kvp.Value;
			}
		}

		public void OnAfterDeserialize() {
			if (data != null) {
				this.Clear();
				for (int i = 0, n = data.Length; i < n; i += 2) {
					var val = data[i + 1];
					var ti = val >> 16;
					var r = (val >> 13) & 0b111;
					var uv = (val >> 10) & 0b111;
					var o = (val) & 0b1111111111;
					this[data[i]] = new Bloxels.SideExtraData(ti, (r == 0b111 ? -1 : r), (uv == 0b111 ? -1 : uv), o);
				}
			}
		}

		public void OnBeforeSerialize() {
			int n = this.Count;
			data = new int[n * 2];

			int i = 0;
			foreach(var kvp in this) {
				//  textureIndex(16) / rotation (3) / uv set (3) / offset (5+5)
				data[i] = kvp.Key;
				var val = kvp.Value;
				data[i+1] = (val.ti << 16) | ((val.r < 0 ? 0b111 : val.r) << 13) | ((val.uv < 0 ? 0b111 : val.uv) << 10) | (val.o);
				i += 2;
			}
		}
	}
}
