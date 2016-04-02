using System;
using System.Collections;
using System.Collections.Generic;

namespace JLChnToZ.Voxels {
	/// <summary>
	/// Contains muti dimentional iterators.
	/// </summary>
	public static class MultiDimenIterator {
		/// <summary>
		/// Iterate through 1 dimentional indeces
		/// </summary>
		/// <param name="from">start index</param>
		/// <param name="to">end index</param>
		/// <param name="step">by how many step, default is 1</param>
		/// <returns>Enumerable object can be used once in foreach loop</returns>
		public static IEnumerable<int> Range(
			int from, int to,
			int step) {
			step = Math.Abs(step);
			if (from <= to)
				for (int i = from; i <= to; i += step)
					yield return i;
			else
				for (int i = from; i >= to; i -= step)
					yield return i;
		}

		/// <summary>
		/// Iterate through 2 dimentional indeces (i -&gt; j)
		/// </summary>
		/// <param name="iFrom">start index 1</param>
		/// <param name="iTo">end index 1</param>
		/// <param name="jFrom">start index 2</param>
		/// <param name="jTo">end index 2</param>
		/// <param name="iStep">how many step in index 1</param>
		/// <param name="jStep">how many step in index 2</param>
		/// <returns>Enumerable object can be used once in foreach loop</returns>
		public static IEnumerable<Vec2> Range(
			int iFrom, int iTo,
			int jFrom, int jTo,
			int iStep,
			int jStep) {
			foreach (var i in Range(iFrom, iTo, iStep))
				foreach (var j in Range(jFrom, jTo, jStep))
					yield return new Vec2 { i = i, j = j };
		}

		/// <summary>
		/// Iterate through 3 dimentional indeces (i -&gt; j -&gt; k)
		/// </summary>
		/// <param name="iFrom">start index 1</param>
		/// <param name="iTo">end index 1</param>
		/// <param name="jFrom">start index 2</param>
		/// <param name="jTo">end index 2</param>
		/// <param name="kFrom">start index 3</param>
		/// <param name="kTo">end index 3</param>
		/// <param name="iStep">how many step in index 1</param>
		/// <param name="jStep">how many step in index 2</param>
		/// <param name="kStep">how many step in index 3</param>
		/// <returns></returns>
		public static IEnumerable<Vec3> Range(
			int iFrom, int iTo,
			int jFrom, int jTo,
			int kFrom, int kTo,
			int iStep,
			int jStep,
			int kStep) {
			foreach (var i in Range(iFrom, iTo, iStep))
				foreach (var j in Range(jFrom, jTo, jStep))
					foreach (var k in Range(kFrom, kTo, kStep))
						yield return new Vec3 { i = i, j = j, k = k };
		}

		/// <summary>
		/// Iterate through 4 dimentional indeces (i -&gt; j -&gt; k -&gt; l)
		/// </summary>
		/// <param name="iFrom">start index 1</param>
		/// <param name="iTo">end index 1</param>
		/// <param name="jFrom">start index 2</param>
		/// <param name="jTo">end index 2</param>
		/// <param name="kFrom">start index 3</param>
		/// <param name="kTo">end index 3</param>
		/// <param name="lFrom">start index 4</param>
		/// <param name="lTo">end index 4</param>
		/// <param name="iStep">how many step in index 1</param>
		/// <param name="jStep">how many step in index 2</param>
		/// <param name="kStep">how many step in index 3</param>
		/// <param name="lStep">how many step in index 4</param>
		/// <returns>Enumerable object can be used once in foreach loop</returns>
		public static IEnumerable<Vec4> Range(
			int iFrom, int iTo,
			int jFrom, int jTo,
			int kFrom, int kTo,
			int lFrom, int lTo,
			int iStep,
			int jStep,
			int kStep,
			int lStep) {
			foreach (var i in Range(iFrom, iTo, iStep))
				foreach (var j in Range(jFrom, jTo, jStep))
					foreach (var k in Range(kFrom, kTo, kStep))
						foreach (var l in Range(lFrom, lTo, lStep))
							yield return new Vec4 { i = i, j = j, k = k, l = l };
		}

