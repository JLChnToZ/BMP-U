using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JLChnToZ.Voxels {
	public class MarchingCubeChunks : MonoBehaviour {
    
		const int overflowSize = 1;
		
		public int Size { get { return _size; } }
		public float IsoLevel { get; set; }
		public float defaultValue { get; set; }
		public int queueCount { get { return updateQueue.Count; } }
		public Vec3 WindingOrder = new Vec3(0, 1, 2);
		public float targetValue = 1F;
		
		public GameObject voxelChunkPrefab;
    
		Dictionary<Vec3, MarchingCubeGenerator> childs;
		Queue<MarchingCubeGenerator> updateQueue;
		int _size;
		
		public void ExplodeBrush(float x, float y, float z, float size, float val = 1, bool noised = false) {
			// Start a coroutine version of explode brush
			StartCoroutine(ExplodeBrushCoroutine(x, y, z, size, val, noised));
		}
		
		public IEnumerator ExplodeBrushCoroutine(float x, float y, float z, float size, float val = 1, bool noised = false, System.Action<float, Vector3> countCallback = null) {
			Vector3 center, pointPos;
			float count = 0, newValue = 0, origValue = 0, difference;
			center = new Vector3(x, y, z);
			var positions = new List<Vector3>();
			var weights = new List<float>();
			foreach (var d in MultiDimenIterator.Range(
				Mathf.FloorToInt(x - size),
				Mathf.CeilToInt(x + size),
				Mathf.FloorToInt(y - size),
				Mathf.CeilToInt(y + size),
				Mathf.FloorToInt(z - size),
				Mathf.CeilToInt(z + size), 1, 1, 1)) {
				pointPos = d.ToVector3();
				origValue = this[d.i, d.j, d.k];
				newValue = Mathf.Lerp(
					origValue, noised ? Mathf.Lerp(origValue, val, Mathf.PerlinNoise(d.i, d.j)) : val,
					Mathf.Sqrt(Mathf.Clamp01((size - Vector3.Distance(center, pointPos)) / size)));
				this[d.i, d.j, d.k] = newValue;
				if (countCallback == null)
					continue;
				difference = newValue - origValue;
				positions.Add(pointPos);
				weights.Add(difference);
				count += difference;
			}
			if (countCallback != null)
				countCallback(count, transform.TransformPoint(HelperFunctions.WeightedAverage(positions, weights)));
			yield break;
		}
		
		public void InvertChunks() {
			// Start a coroutine version of invert chunks
			StartCoroutine(InvertChunksCoroutine());
		}
		
		public IEnumerator InvertChunksCoroutine() {
			foreach (var chunk in childs) {
				var GD = chunk.Value.GridData;
				foreach (var d in MultiDimenIterator.Range(0, _size - 1, 0, _size - 1, 0, _size - 1, 1, 1, 1))
					GD[d.i, d.j, d.k] = 1 - GD[d.i, d.j, d.k];
			}
			yield break;
		}
		
		public float this[int x, int y, int z] {
			get {
				var p = new Vec3(
					       Mathf.FloorToInt((float)x / _size),
					       Mathf.FloorToInt((float)y / _size),
					       Mathf.FloorToInt((float)z / _size));
				MarchingCubeGenerator result;
				if(childs == null || !childs.TryGetValue(p, out result))
					return defaultValue;
				return result.GridData[x.mod(_size), y.mod(_size), z.mod(_size)];
			}
			set {
				if(childs == null)
					return;
				setValue(
					x.mod(_size),
					y.mod(_size),
					z.mod(_size),
					Mathf.FloorToInt((float)x / _size),
					Mathf.FloorToInt((float)y / _size),
					Mathf.FloorToInt((float)z / _size), value);
			}
		}
    
		void setValue(int x, int y, int z, int chunkx, int chunky, int chunkz, float val, bool checkborder = true) {
			var p = new Vec3(chunkx, chunky, chunkz);
			MarchingCubeGenerator m;
			if (!childs.TryGetValue(p, out m) || m == null) {
				var go = Instantiate(voxelChunkPrefab) as GameObject;
				go.transform.parent = gameObject.transform;
				go.transform.localPosition = p.ToVector3() * _size;
				go.transform.localScale = Vector3.one;
				go.transform.rotation = Quaternion.identity;
				m = go.GetComponent<MarchingCubeGenerator>();
				m.Init(_size + overflowSize, defaultValue);
				m.WindingOrder = WindingOrder;
				childs[p] = m;
			}
			m.GridData[x, y, z] = val;
			if (!updateQueue.Contains(m))
				updateQueue.Enqueue(m);
			if (!checkborder)
				return;
			if (x < overflowSize)
				setValue(x + _size, y, z, chunkx - 1, chunky, chunkz, val, false);
			if (y < overflowSize)
				setValue(x, y + _size, z, chunkx, chunky - 1, chunkz, val, false);
			if (z < overflowSize)
				setValue(x, y, z + _size, chunkx, chunky, chunkz - 1, val, false);
			if (x < overflowSize && y < overflowSize)
				setValue(x + _size, y + _size, z, chunkx - 1, chunky - 1, chunkz, val, false);
			if (y < overflowSize && z < overflowSize)
				setValue(x, y + _size, z + _size, chunkx, chunky - 1, chunkz - 1, val, false);
			if (x < overflowSize && z < overflowSize)
				setValue(x + _size, y, z + _size, chunkx - 1, chunky, chunkz - 1, val, false);
			if (x < overflowSize && y < overflowSize && z < overflowSize)
				setValue(x + _size, y + _size, z + _size, chunkx - 1, chunky - 1, chunkz - 1, val, false);
		}
    
		public void Initialize(int Size) {
			_size = Size;
			if (childs != null) {
				foreach (var child in childs.Values)
					Destroy(child.gameObject);
				childs.Clear();
			} else
				childs = new Dictionary<Vec3, MarchingCubeGenerator>();
			if (updateQueue != null)
				updateQueue.Clear();
			else
				updateQueue = new Queue<MarchingCubeGenerator>();
		}
    
		public Coroutine StartUpdateQueue() {
			return StartCoroutine(DoUpdateQueue());
		}
    
		IEnumerator DoUpdateQueue() {
			while (updateQueue.Count > 0) {
				bool isDone = false;
				updateQueue.Dequeue().Generate(IsoLevel, () => isDone = true);
				// while (!isDone)
					yield return 1;
			}
		}
	}
}