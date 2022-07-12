using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RatKing.Bloxels {

	[System.Serializable]
	public class TriangleList {
		public int index;
		public List<int> indices;
		public TriangleList(int idx) { index = idx; indices = ListPool<int>.Create(); }
		public TriangleList(int idx, int size) { index = idx; indices = ListPool<int>.Create(size); }
		public TriangleList(int idx, IEnumerable<int> before) { index = idx; indices = ListPool<int>.Create(before); }
		public void Dispose() { ListPool<int>.Dispose(ref indices); }
	}


	[System.Serializable]
	public class BloxelMeshData {
		public int vertexCount;
		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Vector2> uvs;
		public List<int> preferDirs;
		//
		public List<TriangleList> triangles;
		//
		public int submeshCount;
		//														 
		[SerializeField, HideInInspector] int dir; // used for inner data only right now, ...
		[SerializeField, HideInInspector] int rot; // ... for templates with UVs rotation with direction and rotation
		public bool sideHasInnerClippedVertices; // used only for sides data, specifically for noise factor calculation
		//
		static BloxelTemplate[] tmpBlxTemplate = new BloxelTemplate[27];
		static float[] tmpBlxNoisStr = new float[27];
		static float[] tmpBlxNoisFac = new float[27];
		static Vector3[] tmpNoisLerpPos = new Vector3[8];

		//

		//public BloxelMeshData(int estimatedSize) {
		public BloxelMeshData(int estimatedSize, bool hasUVs) {
			vertices = ListPool<Vector3>.Create(estimatedSize);
			normals = ListPool<Vector3>.Create(estimatedSize);
			if (hasUVs) { uvs = ListPool<Vector2>.Create(estimatedSize); }
			submeshCount = 1;
			triangles = new List<TriangleList>();
			triangles.Add(new TriangleList(0, estimatedSize * 2));
			vertexCount = 0;
		}

		//public BloxelMeshData(int estimatedSize, int submeshCount) {
		public BloxelMeshData(int estimatedSize, bool hasUVs, int submeshCount) {
			vertices = ListPool<Vector3>.Create(estimatedSize);
			normals = ListPool<Vector3>.Create(estimatedSize);
			if (hasUVs) { uvs = ListPool<Vector2>.Create(estimatedSize); }
			this.submeshCount = submeshCount;
			triangles = new List<TriangleList>();
			for (int i = 0; i < this.submeshCount; ++i) { triangles.Add(new TriangleList(i, estimatedSize)); }
			vertexCount = 0;
		}

		public BloxelMeshData(Mesh mesh, bool withUVs) {
			vertices = ListPool<Vector3>.Create(mesh.vertices);
			vertexCount = mesh.vertexCount;
			normals = ListPool<Vector3>.Create(mesh.normals);
			if (withUVs) { uvs = ListPool<Vector2>.Create(mesh.uv); }
			submeshCount = mesh.subMeshCount;
			triangles = new List<TriangleList>();
			for (int i = 0; i < this.submeshCount; ++i) { triangles.Add(new TriangleList(i, mesh.GetTriangles(i))); }
		}

		public virtual void Dispose() {
			ListPool<Vector3>.Dispose(ref vertices);
			ListPool<Vector3>.Dispose(ref normals);
			vertexCount = 0;
			if (uvs != null) { ListPool<Vector2>.Dispose(ref uvs); }
			if (preferDirs != null) { ListPool<int>.Dispose(ref preferDirs); }
			foreach (var t in triangles) { t.Dispose(); }
			triangles.Clear();
			submeshCount = 1;
			dir = rot = 0;
		}

		//

		public void AddSimpleData(Vector3 vert, Vector3 norm) {
			vertices.Add(vert);
			normals.Add(norm);
			++vertexCount;
		}

		public List<int> GetTriangles(int meshIdx) {
			return triangles[meshIdx].indices;
		}

		public void AddTriangle(int meshIdx, int v0, int v1, int v2) {
			var tris = triangles[0];
			if (submeshCount > meshIdx) { tris = triangles.Find(t => t.index == meshIdx); }
			tris.indices.Add(v0);
			tris.indices.Add(v1);
			tris.indices.Add(v2);
		}

		public enum Handling { /*Share,*/ DeepCopy, Empty, SetNull }

		// with shared vertices, normals, uvs, preferdirs, but not triangles...
		public BloxelMeshData(BloxelMeshData other, Handling trisHandling, Handling uvsHandling) {
			dir = other.dir;
			rot = other.rot;
			vertices = ListPool<Vector3>.Create(other.vertices);
			vertexCount = other.vertexCount;
			normals = ListPool<Vector3>.Create(other.normals);
			//
			if (uvsHandling == Handling.DeepCopy && other.uvs != null) {
				uvs = ListPool<Vector2>.Create(other.uvs.Count);
				foreach (var u in other.uvs) { uvs.Add(u); }
			} else if (uvsHandling == Handling.Empty && other.uvs != null) {
				uvs = ListPool<Vector2>.Create(other.uvs.Count);
			} else { // SetNull
				uvs = null;
			}
			//
			if (trisHandling == Handling.DeepCopy) {
				triangles = new List<TriangleList>();
				foreach (var tri in other.triangles) { triangles.Add(new TriangleList(tri.index, tri.indices)); }
			} else if (trisHandling == Handling.Empty) {
				triangles = new List<TriangleList>();
				foreach (var tri in other.triangles) { triangles.Add(new TriangleList(tri.index)); }
			} else { // SetNull
				triangles = null;
			}
		}

		//

		public void AssignToMesh(Mesh m) {
			m.SetVertices(vertices);
			m.subMeshCount = 0;
			for (var iter = triangles.GetEnumerator(); iter.MoveNext();) {
				var tris = iter.Current.indices;
				if (tris.Count != 0) {
					m.subMeshCount++;
					m.SetTriangles(tris, m.subMeshCount - 1);
				}
			}
			m.SetNormals(normals);
			m.RecalculateBounds();
			if (uvs != null) { m.SetUVs(0, uvs); }
		}

		static void CalculateNoisePositions(BloxelChunk chunk, ref Vector3 pos) {
			var cwp = chunk.WorldPos;
			var cwpp = cwp + pos;
			var texes = BloxelUtility.ProjectSettings.TexturesByID;
			var p3 = Base.Position3.FlooredVector(cwpp);
			for (int y = -1, i = 0; y <= 1; ++y) {
				for (int z = -1; z <= 1; ++z) {
					for (int x = -1; x <= 1; ++x, ++i) {
						var bloxel = chunk.lvl.GetBloxel(p3.x + x, p3.y + y, p3.z + z);
						tmpBlxTemplate[i] = bloxel.template;
						var tex = texes[chunk.lvl.TextureUIDs[bloxel.textureIdx]];
						tmpBlxNoisStr[i] = tex.noiseStrength; // "missing" texture has noiseStrength 99999, so it's okay
						tmpBlxNoisFac[i] = tex.noiseScale; // "missing" texture has noiseFactor 99999, so it's okay
					}
				}
			}

			Vector3 NoisePos(Vector3 p, float factor) {
				return new Vector3(Base.Randomness.SimplexNoise.NoiseMinusPlus1(p.z * factor, p.y * factor, p.x * factor),
								   Base.Randomness.SimplexNoise.NoiseMinusPlus1(p.y * factor, p.x * factor, p.z * factor),
								   Base.Randomness.SimplexNoise.NoiseMinusPlus1(p.x * factor, p.z * factor, p.y * factor));
			}
			bool GetNoiseStrength(int b, out float v) {
				v = Mathf.Min(tmpBlxNoisStr[b], tmpBlxNoisStr[b + 1], tmpBlxNoisStr[b + 3], tmpBlxNoisStr[b + 4], tmpBlxNoisStr[b + 9], tmpBlxNoisStr[b + 10], tmpBlxNoisStr[b + 12], tmpBlxNoisStr[b + 13]);
				if (v == 0f) { return false; }
				// public enum FaceDir { 0 Top, 1 Back, 2 Left, 3 Bottom, 4 Front, 5 Right }
				if (tmpBlxTemplate[b].HasClippedInnerVertices(tmpBlxTemplate[b + 1], 5)) { v = 0f; return false; }
				if (tmpBlxTemplate[b].HasClippedInnerVertices(tmpBlxTemplate[b + 3], 4)) { v = 0f; return false; } // TODO correct number? or must be 1?
				if (tmpBlxTemplate[b].HasClippedInnerVertices(tmpBlxTemplate[b + 9], 0)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 4].HasClippedInnerVertices(tmpBlxTemplate[b + 1], 1)) { v = 0f; return false; } // TODO correct number? or must be 4?
				if (tmpBlxTemplate[b + 4].HasClippedInnerVertices(tmpBlxTemplate[b + 3], 2)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 4].HasClippedInnerVertices(tmpBlxTemplate[b + 13], 0)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 10].HasClippedInnerVertices(tmpBlxTemplate[b + 1], 3)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 10].HasClippedInnerVertices(tmpBlxTemplate[b + 9], 2)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 10].HasClippedInnerVertices(tmpBlxTemplate[b + 13], 4)) { v = 0f; return false; } // TODO correct number? or must be 1?
				if (tmpBlxTemplate[b + 12].HasClippedInnerVertices(tmpBlxTemplate[b + 3], 3)) { v = 0f; return false; }
				if (tmpBlxTemplate[b + 12].HasClippedInnerVertices(tmpBlxTemplate[b + 9], 1)) { v = 0f; return false; } // TODO correct number? or must be 4?
				if (tmpBlxTemplate[b + 12].HasClippedInnerVertices(tmpBlxTemplate[b + 13], 5)) { v = 0f; return false; }
				return true;
			}
			float GetNoiseFactor(int b) { return Mathf.Min(tmpBlxNoisFac[b], tmpBlxNoisFac[b + 1], tmpBlxNoisFac[b + 3], tmpBlxNoisFac[b + 4], tmpBlxNoisFac[b + 9], tmpBlxNoisFac[b + 10], tmpBlxNoisFac[b + 12], tmpBlxNoisFac[b + 13]); }

			float str;
			tmpNoisLerpPos[0] = GetNoiseStrength( 0, out str) ? (str * NoisePos(cwpp + new Vector3(-0.5f, -0.5f, -0.5f), GetNoiseFactor( 0))) : Vector3.zero;
			tmpNoisLerpPos[1] = GetNoiseStrength( 1, out str) ? (str * NoisePos(cwpp + new Vector3( 0.5f, -0.5f, -0.5f), GetNoiseFactor( 1))) : Vector3.zero;
			tmpNoisLerpPos[2] = GetNoiseStrength( 3, out str) ? (str * NoisePos(cwpp + new Vector3(-0.5f, -0.5f,  0.5f), GetNoiseFactor( 3))) : Vector3.zero;
			tmpNoisLerpPos[3] = GetNoiseStrength( 4, out str) ? (str * NoisePos(cwpp + new Vector3( 0.5f, -0.5f,  0.5f), GetNoiseFactor( 4))) : Vector3.zero;
			tmpNoisLerpPos[4] = GetNoiseStrength( 9, out str) ? (str * NoisePos(cwpp + new Vector3(-0.5f,  0.5f, -0.5f), GetNoiseFactor( 9))) : Vector3.zero;
			tmpNoisLerpPos[5] = GetNoiseStrength(10, out str) ? (str * NoisePos(cwpp + new Vector3( 0.5f,  0.5f, -0.5f), GetNoiseFactor(10))) : Vector3.zero;
			tmpNoisLerpPos[6] = GetNoiseStrength(12, out str) ? (str * NoisePos(cwpp + new Vector3(-0.5f,  0.5f,  0.5f), GetNoiseFactor(12))) : Vector3.zero;
			tmpNoisLerpPos[7] = GetNoiseStrength(13, out str) ? (str * NoisePos(cwpp + new Vector3( 0.5f,  0.5f,  0.5f), GetNoiseFactor(13))) : Vector3.zero;
		}

		static Vector3 NoiseLerpPos(Vector3 v) {
			v.x += 0.5f; v.y += 0.5f; v.z += 0.5f;
			return Vector3.Lerp(
				Vector3.Lerp(Vector3.Lerp(tmpNoisLerpPos[0], tmpNoisLerpPos[1], v.x), Vector3.Lerp(tmpNoisLerpPos[2], tmpNoisLerpPos[3], v.x), v.z),
				Vector3.Lerp(Vector3.Lerp(tmpNoisLerpPos[4], tmpNoisLerpPos[5], v.x), Vector3.Lerp(tmpNoisLerpPos[6], tmpNoisLerpPos[7], v.x), v.z),
				v.y
			);
		}

		/// <summary>
		/// this versionof AddTo doesn't need the direction and is thus a bit slower?
		/// needs to be optimized!!!
		/// // -> gets called for inner data
		/// </summary>
		/// <param name="other"></param>
		/// <param name="tex"></param>
		/// <param name="chunk"></param>
		/// <param name="pos"></param>
		/// <param name="vertexCount"></param>
		public void AddTo(BloxelMeshData other, BloxelTexture origTex, BloxelTexture tex, int extraRot, int offX, int offY, BloxelChunk chunk, Vector3 pos, ref int vertexCount, Bloxel.BuildMode buildMode) {
			if (triangles == null || triangles.Count == 0) { return; } // is empty

			var v2 = Vector2.zero;
			var wp = pos + chunk.WorldPos;

			// r0 ->  x/y/ z
			// r1 -> -z/y/ x
			// r2 -> -x/y/-z
			// r3 ->  z/y/-x
			switch (rot) {
				case 1: wp = new Vector3(-wp.z, wp.y,  wp.x); break;
				case 2: wp = new Vector3(-wp.x, wp.y, -wp.z); break;
				case 3: wp = new Vector3( wp.z, wp.y, -wp.x); break;
			}

			// d0 ->  x /  y /  z
			// d1 -> -x /  y / -z ??
			// d2 -> -y /  x /  z
			// d3 ->  y / -x /  z
			// d4 ->  x /  z / -y
			// d5 ->  x / -z /  y
			switch (dir) {
				//case 1: wp = new Vector3(-wp.x,  wp.y, -wp.z); break;
				case 2: wp = new Vector3(-wp.y,  wp.x,  wp.z); break;
				case 3: wp = new Vector3( wp.y, -wp.x,  wp.z); break;
				case 4: wp = new Vector3( wp.x,  wp.z, -wp.y); break;
				case 5: wp = new Vector3( wp.x, -wp.z,  wp.y); break;
			}


			//if (tex.noise <= 0f) {
			if (origTex.noiseStrength <= 0f || buildMode == Bloxel.BuildMode.CollisionEditor) {
				for (int i = 0; i < this.vertexCount; ++i) { // TODO optimize
					if (preferDirs != null) { v2 = ClipperTools.GetVec2FromVec3Delegates[preferDirs[i]](wp); }
					var texX = GetRotatedUV_X(v2, tex, extraRot, offX);
					var texY = GetRotatedUV_Y(v2, tex, extraRot, offY);
					var texA = tex.pos[texY * tex.countX + texX];
					var texS = tex.size[texY * tex.countX + texX];
					other.vertices.Add(vertices[i] + pos);
					other.normals.Add(normals[i]);
					if (buildMode == Bloxel.BuildMode.Render && uvs != null && other.uvs != null) { other.uvs.Add(texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS)); }
				}
			}
			else {
				// experiment with noise

				CalculateNoisePositions(chunk, ref pos);
				
				for (int i = 0; i < this.vertexCount; ++i) {
					if (preferDirs != null) { v2 = ClipperTools.GetVec2FromVec3Delegates[preferDirs[i]](wp); }
					var texX = GetRotatedUV_X(v2, tex, extraRot, offX);
					var texY = GetRotatedUV_Y(v2, tex, extraRot, offY);
					var texA = tex.pos[texY * tex.countX + texX];
					var texS = tex.size[texY * tex.countX + texX];

					var n = NoiseLerpPos(vertices[i]);

					other.vertices.Add(vertices[i] + pos + n);
					other.normals.Add((normals[i] + n).normalized); // TODO change
					if (buildMode == Bloxel.BuildMode.Render && uvs != null && other.uvs != null) { other.uvs.Add(texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS)); }
				}
			}

	//		for (int i = 0; i < this.vertexCount; ++i) { // TODO optimize
	//			if (preferDirs != null) { v2 = ClipperTools.GetVec2FromVec3Delegates[preferDirs[i]](wp); }
	//			var texX = GetRotatedUV_X(v2, tex, extraRot, offX);
	//			var texY = GetRotatedUV_Y(v2, tex, extraRot, offY);
	//			var texA = tex.pos[texY * tex.countX + texX];
	//			var texS = tex.size[texY * tex.countX + texX];
	//
	//			//if (!forCollider) Debug.Log("N " + i + ")" + tex.name + " " + vertices[i] + "/" + wp + "  " + dir + "/" + rot + " pd:" + preferDirs[i] + " --> " + texX + "/" + texY + "/" + texA + "/" + texS);
	//
	//			var vp = vertices[i] + pos;
	//			if (tex.noise > 0f) { other.vertices.Add(vp + Noise(vp + chunk.WorldPos, tex.noise)); }
	//			else { other.vertices.Add(vp); }
	//			//other.vertices.Add(vertices[i] + pos);
	//			other.normals.Add(normals[i]);
	//			if (!forCollider && uvs != null && other.uvs != null) { other.uvs.Add(texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS)); }
	//			// TODO less work for collider
	//		}
			other.vertexCount += this.vertexCount;

			var tris = triangles[0].indices;
			var tc = tris.Count;
			var meshIdx = other.submeshCount > tex.texAtlasIdx ? tex.texAtlasIdx : 0;
			var otris = other.triangles.Find(t => t.index == meshIdx).indices;
			for (int i = 0; i < tc; ++i) {
				otris.Add(tris[i] + vertexCount);
			}
			vertexCount += this.vertexCount;
		}

		// gets called for sides
		public void AddTo(BloxelMeshData other, BloxelTexture origTex, BloxelTexture tex, int extraRot, int offX, int offY, int dir, BloxelChunk chunk, Vector3 pos, ref int vertexCount, Bloxel.BuildMode buildMode) {
			if (triangles == null || triangles.Count == 0) { return; } // is empty

			var v2 = ClipperTools.GetVec2FromVec3Delegates[dir](pos + chunk.WorldPos);
			var texX = GetRotatedUV_X(v2, tex, extraRot, offX); // (int)Mathf.Repeat(Mathf.Floor(v2.x), tex.countX);
			var texY = GetRotatedUV_Y(v2, tex, extraRot, offY); // (int)Mathf.Repeat(Mathf.Floor(-v2.y), tex.countY);
			var texA = tex.pos[texY * tex.countX + texX];
			var texS = tex.size[texY * tex.countX + texX];
			
			if (origTex.noiseStrength <= 0f || buildMode == Bloxel.BuildMode.CollisionEditor) {
				for (int i = 0; i < this.vertexCount; ++i) {
					other.vertices.Add(vertices[i] + pos);
					other.normals.Add(normals[i]);
					if (buildMode == Bloxel.BuildMode.Render && uvs != null && other.uvs != null) { other.uvs.Add(texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS)); }
				}
			}
			else {
				// experiment with noise
				CalculateNoisePositions(chunk, ref pos);
				
				for (int i = 0; i < this.vertexCount; ++i) {
					var n = NoiseLerpPos(vertices[i]);
					other.vertices.Add(vertices[i] + pos + n);
					other.normals.Add((normals[i] + n).normalized); // TODO change
					if (buildMode == Bloxel.BuildMode.Render && uvs != null && other.uvs != null) { other.uvs.Add(texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS)); }
				}
			}
			other.vertexCount += this.vertexCount;

			var tris = triangles[0].indices;
			var tc = tris.Count;
			var meshIdx = other.submeshCount > tex.texAtlasIdx ? tex.texAtlasIdx : 0;
			var otris = other.triangles.Find(t => t.index == meshIdx).indices;
			for (int i = 0; i < tc; ++i) {
				otris.Add(tris[i] + vertexCount);
			}
			/*
			var tc = triangles.Count;
			for (int i = 0; i < tc; ++i) {
				other.triangles.Add(triangles[i] + vertexCount);
			}*/
			vertexCount += this.vertexCount;
		}

		// called by inner data
		public void ChangeUVsOf(Vector2[] meshUVs, BloxelTexture tex, int extraRot, int offX, int offY, Vector3 worldPos, ref int vertexCount) {
			var wp = worldPos;
			switch (rot) {
				case 1: wp = new Vector3(-wp.z, wp.y,  wp.x); break;
				case 2: wp = new Vector3(-wp.x, wp.y, -wp.z); break;
				case 3: wp = new Vector3( wp.z, wp.y, -wp.x); break;
			}
			switch (dir) {
				//case 1: wp = new Vector3(-wp.x,  wp.y, -wp.z); break;
				case 2: wp = new Vector3(-wp.y,  wp.x,  wp.z); break;
				case 3: wp = new Vector3( wp.y, -wp.x,  wp.z); break;
				case 4: wp = new Vector3( wp.x,  wp.z, -wp.y); break;
				case 5: wp = new Vector3( wp.x, -wp.z,  wp.y); break;
			}

			for (int i = 0; i < this.vertexCount; ++i, ++vertexCount) {
				var v2 = preferDirs == null ? Vector2.zero : ClipperTools.GetVec2FromVec3Delegates[preferDirs[i]](wp);
				var texX = GetRotatedUV_X(v2, tex, extraRot, offX); // (int)Mathf.Repeat(Mathf.Floor(v2.x), tex.countX);
				var texY = GetRotatedUV_Y(v2, tex, extraRot, offY); // (int)Mathf.Repeat(Mathf.Floor(-v2.y), tex.countY);
				var texA = tex.pos[texY * tex.countX + texX];
				var texS = tex.size[texY * tex.countX + texX];

//				Debug.Log("U " + tex.name + " " + vertices[i] + " " + dir + "/" + rot + " " + extraRot + " ... " + texX + "/" + texY + "/" + texA + "/" + texS);

				if (uvs != null) { meshUVs[i] = texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS); }
			}
		}

		// called by side data
		public void ChangeUVsOf(Vector2[] meshUVs, BloxelTexture tex, int extraRot, int offX, int offY, int dir, Vector3 worldPos, ref int vertexCount) {
			var v2 = ClipperTools.GetVec2FromVec3Delegates[dir](worldPos);
			var texX = GetRotatedUV_X(v2, tex, extraRot, offX); // (int)Mathf.Repeat(Mathf.Floor(-v2.y), tex.countX);
			var texY = GetRotatedUV_Y(v2, tex, extraRot, offY); // (int)Mathf.Repeat(Mathf.Floor(-v2.x), tex.countY);
			//var texX = (int)Mathf.Repeat(Mathf.Floor(v2.y), tex.countX); // CCW
			//var texY = (int)Mathf.Repeat(Mathf.Floor(v2.x), tex.countY); // CCW
			var texA = tex.pos[texY * tex.countX + texX];
			var texS = tex.size[texY * tex.countX + texX];
			for (int i = 0; i < this.vertexCount; ++i, ++vertexCount) {
				if (uvs != null) { meshUVs[i] = texA + Vector2.Scale(GetRotatedUV(uvs[i], extraRot), texS); }
			}
		}

		// called by secret doors ...
		public void ChangeUVsOf(Vector2[] meshUVs, int subMesh, BloxelTexture tex, int extraRot, Transform transform, int startTri = 0, int triCount = -1) {
			var worldPos = transform.position;

			if (preferDirs == null) {
				for (int i = 0; i < this.vertexCount; ++i) {
					var texA = tex.pos[0];
					var texS = tex.size[0];
					if (uvs != null) { meshUVs[i] = texA + Vector2.Scale(uvs[i], texS); }
				}
				return;
			}

			int[] indices = new int[3];
			//for (var iter_submeshes = triangles.GetEnumerator(); iter_submeshes.MoveNext();) {
			//var tris = iter_submeshes.Current.Value;
			var tris = triangles[subMesh].indices;
			startTri *= 3;
			if (triCount < 0) { triCount = tris.Count; } else { triCount = startTri + triCount * 3; }
			for (int t = startTri; t < triCount; t += 3) {
				indices[0] = tris[t + 0];
				indices[1] = tris[t + 1];
				indices[2] = tris[t + 2];
				Vector3 v_a = vertices[indices[0]], v_b = vertices[indices[1]], v_c = vertices[indices[2]];
				//Vector3 v_m = (v_a + v_b + v_c) * 0.3333333333f;
				Vector3 v_m = (transform.TransformPoint(v_a) + transform.TransformPoint(v_b) + transform.TransformPoint(v_c)) * 0.3333333333f;
				//float a = Vector3.Distance(v_b, v_c), b = Vector3.Distance(v_a, v_c), c = Vector3.Distance(v_b, v_c);
				//Vector3 v_m = worldPos + (v_a * a + v_b * b + v_c * c) / (a + b + c);
				// v_m.x = Mathf.Floor(v_m.x); v_m.y = Mathf.Ceil(v_m.y); v_m.z = Mathf.Ceil(v_m.z);

				for (int ii = 0; ii < 3; ++ii) {
					int i = indices[ii];
					var p = preferDirs[i];
					var m = ClipperTools.GetVec2FromVec3Delegates[p](
						/*worldPos + */
						new Vector3(
							(p == 0 || p == 1 || p == 2 || p == 3) ? Mathf.Floor(v_m.x) : Mathf.Ceil(v_m.x),
							Mathf.Ceil(v_m.y),
							(p == 0 || p == 1 || p == 2) ? Mathf.Ceil(v_m.z) : Mathf.Floor(v_m.z))
					);

					var texX = (int) Mathf.Repeat(Mathf.Floor(m.x), tex.countX);
					var texY = (int) Mathf.Repeat(Mathf.Floor(-m.y), tex.countY);
					var texA = tex.pos[texY * tex.countX + texX];
					var texS = tex.size[texY * tex.countX + texX];

					if (uvs != null) { meshUVs[i] = texA + Vector2.Scale(uvs[i], texS); }
				}
			}
			//}
		}

		int GetRotatedUV_X(Vector2 v2, BloxelTexture tex, int rot, int offs) {
			rot = (int)Mathf.Repeat(rot, 4);
			// 0: v2.x
			// 1: -v2.y
			// 2: -v2.x
			// 3: v2.y
			var x = (((rot + 1) % 2) * v2.x + (rot % 2) * v2.y) * ((rot % 3) == 0 ? 1f : -1f);
			return (int)Mathf.Repeat(Mathf.Floor(x + offs), tex.countX);
		}

		int GetRotatedUV_Y(Vector2 v2, BloxelTexture tex, int rot, int offs) {
			rot = (int)Mathf.Repeat(rot, 4); // rot % 4;
			// 0: -v2.y
			// 1: -v2.x
			// 2: v2.y
			// 3: v2.x
			var y = ((rot % 2) * v2.x + ((rot + 1) % 2) * v2.y) * (rot > 1 ? 1f : -1f);
			return (int)Mathf.Repeat(Mathf.Floor(y + offs), tex.countY);
		}
		Vector2 GetRotatedUV(Vector2 uv, int rot) {
			rot = (int)Mathf.Repeat(rot, 4); // rot % 4;
			// 0: uv.x, uv.y
			// 1: 1-uv.y, uv.x
			// 2: 1-uv.x, 1-uv.y
			// 3: uv.y, 1-uv.x
			var x = ((rot + 1) % 2) * uv.x + (rot % 2) * uv.y;
			if (rot % 3 != 0) { x = 1f - x; }
			var y = (rot % 2) * uv.x + ((rot + 1) % 2) * uv.y;
			if (rot > 1) { y = 1f - y; }
			return new Vector2(x, y);
		}

		// only to be used when there is only one submesh!
		// will remove from all share groups
		public void ReduceData() {
			if (triangles.Count != 1) { Debug.LogWarning("should not reduce data of this VMD"); return; }
			//
			var tris = triangles[0].indices;
			int trisCount = tris.Count;
			var newVertices = ListPool<Vector3>.Create(trisCount / 2);
			var newNormals = ListPool<Vector3>.Create(trisCount / 2);
			var newUVs = uvs != null ? ListPool<Vector2>.Create(trisCount / 2) : null;
			var newPreferDirs = preferDirs != null ? ListPool<int>.Create(trisCount / 2) : null;
			var newTriangles = new int[trisCount];
			var done = ListPool<int>.Create(trisCount / 2);
			for (int i = 0; i < trisCount; ++i) {
				int vIdx = tris[i];
				newTriangles[i] = done.IndexOf(vIdx);
				if (newTriangles[i] == -1) {
					newVertices.Add(vertices[vIdx]);
					newNormals.Add(normals[vIdx]);
					if (uvs != null && uvs.Count > 0) { /* Debug.Log(tris.Count + " " + UVs.Count + " " + vIdx); */ newUVs.Add(uvs[vIdx]); }
					if (preferDirs != null && preferDirs.Count > 0) { Debug.Log(i + "/" + preferDirs.Count); newPreferDirs.Add(preferDirs[vIdx]); }
					newTriangles[i] = done.Count; // is sorted
					done.Add(vIdx);
				}
			}
			///
			ListPool<Vector3>.Dispose(ref vertices);
			ListPool<Vector3>.Dispose(ref normals);
			if (uvs != null) { ListPool<Vector2>.Dispose(ref uvs); }
			if (preferDirs != null) { ListPool<int>.Dispose(ref preferDirs); }
			triangles[0].Dispose(); //}
			vertices = newVertices;
			vertexCount = newVertices.Count;
			normals = newNormals;
			uvs = newUVs;
			preferDirs = newPreferDirs;
			triangles[0].indices = ListPool<int>.Create(newTriangles);
			ListPool<int>.Dispose(ref done);
		}

		public void GenerateUVs_PlanarMapping_Centered(int direction /*, Vector2 uvFactors*/ ) {
			PrepareUVs();
			var GetVector2 = ClipperTools.GetVec2FromVec3Delegates[direction]; // TODO rotation
			for (int i = 0; i < vertexCount; ++i) {
				var v = GetVector2(vertices[i]);
				//if (i > 0) { var t = v.y; v.y = -v.x; v.x = t; } // TODO TEST
				uvs.Add(new Vector2(v.x + 0.5f, v.y + 0.5f));
			}
		}

		// used for inner side data of bloxels
		public void GenerateUVs_BoxMapping_Centered(/*int dir, int rot, */int preferredDir = -1, float addAngle = 0f) {
			PrepareUVs();
			for (int i = 0; i < vertexCount; ++i) {
				var n = normals[i];
				var curDIdx = 0;
				var curAngle = 180f; // Vector3.Dot(Bloxels.faceDirVectors[0], n);
				for (int d = 0; d < 6; ++d) {
					//var dot = Vector3.Dot(Bloxels.faceDirVectors[d], n);
					var angle = Vector3.Angle(BloxelUtility.faceDirVectors[d], n);
					if (d == preferredDir) { angle -= addAngle; } // TODO
					if (angle < curAngle) { curAngle = angle; curDIdx = d; }
				}
				preferDirs.Add(curDIdx);
				var r = ClipperTools.GetVec2FromVec3Delegates[curDIdx](vertices[i]);
				uvs.Add(new Vector2(r.x + 0.5f, r.y + 0.5f));
			}
			//this.dir = dir;
			//this.rot = rot;
		}

		// not used right now
		public void GenerateUVs_BoxMapping_Centered(/*int dir, int rot, */Vector3 offset, int preferredDir = -1, float addAngle = 0f) {
			PrepareUVs();
			for (int i = 0; i < vertexCount; ++i) {
				var n = normals[i];
				var curDIdx = 0;
				var curAngle = 180f; // Vector3.Dot(Bloxels.faceDirVectors[0], n);
				for (int d = 0; d < 6; ++d) {
					//var dot = Vector3.Dot(Bloxels.faceDirVectors[d], n);
					var angle = Vector3.Angle(BloxelUtility.faceDirVectors[d], n);
					if (d == preferredDir) { angle -= addAngle; } // TODO
					if (angle < curAngle) { curAngle = angle; curDIdx = d; }
				}
				// TODO if (dir > 0 || rot > 0) { curDIdx = ((int)Bloxels.GetRotatedDir((Bloxels.FaceDir)curDIdx, dir, rot) + 2) % 6; }
				preferDirs.Add(curDIdx);
				var r = ClipperTools.GetVec2FromVec3Delegates[curDIdx](vertices[i] + offset);
				//uvArray[i] = new Vector2(v.x + 0.5f, v.y - 0.5f);
				//if (wrapped) { uvArray[i] = new Vector2(Mathf.Repeat(r.x + 0.5f, 1f), Mathf.Repeat(r.y + 0.5f, 1f)); }
				uvs.Add(new Vector2(r.x + 0.5f, r.y + 0.5f));
			}
			//this.dir = dir;
			//this.rot = rot;
		}

		// used by secret door
		// TODO call SetDirAndRot afterwards?
		public void GenerateUVs_BoxMapping(/*int dir, int rot, */Transform transform, int preferredDir = -1, float addAngle = 0f) {
			PrepareUVs();

			var uvArray = new Vector2[vertexCount];
			for (int i = 0; i < vertexCount; ++i) {
				var n = transform.TransformDirection(normals[i]);
				var curDIdx = 0;
				var curAngle = 180f;
				for (int d = 0; d < 6; ++d) {
					var angle = Vector3.Angle(BloxelUtility.faceDirVectors[d], n);
					if (d == preferredDir) { angle -= addAngle; } // TODO
					if (angle < curAngle) { curAngle = angle; curDIdx = d; }
				}
				preferDirs.Add(curDIdx);
			}

			// iterate over triangles instead of vertices only, because at position 1 a uv coord could be 0 or 1 -
			// have to check this by calculating the midpoint of the triangle
			int[] indices = new int[3];
			Vector3[] vertices = new Vector3[3];
			for (var iter_submeshes = triangles.GetEnumerator(); iter_submeshes.MoveNext();) {
				var tris = iter_submeshes.Current.indices;
				var triCount = tris.Count;
				for (int t = 0; t < triCount; t += 3) {
					indices[0] = tris[t + 0];
					indices[1] = tris[t + 1];
					indices[2] = tris[t + 2];
					vertices[0] = transform.TransformVector(this.vertices[indices[0]]);
					vertices[1] = transform.TransformVector(this.vertices[indices[1]]);
					vertices[2] = transform.TransformVector(this.vertices[indices[2]]);
					Vector3 v_m = (vertices[0] + vertices[1] + vertices[2]) * 0.3333333333f;
					//float a = Vector3.Distance(v_b, v_c), b = Vector3.Distance(v_a, v_c), c = Vector3.Distance(v_b, v_c);
					//Vector3 v_m = (v_a * a + v_b * b + v_c * c) / (a + b + c);

					var m = ClipperTools.GetVec2FromVec3Delegates[preferDirs[indices[0]]](v_m);
					for (int i = 0; i < 3; ++i) {
						var p = preferDirs[indices[i]];
						//var m = ClipperTools.GetVec2FromVec3Delegates[p](v_m);
						var r = ClipperTools.GetVec2FromVec3Delegates[p](vertices[i]);
						uvArray[indices[i]] = new Vector2(r.x - Mathf.Floor(m.x), r.y - Mathf.Floor(m.y));
					}
				}
			}
			uvs.AddRange(uvArray);
			//this.dir = dir;
			//this.rot = rot;
		}

		public void CreatePreferDirs() {
			// TODO: have to analyze triangles and get their directions.
		}

		public void SetDirAndRot(int dir, int rot) {
			this.dir = dir;
			this.rot = rot;
		}

		public void ApplyUVsFrom(BloxelMeshData from, Handling handling) {
			uvs = null;
			if (handling == Handling.DeepCopy) {
				if (uvs == null) { uvs = ListPool<Vector2>.Create(vertexCount); }
				else { uvs.Clear(); }
				if (preferDirs == null) { preferDirs = from.preferDirs != null ? ListPool<int>.Create(vertexCount) : null; }
				else if (from.preferDirs == null) { ListPool<int>.Dispose(ref preferDirs); }
				else { preferDirs.Clear(); }
				foreach (var u in from.uvs) { uvs.Add(u); }
				if (from.preferDirs != null) { foreach (var p in from.preferDirs) { preferDirs.Add(p); } }
			} else if (handling == Handling.Empty) {
				if (uvs == null) { uvs = ListPool<Vector2>.Create(vertexCount); }
				else { uvs.Clear(); }
				if (preferDirs == null) { preferDirs = ListPool<int>.Create(vertexCount); }
				else { preferDirs.Clear(); }
			} else {
				if (uvs != null) { ListPool<Vector2>.Dispose(ref uvs); }
				if (preferDirs != null) { ListPool<int>.Dispose(ref preferDirs); }
				uvs = null;
				preferDirs = null;
			}
		}

		void PrepareUVs() {
			dir = rot = 0;
			if (uvs == null) { uvs = ListPool<Vector2>.Create(vertexCount); } else { uvs.Clear(); }
			if (preferDirs == null) { preferDirs = ListPool<int>.Create(vertexCount); } else { preferDirs.Clear(); }
		}
	}

}