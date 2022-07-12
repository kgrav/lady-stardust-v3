using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RatKing.Bloxels;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEditor;
#endif

namespace RatKing {

	[ExecuteInEditMode]
	public class BloxelJoist : MonoBehaviour {
		[SerializeField] bool createOnAwake = true;
		[SerializeField] bool caps = false;
		[SerializeField] bool hasCollider = true;
		[SerializeField] bool texRotate = false;
		[SerializeField] float height = 1f;
		public float Height => height;
		[SerializeField] float width = 0.25f;
		[SerializeField] float length = 0.25f;
		[SerializeField] int texOffsetX = 0;
		[SerializeField] int texOffsetY = 0;
		[SerializeField] float noise = 0f;
		[SerializeField] int noiseSeed = 0;
		[SerializeField] BloxelTexture texture = null;
		public BloxelTexture Texture => texture;
		//
		[HideInInspector] public MeshFilter mf;
		[HideInInspector] public MeshRenderer mr;
		[HideInInspector] public MeshCollider mc;
		[HideInInspector] public BloxelLevel parent;

		//

		void Awake() {
			if (mf == null) { mf = gameObject.GetComponent<MeshFilter>(); }
			if (mr == null) { mr = gameObject.GetComponent<MeshRenderer>(); }
			if (mc == null) { mc = gameObject.GetComponent<MeshCollider>(); }
			if (Application.isPlaying && createOnAwake && (mf == null || mf.sharedMesh == null)) { UpdateMesh(); }
		}

#if UNITY_EDITOR
		void Update() {
			if (Application.isPlaying) { return; }
			if (parent != null) {
				parent.Joists.RemoveAll(bj => bj == null);
				if (!parent.Joists.Contains(this)) {
					parent.Joists.Add(this);
					Debug.Log("Joist was duplicated");
					if (mf != null && mf.sharedMesh != null) {
						if (parent.Joists.Exists(bj => bj != this && bj.mf != null && bj.mf.sharedMesh == mf.sharedMesh)) {
							MakeMeshUnique();
						}
					}
				}
			}
			CheckForMeshPrefabness();
		}

		public void CheckForMeshPrefabness() {
			if (!PrefabUtility.IsPartOfPrefabInstance(gameObject) && !gameObject.scene.path.EndsWith(".prefab") && PrefabStageUtility.GetCurrentPrefabStage() == null) {
				if (mf != null && mf.sharedMesh != null) {
					var path = AssetDatabase.GetAssetPath(mf.sharedMesh);
					if (!string.IsNullOrWhiteSpace(path)) {
						MakeMeshUnique();
					}
				}
			}
		}

		void OnDestroy() {
			if (!Application.isPlaying && PrefabStageUtility.GetCurrentPrefabStage() != null && mf != null && mf.sharedMesh != null) {
				var e = UnityEngine.Event.current;
				if (e != null && e.type == EventType.ExecuteCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete")) {
					if (parent != null) { parent.Joists.Remove(this); }
					AssetDatabase.RemoveObjectFromAsset(mf.sharedMesh);
				}
			}
		}
#endif

#if UNITY_EDITOR
		void MakeMeshUnique() {
			if (mf == null || mf.sharedMesh == null) { return; }
			Debug.Log("Making meshes of Joist " + name + " unique...");

			mf.sharedMesh = Instantiate(mf.sharedMesh);
			if (mc != null) { mc.sharedMesh = mf.sharedMesh; }
			if (PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
				var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (prefabStage != null) {
#if UNITY_2020_2_OR_NEWER
					var path = prefabStage.assetPath;
#else
					var path = prefabStage.prefabAssetPath;
#endif
					AssetDatabase.AddObjectToAsset(mf.sharedMesh, path); // PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject));
					Debug.Log("path " + path + " ... " + transform.parent);
					PrefabUtility.SaveAsPrefabAsset(transform.parent.root.gameObject, path);
				}
			}
		}
#endif
		
		public void SetParent(BloxelLevel parent) {
			if (this.parent == parent) { return; }
			if (this.parent != null) {
				this.parent.Joists.Remove(this);
			}
			if (parent != null) {
				parent.Joists.Add(this);
				transform.SetParent(parent.transform);
			}
			else {
				transform.SetParent(null);
			}
			this.parent = parent;
		}

		//