		/// <summary>
		/// Iterate through 5 dimentional indeces (i -&gt; j -&gt; k -&gt; l -&gt; m)
		/// </summary>
		/// <param name="iFrom">start index 1</param>
		/// <param name="iTo">end index 1</param>
		/// <param name="jFrom">start index 2</param>
		/// <param name="jTo">end index 2</param>
		/// <param name="kFrom">start index 3</param>
		/// <param name="kTo">end index 3</param>
		/// <param name="lFrom">start index 4</param>
		/// <param name="lTo">end index 4</param>
		/// <param name="mFrom">start index 5</param>
		/// <param name="mTo">end index 5</param>
		/// <param name="iStep">how many step in index 1</param>
		/// <param name="jStep">how many step in index 2</param>
		/// <param name="kStep">how many step in index 3</param>
		/// <param name="lStep">how many step in index 4</param>
		/// <param name="mStep">how many step in index 5</param>
		/// <returns>Enumerable object can be used once in foreach loop</returns>
		public static IEnumerable<Vec5> Range(
			int iFrom, int iTo,
			int jFrom, int jTo,
			int kFrom, int kTo,
			int lFrom, int lTo,
			int mFrom, int mTo,
			int iStep,
			int jStep,
			int kStep,
			int lStep,
			int mStep) {
			foreach (var i in Range(iFrom, iTo, iStep))
				foreach (var j in Range(jFrom, jTo, jStep))
					foreach (var k in Range(kFrom, kTo, kStep))
						foreach (var l in Range(lFrom, lTo, lStep))
							foreach (var m in Range(mFrom, mTo, mStep))
								yield return new Vec5 { i = i, j = j, k = k, l = l, m = m };
		}
		
	}

	/// <summary>
	/// Respends 2 dimentional index
	/// </summary>
	[Serializable]
	[UnityEngine.SerializeField]
	public struct Vec2 : IEquatable<Vec2> {
		public int i;
		public int j;
		
		public Vec2(int i, int j) {
			this.i = i;
			this.j = j;
		}

		public override bool Equals(object obj) {
			return obj is Vec2 && Equals((Vec2)obj);
		}

		public bool Equals(Vec2 other) {
			return i == other.i && j == other.j;
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + i.GetHashCode();
				hash = hash * 23 + j.GetHashCode();
				return hash;
			}
		}

		public override string ToString() {
			return string.Format("[{0}, {1}]", i, j);
		}
	}

	/// <summary>
	/// Respends 3 dimentional index
	/// </summary>
	[Serializable]
	[UnityEngine.SerializeField]
	public struct Vec3 : IEquatable<Vec3> {
		public int i;
		public int j;
		public int k;
		
		public Vec3(int i, int j, int k) {
			this.i = i;
			this.j = j;
			this.k = k;
		}

		public override bool Equals(object obj) {
			return obj is Vec3 && Equals((Vec3)obj);
		}

		public bool Equals(Vec3 other) {
			return i == other.i && j == other.j && k == other.k;
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + i.GetHashCode();
				hash = hash * 23 + j.GetHashCode();
				hash = hash * 23 + k.GetHashCode();
				return hash;
			}
		}

		public override string ToString() {
			return string.Format("[{0}, {1}, {2}]", i, j, k);
		}
	}

	/// <summary>
	/// Respends 4 dimentional index
	/// </summary>
	[Serializable]
	[UnityEngine.SerializeField]
	public struct Vec4 : IEquatable<Vec4> {
		public int i;
		public int j;
		public int k;
		public int l;
		
		public Vec4(int i, int j, int k, int l) {
			this.i = i;
			this.j = j;
			this.k = k;
			this.l = l;
		}

		public override bool Equals(object obj) {
			return obj is Vec3 && Equals((Vec4)obj);
		}

		public bool Equals(Vec4 other) {
			return i == other.i && j == other.j && k == other.k && l == other.l;
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + i.GetHashCode();
				hash = hash * 23 + j.GetHashCode();
				hash = hash * 23 + k.GetHashCode();
				hash = hash * 23 + l.GetHashCode();
				return hash;
			}
		}

		public override string ToString() {
			return string.Format("[{0}, {1}, {2}, {3}]", i, j, k, l);
		}
	}

	/// <summary>
	/// Respends 5 dimentional index
	/// </summary>
	[Serializable]
	[UnityEngine.SerializeField]
	public struct Vec5 : IEquatable<Vec5> {
		public int i;
		public int j;
		public int k;
		public int l;
		public int m;
		
		public Vec5(int i, int j, int k, int l, int m) {
			this.i = i;
			this.j = j;
			this.k = k;
			this.l = l;
			this.m = m;
		}

		public override bool Equals(object obj) {
			return obj is Vec3 && Equals((Vec5)obj);
		}

		public bool Equals(Vec5 other) {
			return i == other.i && j == other.j && k == other.k && l == other.l && m == other.m;
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + i.GetHashCode();
				hash = hash * 23 + j.GetHashCode();
				hash = hash * 23 + k.GetHashCode();
				hash = hash * 23 + l.GetHashCode();
				hash = hash * 23 + m.GetHashCode();
				return hash;
			}
		}

		public override string ToString() {
			return string.Format("[{0}, {1}, {2}, {3}, {4}]", i, j, k, l, m);
		}
	}
}
