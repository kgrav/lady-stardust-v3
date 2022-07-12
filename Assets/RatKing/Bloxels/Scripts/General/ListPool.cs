using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RatKing.Bloxels;

namespace RatKing {
	
	public static class ListPool<T> {
		static Stack<List<T>> pool = null;
		//
		public static List<T> Create() {
			if (pool != null && pool.Count > 0) { return pool.Pop(); }
			return new List<T>();
		}
		public static List<T> Create(int size) {
			if (size < 0) { return null; }
			if (pool != null && pool.Count > 0) { var l = pool.Pop(); if (size > l.Capacity) { l.Capacity = size; } return l; }
			return new List<T>(size);
		}
		public static List<T> Create(IEnumerable<T> before) {
			if (pool != null && pool.Count > 0) { var l = pool.Pop(); if (before != null) { l.AddRange(before); } return l; }
			return new List<T>(before);
		}
		public static void Dispose(ref List<T> list) {
			if (list != null) { list.Clear(); } else { list = new List<T>(); }
			if (pool == null) { pool = new Stack<List<T>>(); }
			pool.Push(list);
			list = null;
		}
	}
	
}