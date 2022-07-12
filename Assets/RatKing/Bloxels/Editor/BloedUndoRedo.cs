using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RatKing {

	public static class BloedUndoRedo {
		public static readonly int maxCount = 200;
		//
		static List<System.Action> stackUndo = new List<System.Action>();
		static List<System.Func<bool>> stackRedo = new List<System.Func<bool>>();
		static int curIndex = 0;

		//

		public static void AddAction(System.Func<bool> action, System.Action undoAction) {
			if (action()) {
				for (int idx = stackUndo.Count - 1; curIndex > 0; --curIndex, --idx) {
					stackUndo.RemoveAt(idx);
					stackRedo.RemoveAt(idx);
				}
				stackUndo.Add(undoAction);
				stackRedo.Add(action);
				while (stackUndo.Count > maxCount) {
					stackUndo.RemoveAt(0);
					stackRedo.RemoveAt(0);
				}
			}
		}

		public static bool Undo() {
			if (curIndex >= stackUndo.Count) { return false; }
			curIndex++;
			stackUndo[stackUndo.Count - curIndex]();
			return true;
		}

		public static bool Redo() {
			if (curIndex <= 0) { return false; }
			stackRedo[stackRedo.Count - curIndex]();
			curIndex--;
			return true;
		}

		public static void ResetStacks() {
			stackRedo.Clear();
			stackUndo.Clear();
			curIndex = 0;
		}
	}

}