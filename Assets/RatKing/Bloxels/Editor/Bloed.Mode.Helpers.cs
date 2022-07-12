using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RatKing.Bloxels;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace RatKing {

	public partial class Bloed : EditorWindow {

		static string[] optionsMark = new[] {
			"Mark HIDDEN Bloxels",
			"MISSING Bloxels",
			"Mark ALL ExtraSides",
			"INVISIBLE Only"
		};

		[SerializeField] bool affectAllBloxelLevels = true;
		[SerializeField] bool affectJoistsToo = true;
		//
		static List<BloxelLevel> testedLevels = new List<BloxelLevel>();

		public static void RecreateChunkMeshes(BloxelLevel level) {
			if (level == null) { return; }
			//if (PrefabStageUtility.GetCurrentPrefabStage() == null && PrefabUtility.IsPartOfPrefabInstance(level)) { return; }
			if (PrefabUtility.IsPartOfPrefabInstance(level)) { return; }
			level.CheckForMeshPrefabness();
			level.UpdateSeveralChunksStart();
			foreach (var c in level.Chunks) {
				level.UpdateChunkAsPartFromSeveral(c.Value, false);
				c.Value.gameObject.layer = level.Settings.ChunkLayer;
				c.Value.gameObject.tag = level.Settings.ChunkTag;
				GameObjectUtility.SetStaticEditorFlags(c.Value.gameObject, level.Settings.ChunkEditorFlags);
			}
			level.UpdateSeveralChunksEnd();
		}

		void OnGUI_Helpers(Color guiNormalColor) {

			var isPrefabStage = BloxelUtility.TryGetPrefabStage(out var prefabStage); // !PrefabUtility.IsPartOfPrefabInstance(BloxelLevel.Current) && TryGetPrefabStage(out var prefabStage);

			EditorGUIUtility.labelWidth = 10f;
			var curLevel = BloxelLevel.Current;
			if (curLevel == null) { return; }
			testedLevels.Clear();
			if (!isPrefabStage) { testedLevels.AddRange(GameObject.FindObjectsOfType<BloxelLevel>()); }
			else { testedLevels.AddRange(prefabStage.prefabContentsRoot.GetComponentsInChildren<BloxelLevel>()); }

			GUILayout.BeginVertical("box");

			GUILayout.Label("Handle Chunk Meshes + Joists");

			//if (!isPrefabStage) {
			affectAllBloxelLevels = GUILayout.Toggle(affectAllBloxelLevels, "Affect ALL Bloxel Levels");
			if (!affectAllBloxelLevels && PrefabUtility.IsPartOfPrefabInstance(BloxelLevel.Current)) {
				GUILayout.BeginVertical("box");
				GUILayout.Label("<color=red>Unfortunately it's not allowed to edit a prefab instance directly. Either unpack the prefab or open the prefab editor.</color>", wrappedStyle);
				GUILayout.EndVertical();
			}
			else if (affectAllBloxelLevels && testedLevels.Exists(l => PrefabUtility.IsPartOfPrefabInstance(l))) {
				GUILayout.BeginVertical("box");
				GUILayout.Label("<color=yellow>Attention: Some BloxelLevels in this scene are prefabs and can only be edited in prefab mode.</color>", wrappedStyle);
				GUILayout.EndVertical();
			}
			affectJoistsToo = GUILayout.Toggle(affectJoistsToo, "Affect Joists Too");
			//}

			var plural = (affectAllBloxelLevels && !isPrefabStage) ? "es" : "";

			void RemoveChunkMeshes(BloxelLevel level) {
				if (!isPrefabStage && PrefabUtility.IsPartOfPrefabInstance(level)) { return; }
				curMarked.Clear();
				level.CheckForMeshPrefabness();
				foreach (var c in level.Chunks) {
					// destroy the meshes
					if (c.Value.rm != null) { DestroyImmediate(c.Value.rm, isPrefabStage); c.Value.rm = null; }
					if (c.Value.cm != null) { DestroyImmediate(c.Value.cm, isPrefabStage); c.Value.cm = null; }
					// destroy the components
					if (c.Value.mf != null) { DestroyImmediate(c.Value.mf, isPrefabStage); c.Value.mf = null; }
					if (c.Value.mr != null) { DestroyImmediate(c.Value.mr, isPrefabStage); c.Value.mr = null; }
					if (c.Value.mc != null) { DestroyImmediate(c.Value.mc, isPrefabStage); c.Value.mc = null; }
				}
				MarkSceneDirty();
			}

			if (GUILayout.Button("<color=#ff0000>Remove</color> Mesh" + plural)) {
				if (affectAllBloxelLevels) {
					foreach (var l in testedLevels) { RemoveChunkMeshes(l); }
				}
				else {
					RemoveChunkMeshes(curLevel);
				}
				if (affectJoistsToo) {
					foreach (var j in Resources.FindObjectsOfTypeAll<BloxelJoist>()) { if (j.gameObject.scene.IsValid()) { j.RemoveMeshImmediate(); } }
				}
			}

			if (GUILayout.Button("<color=#00ff00>Recreate</color> Mesh" + plural)) {
				curMarked.Clear();
				if (affectAllBloxelLevels) {
					foreach (var l in testedLevels) { RecreateChunkMeshes(l); }
					MarkSceneDirty();
				}
				else {
					RecreateChunkMeshes(curLevel);
					MarkSceneDirty();
				}
				if (affectJoistsToo) {
					foreach (var j in Resources.FindObjectsOfTypeAll<BloxelJoist>()) { if (j.gameObject.scene.IsValid()) { j.UpdateMesh(); } }
				}
			}

			void CreateSecondaryUVs(BloxelLevel level) {
				if (PrefabUtility.IsPartOfPrefabInstance(level)) { return; }
				level.CheckForMeshPrefabness();
				curMarked.Clear();
				//level.gameObject.hideFlags = HideFlags.None;
				UnwrapParam.SetDefaults(out var us);
				us.packMargin = 0.1f; // TODO Test value here, change as needed
				foreach (var c in level.Chunks) {
					c.Value.gameObject.hideFlags = HideFlags.None;
					if (c.Value.rm != null) { Unwrapping.GenerateSecondaryUVSet(c.Value.rm, us); }
				}
				MarkSceneDirty();
			}

			if (GUILayout.Button("<color=#ffff00>Generate</color> Secondary UVs")) {
				if (affectAllBloxelLevels) {
					foreach (var l in testedLevels) { if (!PrefabUtility.IsPartOfPrefabInstance(l)) { CreateSecondaryUVs(l); } }
				}
				else {
					CreateSecondaryUVs(curLevel);
				}
				if (affectJoistsToo) {
					foreach (var j in Resources.FindObjectsOfTypeAll<BloxelJoist>()) { if (j.gameObject.scene.IsValid()) { j.CreateSecondaryUVs(); } }
				}
			}

			GUILayout.EndVertical();

			GUILayout.Space(5f);

			GUILayout.BeginVertical("box");

			GUILayout.Label("Mark Bloxels And Sides Of Current Level");

			var selectedMark = GUILayout.SelectionGrid(-1, optionsMark, 2);

			// Mark HIDDEN Bloxels
			if (selectedMark == 0) {
				MarkHiddenBloxels(curLevel, curMarked);
				Debug.Log("Found " + curMarked.Count + " hidden bloxel(s).");
			}

			// Mark MISSING BLoxels
			if (selectedMark == 1) {
				MarkMissing(curLevel, curMarked);
				Debug.Log("Found " + curMarked.Count + " missing bloxel(s).");
			}

			// Mark ALL ExtraSideData
			if (selectedMark == 2) {
				MarkAllSideData(curLevel, curMarked);
				Debug.Log("Found " + curMarked.Count + " extra side data(s) overall.");
			}

			// Mark INVISIBLE ExtraSideData Only
			if (selectedMark == 3) {
				MarkInvisibleBloxelSides(curLevel, ref curMarked);
				Debug.Log("Found " + curMarked.Count + " invisible extra side data(s).");
			}

			GUILayout.EndVertical();

			GUILayout.Space(5f);

			GUILayout.BeginVertical("box");

			GUILayout.Label("Optimize Current Level - NO UNDO");

			if (GUILayout.Button("Remove All HIDDEN Bloxels")) {
				curMarked.Clear();
				var data = new List<MarkData>();
				MarkHiddenBloxels(curLevel, data);
				if (data.Count > 0) {
					curLevel.UpdateSeveralChunksStart();
					foreach (var d in data) {
						var sb = curLevel.Settings.GetStandardBloxel(d.pos);
						curLevel.ChangeBloxel(d.pos, sb.template, sb.textureIdx);
					}
					curLevel.UpdateSeveralChunksEnd();
					MarkSceneDirty();
					Debug.Log(data.Count + " hidden bloxel(s) removed!");
				}
				else {
					Debug.Log("No hidden bloxels removed!");
				}
				EditorApplication.delayCall += () => SceneView.RepaintAll();
			}

			if (GUILayout.Button("Clean Up MISSING Bloxels")) {
				curMarked.Clear();
				var data = new List<MarkData>();
				MarkMissing(curLevel, data);
				if (data.Count > 0) {
					curLevel.UpdateSeveralChunksStart();
					foreach (var d in data) {
						var ob = curLevel.GetBloxel(d.pos);
						var sb = curLevel.Settings.GetStandardBloxel(d.pos);
						var tp = ob.template;
						if (tp == null || tp == ProjectSettings.MissingTemplate) { tp = sb.template; }
						var tx = ob.textureIdx <= 0 && !sb.template.IsAir ? sb.textureIdx : ob.textureIdx;
						curLevel.ChangeBloxel(d.pos, tp, tx <= 0 ? 1 : tx);
					}
					curLevel.UpdateSeveralChunksEnd();
					MarkSceneDirty();
					Debug.Log(data.Count + " missing bloxel(s) removed!");
				}
				else {
					Debug.Log("No missing bloxels cleaned up!");
				}
				EditorApplication.delayCall += () => SceneView.RepaintAll();
			}

			if (GUILayout.Button("Remove All INVISIBLE ExtraSideDatas")) {
				curMarked.Clear();
				var data = new List<MarkData>();
				MarkInvisibleBloxelSides(curLevel, ref data);
				if (data.Count > 0) {
					curLevel.UpdateSeveralChunksStart();
					foreach (var d in data) { curLevel.RemoveBloxelTextureSideData(d.pos, d.dir); }
					curLevel.UpdateSeveralChunksEnd();
					MarkSceneDirty();
					Debug.Log(data.Count + " invisible side data(s) removed!");
				}
				else {
					Debug.Log("No invisible side datas removed!");
				}
				EditorApplication.delayCall += () => SceneView.RepaintAll();
			}

			if (GUILayout.Button("OPTIMIZE Bloxels With ExtraSideData")) {
				curMarked.Clear();
				var count = OptimizeAllBloxelsWithSideData(curLevel);
				if (count > 0) {
					MarkSceneDirty();
					Debug.Log(count + " bloxel(s) optimized!");
				}
				else {
					Debug.Log("No bloxels optimized!");
				}
				EditorApplication.delayCall += () => SceneView.RepaintAll();
			}

			GUILayout.EndVertical();

			if (projectSettings.ShowInternals) {

				GUILayout.Space(5f);

				GUILayout.BeginVertical("box");

				GUILayout.Label("<color=red>DEBUG STUFF</color>");

				if (GUILayout.Button("Mark All Changed Bloxels In Current Chunk")) {
					MarkAllChangedBloxelsInChunk(curLevel, Base.Position3.FlooredVector(curViewPivot), curMarked);
				}

				if (GUILayout.Button("Mark Inconsistent Bloxel Data In Current Level")) {
					MarkInconsistentBloxelData(curLevel, curMarked);
				}

				if (GUILayout.Button("Fix Inconsistent Bloxel Data")) {
					curMarked.Clear();
					var changed = false;
					if (affectAllBloxelLevels) { foreach (var l in testedLevels) { changed = FixInconsistentBloxelData(l) || changed; } }
					else { changed = FixInconsistentBloxelData(curLevel); }
					if (changed) { MarkSceneDirty(); EditorApplication.delayCall += () => SceneView.RepaintAll(); }
				}

				GUILayout.EndVertical();
			}
		}

		void MarkHiddenBloxels(BloxelLevel level, List<MarkData> data) {
			data.Clear();
			var cs = level.chunkSize;
			foreach (var pc in level.Chunks) {
				var chunk = pc.Value;
				var cpos = chunk.Pos * cs;
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					var bTmp = chunk.GetChangedBloxelTemplateAt(i);
					var relPos = level.GetBloxelRelPosByIndex(chunk.changedBloxelsDataPosIndices[i]);
					if (relPos.x >= 0 && relPos.y >= 0 && relPos.z >= 0 && relPos.x < cs && relPos.y < cs && relPos.z < cs) {
						var pos = relPos + cpos;
						var stdBlx = level.Settings.GetStandardBloxel(pos);
						if (bTmp == stdBlx.template) {
							var isVisible = false;
							for (int d = 0; d < 6; ++d) {
								var nPos = pos + BloxelUtility.faceDirPositions[d];
								var nTmp = level.GetBloxel(nPos).template;
								// if (nTmp != null && !nTmp.HasFullWallSide((d + 3) % 6)) { isVisible = true; break; }
								if (nTmp != null && !bTmp.IsEmptyTo(nTmp, d)) { isVisible = true; break; }
							}
							if (!isVisible) {
								data.Add(new MarkData(pos));
							}
						}
					}
					//
				}
			}
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		void MarkMissing(BloxelLevel level, List<MarkData> data) {
			// TODO also do tex directions, if needed
			data.Clear();
			var cs = level.chunkSize;
			foreach (var pc in level.Chunks) {
				var chunk = pc.Value;
				var cpos = chunk.Pos * cs;
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					var bx = chunk.GetChangedBloxelAt(i);
					if (bx.template == null || bx.template == ProjectSettings.MissingTemplate || bx.templateUID == "$MISSING") {
						data.Add(new MarkData(level.GetBloxelRelPosByIndex(chunk.changedBloxelsDataPosIndices[i]) + cpos));
					}
					else if (bx.textureIdx <= 0 && !bx.template.IsAir) {
						var pos = level.GetBloxelRelPosByIndex(chunk.changedBloxelsDataPosIndices[i]) + cpos;
						var sb = level.GetStandardBloxel(pos);
						if (sb.textureIdx > 0) {
							Debug.Log(i + ") " + bx.textureIdx + " - " + sb.textureIdx);
							data.Add(new MarkData(pos));
						}
					}
				}
			}
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		void MarkInvisibleBloxelSides(BloxelLevel level, ref List<MarkData> data) {
			data.Clear();
			var cs = level.chunkSize;
			foreach (var pc in level.Chunks) {
				var chunk = pc.Value;
				var cpos = chunk.Pos * cs;
				foreach (var sd in chunk.textureSideExtraDataByRelPos) {
					var relPos = level.GetBloxelRelPosByIndex(sd.Key / 7);
					if (relPos.x >= 0 && relPos.y >= 0 && relPos.z >= 0 && relPos.x < cs && relPos.y < cs && relPos.z < cs) {
						int dir = sd.Key % 7;
						var pos = relPos + cpos;
						var bloxel = level.GetBloxel(pos);
						var sdv = sd.Value;
						if (sdv.r <= 0 && sdv.uv <= 0 && sdv.o <= 0 && (sdv.ti == -1 || sdv.ti == bloxel.textureIdx)) { data.Add(new MarkData(pos, dir)); continue; }
						// all sides apart from inner
						if (dir != 6) {
							if (bloxel.template.HasAirSide(dir)) { data.Add(new MarkData(pos, dir)); continue; }
							var nPos = pos + BloxelUtility.faceDirPositions[dir];
							var nTmp = level.GetBloxel(nPos).template;
							if (nTmp != null && bloxel.template.IsEmptyTo(nTmp, dir)) { data.Add(new MarkData(pos, dir)); continue; }
						}
						else {
							if (!bloxel.template.HasInnerData()) { data.Add(new MarkData(pos, dir)); continue; }
						}
					}
				}
			}
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		void MarkAllSideData(BloxelLevel level, List<MarkData> data) {
			data.Clear();
			var cs = level.chunkSize;
			foreach (var pc in level.Chunks) {
				var chunk = pc.Value;
				var cpos = chunk.Pos * cs;
				foreach (var sd in chunk.textureSideExtraDataByRelPos) {
					//Debug.Log(sd.Key + ": " + sd.Value);
					var relPos = level.GetBloxelRelPosByIndex(sd.Key / 7);
					if (relPos.x >= 0 && relPos.y >= 0 && relPos.z >= 0 && relPos.x < cs && relPos.y < cs && relPos.z < cs) {
						var pos = relPos + cpos;
						data.Add(new MarkData(pos, sd.Key % 7));
					}
				}
			}
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		int OptimizeAllBloxelsWithSideData(BloxelLevel level) {
			// TODO - for removing single side data that just hides a full bloxel

			var cs = level.chunkSize;
			var bloxelsWithSideData = new Dictionary<Base.Position3, SideExtraData[]>();
			foreach (var pc in level.Chunks) {
				var chunk = pc.Value;
				var cpos = chunk.Pos * cs;
				foreach (var sd in chunk.textureSideExtraDataByRelPos) {
					var relPos = level.GetBloxelRelPosByIndex(sd.Key / 7);
					if (relPos.x < 0 || relPos.y < 0 || relPos.z < 0 || relPos.x >= cs || relPos.y >= cs || relPos.z >= cs) { continue; }
					var pos = relPos + cpos;
					if (!bloxelsWithSideData.TryGetValue(pos, out var list)) {
						bloxelsWithSideData[pos] = list = new SideExtraData[7];
						for (int i = 0; i < 7; ++i) { list[i] = list[i].CopyWithTexture(-2); } // not set
					}
					list[sd.Key % 7] = sd.Value;
					//curMarked.Add(new MarkData(pos, sd.Key % 7));
				}
			}

			var optimizedCount = 0;
			var sideDataCount = new Dictionary<int, int>();
			var tidxCount = 0;
			foreach (var bsd in bloxelsWithSideData) {
				sideDataCount.Clear();
				var pos = bsd.Key;
				var list = bsd.Value;
				var bloxel = level.GetBloxel(pos);

				// assign the bloxel's texture indices to the remaining, visible sides
				for (int d = 0; d < 6; ++d) {
					if (list[d].ti == -1) { list[d] = list[d].CopyWithTexture(bloxel.textureIdx); continue; }
					if (list[d].ti != -2) { continue; }
					var nPos = pos + BloxelUtility.faceDirPositions[d];
					var nTmp = level.GetBloxel(nPos).template;
					//if (nTmp != null && !nTmp.HasFullWallSide((d + 3) % 6)) { list[d] = list[d].WithTexture(bloxel.textureIdx); }
					if (nTmp != null && !bloxel.template.IsEmptyTo(nTmp, d)) { list[d] = list[d].CopyWithTexture(bloxel.textureIdx); }
				}
				if (bloxel.template.HasInnerData() && list[6].ti == -2) { list[6] = list[6].CopyWithTexture(bloxel.textureIdx); }

				// count the amount of each texture index
				for (int i = 0; i < 7; ++i) {
					if (list[i].ti == -2) { continue; }
					if (list[i].r > 0 || list[i].uv > 0 || list[i].o > 0) { continue; } // ignore this data, it would still exist after optimizing
					if (!sideDataCount.TryGetValue(list[i].ti, out tidxCount)) { sideDataCount[list[i].ti] = tidxCount + 1; }
					else { sideDataCount[list[i].ti] = 1; }
				}

				// which texture index is there the most?
				int maxCount = 0;
				int maxCountTidx = -1;
				foreach (var sdc in sideDataCount) { if (sdc.Value > maxCount) { maxCount = sdc.Value; maxCountTidx = sdc.Key; } }

				// ignore this one, the data is optimized already ?
				if (maxCountTidx == -1 || sideDataCount.TryGetValue(bloxel.textureIdx, out tidxCount) && tidxCount == maxCount) { continue; }

				//Debug.Log(maxCount + " ... " + maxCountTidx + " ... " + pos);

				// otherwise change the bloxels texture and change the side data accordingly
				var oldTidx = bloxel.textureIdx;
				var oldSideData = new SideExtraData(oldTidx, 0, 0);
				level.ChangeBloxel(pos, bloxel.template, maxCountTidx);
				for (int i = 0; i < 7; ++i) {
					if (list[i].ti == -2) { continue; } // invisible
					if (list[i].ti == maxCountTidx) { level.RemoveBloxelTextureSideData(pos, i); continue; }
					if (list[i].ti == oldTidx) { level.ChangeBloxelTextureSide(pos, i, oldSideData); }
				}
				optimizedCount++;
			}

			return optimizedCount;
		}

		void MarkAllChangedBloxelsInChunk(BloxelLevel level, Base.Position3 absPos, List<MarkData> data) {
			data.Clear();
			var cs = level.chunkSize;
			var chunkPos = level.GetChunkPos(absPos);
			var chunk = level.GetChunk(absPos);
			if (chunk != null) {
				var cpos = chunk.Pos * cs;
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					int bi = chunk.changedBloxelsDataPosIndices[i];
					var pos = chunk.lvl.GetBloxelRelPosByIndex(bi);
					var outer = pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x > cs - 1 || pos.y > cs - 1 || pos.z > cs - 1;
					data.Add(new MarkData(pos + cpos, outer ? Color.yellow.WithAlpha(0.5f) : Color.white));
				}
			}
			Debug.Log("Found " + curMarked.Count + " changed bloxels in chunk " + chunkPos + "!");
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		void MarkInconsistentBloxelData(BloxelLevel level, List<MarkData> data) {
			data.Clear();
			var cs = level.chunkSize;
			var tempIndices = new List<int>();
			var missingChunks = new HashSet<Base.Position3>();
			foreach (var kvp in level.Chunks) {
				var chunk = kvp.Value;
				var cpos = chunk.Pos * cs;
				tempIndices.Clear();
				tempIndices.AddRange(chunk.changedBloxelsDataPosIndices);

				// double bloxels

				var hash = new HashSet<int>(tempIndices);
				if (hash.Count != tempIndices.Count) {
					foreach (var ti in hash) {
						if (tempIndices.FindAll(i => i == ti).Count > 1) {
							data.Add(new MarkData(level.GetBloxelRelPosByIndex(ti) + cpos, Color.magenta));
						}
					}
				}

				// outer bloxels - correct data?
						
				void CheckConsistency(int x, int y, int z) {
					var idx = level.GetBloxelRelPosIndex(x, y, z);
					var pos = new Base.Position3(x, y, z) + cpos;
					var realBloxel = level.GetBloxel(pos);
					var standardBloxel = level.Settings.GetStandardBloxel(pos);
					if (realBloxel != standardBloxel) {
						var pl = tempIndices.IndexOf(idx);
						if (pl == -1) { // the changed bloxel does not exist even though it should
							//Debug.Log(chunk.Pos + " - " + idx + "/" + pos + ") " + realBloxel.templateUID + "==" + standardBloxel.templateUID + " / " + realBloxel.textureIdx + "==" + standardBloxel.textureIdx + " ... " + pos, chunk);
							data.Add(new MarkData(pos, Color.red));
						}
						else if (chunk.GetChangedBloxelAt(pl) != realBloxel) { // the changed bloxel is not the real one
							//Debug.Log(chunk.Pos + " - " + idx + "/" + pos + ") " + realBloxel.templateUID + "==" + chunk.GetChangedBloxelAt(pl).templateUID + " / " + realBloxel.textureIdx + "==" + chunk.GetChangedBloxelAt(pl).textureIdx + " ... " + pos, chunk);
							data.Add(new MarkData(pos, new Color(1f, 0.5f, 0f)));
						}
					}
					else if (tempIndices.Contains(idx)) { // there is a changed bloxel even though there shouldn't
						data.Add(new MarkData(pos, Color.yellow));
					}
				}
						
				for (int y = -1; y < cs + 1; ++y) {
					for (int x = -1; x < cs + 1; ++x) { CheckConsistency(x, y, -1); CheckConsistency(x, y, cs); }
					for (int z = 0; z < cs; ++z) { CheckConsistency(-1, y, z); CheckConsistency(cs, y, z); }
				}
				for (int z = 0; z < cs; ++z) {
					for (int x = 0; x < cs; ++x) { CheckConsistency(x, -1, z); CheckConsistency(x, cs, z); }
				}

				// chunks - don't exist even though they should?
						
				bool CheckChunks(int x, int y, int z, int addX, int addY, int addZ) {
					var pos = new Base.Position3(x, y, z) + cpos;
					var realBloxel = level.GetBloxel(pos);
					var standardBloxel = level.Settings.GetStandardBloxel(pos);
					if (realBloxel != standardBloxel) {
						var addPos = pos + new Base.Position3(addX, addY, addZ);
						if (level.GetChunk(addPos) == null && !missingChunks.Contains(level.GetChunkPos(addPos))) {
							missingChunks.Add(level.GetChunkPos(addPos));
							//Debug.Log("Found missing bloxel chunk at " + (level.GetChunkPos(addPos)));
							//data.Add(new MarkData(addPos, Color.yellow));
						}
						return true;
					}
					return false;
				}

				var cm = cs - 1;
				     for (int y = 0; y < cs; ++y) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, y,  0, 0, 0, -1)) { goto cc2; } } }
				cc2: for (int y = 0; y < cs; ++y) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, y, cm, 0, 0,  1)) { goto cc3; } } }
				cc3: for (int z = 0; z < cs; ++z) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0, z, 0, -1, 0)) { goto cc4; } } }
				cc4: for (int z = 0; z < cs; ++z) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm, z, 0,  1, 0)) { goto cc5; } } }
				cc5: for (int z = 0; z < cs; ++z) { for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y, z, -1, 0, 0)) { goto cc6; } } }
				cc6: for (int z = 0; z < cs; ++z) { for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y, z,  1, 0, 0)) { goto cc7; } } }
				cc7: for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0,  0,  0, -1, -1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0, cm,  0, -1,  1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm,  0,  0,  1, -1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm, cm,  0,  1,  1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y,  0, -1,  0, -1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y, cm, -1,  0,  1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y,  0,  1,  0, -1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y, cm,  1,  0,  1)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks( 0,  0, z, -1, -1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks( 0, cm, z, -1,  1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks(cm,  0, z,  1, -1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks(cm, cm, z,  1,  1,  0)) { break; } }
					 CheckChunks( 0,  0,  0, -1, -1, -1); CheckChunks( 0,  0, cm, -1, -1,  1);
					 CheckChunks( 0, cm,  0, -1,  1, -1); CheckChunks(cm,  0, cm,  1, -1,  1);
					 CheckChunks(cm,  0,  0,  1, -1, -1); CheckChunks(cm, cm, cm,  1,  1,  1);
					 CheckChunks( 0, cm, cm, -1,  1,  1); CheckChunks(cm, cm,  0,  1,  1, -1);
			}
			Debug.Log("Found " + data.Count + " inconsistent bloxels and " + missingChunks.Count + " missing chunks!");	
			EditorApplication.delayCall += () => SceneView.RepaintAll();
		}

		bool FixInconsistentBloxelData(BloxelLevel level) {
			var c = 0;
			var cs = level.chunkSize;
			var listIdx = new List<int>();
			var listNew = new List<(int, int, int)>();
			var missingChunks = new HashSet<Base.Position3>();
			foreach (var kvp in level.Chunks) {
				var chunk = kvp.Value;
				if (chunk == null) { continue; }
				var cpos = chunk.Pos * cs;
				listIdx.Clear();
				listIdx.AddRange(chunk.changedBloxelsDataPosIndices);

				// double bloxels

				var hash = new HashSet<int>(listIdx);
				if (hash.Count != listIdx.Count) {
					foreach (var ti in hash) {
						for (int i = chunk.changedBloxelsNum - 1, min = listIdx.IndexOf(ti); i > min; --i) {
							if (listIdx[i] == ti) {
								listIdx.RemoveAt(i);
								chunk.changedBloxelsTemplateIndices.RemoveAt(i);
								chunk.changedBloxelsTextureIndices.RemoveAt(i);
								--chunk.changedBloxelsNum;
								++c;
							}
						}
					}
				}

				listNew.Clear();
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					listNew.Add((listIdx[i], chunk.changedBloxelsTemplateIndices[i], chunk.changedBloxelsTextureIndices[i]));
				}

				// other bloxels - correct data?

				void FixConsistency(int x, int y, int z) {
					var idx = level.GetBloxelRelPosIndex(x, y, z);
					var pos = new Base.Position3(x, y, z) + cpos;
					var realBloxel = level.GetBloxel(pos);
					var standardBloxel = level.Settings.GetStandardBloxel(pos);
					if (realBloxel != standardBloxel) {
						var pl = listIdx.IndexOf(idx);
						if (pl == -1) {
							++c; // missing
							listNew.Add((idx, level.TemplateUIDs.IndexOf(realBloxel.templateUID), realBloxel.textureIdx));
						}
						else if (chunk.GetChangedBloxelAt(pl) != realBloxel) {
							++c; // different
							var i = listNew.FindIndex(ln => ln.Item1 == idx);
							listNew[i] = (idx, level.TemplateUIDs.IndexOf(realBloxel.templateUID), realBloxel.textureIdx);
						}
					}
					else if (listIdx.Contains(idx)) {
						++c; // too much
						listNew.RemoveAll(ln => ln.Item1 == idx);
					}
				}
						
				for (int y = -1; y < cs + 1; ++y) {
					for (int x = -1; x < cs + 1; ++x) { FixConsistency(x, y, -1); FixConsistency(x, y, cs); }
					for (int z = 0; z < cs; ++z) { FixConsistency(-1, y, z); FixConsistency(cs, y, z); }
				}
				for (int z = 0; z < cs; ++z) {
					for (int x = 0; x < cs; ++x) { FixConsistency(x, -1, z); FixConsistency(x, cs, z); }
				}

				listNew.Sort((a, b) => a.Item1 - b.Item1);
				chunk.changedBloxelsNum = listNew.Count;
				chunk.changedBloxelsDataPosIndices = new int[chunk.changedBloxelsNum];
				chunk.changedBloxelsTemplateIndices.Clear();
				chunk.changedBloxelsTextureIndices.Clear();
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					chunk.changedBloxelsDataPosIndices[i] = listNew[i].Item1;
					chunk.changedBloxelsTemplateIndices.Add(listNew[i].Item2);
					chunk.changedBloxelsTextureIndices.Add(listNew[i].Item3);
				}
				
				// chunks - don't exist even though they should?

				bool CheckChunks(int x, int y, int z, int addX, int addY, int addZ) {
					var pos = new Base.Position3(x, y, z) + cpos;
					var realBloxel = level.GetBloxel(pos);
					var standardBloxel = level.Settings.GetStandardBloxel(pos);
					if (realBloxel != standardBloxel) {
						var addPos = pos + new Base.Position3(addX, addY, addZ);
						if (level.GetChunk(addPos) == null && !missingChunks.Contains(level.GetChunkPos(addPos))) {
							missingChunks.Add(level.GetChunkPos(addPos));
						}
						return true;
					}
					return false;
				}

				var cm = cs - 1;
				     for (int y = 0; y < cs; ++y) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, y,  0, 0, 0, -1)) { goto fc2; } } }
				fc2: for (int y = 0; y < cs; ++y) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, y, cm, 0, 0,  1)) { goto fc3; } } }
				fc3: for (int z = 0; z < cs; ++z) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0, z, 0, -1, 0)) { goto fc4; } } }
				fc4: for (int z = 0; z < cs; ++z) { for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm, z, 0,  1, 0)) { goto fc5; } } }
				fc5: for (int z = 0; z < cs; ++z) { for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y, z, -1, 0, 0)) { goto fc6; } } }
				fc6: for (int z = 0; z < cs; ++z) { for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y, z,  1, 0, 0)) { goto fc7; } } }
				fc7: for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0,  0,  0, -1, -1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x,  0, cm,  0, -1,  1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm,  0,  0,  1, -1)) { break; } }
					 for (int x = 0; x < cs; ++x) { if (CheckChunks(x, cm, cm,  0,  1,  1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y,  0, -1,  0, -1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks( 0, y, cm, -1,  0,  1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y,  0,  1,  0, -1)) { break; } }
					 for (int y = 0; y < cs; ++y) { if (CheckChunks(cm, y, cm,  1,  0,  1)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks( 0,  0, z, -1, -1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks( 0, cm, z, -1,  1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks(cm,  0, z,  1, -1,  0)) { break; } }
					 for (int z = 0; z < cs; ++z) { if (CheckChunks(cm, cm, z,  1,  1,  0)) { break; } }
					 CheckChunks( 0,  0,  0, -1, -1, -1); CheckChunks( 0,  0, cm, -1, -1,  1);
					 CheckChunks( 0, cm,  0, -1,  1, -1); CheckChunks(cm,  0, cm,  1, -1,  1);
					 CheckChunks(cm,  0,  0,  1, -1, -1); CheckChunks(cm, cm, cm,  1,  1,  1);
					 CheckChunks( 0, cm, cm, -1,  1,  1); CheckChunks(cm, cm,  0,  1,  1, -1);
			}

			if (missingChunks.Count > 0) {
				foreach (var mc in missingChunks) {
					var cpos = mc * cs;
					var chunk = level.InstantiateChunk(mc);
							
					listNew.Clear();
					void AddData(int x, int y, int z) {
						var pos = new Base.Position3(x, y, z) + cpos;
						var realBloxel = level.GetBloxel(pos);
						if (realBloxel != level.Settings.GetStandardBloxel(pos)) {
							listNew.Add((
								level.GetBloxelRelPosIndex(x, y, z),
								level.TemplateUIDs.IndexOf(realBloxel.templateUID),
								realBloxel.textureIdx));
						}
					}

					for (int y = -1; y < cs + 1; ++y) {
						for (int x = -1; x < cs + 1; ++x) { AddData(x, y, -1); AddData(x, y, cs); }
						for (int z = 0; z < cs; ++z) { AddData(-1, y, z); AddData(cs, y, z); }
					}
					for (int z = 0; z < cs; ++z) {
						for (int x = 0; x < cs; ++x) { AddData(x, -1, z); AddData(x, cs, z); }
					}
							
					listNew.Sort((a, b) => a.Item1 - b.Item1);
					chunk.changedBloxelsNum = listNew.Count;
					chunk.changedBloxelsDataPosIndices = new int[chunk.changedBloxelsNum];
					// should be empty chunk.changedBloxelsTemplateIndices.Clear();
					// should be empty chunk.changedBloxelsTextureIndices.Clear();
					for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
						chunk.changedBloxelsDataPosIndices[i] = listNew[i].Item1;
						chunk.changedBloxelsTemplateIndices.Add(listNew[i].Item2);
						chunk.changedBloxelsTextureIndices.Add(listNew[i].Item3);
					}
				}
			}
					
			Debug.Log("Fixed " + c + " inconsistent bloxels and " + missingChunks.Count + " missing bloxel chunks!");

			if (c > 0 || missingChunks.Count > 0) { RecreateChunkMeshes(level); return true; }
			return false;
		}
	}

}