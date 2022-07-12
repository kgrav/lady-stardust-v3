namespace RatKing.Bloxels {

	[System.Serializable]
	public struct SideExtraData {
		public int ti; // texture index
		public int r; // rotation
		public int uv; // UV set
		public int o; // offset - 5 bit x + 5 bit y
		//
		//
		public static bool operator ==(SideExtraData a, SideExtraData b) { return a.ti == b.ti && a.r == b.r && a.uv == b.uv && a.o == b.o; }
		public static bool operator !=(SideExtraData a, SideExtraData b) { return a.ti != b.ti || a.r != b.r || a.uv != b.uv || a.o != b.o; }
		public override bool Equals(object o) { SideExtraData sed = (SideExtraData)o; return sed == this; }
		public override int GetHashCode() { return base.GetHashCode(); }
		public override string ToString() { return ti + ", " + r + ", " + uv + ", " + o; }
		public bool Equals(SideExtraData other) { return ti == other.ti && r == other.r && uv == other.uv && o == other.o; }
		//
		public SideExtraData(int texture = -1, int rotation = -1, int uvSet = -1, int offset = -1) {
			this.ti = texture;
			this.r = rotation;
			this.uv = uvSet;
			this.o = offset;
		}
		//public bool IsNoChange() {
		//	return ti == -1 && r < 0 && uv < 0 && o < 0;
		//	//return ti == -1 && r == -1 && uv == -1 && o == -1;
		//}
		public bool Is(ref SideExtraData other) {
			var isTI = ti == -1 || (ti == other.ti);
			var isR = r == -1 || (r == 0 && other.r == -1) || (r == other.r);
			var isUV = uv == -1 || (uv == 0 && other.uv == -1) || (uv == other.uv);
			var isO = o == -1 || (o == 0 && other.o == -1) || (o == other.o);
			return isTI && isR && isUV && isO;
		}
		public bool IsTextureOnlySet(int textureIndex) {
			return r <= 0 && uv <= 0 && o <= 0 && this.ti == textureIndex;
			//return r == -1 && uv == -1 && this.ti == textureIndex;
		}
		public bool IsSomethingChanged(int textureIndex = -1) {
			return r > 0 || uv > 0 || o > 0 || (textureIndex >= 0 && ti != textureIndex);
		}
		public bool GetsChangedBy(ref SideExtraData other) {
			return (other.ti >= 0 && ti != other.ti)
				|| (other.r >= 0 && r != other.r)
				|| (other.uv >= 0 && uv != other.uv)
				|| (other.o >= 0 && o != other.o);
		}
		public bool Compared(ref SideExtraData other) {
			return (ti == -1 || other.ti == -1 || ti == other.ti)
				&& (r == -1 || other.r == -1 || r == other.r)
				&& (uv == -1 || other.uv == -1 || uv == other.uv)
				&& (o == -1 || other.o == -1 || o == other.o);
		}
		public bool Changes(ref Bloxel bloxel, int faceDir) {
			return (ti >= 0 && ti != bloxel.textureIdx)
				|| r > 0
				|| o > 0
				|| (faceDir == 6 && uv > 0 && bloxel.template.Type != null && uv < bloxel.template.Type.innerDirHandlings.Length);
		}
		public SideExtraData ChangeBy(ref SideExtraData other) {
			if (other.ti >= 0) { ti = other.ti; }
			if (other.r >= 0) { r = other.r; }
			if (other.uv >= 0) { uv = other.uv; }
			if (other.o >= 0) { o = other.o; }
			return this;
		}
		public SideExtraData CopyWithTexture(int textureIdx) {
			return new SideExtraData(textureIdx, r, uv, o);
		}
		public static SideExtraData dontChange { get { return new SideExtraData(-1, -1, -1, -1); } }
	}
	
}