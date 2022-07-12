namespace RatKing {

	public class TestCustomMethodProvider : Bloxels.IBloxelCustomStandardMethodProvider {
		public int StandardBloxelsNeeded => 2;

		public int CustomStandardMethod(Base.Position3 pos) {
			if ((pos.x % 5 == 0 && pos.z % 5 == 0) || pos.y < 1) { return 1; }
			return 0;
		}
	}

}