		public void SetCaps(bool caps, bool instantUpdate = true) {
			if (this.caps == caps) { return; }
			this.caps = caps;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetCollider(bool hasCollider) {
			if (this.hasCollider == hasCollider) { return; }
			if (this.hasCollider) { DestroyImmediate(mc); }
			else { mc = gameObject.GetOrAddComponent<MeshCollider>(); }
			this.hasCollider = hasCollider;
		}

		public void SetTexRotate(bool texRotate, bool instantUpdate = true) {
			if (this.texRotate == texRotate) { return; }
			this.texRotate = texRotate;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetHeight(float height, bool instantUpdate = true) {
			if (this.height == height) { return; }
			this.height = height;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetSize(float width, float length, bool instantUpdate = true) {
			if (this.width == width && this.length == length) { return; }
			this.width = width;
			this.length = length;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetTexOffset(int x, int y, bool instantUpdate = true) {
			if (this.texOffsetX == x && this.texOffsetY == y) { return; }
			this.texOffsetX = x;
			this.texOffsetY = y;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetNoise(float noise, int seed, bool instantUpdate = true) {
			if (this.noise == noise && this.noiseSeed == seed) { return; }
			this.noise = noise;
			this.noiseSeed = seed;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetNoiseSeed(int seed, bool instantUpdate = true) {
			if (this.noiseSeed == seed) { return; }
			this.noiseSeed = seed;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void SetTexture(BloxelTexture texture, bool instantUpdate = true) {
			if (this.texture == texture) { return; }
			this.texture = texture;
			if (instantUpdate) { UpdateMesh(); }
		}

		public void Place(bool ceiling = false) {
			float sign = ceiling ? 1f : -1f;
			float offsetFactor = ceiling ? 1f : 0f;
			var up = transform.up * sign;
			var offset = offsetFactor * height;
			var origin = transform.position + up * (offset - 0.1f) * transform.lossyScale.y;
			var ray = new Ray(origin, up);
			if (BloxelUtility.Raycast(ray, out var hit, 100f, RatKing.Bloxels.BloxelUtility.CurLevelSettings.ChunkLayerMask | LayerMask.GetMask("Default"))) {
				transform.position = hit.point - (up * offset) * transform.lossyScale.y;
			}
		}

		public bool Fit(bool floor = false) {
			float sign = floor ? -1f : 1f;
			float factor = floor ? 0f : 1f;
			var origin = transform.position + transform.up * (height * factor - sign * 0.1f) * transform.lossyScale.y;
			var ray = new Ray(origin, sign * transform.up);
			if (BloxelUtility.Raycast(ray, out var hit, 100f, RatKing.Bloxels.BloxelUtility.CurLevelSettings.ChunkLayerMask | LayerMask.GetMask("Default"))) {
				transform.position = Vector3.Lerp(hit.point, transform.position, factor);
				height += hit.distance / transform.lossyScale.y - 0.1f; // * transform.lossyScale.y;
				return true;
			}
			return false;
		}

		Vector3 NoiseVec(float x, float y, float z) {
			var v = new Vector3(x, y, z);
			if (noise <= 0f) { return v; }
			var r = new Vector3(
				Base.Randomness.SimplexNoise.NoiseMinusPlus1(noiseSeed + 123.5234f + x, 0.1f - y, -1.2112f + z) * noise,
				Base.Randomness.SimplexNoise.NoiseMinusPlus1(-x + 4231f, -y - noiseSeed + 23512.33f, 0.11f - z) * 0.5f * noise,
				Base.Randomness.SimplexNoise.NoiseMinusPlus1(x - 745423f, y + 12f, z + noiseSeed + 67642.3421f) * noise);
			return v + r;
		}

		public void RemoveMeshImmediate() {
			if (gameObject.TryGetComponent<MeshFilter>(out var mf)) {
#if UNITY_EDITOR
				if (PrefabStageUtility.GetCurrentPrefabStage() != null) { AssetDatabase.RemoveObjectFromAsset(mf.sharedMesh); }
#endif
				DestroyImmediate(mf.sharedMesh);
				DestroyImmediate(mf);
			}
			if (gameObject.TryGetComponent<MeshRenderer>(out var mr)) {
				DestroyImmediate(mr);
			}
			if (hasCollider && gameObject.TryGetComponent<MeshCollider>(out var mc)) {
				DestroyImmediate(mc);
			}
		}

		public void UpdateMesh() {
			if (mf == null) { mf = gameObject.GetOrAddComponent<MeshFilter>(); }
			if (mr == null) { mr = gameObject.GetOrAddComponent<MeshRenderer>(); }
			if (hasCollider && mc == null) { mc = gameObject.GetOrAddComponent<MeshCollider>(); }
			
			//Debug.Log("Update Mesh " + Time.frameCount);
			width = Mathf.Clamp01(width);
			length = Mathf.Clamp01(length);
			texOffsetX = Mathf.Max(0, texOffsetX);
			texOffsetY = Mathf.Max(0, texOffsetY);
			if (height <= 0.01f) { height = 0.01f; }
			if (texture == null) { texture = BloxelUtility.ProjectSettings.MissingTexture; }

			var mat = mr.material = BloxelUtility.ProjectSettings.TexAtlases[texture.texAtlasIdx].Material;

			//  build mesh
			var mesh = mf.sharedMesh;
			if (mesh == null) {
				mf.sharedMesh = mesh = new Mesh();
				mesh.name = "Joist " + GetInstanceID().ToString();
#if UNITY_EDITOR
				var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (prefabStage != null) {
#if UNITY_2020_2_OR_NEWER
					var path = prefabStage.assetPath;
#else
					var path = prefabStage.prefabAssetPath;
#endif
					AssetDatabase.AddObjectToAsset(mesh, path);
					PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, path);
				}
#endif
			}
			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			List<int> triangles = new List<int>();

			var countX = texRotate ? texture.countY : texture.countX;
			var countY = texRotate ? texture.countX : texture.countY;

			var rect = texture.rect;
			var center = rect.center;
			if (texRotate) {
				// change the rect inside the atlas, the individual uvs get rotated back later
				var texRatio = mat.mainTexture.width / (float)mat.mainTexture.height;
				var texRatioInv = 1f / texRatio;
				var pos = rect.position;
				float temp;
				pos -= center; pos.x *= texRatio;
				temp = pos.x; pos.x = pos.y; pos.y = temp;
				pos.x *= texRatioInv;
				temp = rect.width; rect.width = rect.height; rect.height = temp;
				rect.position = pos;
				rect.center = center;
			}

			float tw = rect.width / countX;
			float th = rect.height / countY;

			int tox1 = texOffsetX % countX; // tex u offset for z axis sides (forward/backward)
			int tox2 = (texOffsetX / countX) % countX; // tex u offset for x axis sides (left/right)
				
			float u_z_0 = (1f - width) * 0.5f * tw + tox1 * tw; // start u tex coord for z axis
			float u_z_1 = u_z_0 + width * tw;

			float u_x_0 = (1f - length) * 0.5f * tw + tox2 * tw; // start u tex coord for x axis
			float u_x_1 = u_x_0 + length * tw;

			int v_z = texOffsetY % countY; // v tex coord in z axis
			int v_x = (texOffsetY / countY) % countY; // v tex coord in x axis

			float w = width * 0.5f, l = length * 0.5f;
			int count = 0;
			//if (height - Mathf.Floor(height) < 0.01f) { height = Mathf.Floor(height); }
			var targetH = Mathf.FloorToInt(height);
			if (height - targetH <= noise) { targetH--; }
			for (int h0 = 0; h0 <= targetH; ++h0) {
				bool isFirst = count == 0;
				bool isLast = h0 == targetH;
				float yFactor = isLast ? (height - h0) : 1f;
				float h1 = h0 + yFactor;

				var vec_f_0 = NoiseVec(w, h0, l); // front side
				var vec_f_1 = NoiseVec(w, h1, l);
				var vec_f_2 = NoiseVec(-w, h1, l);
				var vec_f_3 = NoiseVec(-w, h0, l);
				var vec_r_0 = NoiseVec(w, h0, -l); // right side
				var vec_r_1 = NoiseVec(w, h1, -l);
				var vec_b_0 = NoiseVec(-w, h0, -l); // back side
				var vec_b_1 = NoiseVec(-w, h1, -l);
				if (isFirst) { vec_f_0.y = vec_f_3.y = vec_r_0.y = vec_b_0.y = h0; }
				if (isLast) { vec_f_1.y = vec_f_2.y = vec_r_1.y = vec_b_1.y = h1; }

				var uv_z_00 = new Vector2(rect.x + u_z_0, rect.y + (v_z * th));
				var uv_z_11 = new Vector2(rect.x + u_z_1, rect.y + (v_z + Mathf.Clamp01(yFactor)) * th);
				var uv_z_01 = new Vector2(uv_z_00.x, uv_z_11.y);
				var uv_z_10 = new Vector2(uv_z_11.x, uv_z_00.y);
					
				var uv_x_00 = new Vector2(rect.x + u_x_0, rect.y + (v_x * th));
				var uv_x_11 = new Vector2(rect.x + u_x_1, rect.y + (v_x + Mathf.Clamp01(yFactor)) * th);
				var uv_x_01 = new Vector2(uv_x_00.x, uv_x_11.y);
				var uv_x_10 = new Vector2(uv_x_11.x, uv_x_00.y);

				v_z = (v_z + 1) % countY;
				v_x = (v_x + 1) % countY;

				// front side
				vertices.AddRange(new[] { vec_f_0, vec_f_1, vec_f_2, vec_f_3 });
				normals.AddRange(new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward });
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;

				// right side
				vertices.AddRange(new[] { vec_r_0, vec_r_1, vec_f_1, vec_f_0 });
				normals.AddRange(new[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right });
				uvs.AddRange(new[] { uv_x_00, uv_x_01, uv_x_11, uv_x_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;

				// back side
				vertices.AddRange(new[] { vec_b_0, vec_b_1, vec_r_1, vec_r_0 });
				normals.AddRange(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;

				// left side
				vertices.AddRange(new[] { vec_f_3, vec_f_2, vec_b_1, vec_b_0 });
				normals.AddRange(new[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left });
				uvs.AddRange(new[] { uv_x_00, uv_x_01, uv_x_11, uv_x_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;
			}

			if (caps) {
				var h = height;
				var tox3 = texRotate ? ((texOffsetX / countY) % countY) : tox2;

				float u_y_0 = (1f - length) * 0.5f * th + tox3 * th; // start u tex coord for y axis (top/bottom) side
				float u_y_1 = u_y_0 + length * th;

				var uv_z_00 = new Vector2(rect.x + u_z_0, rect.y + u_y_0);
				var uv_z_11 = new Vector2(rect.x + u_z_1, rect.y + u_y_1);
				var uv_z_01 = new Vector2(uv_z_00.x, uv_z_11.y);
				var uv_z_10 = new Vector2(uv_z_11.x, uv_z_00.y);

				// top
				vertices.AddRange(new[] { NoiseVec(-w, h, -l).WithY(h), NoiseVec(-w, h, l).WithY(h), NoiseVec(w, h, l).WithY(h), NoiseVec(w, h, -l).WithY(h) });
				normals.AddRange(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;

				// bottom
				vertices.AddRange(new[] { NoiseVec(-w, 0f, l).WithY(0f), NoiseVec(-w, 0f, -l).WithY(0f), NoiseVec(w, 0f, -l).WithY(0f), NoiseVec(w, 0f, l).WithY(0f) });
				normals.AddRange(new[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down });
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 });
				triangles.AddRange(new[] { count + 0, count + 1, count + 2, count + 2, count + 3, count + 0 }); count += 4;
			}

			if (texRotate) {
				for (int i = 0; i < count; ++i) {
					var uv = uvs[i]; uv -= center;
					var temp = uv.x; uv.x = uv.y; uv.y = -temp;
					uv += center; uvs[i] = uv;
				}
			}

			mesh.Clear();
			if (vertices.Count > 0) {
				mesh.SetVertices(vertices);
				mesh.SetNormals(normals);
				mesh.SetUVs(0, uvs);
				mesh.SetTriangles(triangles, 0);
				if (hasCollider) { mc.sharedMesh = mesh; }
			}
			mesh.RecalculateBounds();
		}

		public void UpdateUVsOnly() {
			if (mf == null) { return; }
			
			//Debug.Log("Update Mesh " + Time.frameCount);
			width = Mathf.Clamp01(width);
			length = Mathf.Clamp01(length);
			texOffsetX = Mathf.Max(0, texOffsetX);
			texOffsetY = Mathf.Max(0, texOffsetY);
			if (height <= 0.01f) { height = 0.01f; }
			if (texture == null) { return; }

			var mat = GetComponent<Renderer>().material = BloxelUtility.ProjectSettings.TexAtlases[texture.texAtlasIdx].Material;

			//  build mesh
			var mesh = mf.sharedMesh;
			if (mesh == null) { return; }

			List<Vector2> uvs = new List<Vector2>();

			var countX = texRotate ? texture.countY : texture.countX;
			var countY = texRotate ? texture.countX : texture.countY;

			var rect = texture.rect;
			var center = rect.center;
			if (texRotate) {
				// change the rect inside the atlas, the individual uvs get rotated back later
				var texRatio = mat.mainTexture.width / (float)mat.mainTexture.height;
				var texRatioInv = 1f / texRatio;
				var pos = rect.position;
				float temp;
				pos -= center; pos.x *= texRatio;
				temp = pos.x; pos.x = pos.y; pos.y = temp;
				pos.x *= texRatioInv;
				temp = rect.width; rect.width = rect.height; rect.height = temp;
				rect.position = pos;
				rect.center = center;
			}

			float tw = rect.width / countX;
			float th = rect.height / countY;

			int tox1 = texOffsetX % countX; // tex u offset for z axis sides (forward/backward)
			int tox2 = (texOffsetX / countX) % countX; // tex u offset for x axis sides (left/right)
				
			float u_z_0 = (1f - width) * 0.5f * tw + tox1 * tw; // start u tex coord for z axis
			float u_z_1 = u_z_0 + width * tw;

			float u_x_0 = (1f - length) * 0.5f * tw + tox2 * tw; // start u tex coord for x axis
			float u_x_1 = u_x_0 + length * tw;

			int v_z = texOffsetY % countY; // v tex coord in z axis
			int v_x = (texOffsetY / countY) % countY; // v tex coord in x axis

			int count = 0;
			float targetH = Mathf.Floor(height);
			for (float h0 = 0; h0 <= targetH; h0 += 1f) {
				var isLast = h0 + 1f >= height;
				float yFactor = isLast ? (height - h0) : 1f;
				if (yFactor < 0.01f) { break; }

				var uv_z_00 = new Vector2(rect.x + u_z_0, rect.y + (v_z * th));
				var uv_z_11 = new Vector2(rect.x + u_z_1, rect.y + (v_z + yFactor) * th);
				var uv_z_01 = new Vector2(uv_z_00.x, uv_z_11.y);
				var uv_z_10 = new Vector2(uv_z_11.x, uv_z_00.y);
					
				var uv_x_00 = new Vector2(rect.x + u_x_0, rect.y + (v_x * th));
				var uv_x_11 = new Vector2(rect.x + u_x_1, rect.y + (v_x + yFactor) * th);
				var uv_x_01 = new Vector2(uv_x_00.x, uv_x_11.y);
				var uv_x_10 = new Vector2(uv_x_11.x, uv_x_00.y);

				v_z = (v_z + 1) % countY;
				v_x = (v_x + 1) % countY;

				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 }); count += 4; // front side
				uvs.AddRange(new[] { uv_x_00, uv_x_01, uv_x_11, uv_x_10 }); count += 4; // right side
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 }); count += 4; // back side
				uvs.AddRange(new[] { uv_x_00, uv_x_01, uv_x_11, uv_x_10 }); count += 4; // left side
			}

			if (caps) {
				var tox3 = texRotate ? ((texOffsetX / countY) % countY) : tox2;

				float u_y_0 = (1f - length) * 0.5f * th + tox3 * th; // start u tex coord for y axis (top/bottom) side
				float u_y_1 = u_y_0 + length * th;

				var uv_z_00 = new Vector2(rect.x + u_z_0, rect.y + u_y_0);
				var uv_z_11 = new Vector2(rect.x + u_z_1, rect.y + u_y_1);
				var uv_z_01 = new Vector2(uv_z_00.x, uv_z_11.y);
				var uv_z_10 = new Vector2(uv_z_11.x, uv_z_00.y);
				
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 }); count += 4; // top
				uvs.AddRange(new[] { uv_z_00, uv_z_01, uv_z_11, uv_z_10 }); count += 4; // bottom
			}

			if (uvs.Count == 0) { return; }
			if (uvs.Count != mesh.vertexCount) {
				UpdateMesh();
				return;
			}

			if (texRotate) {
				for (int i = 0; i < count; ++i) {
					var uv = uvs[i]; uv -= center;
					var temp = uv.x; uv.x = uv.y; uv.y = -temp;
					uv += center; uvs[i] = uv;
				}
			}

			mesh.SetUVs(0, uvs);
			if (mc != null) { mc.sharedMesh = mesh; }
		}

#if UNITY_EDITOR
		public void CreateSecondaryUVs() {
			if (mf != null && mf.sharedMesh != null) {
				UnityEditor.Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
			}
		}
#endif
	}

}