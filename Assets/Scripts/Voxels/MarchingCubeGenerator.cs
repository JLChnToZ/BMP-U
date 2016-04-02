using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JLChnToZ.Voxels {
    [RequireComponent(typeof(MeshFilter))]
	public class MarchingCubeGenerator : MonoBehaviour {
		public int Size { get; private set; }

		public float[,,] GridData;
		public Vector3[,,] GridNormals;
		public Mesh primitive;
		public Vec3 WindingOrder = new Vec3(0, 1, 2);
		
		MeshFilter mf;
		MeshCollider mc;
		MeshRenderer mr;

		void Awake() {
		}
		
		void OnEnable() {
			mf = GetComponent<MeshFilter>();
			mc = GetComponent<MeshCollider>();
			mr = GetComponent<MeshRenderer>();
		}

		public void Init(int Size, float[,,] GridData) {
			this.Size = Size;
			this.GridData = GridData;
			OnEnable();
		}

		public void Init(int Size, float defaultValue = 0) {
			var f = new float[Size, Size, Size];
			if (defaultValue != 0)
				foreach (var d in MultiDimenIterator.Range(0, f.GetLength(0) - 1, 0, f.GetLength(1) - 1, 0, f.GetLength(2) - 1, 1, 1, 1))
					f[d.i, d.j, d.k] = defaultValue;
			Init(Size, f);
		}

		public void Generate(float isolevel, Action callback = null) {
			MarchingCubes.Target = isolevel;
			MarchingCubes.SetWindingOrder(WindingOrder.i, WindingOrder.j, WindingOrder.k);
			MarchingCubes.GeneratorMode = MarchingCubes.Mode.MarchingCube;
			// CalculateNormals();
			primitive = CreateMesh();
			if (mf != null)
				mf.mesh = primitive;
			if (mc != null)
				mc.sharedMesh = primitive;
			if (callback != null)
				callback();
		}
		
		public void CalculateNormals() {
			//float startTime = Time.realtimeSinceStartup;
			
			//This calculates the normal of each voxel. If you have a 3d array of data
			//the normal is the derivitive of the x, y and z axis.
			//Normally you need to flip the normal (*-1) but it is not needed in this case.
			//If you dont call this function the normals that Unity generates for a mesh are used.
			
			int w = GridData.GetLength(0);
			int h = GridData.GetLength(1);
			int l = GridData.GetLength(2);
			
			GridNormals = GridNormals ?? new Vector3[w, h, l];
			
			for (int x = 2; x < w - 2; x++)
				for (int y = 2; y < h - 2; y++)
					for (int z = 2; z < l - 2; z++) {
						float dx = GridData[x + 1, y, z] - GridData[x - 1, y, z];
						float dy = GridData[x, y + 1, z] - GridData[x, y - 1, z];
						float dz = GridData[x, y, z + 1] - GridData[x, y, z - 1];
				
						GridNormals[x, y, z] = Vector3.Normalize(new Vector3(dx, dy, dz));
					}
		}
		
		Vector3 TriLinearInterpNormal(Vector3 pos) {
			int x = (int)pos.x;
			int y = (int)pos.y;
			int z = (int)pos.z;
			
			float fx = pos.x - x;
			float fy = pos.y - y;
			float fz = pos.z - z;
			
			Vector3 x0 = GridNormals[x, y, z] * (1.0f - fx) + GridNormals[x + 1, y, z] * fx;
			Vector3 x1 = GridNormals[x, y, z + 1] * (1.0f - fx) + GridNormals[x + 1, y, z + 1] * fx;
			
			Vector3 x2 = GridNormals[x, y + 1, z] * (1.0f - fx) + GridNormals[x + 1, y + 1, z] * fx;
			Vector3 x3 = GridNormals[x, y + 1, z + 1] * (1.0f - fx) + GridNormals[x + 1, y + 1, z + 1] * fx;
			
			Vector3 z0 = x0 * (1.0f - fz) + x1 * fz;
			Vector3 z1 = x2 * (1.0f - fz) + x3 * fz;
			
			return z0 * (1.0f - fy) + z1 * fy;
		}
		
		public Mesh CreateMesh() {
			//float startTime = Time.realtimeSinceStartup;
			
			Mesh mesh = MarchingCubes.CreateMesh(GridData);
			if (mesh == null)
				return null;
			
			int size = mesh.vertices.Length;
			
			if (GridNormals != null) {
				Vector3[] normals = new Vector3[size];
				Vector3[] verts = mesh.vertices;
				
				//Each verts in the mesh generated is its position in the voxel array
				//and you can use this to find what the normal at this position.
				//The verts are not at whole numbers though so you need to use trilinear interpolation
				//to find the normal for that position
				
				for (int i = 0; i < size; i++)
					normals[i] = TriLinearInterpNormal(verts[i]);
				
				mesh.normals = normals;
			} else {
				mesh.RecalculateNormals();
			}
			
			Color[] control = new Color[size];
			Vector3[] meshNormals = mesh.normals;
			
			for (int i = 0; i < size; i++) {
				//This creates a control map used to texture the mesh based on the slope
				//of the vert. Its very basic and if you modify how this works yoou will
				//you will probably need to modify the shader as well.
				float dpUp = Vector3.Dot(meshNormals[i], Vector3.up);
				
				float R = Mathf.Pow(Mathf.Abs(dpUp), 2.0f);
				float G = Mathf.Max(0.0f, dpUp); // (Mathf.Max(0.0f, dpUp) < 0.8f) ? 0.0f : 1.0f;
				
				//Whats left end up being the rock face on the vertical areas

				control[i] = new Color(R, G, 0, 0);
			}
			
			//May as well store in colors
			mesh.colors = control;
			
			return mesh;
			
			//Debug.Log("Create mesh time = " + (Time.realtimeSinceStartup-startTime).ToString() );
		}
	}
}