using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RatKing.Bloxels {

	public interface IBloxelCustomStandardMethodProvider {
		int StandardBloxelsNeeded { get; }
		int CustomStandardMethod(Base.Position3 pos);
	}

}