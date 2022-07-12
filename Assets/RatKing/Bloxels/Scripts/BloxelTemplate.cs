using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using UnityEngine;
using ClipperPath = System.Collections.Generic.List<ClipperLib.IntPoint>;
using ClipperPaths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace RatKing.Bloxels {

	public class BloxelTemplate : ScriptableObject {
		// TODO these were properties, but those can't be serialized, so they're normal public. make them hidden via SerializeField?
		public string ID; // ID of type, if existing
		public string UID; // same as name of template, but needs to be saved here for serialization purposes
		public bool IsAir = false;
		public int Dir;
		public int Rot;
		public Mesh mesh;
		public BloxelType Type;
		public BloxelTemplate ColliderTemplate;
		//
		[SerializeField] int sideIsEmpty;
		[SerializeField] int sideIsFullWall;
		[SerializeField] BloxelMeshData[] innerDatas;
		[SerializeField] BloxelMeshData[] sidesDataFull = new BloxelMeshData[6];
		[SerializeField] StringBMDArrayDictionary sidesDataClipped; // amount of templates * 6
		//
		ClipperPaths[] sidesClipPath = new ClipperPaths[6]; // TODO not serialized - not necessary
		bool prepared; // TODO not serialized - not necessary

		//

		public void SetColliderTemplate(BloxelTemplate template) {
			if (template == null) { template = this; }
			ColliderTemplate = template;
		}

		public virtual void Init(string UID, Mesh mesh = null) {
			this.name = this.UID = UID;
			ID = UID.ToUpper();
			if (mesh == null) {
				IsAir = true;
				sideIsEmpty = 63;
				prepared = true;
			} else {
				IsAir = false;
				PrepareMesh(mesh);
			}
			this.mesh = mesh;
			ColliderTemplate = this;
		}

		public virtual void Init(string UID, BloxelType bt, int dir, int rot) {
			this.name = this.UID = UID;
			Type = bt;
			this.Dir = dir;
			this.Rot = rot;
			this.mesh = bt.mesh;
			//innerDirRotate = vt.innerDirRotate;
			//preferredInnerDir = vt.hasPreferredInnerDir ? (innerDirRotate ? Bloxels.GetRotatedDir(vt.preferredInnerDir, dir, rot) : vt.preferredInnerDir) : FaceDir.Top;
			//preferredInnerDirAddAngle = vt.hasPreferredInnerDir ? vt.preferredInnerDirAddAngle : -1f;
			//preferredInnerDir = innerDirHandling.hasPreferredDir ? (innerDirHandling.dirRotate ? Bloxels.GetRotatedDir(innerDirHandling.preferredDir, dir, rot) : innerDirHandling.preferredDir) : FaceDir.Top;
			//preferredInnerDirAddAngle = innerDirHandling.hasPreferredDir ? innerDirHandling.preferredDirAddAngle : -1f;
			// Debug.Log(name + " " + vt.preferredInnerDir + " -> " + dir + " " + rot + " -> " + preferredInnerDir);
			ID = bt.ID;
			PrepareMesh(BloxelUtility.CreateRotatedMesh(bt.mesh, dir, rot));
			ColliderTemplate = this;
			bt.Templates.Add(this);
		}

		/// <summary>
		/// Prepare the bloxel mesh - its sides and inner mesh -
		/// so it can be used for clipping with other bloxels
		/// </summary>
		void PrepareMesh(Mesh mesh) {
			for (int i = 0; i < 6; ++i) {
				sidesClipPath[i] = ClipperTools.CreateBloxelFace(mesh, (BloxelUtility.FaceDir) i);
				if (sidesClipPath[i] == null || sidesClipPath[i].Count == 0) {
					sideIsEmpty |= (1 << i);
				} else if (ClipperTools.IsFullBloxelWall(sidesClipPath[i])) {
					sideIsFullWall |= (1 << i);
				}
			}

			var bmd = new BloxelMeshData(mesh, true);
			var innerTris = bmd.GetTriangles(0); // <- this is a reference!

			// get the mesh data for the inner part and the sides
			for (int i3 = innerTris.Count - 3; i3 >= 0; i3 -= 3) {
				for (int d = 0; d < 6; ++d) {
					var dir = BloxelUtility.faceDirVectors[d];
					if (ClipperTools.Vec3TimesVec3(bmd.vertices[innerTris[i3]], dir) > 0.499f &&
						ClipperTools.Vec3TimesVec3(bmd.vertices[innerTris[i3 + 1]], dir) > 0.499f &&
						ClipperTools.Vec3TimesVec3(bmd.vertices[innerTris[i3 + 2]], dir) > 0.499f) {
						if (sidesDataFull[d] == null) {
							// add a side if necessary
							sidesDataFull[d] = new BloxelMeshData(bmd, BloxelMeshData.Handling.Empty, BloxelMeshData.Handling.DeepCopy);
						}
						sidesDataFull[d].AddTriangle(0, innerTris[i3], innerTris[i3 + 1], innerTris[i3 + 2]);
						// remove the triangle (ie. the indices) from the inner data
						innerTris.RemoveRange(i3, 3);
						break;
					}
				}
			}
#if DEBUG_LOG
			string logText = "";
#endif
			// no inner data left?
			if (innerTris.Count == 0) {
#if DEBUG_LOG
				logText += " (no inner data for this bloxel)";
#endif
				//innerData.Dispose();
				//innerData = TempMeshDataEmpty.GetInstance();
			}
			else {
				// this will remove all vertices not needed anymore
				bmd.ReduceData();
				// make new innerdatas
				var count = Type.innerDirHandlings.Length;
				innerDatas = new BloxelMeshData[count];
				for (int i = 0; i < count; ++i) {
					var id = innerDatas[i] = new BloxelMeshData(bmd, BloxelMeshData.Handling.DeepCopy, BloxelMeshData.Handling.Empty);
					// give uvs to the inner data if necessary
					var idh = Type.innerDirHandlings[i];
					if (idh == null) { Debug.LogWarning("error w/ " + i + " of " + ID); Type.innerDirHandlings[i] = idh = new BloxelType.InnerDirHandling(); }
					if (idh.type == BloxelType.InnerDirHandling.Type.None) {
						// do nothing
					}
					else { // if (idh.type == BloxelType.InnerDirHandling.Type.BoxMap) {
						if (!idh.dirRotate || (Dir == 0 && Rot == 0)) {
							if (idh.preferredDirAddAngle > 0f) {
								//var rotDir = BloxelUtility.GetRotatedDir(idh.preferredDir, Dir, Rot);
								//id.GenerateUVs_BoxMapping_Centered(0, 0, (int)rotDir, idh.preferredDirAddAngle);
								id.GenerateUVs_BoxMapping_Centered((int)idh.preferredDir, idh.preferredDirAddAngle);
							}
							else {
								id.GenerateUVs_BoxMapping_Centered();
							}
						}
						else { // if (Type != null && Type.AllTemplates.Count > 0) {
							   //	id.GenerateUVs_BoxMapping_Centered(Dir, Rot);
							id.ApplyUVsFrom(Type.Templates[0].innerDatas[i], BloxelMeshData.Handling.DeepCopy);
							id.SetDirAndRot(Dir, Rot);
						}
					}
					
					if (idh.type == BloxelType.InnerDirHandling.Type.KeepUVs) {
						id.uvs = ListPool<Vector2>.Create(bmd.uvs);
						var pd = id.preferDirs[0];
						for (int j = 0; j < id.preferDirs.Count; ++j) { id.preferDirs[j] = pd; }
						//foreach (var u in bmd.uvs) { id.uvs.Add(u); }
						//id.preferDirs = ListPool<int>.Create(bmd.uvs.Count);
						//foreach (var u in bmd.uvs) { id.preferDirs.Add(1); }
						//innerData.CreatePreferDirs();
					}
					//Debug.Log(ID + ": id:" + i + " d:" + Dir + " r: " + Rot + " vc:" + innerDatas[i].VertexCount + " uvc:" + innerDatas[i].UVs.Count + " " + idh.type);
					//Debug.Log(ID + "|" + Dir + "|" + Rot + ": " + i + ") " + idh.dirRotate + "? " + (id.PreferDirs != null ? id.PreferDirs.List.Count : -1));
				}
			}
			for (int d = 0; d < 6; ++d) {
				if (sidesDataFull[d] != null) {
					//Debug.Log("reducing " + Name + " ... " + d);
					sidesDataFull[d].ReduceData();
					sidesDataFull[d].GenerateUVs_PlanarMapping_Centered(d);
				}
				if (sidesDataFull[d] == null || sidesDataFull[d].vertexCount == 0) {
					if (sidesDataFull[d] != null) { Debug.Log("full side data reduced to 0!"); }
					sidesDataFull[d] = null;
				}
			}
			prepared = true;
#if DEBUG_LOG
			Debug.Log("Prepared bloxel template with mesh " + mesh.name + logText);
#endif
		}

		//

		public virtual void Build(BloxelMeshData bmd, int index, int textureIndex, BloxelChunk chunk, ref int vertexCount, Bloxel.BuildMode buildMode) {
			var texes = BloxelUtility.ProjectSettings.TexturesByID;
			var tex = texes[chunk.lvl.TextureUIDs[textureIndex]];
			if (buildMode == Bloxel.BuildMode.CollisionGame && !tex.generateCollider) {
				return;
			}

			var tempBloxels = BloxelLevel.tempBloxels;
			var pos = BloxelLevel.tempBloxelPosRel.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);

			// add inner data in any case
			if (HasInnerData()) {
				var hasData = chunk.textureSideExtraDataByRelPos.TryGetValue(index * 7 + 6, out var sideData);
	//			var tex = texes[hasData && sideData.ti >= 0 ? sideData.ti : textureIndex];
				var rot = (hasData && sideData.r > 0) ? sideData.r : 0;
				var offX = (hasData && sideData.o > 0) ? (sideData.o & 31) : 0;
				var offY = (hasData && sideData.o > 0) ? (sideData.o >> 5) : 0;
				var uvset = (hasData && bmd.uvs != null) && sideData.uv > 0 ? sideData.uv : 0;
				if (uvset >= innerDatas.Length) { /* Debug.Log("<color=yellow>" + uvset + " >= " + innerDatas.Length + " of " + ID + "!</color> (" + rot + " " + sideData.ti + " " + (index * 7 + 6) + ")"); */ uvset = 0; }
				var texR = tex.GetComposite(6);
				innerDatas[uvset].AddTo(bmd, texR, hasData && sideData.ti >= 0 ? texes[chunk.lvl.TextureUIDs[sideData.ti]].GetComposite(6) : texR, rot, offX, offY, chunk, pos, ref vertexCount, buildMode);
			}

			// add sides
			//if (sidesDataClipped != null && sidesDataClipped.Length > 0) {
			for (int dir = 0, tsIndex = index * 7; dir < 6; ++dir, ++tsIndex) {
				var texR = tex.GetComposite(dir);
				var bn = tempBloxels[index + chunk.lvl.neighbourDirAdd[dir]];
				//if (dirs == null || dirs.Length == 0) { continue; }
				//if (vn == null || vn.template == null) { Debug.Log(pos + " " + dir + " " + tsIndex + " " + index + " ... "); continue; }
				var hasData = chunk.textureSideExtraDataByRelPos.TryGetValue(index * 7 + dir, out var sideData);
				//var tex = texes[hasData && sideData.ti >= 0 ? sideData.ti : textureIndex];
				var bnTex = texes[chunk.lvl.TextureUIDs[bn.textureIdx]];
				var clipNotAir = (bnTex.generateCollider && buildMode != Bloxel.BuildMode.Render) || (texR.neighbourFlags & bnTex.flags) != 0;
				BloxelMeshData clipped = clipNotAir ? sidesDataClipped[(buildMode == Bloxel.BuildMode.CollisionGame ? bn.template.ColliderTemplate : bn.template).UID][dir]
													: sidesDataClipped["AIR"][dir];
				var rot = (hasData && sideData.r > 0) ? sideData.r : 0;
				var offX = (hasData && sideData.o > 0) ? (sideData.o & 31) : 0;
				var offY = (hasData && sideData.o > 0) ? (sideData.o >> 5) : 0;
				// var uvset ... different uvsets not used for sides
				clipped?.AddTo(bmd, texR, hasData && sideData.ti >= 0 ? texes[chunk.lvl.TextureUIDs[sideData.ti]].GetComposite(dir) : texR, rot, offX, offY, dir, chunk, pos, ref vertexCount, buildMode);
				//clipped?.AddTo(bmd, tex, rot, offX, offY, dir, chunk, pos, ref vertexCount, forCollider);
			}
			//}
		}

		public virtual void ChangeUVs(Vector2[] uvs, int index, int textureIndex, BloxelChunk chunk, ref int vertexCount) {
			var texes = BloxelUtility.ProjectSettings.TexturesByID;
			var tempBloxels = BloxelLevel.tempBloxels;
			var pos = BloxelLevel.tempBloxelPosRel.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);
			SideExtraData sideData;

			// add inner data in any case
			if (HasInnerData()) {
				//innerData.ChangeUVsOf(uvs, index, textureIndex, bloxels.BloxelTextures, chunk, pos, ref vertexCount);
				//public virtual void ChangeUVsOf(Vector2[] meshUVs, int index, int textureIndex, List<BloxelTexture> textures, Chunk chunk, Vector3 pos, ref int vertexCount) {
				var hasData = chunk.textureSideExtraDataByRelPos.TryGetValue(index * 7 + 6, out sideData);
				var tex = texes[chunk.lvl.TextureUIDs[hasData && sideData.ti >= 0 ? sideData.ti : textureIndex]].GetComposite(6);
				var rot = (hasData && sideData.r > 0) ? sideData.r : 0;
				var offX = (hasData && sideData.o > 0) ? (sideData.o & 31) : 0;
				var offY = (hasData && sideData.o > 0) ? (sideData.o >> 5) : 0;
				var uvset = (hasData && sideData.uv > 0) ? sideData.uv : 0;
				if (uvset >= innerDatas.Length) { Debug.Log(ID + " " + UID + " <color=yellow>" + uvset + " >= " + innerDatas.Length + " of " + ID + "!</color> (" + rot + " " + sideData.ti + " " + (index * 7 + 6) + ")"); uvset = 0; }
				innerDatas[uvset].ChangeUVsOf(uvs, tex, rot, offX, offY, chunk.WorldPos + pos, ref vertexCount);
			}

			// add sides
			for (int dir = 0, tsIndex = index * 7; dir < 6; ++dir, ++tsIndex) {
				var bn = tempBloxels[index + chunk.lvl.neighbourDirAdd[dir]];
				//sidesDataClipped[vn.template.Index][dir].ChangeUVsOf(uvs, index, textureIndex, bloxels.BloxelTextures, dir, chunk, pos, ref vertexCount);
				//public virtual void ChangeUVsOf(Vector2[] meshUVs, int index, int textureIndex, List<BloxelTexture> textures, int dir, Chunk chunk, Vector3 pos, ref int vertexCount) {
				var hasData = chunk.textureSideExtraDataByRelPos.TryGetValue(index * 7 + dir, out sideData);
				var tex = texes[chunk.lvl.TextureUIDs[hasData && sideData.ti >= 0 ? sideData.ti : textureIndex]].GetComposite(dir);
				var rot = (hasData && sideData.r > 0) ? sideData.r : 0;
				var offX = (hasData && sideData.o > 0) ? (sideData.o & 31) : 0;
				var offY = (hasData && sideData.o > 0) ? (sideData.o >> 5) : 0;
				// var uvset ... different uvsets not used for sides
				sidesDataClipped[bn.template.UID][dir].ChangeUVsOf(uvs, tex, rot, offX, offY, dir, chunk.WorldPos + pos, ref vertexCount);
			}
		}

		//

		public virtual bool HasInnerData() {
			return innerDatas != null && innerDatas.Length > 0;
		}

		public virtual bool HasFullWallSide(int dir) {
			return (sideIsFullWall & (1 << dir)) != 0;
		}

		public virtual bool HasAirSide(int dir) {
			return (sideIsEmpty & (1 << dir)) != 0;
		}

		public virtual bool IsEmptyTo(BloxelTemplate other, int dir) {
			//return sidesDataClipped == null || sidesDataClipped[templateIndex][dir] == null; // || sidesDataClipped[templateIndex][dir].vertexCount == 0;
			//return /*sidesDataClipped == null || sidesDataClipped[templateIndex][dir] == null ||*/ sidesDataClipped[templateIndex].dirs[dir].vertexCount == 0;
			var bmd = sidesDataClipped[other.UID][dir];
			return bmd == null || bmd.vertexCount == 0;
		}

		//

		public virtual void InitSidesDataClipped(int templateCount) {
			sidesDataClipped = new StringBMDArrayDictionary(templateCount);
			// for (int i = 0; i < templateCount; ++i) { sidesDataClipped[i] = new ClippedSideData(); }
		}

		public virtual void CreateSideBySideData(BloxelTemplate other) {
			if (!prepared || !other.prepared || sidesDataClipped == null) {
				Debug.LogError("Trying to create side by side data for (" + ID + "_" + Dir + "_" + Rot + ") without being prepared!");
				return;
			}
			sidesDataClipped[other.UID] = new BloxelMeshData[6];
			for (int dir = 0; dir < 6; ++dir) {
				CreateSideData(dir, other);
			}
		}

		public bool HasClippedInnerVertices(BloxelTemplate other, int direction) {
			if (other == null || sidesDataClipped == null || sidesDataClipped.Count == 0) { return false; }
			if (sidesDataFull[direction] == null || sidesDataFull[direction].vertexCount == 0) { return false; }
			if (!sidesDataClipped.TryGetValue(other.UID, out var clippedData)) { return false; }
			var data = clippedData[direction];
			if (data == null || data.vertexCount == 0) { return false; }
			return data.sideHasInnerClippedVertices;
		}

		/// <summary>
		/// Create the temp mesh data for one bloxel side
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="other"></param>
		void CreateSideData(int direction, BloxelTemplate other) {
			int oppositeDirection = (direction + 3) % 6;

			if (HasAirSide(direction) || (!other.IsAir && other.HasFullWallSide(oppositeDirection))) {
				// special case 1: other side is full wall or own side is air -> nada
				sidesDataClipped[other.UID][direction] = null; // TODO is this making sense?
			} else if (other.IsAir || other.HasAirSide(oppositeDirection)) {
				// special case 2: other side is air -> full side
				sidesDataClipped[other.UID][direction] = sidesDataFull[direction];
			} else {
				// no special case -> do the clipping, save the result
				if (ClipperTools.ClipTwoPolys(sidesClipPath[direction], other.sidesClipPath[oppositeDirection], out var resTris, out var resPoints)) {
					var count = resPoints.Count;
					var tmd = new BloxelMeshData(count, true, 1); // ScriptableObject.CreateInstance<BloxelMeshData>().Init(count, true, 1);
					// tmd.ResetTriangles();
					for (int i = 0; i < count; ++i) {
						tmd.AddSimpleData(ClipperTools.GetVec3FromVec2DelegatesCentered[direction](resPoints[i]), BloxelUtility.faceDirVectors[direction]);
						// tmd.uv.Add(vec2D0505 + resPoints[i]); // TODO ?
					}
					var tris = tmd.GetTriangles(0);
					// TODO: optimize, correcting
					if (direction < 3) {
						// "negative"
						for (int i = 0; i < resTris.Count; i += 3) {
							tris.Add(resTris[i + 0]);
							tris.Add(resTris[i + 2]);
							tris.Add(resTris[i + 1]);
						}
					} else {
						// "positive"
						tris.AddRange(resTris);
					}

					// for noise calculation
					var full = sidesDataFull[direction].vertices;
					foreach (var v in tmd.vertices) {
						if ((v.x < -0.499f || v.x > 0.499f ? 1 : 0) + (v.y < -0.499f || v.y > 0.499f ? 1 : 0) + (v.z < -0.499f || v.z > 0.499f ? 1 : 0) == 2) { continue; }
						bool found = false;
						foreach (var f in full) { if (Base.Math.Approx(v, f, 0.01f)) { found = true; } }
						if (!found) {
							tmd.sideHasInnerClippedVertices = true;
							break;
						}
					}

					sidesDataClipped[other.UID][direction] = tmd;
					tmd.GenerateUVs_PlanarMapping_Centered(direction); //, bloxels.uvFactors);
				} else {
					// TODO - possibly also full?
					// TODO - check return result of ClipTwoPolys!

					sidesDataClipped[other.UID][direction] = null; // TODO BloxelMeshDataEmpty.GetInstance(); // TODO is this making sense?
				}
			}
		}
	}

}