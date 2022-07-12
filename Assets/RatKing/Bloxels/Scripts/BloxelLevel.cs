using System.Collections.Generic;
using UnityEngine;
using RatKing.Base;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEditor;
#endif

namespace RatKing.Bloxels {

	[ExecuteInEditMode]
	[SelectionBase]
	public class BloxelLevel : MonoBehaviour {
		static BloxelLevel current = null;
		public static BloxelLevel Current {
			get {
#if UNITY_EDITOR
				if (!Application.isPlaying) {
					var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
					if (prefabStage != null && current != null && current.gameObject.scene != prefabStage.scene) {
#if UNITY_2020_1_OR_NEWER
						var bl = prefabStage.FindComponentOfType<BloxelLevel>();
#else
						var bl = prefabStage.prefabContentsRoot.GetComponentInChildren<BloxelLevel>(true);
#endif
						if (bl != null) { bl.isCurrent = true; current = bl; }
						return bl;
					}
				}
#endif
						if (current != null) { if (!current.isCurrent) { current.isCurrent = true; } return current; }
				var bls = GameObject.FindObjectsOfType<BloxelLevel>();
				if (bls.Length == 0) { return null; }
				foreach (var l in bls) { if (l.isCurrent) { return current = l; } }
				bls[0].isCurrent = true;
				return current = bls[0];
			}
		}
		public static BloxelLevelSettings CurrentSettings => Current != null ? Current.Settings : null;
		//
		[SerializeField] bool isCurrent = false;
		public bool IsCurrent {
			get {
				if (isCurrent && current != null && current != this) { isCurrent = false; }
				return isCurrent;
			}
		}
		public static void SetCurrent(BloxelLevel level) {
			if (level != null && Current == level) { level.isCurrent = true; return; }
#if UNITY_EDITOR
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null) {
				foreach (var l in prefabStage.prefabContentsRoot.GetComponentsInChildren<BloxelLevel>()) { l.isCurrent = (level == l); }
			}
#endif
			foreach (var l in GameObject.FindObjectsOfType<BloxelLevel>()) { l.isCurrent = (level == l); }
#if UNITY_EDITOR
#endif
			if (level != null) { level.isCurrent = true; }
			current = level;
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				EditorApplication.RepaintHierarchyWindow();
			}
#endif
		}

		public static void SetCurrentDirectly(BloxelLevel level) {
			if (level != null) { level.isCurrent = true; }
			current = level;
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				EditorApplication.RepaintHierarchyWindow();
			}
#endif
		}

		[SerializeField] bool recreateOnStart = false;
		[SerializeField] BloxelLevelSettings settings = null;
		public BloxelLevelSettings Settings => settings;
		[SerializeField] BloxelProjectSettings projectSettings = null;
		public BloxelProjectSettings ProjectSettings => projectSettings;
		[SerializeField] Position3BloxelChunkDictionary chunks = new Position3BloxelChunkDictionary();
		public Position3BloxelChunkDictionary Chunks => chunks;
		[SerializeField] public List<string> TemplateUIDs = new List<string>();
		[SerializeField] public List<string> TextureUIDs = new List<string>();
		[SerializeField] public List<BloxelJoist> Joists = new List<BloxelJoist>();
		
		// do not change these after initialisation
		[HideInInspector] public int chunkSize = 8;
		[System.NonSerialized] public int chunkSizeA1 = 0; // chunkSize + 1; // add 1
		[System.NonSerialized] public int chunkSizeA2 = 0; // chunkSize + 2; // add 2
		[System.NonSerialized] public int chunkSizeA2P2 = 0; // chunkSizeA2 * chunkSizeA2; // add 2, power 2
		[System.NonSerialized] public int chunkSizeA2M2 = 0; // chunkSizeA2 * 2; // add 2, multiplicate with 2
		[System.NonSerialized] public int[] neighbourDirAdd = null;

		// temporary:
		public static Bloxel[] tempBloxels = new Bloxel[66 * 66 * 66]; // working on this while building // TODO public/only generate when necessary
		public static Position3 tempBloxelPosRel = new Position3(); // TODO public
		static List<int> tempTexSideDataToRemove = new List<int>();
		static HashSet<BloxelChunk> tempChunksToGetUpdatedUVsOnly = new HashSet<BloxelChunk>();
		static HashSet<BloxelChunk> tempChunksToGetUpdated = new HashSet<BloxelChunk>();
		static bool updateChunkAuto = true;
		//
		//public static Bounds Bounds { get; private set; } // TODO needed?
		static int changedBloxelsCount = 0;

		//

		public void Init(BloxelLevelSettings settings, BloxelProjectSettings projectSettings, int chunkSize) {
			InitChunkSize(chunkSize);
			//
			this.settings = settings;
			this.projectSettings = projectSettings;
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				SetCurrent(this);
			}
#endif
		}

		void Start() {
			if (!Application.isPlaying) {
				UpdateListsOfUIDs();
				settings.ResetStandardBloxels();
			}
			else {
				if (recreateOnStart) {
					UpdateSeveralChunksStart();
					foreach (var c in chunks) { UpdateChunkAsPartFromSeveral(c.Value, false); }
					UpdateSeveralChunksEnd();
				}
				else {
					foreach (var c in chunks) {
						UpdateChunkCollisionOnly(c.Value, false);
					}
				}
			}
		}

		void InitChunkSize(int chunkSize) {
			this.chunkSize = chunkSize;
			chunkSizeA1 = chunkSize + 1; // add 1
			chunkSizeA2 = chunkSize + 2; // add 2
			chunkSizeA2P2 = chunkSizeA2 * chunkSizeA2; // add 2, power 2
			chunkSizeA2M2 = chunkSizeA2 * 2; // add 2, multiplicate with 2
			neighbourDirAdd = new[] { chunkSizeA2P2, -chunkSizeA2, -1, -chunkSizeA2P2, chunkSizeA2, 1 };
		}

		void OnEnable() {
			InitChunkSize(chunkSize);

			if (settings != null) { settings.ResetStandardBloxels(); }
			if (current != null && isCurrent && current != this) { isCurrent = false; }

#if UNITY_EDITOR
			Joists.RemoveAll(bj => bj == null);
#endif
		}

#if UNITY_EDITOR

		// When the level object was copied, create new meshes
		// https://answers.unity.com/questions/1274030/unity-duplicate-event.html
		void OnValidate() {
			if (Application.isPlaying) { return; }
			var e = UnityEngine.Event.current;
			if (e != null && e.type == EventType.ExecuteCommand && e.commandName == "Duplicate") {
				isCurrent = false;
				if (!PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
					MakeMeshesUnique(true);
				}
			}
		}

		void Update() {
			if (!Application.isPlaying) { CheckForMeshPrefabness(); }
		}

		// unpacking a prefab -> make the meshes a part of the scene instead of the prefab!
		public void CheckForMeshPrefabness() {
			if (gameObject.scene == null || gameObject.scene.path == null) { return; }
			if (!PrefabUtility.IsPartOfPrefabInstance(gameObject) && !gameObject.scene.path.EndsWith(".prefab") && PrefabStageUtility.GetCurrentPrefabStage() == null) {
				var mf = GetComponentInChildren<MeshFilter>();
				if (mf != null && mf.sharedMesh != null) {
					var path = AssetDatabase.GetAssetPath(mf.sharedMesh);
					if (!string.IsNullOrWhiteSpace(path)) {
						Debug.Log("Making meshes of BloxelLevel " + name + " in scene " + gameObject.scene.path + " unique...");
						MakeMeshesUnique(false); // TODO? BloxelUtility.CurLevel.MakeMeshesUnique(false);
					}
				}
			}
		}

		// Make sure prefabs have the meshes as subassets
		// When creating a prefab out of this bloxel level, this should be called
		public void PrefabInstanceUpdated(GameObject prefab) {
			if (chunks.Count == 0) { return; }
			var path = AssetDatabase.GetAssetPath(prefab);
			var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
			var hasMesh = false;
			foreach (var sa in subAssets) { if (sa is Mesh) { hasMesh = true; break; } }
		
			if (!hasMesh) {
				Debug.Log("Adding meshes of BloxelLevel " + name + " to its prefab... path: " + path);
				var plvl = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(this, path);
				EditorApplication.delayCall += () => {
					foreach (var c in chunks) {
						var pc = plvl.chunks[c.Key];
						if (c.Value.rm != null) { AssetDatabase.AddObjectToAsset(c.Value.rm, prefab); c.Value.mf.sharedMesh = pc.mf.sharedMesh = pc.rm = c.Value.rm; }
						if (c.Value.cm != null) { AssetDatabase.AddObjectToAsset(c.Value.cm, prefab); c.Value.mc.sharedMesh = pc.mc.sharedMesh = pc.cm = c.Value.cm; }
					}
					for (int i = 0; i < Joists.Count; ++i) {
						var pj = plvl.Joists[i];
						if (Joists[i].mf != null) {
							var mesh = Joists[i].mf.sharedMesh;
							AssetDatabase.AddObjectToAsset(mesh, prefab);
							Joists[i].mf.sharedMesh = pj.mf.sharedMesh = mesh;
							if (Joists[i].mc != null) {
								Joists[i].mc.sharedMesh = pj.mc.sharedMesh = mesh;
							}
						}
					}
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
					PrefabUtility.ApplyObjectOverride(gameObject, path, InteractionMode.AutomatedAction);
					// var original = PrefabUtility.LoadPrefabContents(path);
				};
			}
		}

		public void MakeMeshesUnique(bool delayed) {
			//Debug.Log(name + " makes meshes unique");
			void Do() {
				if (PrefabStageUtility.GetCurrentPrefabStage() == null) {
					foreach (var c in chunks) {
						if (c.Value.rm != null) { c.Value.mf.sharedMesh = c.Value.rm = Instantiate(c.Value.rm); }
						if (c.Value.cm != null) { c.Value.mc.sharedMesh = c.Value.cm = Instantiate(c.Value.cm); }
					}
				}
				else {
					bool recreate = false;
					foreach (var c in chunks) {
						if (c.Value.rm != null && c.Value.mf.sharedMesh != null) { recreate = true; c.Value.mf.sharedMesh = c.Value.rm = null; }
						if (c.Value.cm != null && c.Value.mc.sharedMesh != null) { recreate = true; c.Value.mc.sharedMesh = c.Value.cm = null; }
					}
					if (recreate) {
						UpdateSeveralChunksStart();
						foreach (var c in chunks) { UpdateChunkAsPartFromSeveral(c.Value, false); }
						UpdateSeveralChunksEnd();
					}
				}
			}
			if (!delayed) {
				Do();
			}
			else {
				EditorApplication.delayCall += () => {
					Do();
				};
			}
		}

		void OnDestroy() {
			var e = UnityEngine.Event.current;
			if (e != null && e.type == EventType.ExecuteCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete")) {
				if (!Application.isPlaying && chunks.Count > 0 && PrefabStageUtility.GetCurrentPrefabStage() != null && !PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
					foreach (var c in chunks) {
						if (c.Value.rm != null) { AssetDatabase.RemoveObjectFromAsset(c.Value.rm); }
						if (c.Value.cm != null) { AssetDatabase.RemoveObjectFromAsset(c.Value.cm); }
					}
					for (int i = 0; i < Joists.Count; ++i) {
						if (Joists[i].mf != null) { AssetDatabase.RemoveObjectFromAsset(Joists[i].mf.sharedMesh); }
					}
					chunks.Clear();
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}
		}
#endif

		public void UpdateListsOfUIDs() {
			//Debug.Log("BloxelLevel: UpdateListsOfUIDs");
			var invalidIndices = new Stack<int>();
			for (int i = TemplateUIDs.Count - 1; i >= 0; --i) {
				if (!projectSettings.TemplatesByUID.ContainsKey(TemplateUIDs[i])) { invalidIndices.Push(i); TemplateUIDs[i] = "$INVALID"; }
			}
			foreach (var t in projectSettings.TemplatesByUID) {
				if (TemplateUIDs.Contains(t.Key)) { continue; }
				// found new one
				if (invalidIndices.Count > 0) { TemplateUIDs[invalidIndices.Pop()] = t.Key; }
				else { TemplateUIDs.Add(t.Key); }
			}
			while (TemplateUIDs.Count > 0 && TemplateUIDs[TemplateUIDs.Count - 1] == "$INVALID") {
				TemplateUIDs.RemoveAt(TemplateUIDs.Count - 1);
			}

			invalidIndices.Clear();
			for (int i = TextureUIDs.Count - 1; i >= 0; --i) {
				if (!projectSettings.TexturesByID.ContainsKey(TextureUIDs[i])) { invalidIndices.Push(i); TextureUIDs[i] = "$INVALID"; }
			}
			foreach (var t in projectSettings.TexturesByID) {
				if (TextureUIDs.Contains(t.Key)) { continue; }
				// found new one
				if (invalidIndices.Count > 0) { TextureUIDs[invalidIndices.Pop()] = t.Key; }
				else { TextureUIDs.Add(t.Key); }
			}
			while (TextureUIDs.Count > 0 && TextureUIDs[TextureUIDs.Count - 1] == "$INVALID") {
				TextureUIDs.RemoveAt(TextureUIDs.Count - 1);
			}
		}

		/*
		/// <summary>
		/// Clear the whole level - this is NOT undoable!
		/// only called by level editor now
		/// </summary>
		public static void ClearChunks() {
			//level.Generate();
			if (tempBloxels == null) { tempBloxels = new Bloxel[chunkSizeA2 * chunkSizeA2P2]; }

			if (CurLevel != null) { GameObject.DestroyImmediate(CurLevel.gameObject); }
			updateChunkAuto = true;
			CurLevel.Chunks.Clear();
			ChangedBloxelsCount = 0;
			Bounds = new Bounds();
		}
		*/

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// BLOXELS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public Bloxel GetStandardBloxel(Position3 pos) {
			return settings.GetStandardBloxel(pos);
		}

		public Bloxel GetStandardBloxel(int x, int y, int z) {
			return settings.GetStandardBloxel(new Position3(x, y, z));
		}

		public void GetStandardBloxels(Bloxel[] tempBloxels, int cpx, int cpy, int cpz, int start, int end) {
			//var sb = CurLevelSettings.GetStandardBloxel();
			//var s = size - start;
			//s = s * s * s;
			//for (int i = 0; i < s; ++i) { tempBloxels[i] = sb; }
			for (int y = start, i = 0; y < end; ++y) {
				for (int z = start; z < end; ++z) {
					for (int x = start; x < end; ++x, ++i) {
						tempBloxels[i] = settings.GetStandardBloxel(new Position3(x + cpx, y + cpy, z + cpz));
					}
				}
			}
		}

		//

		/// <summary>
		/// Get a bloxel template according its pos
		/// </summary>
		/// <param name="posAbs"></param>
		/// <returns></returns>
		public Bloxel GetBloxel(Position3 posAbs) {
			var chunkPos = new Position3(
				(posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize,
				(posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize,
				(posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize
			);
			if (chunks.TryGetValue(chunkPos, out var chunk) && chunk.changedBloxelsNum != 0) { // TODO: optimize - how?
				var posRel = posAbs - (chunkPos * chunkSize);
				int posRelIndex = GetBloxelRelPosIndex(posRel);
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					// BloxelPosIndices are sorted, so we can stop searching as soon as it's bigger posRelIndex
					if (chunk.changedBloxelsDataPosIndices[i] >= posRelIndex) {
						if (chunk.changedBloxelsDataPosIndices[i] == posRelIndex) {
							return chunk.GetChangedBloxelAt(i);
						}
						break;
					}
				}
			}
			return GetStandardBloxel(posAbs);
		}

		/// <summary>
		/// Get a bloxel template according its pos
		/// </summary>
		/// <param name="x">x pos</param>
		/// <param name="y">y pos</param>
		/// <param name="z">z pos</param>
		/// <returns></returns>
		public Bloxel GetBloxel(int x, int y, int z) {
			return GetBloxel(new Position3(x, y, z));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="posAbs"></param>
		/// <param name="faceDir"></param>
		/// <returns></returns>
		public  bool GetBloxelTexSide(Position3 posAbs, int faceDir, out SideExtraData extraSideData) {
			Position3 chunkPos = new Position3(
				(posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize,
				(posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize,
				(posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize
			);

			if (Chunks.TryGetValue(chunkPos, out var curChunk) && curChunk.changedBloxelsNum != 0) { // TODO: optimize - how?
				var posRel = posAbs - (chunkPos * chunkSize);
				int posRelIndex = GetBloxelRelPosIndex(posRel);

				var tsIndex = posRelIndex * 7 + faceDir;

				// has side data
				if (curChunk.textureSideExtraDataByRelPos.TryGetValue(tsIndex, out extraSideData)) { return true; }

				for (int i = 0; i < curChunk.changedBloxelsNum; ++i) {
					// BloxelPosIndices are sorted, so we can stop searching as soon as it's bigger posRelIndex
					if (curChunk.changedBloxelsDataPosIndices[i] >= posRelIndex) {
						if (curChunk.changedBloxelsDataPosIndices[i] == posRelIndex) {
							extraSideData = new SideExtraData(curChunk.GetChangedBloxelAt(i).textureIdx, 0, 0);
							return false;
						}
						break;
					}
				}
			}
			extraSideData = new SideExtraData(GetStandardBloxel(posAbs.x, posAbs.y, posAbs.z).textureIdx, 0, 0);
			return false;
		}

		/// <summary>
		/// Get a List of all bloxels - don't do it often
		/// </summary>
		/// <param name="sort">sort the list by position (y, then x, then z) or not</param>
		/// <returns></returns>
		public List<DumpBloxel> DumpAllBloxels(bool sort = false) {
			var dump = new List<DumpBloxel>(changedBloxelsCount);
			for (var iter = Chunks.GetEnumerator(); iter.MoveNext();) {
				var chunk = iter.Current.Value;
				var cpos = chunk.Pos * chunkSize;
				for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
					var pos = GetBloxelRelPosByIndex(chunk.changedBloxelsDataPosIndices[i]);
					if (pos.x >= 0 && pos.y >= 0 && pos.z >= 0 && pos.x < chunkSize && pos.y < chunkSize && pos.z < chunkSize) {
						// print(pos + " + " + cpos + " = " + (pos + cpos));
						dump.Add(new DumpBloxel(pos + cpos, chunk.GetChangedBloxelAt(i)));
					}
				}
			}
			if (sort) {
				dump.Sort((a, b) => {
					return (a.pos.y != b.pos.y) ? (b.pos.y - a.pos.y) : (a.pos.x != b.pos.x) ? (b.pos.x - a.pos.x) : b.pos.z - a.pos.z;
				});
			}
			return dump;
		}

		/// <summary>
		/// Change a big range of bloxels via list
		/// </summary>
		/// <param name="dump">a list of dumped bloxel data</param>
		public void ApplyBloxelDump(List<DumpBloxel> dump, bool updateChunks) {
			if (dump == null || dump.Count == 0) { return; }
			if (updateChunks) { UpdateSeveralChunksStart(); }
			for (var iter = dump.GetEnumerator(); iter.MoveNext();) {
				var dv = iter.Current;
				ChangeBloxel(dv.pos, dv.template, dv.textureIndex);
			}
			if (updateChunks) { UpdateSeveralChunksEnd(); }
		}

		public List<DumpBloxelSide> DumpAllTexSides() {
			var dump = new List<DumpBloxelSide>();
			for (var iter = Chunks.Values.GetEnumerator(); iter.MoveNext();) {
				var chunk = iter.Current;
				if (chunk.textureSideExtraDataByRelPos.Count > 0) {
					var dvs = new DumpBloxelSide(chunk.Pos, chunk.textureSideExtraDataByRelPos.Count);
					int i = 0;
					for (var iter_side = chunk.textureSideExtraDataByRelPos.GetEnumerator(); iter_side.MoveNext(); ++i) {
						dvs.Set(i, iter_side.Current.Key, iter_side.Current.Value);
					}
					dump.Add(dvs);
				}
			}
			return dump;
		}

		public void ApplySideDataDump(List<DumpBloxelSide> dump, bool updateChunks) {
			if (dump == null || dump.Count == 0) { return; }
			if (updateChunks) { UpdateSeveralChunksStart(); }
			var countChunks = dump.Count;
			for (int i = 0; i < countChunks; ++i) {
				var countTS = dump[i].relativePos.Length;
				var chunk = Chunks[dump[i].chunkPos];
				for (int j = 0; j < countTS; ++j) {
					chunk.textureSideExtraDataByRelPos.Add(dump[i].relativePos[j], dump[i].sideData[j]);
				}
			}
			if (updateChunks) { UpdateSeveralChunksEnd(); }
		}

		//

		/// <summary>
		/// Change a single side's texture
		/// </summary>
		/// <param name="posAbs"></param>
		/// <param name="faceDir"></param>
		/// <param name="textureIndex"></param>
		/// <returns>returns the old data if the side was actually changed, otherwise standard</returns>
		// TODO check if it only changes the uvset -> do not save if it's not inner faceDir!
		public SideExtraData ChangeBloxelTextureSide(Position3 posAbs, int faceDir, SideExtraData newData) {
			Position3 chunkPos = new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize),
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize),
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize)
			);

			BloxelChunk curChunk;
			if (!Chunks.TryGetValue(chunkPos, out curChunk)) { return SideExtraData.dontChange; }
			Position3 posRel = posAbs - (chunkPos * chunkSize);
			int posRelIndex = GetBloxelRelPosIndex(posRel);

			var sideDataDict = curChunk.textureSideExtraDataByRelPos;
			var tsIndex = posRelIndex * 7 + faceDir;
			var bloxel = GetBloxel(posAbs);
			if (bloxel.template.Type == null || newData.uv >= bloxel.template.Type.innerDirHandlings.Length) { newData.uv = 0; }
			SideExtraData oldSideData;

			if (newData.IsTextureOnlySet(bloxel.textureIdx)) {
				// a) this bloxel has this TEXTURE for standard already?
				if (!sideDataDict.ContainsKey(tsIndex)) {
					return SideExtraData.dontChange; // do nothing if there's no changed data anyway
				}
				// face already with extra data textured? -> remove data or modify it
				oldSideData = sideDataDict[tsIndex];
				if (oldSideData.IsSomethingChanged()) { sideDataDict[tsIndex] = newData.CopyWithTexture(-1); } // TODO DEBUG should be curData.textureIndex instead of -1?
				else if (!oldSideData.Compared(ref newData)) { sideDataDict.Remove(tsIndex); }
			}
			else {
				// or b) some data is different from bloxel
				if (sideDataDict.ContainsKey(tsIndex)) {
					// face already has extra data?
					var curData = sideDataDict[tsIndex];
					if (!curData.GetsChangedBy(ref newData)) { return SideExtraData.dontChange; } // already the same tex data -> do nothing
					// otherwise -> change this data
					oldSideData = curData;
					sideDataDict[tsIndex] = curData.ChangeBy(ref newData);
				}
				else {
					// face does not have extra data already? -> add new data
					oldSideData = new SideExtraData(bloxel.textureIdx, 0, 0);
					curChunk.textureSideExtraDataByRelPos.Add(tsIndex, newData);
				}
			}

			if (newData.Is(ref oldSideData)) {
				return SideExtraData.dontChange;
			}

			//var oldTex = ProjectSettings.Textures[oldSideData.ti < 0 ? bloxel.textureIdx : oldSideData.ti];
			//var newTex = ProjectSettings.Textures[newData.ti < 0 ? bloxel.textureIdx : newData.ti];
			var oldTex = GetTextureByIndex(oldSideData.ti < 0 ? bloxel.textureIdx : oldSideData.ti);
			var newTex = GetTextureByIndex(newData.ti < 0 ? bloxel.textureIdx : newData.ti);
			var updateUVOnly = oldTex.texAtlasIdx == newTex.texAtlasIdx;

			// update chunk if necessary
			if (updateUVOnly && Application.isPlaying) {
				if (updateChunkAuto) { UpdateChunkUVsOnly(curChunk); }
				else { tempChunksToGetUpdatedUVsOnly.Add(curChunk); }
			}
			else {
				if (updateChunkAuto) { UpdateChunk(curChunk, true); }
				else { tempChunksToGetUpdated.Add(curChunk); }
			}

			return oldSideData;
		}

		public SideExtraData RemoveBloxelTextureSideData(Position3 posAbs, int faceDir) {
			Position3 chunkPos = new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize),
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize),
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize)
			);

			BloxelChunk curChunk;
			if (!Chunks.TryGetValue(chunkPos, out curChunk)) { Debug.Log("could not get chunk"); return SideExtraData.dontChange; }
			Position3 posRel = posAbs - (chunkPos * chunkSize);
			int posRelIndex = GetBloxelRelPosIndex(posRel);

			var sideDataDict = curChunk.textureSideExtraDataByRelPos;
			var tsIndex = posRelIndex * 7 + faceDir;
			if (!sideDataDict.TryGetValue(tsIndex, out var oldSideData)) {
				Debug.Log("no data anyway");
				return SideExtraData.dontChange; // do nothing if there's no changed data anyway
			}
			
			var bloxel = GetBloxel(posAbs);
			sideDataDict.Remove(tsIndex);

			var updateUVOnly = oldSideData.ti < 0 || GetTextureByIndex(oldSideData.ti).texAtlasIdx == GetTextureByIndex(bloxel.textureIdx).texAtlasIdx;

			// update chunk if necessary
			if (updateUVOnly && Application.isPlaying) {
				if (updateChunkAuto) { UpdateChunkUVsOnly(curChunk); }
				else { tempChunksToGetUpdatedUVsOnly.Add(curChunk); }
			}
			else {
				if (updateChunkAuto) { UpdateChunk(curChunk, true); }
				else { tempChunksToGetUpdated.Add(curChunk); }
			}

			return oldSideData;
		}

		public bool RemoveBloxelTextureSideData(Position3 posAbs) {
			Position3 chunkPos = new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize),
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize),
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize)
			);

			BloxelChunk curChunk;
			if (!Chunks.TryGetValue(chunkPos, out curChunk)) { return false; }
			Position3 posRel = posAbs - (chunkPos * chunkSize);
			int posRelIndex = GetBloxelRelPosIndex(posRel);

			var changed = false;
			var updateUVOnly = !Application.isPlaying;
			var meshIdx = GetTextureByIndex(GetBloxel(posAbs).textureIdx).texAtlasIdx;
			for (int i = 0; i < 7; ++i) {
				var tsIndex = posRelIndex * 7 + i;
				if (curChunk.textureSideExtraDataByRelPos.TryGetValue(tsIndex, out var oldSideData)) {
					curChunk.textureSideExtraDataByRelPos.Remove(tsIndex);
					changed = true;
					updateUVOnly = updateUVOnly && (oldSideData.ti < 0 || GetTextureByIndex(oldSideData.ti).texAtlasIdx == meshIdx);
				}
			}
			if (!changed) { return false; }

			// update chunk if necessary
			if (updateUVOnly && Application.isPlaying) {
				if (updateChunkAuto) { UpdateChunkUVsOnly(curChunk); } else { tempChunksToGetUpdatedUVsOnly.Add(curChunk); }
			}
			else {
				if (updateChunkAuto) { UpdateChunk(curChunk, true); } else { tempChunksToGetUpdated.Add(curChunk); }
			}
			return true;
		}

		/// <summary>
		///  Create or change a specific bloxel
		/// </summary>
		/// <param name="posAbs"></param>
		/// <param name="templateIndex"></param>
		/// <param name="textureIndex"></param>
		/// <returns></returns>
		public bool ChangeBloxel(Position3 posAbs, BloxelTemplate template, int textureIndex) {
			if (template == null) { template = projectSettings.AirTemplate; }
			return ChangeBloxel(posAbs, template, textureIndex, 0, 0, 0);
		}

		bool ChangeBloxel(Position3 posAbs, BloxelTemplate template, int textureIndex, int padX, int padY, int padZ) {
			if (template.IsAir) { textureIndex = 0; }
#if UNITY_EDITOR
			if (padX < -1 || padX > 1) { UnityEngine.Debug.LogError("Padding (X) too big!"); }
			if (padY < -1 || padY > 1) { UnityEngine.Debug.LogError("Padding (Y) too big!"); }
			if (padZ < -1 || padZ > 1) { UnityEngine.Debug.LogError("Padding (Z) too big!"); }
#endif
			Position3 chunkPos = new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize) - padX,
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize) - padY,
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize) - padZ
			);
			// test with standard bloxel in any case
			var standardBloxel = GetStandardBloxel(posAbs);
			if (!Chunks.TryGetValue(chunkPos, out var curChunk)) {
				if (standardBloxel.Is(template, textureIndex)) { return false; }
			}
			Position3 posRel = posAbs - (chunkPos * chunkSize);
			int posRelIndex = GetBloxelRelPosIndex(posRel);
			var num = curChunk != null ? curChunk.changedBloxelsNum : 4;
			var dataPosInd = ListPool<int>.Create(num);
			var dataBloxTex = ListPool<int>.Create(num);
			var dataBloxTmp = ListPool<int>.Create(num);
			if (curChunk != null && curChunk.changedBloxelsNum > 0) {
				// get the already created additional bloxel data
				dataPosInd.AddRange(curChunk.changedBloxelsDataPosIndices);
				dataBloxTex.AddRange(curChunk.changedBloxelsTextureIndices);
				dataBloxTmp.AddRange(curChunk.changedBloxelsTemplateIndices);
			}
			var templateIdx = TemplateUIDs.IndexOf(template.UID);
			var templateIdxBefore = templateIdx;
			var textureBefore = textureIndex;
			if (curChunk != null && /*curChunk.Exists &&*/ dataPosInd.Contains(posRelIndex)) {
				// box was changed already?
				int dpi = dataPosInd.IndexOf(posRelIndex);
				templateIdxBefore = dataBloxTmp[dpi];
				textureBefore = dataBloxTex[dpi];
				if (standardBloxel.Is(template, textureIndex)) {
					// is standard bloxel? -> just remove this additional data
					dataPosInd.RemoveAt(dpi);
					dataBloxTex.RemoveAt(dpi);
					dataBloxTmp.RemoveAt(dpi);
					curChunk.changedBloxelsNum--;
					if (padX == 0 && padY == 0 && padZ == 0) { changedBloxelsCount--; }
				}
				else if (dataBloxTmp[dpi] == templateIdx && (templateIdx <= 1 || dataBloxTex[dpi] == textureIndex)) { // (dataBloxel[dpi].Is(template, textureIndex)) {
					// is same bloxel data already existing?
					return false;
				}
				else {
					// is new box type? -> replace it
					dataBloxTmp[dpi] = templateIdx;
					dataBloxTex[dpi] = textureIndex;
				}
			}
			else if (!standardBloxel.Is(template, textureIndex)) {
				if (curChunk == null) { curChunk = InstantiateChunk(chunkPos); }
				// box was never changed before? -> add it maybe
				int insertIndex = 0;
				while (insertIndex < curChunk.changedBloxelsNum && dataPosInd[insertIndex] < posRelIndex) {
					++insertIndex;
				}
				//templateBefore = sv.template;
				templateIdxBefore = TemplateUIDs.IndexOf(standardBloxel.template.UID);
				textureBefore = standardBloxel.textureIdx;
				// insert it sorted already
				dataPosInd.Insert(insertIndex, posRelIndex);
				dataBloxTmp.Insert(insertIndex, templateIdx);
				dataBloxTex.Insert(insertIndex, textureIndex);
				curChunk.changedBloxelsNum++;
				if (padX == 0 && padY == 0 && padZ == 0) { changedBloxelsCount++; }
			}
			else {
				// nothing changed
				return false;
			}
			// chunk gets the additional data
			curChunk.changedBloxelsDataPosIndices = dataPosInd.ToArray();
			curChunk.changedBloxelsTextureIndices.Clear();
			curChunk.changedBloxelsTextureIndices.AddRange(dataBloxTex); // = dataBloxTex.ToArray();
			curChunk.changedBloxelsTemplateIndices.Clear();
			curChunk.changedBloxelsTemplateIndices.AddRange(dataBloxTmp); // = dataBloxTex.ToArray();
			ListPool<int>.Dispose(ref dataPosInd);
			ListPool<int>.Dispose(ref dataBloxTex);
			ListPool<int>.Dispose(ref dataBloxTmp);
			
			var updateUVOnly = templateIdxBefore == templateIdx;
			if (updateUVOnly && Application.isPlaying) {
				var oldTex = GetTextureByIndex(textureBefore);
				var newTex = GetTextureByIndex(textureIndex);
				updateUVOnly = oldTex.texAtlasIdx == newTex.texAtlasIdx
									&& Mathf.Approximately(oldTex.noiseStrength, newTex.noiseStrength)
									&& Mathf.Approximately(oldTex.noiseScale, newTex.noiseScale);
			}

			// somewhere at the limits of the chunk? -> change neighbour chunk(s) too
			if (padX == 0 && padY == 0 && padZ == 0) {
				var csm1 = chunkSize - 1;
				if (posRel.x % csm1 == 0) {
					var x = posRel.x == 0 ? 1 : -1;
					ChangeBloxel(posAbs, template, textureIndex, x, 0, 0);
					if (posRel.y % csm1 == 0) { ChangeBloxel(posAbs, template, textureIndex, x, posRel.y == 0 ? 1 : -1, 0); }
					if (posRel.z % csm1 == 0) { ChangeBloxel(posAbs, template, textureIndex, x, 0, posRel.z == 0 ? 1 : -1); }
				}
				if (posRel.y % csm1 == 0) {
					var y = posRel.y == 0 ? 1 : -1;
					ChangeBloxel(posAbs, template, textureIndex, 0, y, 0);
					if (posRel.z % csm1 == 0) { ChangeBloxel(posAbs, template, textureIndex, 0, y, posRel.z == 0 ? 1 : -1); }
				}
				if (posRel.z % csm1 == 0) {
					var z = posRel.z == 0 ? 1 : -1;
					ChangeBloxel(posAbs, template, textureIndex, 0, 0, z);
					if (posRel.x % csm1 == 0 && posRel.y % csm1 == 0) { ChangeBloxel(posAbs, template, textureIndex, posRel.x == 0 ? 1 : -1, posRel.y == 0 ? 1 : -1, z); }
				}
			}

			// update chunk?
			//Debug.Log(sv.textureIndex + " " + textureIndex + "   " + BloxelTextures.Count);
			if (updateUVOnly && Application.isPlaying) {
				if (updateChunkAuto) { UpdateChunkUVsOnly(curChunk); } else { tempChunksToGetUpdatedUVsOnly.Add(curChunk); }
			}
			else {
				if (updateChunkAuto) { UpdateChunk(curChunk, true); } else { tempChunksToGetUpdated.Add(curChunk); }
			}

			return true;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// CHUNKS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public BloxelChunk InstantiateChunk(Position3 pos) {
			var chunkGO = new GameObject("chunk " + pos) { tag = settings.ChunkTag, layer = settings.ChunkLayer };
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				GameObjectUtility.SetStaticEditorFlags(chunkGO, settings.ChunkEditorFlags);
			}
#endif
			chunkGO.transform.SetParent(transform);
			var chunk = chunkGO.AddComponent<BloxelChunk>();
			chunkGO.transform.localPosition = chunk.WorldPos = pos.ToVector(chunkSize);
			chunkGO.transform.localRotation = Quaternion.identity;
			chunkGO.transform.localScale = Vector3.one;
			chunk.Pos = pos;
			chunk.lvl = this;
			chunks[pos] = chunk; //.Add(pos, chunk);

			return chunk;
		}

		public Position3 GetChunkPos(Position3 posAbs) {
			return new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize),
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize),
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize)
			);
		}

		public BloxelChunk GetChunk(Position3 posAbs) {
			var chunkPos = new Position3(
				((posAbs.x < 0 ? (posAbs.x - chunkSize + 1) : posAbs.x) / chunkSize),
				((posAbs.y < 0 ? (posAbs.y - chunkSize + 1) : posAbs.y) / chunkSize),
				((posAbs.z < 0 ? (posAbs.z - chunkSize + 1) : posAbs.z) / chunkSize)
			);
			if (!Chunks.TryGetValue(chunkPos, out var curChunk)) { return null; }
			return curChunk;
		}

		// USE THESE WHEN PROCESSING MANY BLOXELS AT ONCE!
		public void UpdateSeveralChunksStart() {
			updateChunkAuto = false;
			tempChunksToGetUpdated.Clear();
			tempChunksToGetUpdatedUVsOnly.Clear();
		}

		public void UpdateChunkAsPartFromSeveral(BloxelChunk chunk, bool uvsOnly = false) {
			if (uvsOnly) { tempChunksToGetUpdatedUVsOnly.Add(chunk); }
			else { tempChunksToGetUpdated.Add(chunk); }
		}

		// USE THESE WHEN PROCESSING MANY BLOXELS AT ONCE!
		public void UpdateSeveralChunksEnd() {
			if (tempChunksToGetUpdatedUVsOnly.Count > 0) {
				tempChunksToGetUpdatedUVsOnly.ExceptWith(tempChunksToGetUpdated);
				for (var iter = tempChunksToGetUpdatedUVsOnly.GetEnumerator(); iter.MoveNext();) {
					UpdateChunkUVsOnly(iter.Current);
				}
				tempChunksToGetUpdatedUVsOnly.Clear();
			}
			if (tempChunksToGetUpdated.Count > 0) {
				for (var iter = tempChunksToGetUpdated.GetEnumerator(); iter.MoveNext();) {
					UpdateChunk(iter.Current, true);
				}
				tempChunksToGetUpdated.Clear();
			}
			updateChunkAuto = true;
		}

		void DestroyChunkMeshes(BloxelChunk chunk) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				var prefStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (chunk.rm != null) { 
					if (prefStage != null) { AssetDatabase.RemoveObjectFromAsset(chunk.rm); }
					chunk.rm.Clear();
					chunk.mf.sharedMesh = null;
				}
				if (chunk.cm != null) {
					if (prefStage != null) { AssetDatabase.RemoveObjectFromAsset(chunk.cm); }
					chunk.cm.Clear();
					chunk.mc.sharedMesh = null;
				}
			}
#endif
		}
		void DestroyChunk(BloxelChunk chunk) {
			Chunks.Remove(chunk.Pos);
			if (!Application.isPlaying) {
#if UNITY_EDITOR
				EditorSceneManager.MarkSceneDirty(chunk.gameObject.scene);
				var prefStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (prefStage != null) {
					if (chunk.rm != null) { AssetDatabase.RemoveObjectFromAsset(chunk.rm); }
					if (chunk.cm != null) { AssetDatabase.RemoveObjectFromAsset(chunk.cm); }
				}
				GameObject.DestroyImmediate(chunk.gameObject);
#else
				GameObject.DestroyImmediate(chunk.gameObject);
#endif
			}
			else {
				GameObject.Destroy(chunk.gameObject);
			}
		}

		/// <summary>
		/// Update the content of a chunk
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="checkTexSideData"></param>
		/// <returns></returns>
		BloxelChunk UpdateChunk(BloxelChunk chunk, bool checkTexSideData) {
#if UNITY_EDITOR
			if (chunk == null) { UnityEngine.Debug.LogError("Chunk must not be null!"); return null; }
#endif
			//tempBloxelPosRel.Reset();
			if (chunk.changedBloxelsNum == 0 && chunk.textureSideExtraDataByRelPos.Count == 0) {
				DestroyChunk(chunk);
				return null;
			}

			// normal chunk data
			// TODO can be optimized?
			var cp = chunk.Pos * chunkSize;
			GetStandardBloxels(tempBloxels, cp.x, cp.y, cp.z, -1, chunkSizeA1);

			// load additional bloxels data (aka user data)
			for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
				int vi = chunk.changedBloxelsDataPosIndices[i];
				tempBloxels[vi] = chunk.GetChangedBloxelAt(i);
			}

			// create chunk mesh data
			var bmdR = new BloxelMeshData(chunkSizeA2P2 * 2, true, projectSettings.TexAtlases.Length); // 2nd parameter: max number of submeshes
			var bmdC = new BloxelMeshData(chunkSizeA2P2 * 2, false, 1);
			int vcr = 0, vcc = 0; // current vertex count
			int index = (chunkSize + 3) * chunkSizeA2 + 1;
			for (tempBloxelPosRel.y = 0; tempBloxelPosRel.y < chunkSize; ++tempBloxelPosRel.y, index += chunkSizeA2M2) {
				for (tempBloxelPosRel.z = 0; tempBloxelPosRel.z < chunkSize; ++tempBloxelPosRel.z, index += 2) {
					for (tempBloxelPosRel.x = 0; tempBloxelPosRel.x < chunkSize; ++tempBloxelPosRel.x, ++index) {
						// build the vertex data
						//Debug.Log(index + " " + (tempBloxels[index].template == null));
						tempBloxels[index].Build(bmdR, index, chunk, ref vcr);
						tempBloxels[index].BuildCollider(bmdC, index, chunk, ref vcc, !Application.isPlaying);
					}
				}
			}

			if (vcr == 0 && chunk.MeshesExist) {
				if (chunk.changedBloxelsNum == 0) { DestroyChunk(chunk); return null; }
				else { DestroyChunkMeshes(chunk); }
			}
			else if (vcr > 0 || chunk.MeshesExist) {
#if UNITY_EDITOR
				var prefStage = PrefabStageUtility.GetCurrentPrefabStage();
#endif
				if (!chunk.MeshesExist) {
					chunk.lvl = this;
					// build chunk, create/change the mesh
					chunk.rm = new Mesh() { name = chunk.name + " render mesh" };
					chunk.cm = new Mesh() { name = chunk.name + " collision mesh" };
#if UNITY_EDITOR
					if (!Application.isPlaying) {
						if (prefStage != null) {
#if UNITY_2020_2_OR_NEWER
							AssetDatabase.AddObjectToAsset(chunk.rm, prefStage.assetPath);
							AssetDatabase.AddObjectToAsset(chunk.cm, prefStage.assetPath);
#else
							AssetDatabase.AddObjectToAsset(chunk.rm, prefStage.prefabAssetPath);
							AssetDatabase.AddObjectToAsset(chunk.cm, prefStage.prefabAssetPath);
#endif
						}
					}
#endif
					chunk.mf = chunk.gameObject.GetOrAddComponent<MeshFilter>();
					chunk.mr = chunk.gameObject.GetOrAddComponent<MeshRenderer>();
					chunk.mr.shadowCastingMode = settings.ShadowCastingMode;
					chunk.mc = chunk.gameObject.GetOrAddComponent<MeshCollider>();
					//var bounds = Bounds;
					//bounds.Encapsulate(new Bounds(chunk.transform.position + Vector3.one * chunkSizeA1 * 0.5f, Vector3.one * chunkSizeA1));
					//Bounds = bounds;
				}
				// mesh creation
				chunk.rm.Clear();
				bmdR.AssignToMesh(chunk.rm);
				chunk.AssignMaterials(ProjectSettings.TexAtlases, bmdR);
				chunk.rm.colors32 = new Color32[0]; // TODO?
				chunk.mf.sharedMesh = chunk.rm;
				chunk.cm.Clear();
				bmdC.AssignToMesh(chunk.cm);
				chunk.mc.sharedMesh = null; // still needed for resetting?
				chunk.mc.sharedMesh = chunk.cm;
			}
			bmdR.Dispose();
			bmdC.Dispose();

			return chunk;
		}

		/// <summary>
		/// Update the content of a chunk, but only collisions
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="checkTexSideData"></param>
		/// <returns></returns>
		public void UpdateChunkCollisionOnly(BloxelChunk chunk, bool forEditor) {
#if UNITY_EDITOR
			if (chunk == null) { UnityEngine.Debug.LogError("Chunk must not be null!"); return; }
#endif
			//tempBloxelPosRel.Reset();
			if (chunk.changedBloxelsNum == 0 && chunk.textureSideExtraDataByRelPos.Count == 0) {
				return;
			}

			// normal chunk data
			// TODO can be optimized?
			var cp = chunk.Pos * chunkSize;
			GetStandardBloxels(tempBloxels, cp.x, cp.y, cp.z, -1, chunkSizeA1);

			// load additional bloxels data (aka user data)
			for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
				int vi = chunk.changedBloxelsDataPosIndices[i];
				tempBloxels[vi] = chunk.GetChangedBloxelAt(i);
			}

			// create chunk mesh data
			var bmdC = new BloxelMeshData(chunkSizeA2P2 * 2, false, 1);
			int vcc = 0; // current vertex count
			int index = (chunkSize + 3) * chunkSizeA2 + 1;
			for (tempBloxelPosRel.y = 0; tempBloxelPosRel.y < chunkSize; ++tempBloxelPosRel.y, index += chunkSizeA2M2) {
				for (tempBloxelPosRel.z = 0; tempBloxelPosRel.z < chunkSize; ++tempBloxelPosRel.z, index += 2) {
					for (tempBloxelPosRel.x = 0; tempBloxelPosRel.x < chunkSize; ++tempBloxelPosRel.x, ++index) {
						// build the vertex data
						tempBloxels[index].BuildCollider(bmdC, index, chunk, ref vcc, forEditor);
					}
				}
			}

			if (vcc > 0 && chunk.MeshesExist) {
				// mesh creation
				chunk.cm.Clear();
				bmdC.AssignToMesh(chunk.cm);
				chunk.mc.sharedMesh = null; // still needed for resetting?
				chunk.mc.sharedMesh = chunk.cm;
			}
			bmdC.Dispose();

			return;
		}

		public int ClearAllChunksExtraTexSideData(System.Action<Position3, int, SideExtraData> OnRemoveTexSideData) {
			int removedCount = 0;
			for (var iter = chunks.Values.GetEnumerator(); iter.MoveNext();) {
				removedCount += ClearChunkExtraTexSideData(iter.Current, OnRemoveTexSideData);
			}
			return removedCount;
		}

		public int ClearChunkExtraTexSideData(BloxelChunk chunk, System.Action<Position3, int, SideExtraData> OnRemoveTexSideData) {
#if UNITY_EDITOR
			if (chunk == null) { UnityEngine.Debug.LogError("Chunk must not be null!"); return 0; }
#endif
			int count = 0;
			// remove extra tex side data
			if (chunk.textureSideExtraDataByRelPos.Count > 0) {
				for (var iter = chunk.textureSideExtraDataByRelPos.Keys.GetEnumerator(); iter.MoveNext();) {
					var idx = iter.Current;
					int dir = idx % 7;
					int bloxIdx = idx / 7;
					var pv = chunk.Pos * chunkSize + GetBloxelRelPosByIndex(bloxIdx);
					var bloxel = GetBloxel(pv);
					var vt = bloxel.template;
					SideExtraData vts = chunk.textureSideExtraDataByRelPos[idx];
					// GetBloxelTexSide(pv, dir, out vts);
					// if (vt.ID == "SLOPE") Debug.Log(vt.ID + " d" + dir + ": " + vts.Changes(bloxel, dir) + " uv" + vts.uvSet + " r" + vts.rotation + " t" + vts.textureIndex);
					if (!vts.Changes(ref bloxel, dir)) {
						// the bloxel tex side extra data is the same as the bloxel?
						if (OnRemoveTexSideData != null) { OnRemoveTexSideData(pv, dir, chunk.textureSideExtraDataByRelPos[idx]); }
						tempTexSideDataToRemove.Add(idx);
					}
					else if (dir != 6) {
						var pn = chunk.Pos * chunkSize + GetBloxelRelPosByIndex(bloxIdx + neighbourDirAdd[dir]);
						if (vt.IsEmptyTo(GetBloxel(pn).template, dir)) {
							// Debug.Log(dir + " " + bloxel.ID + " " + neighbour.ID + "   " + pv + " " + Bloxels.neighbourDirAdd[dir] + " " + pn);
							if (OnRemoveTexSideData != null) { OnRemoveTexSideData(pv, dir, chunk.textureSideExtraDataByRelPos[idx]); }
							tempTexSideDataToRemove.Add(idx);
						}
					}
					else {
						// inner data -> remove when this bloxel doesn't have it
						if (vt.HasInnerData()) {
							//Debug.Log("is inner data " + vts.uvSet + " " +  vt.Type.innerDirHandlings.Length + " "+ vt.ID);
							if (vts.uv > 0 && vt.Type.innerDirHandlings.Length <= vts.uv) {
								vts.uv = 0;
								if (!vts.Changes(ref bloxel, 6)) {
									if (OnRemoveTexSideData != null) { OnRemoveTexSideData(pv, dir, chunk.textureSideExtraDataByRelPos[idx]); }
									tempTexSideDataToRemove.Add(idx);
								}
								else {
									// wrong uvset -> CHANGE data (if it doesn't make the tex side data obsolete anyway)
									chunk.textureSideExtraDataByRelPos[idx] = vts;
									//UnityEngine.Debug.Log("update " + vt.ID);
								}
							}
						}
						else {
							if (OnRemoveTexSideData != null) { OnRemoveTexSideData(pv, dir, chunk.textureSideExtraDataByRelPos[idx]); }
							tempTexSideDataToRemove.Add(idx);
						}
					}
				}
				if (tempTexSideDataToRemove.Count > 0) {
					tempTexSideDataToRemove.ForEach(idx => {
						chunk.textureSideExtraDataByRelPos.Remove(idx);
					});
					count = tempTexSideDataToRemove.Count;
					tempTexSideDataToRemove.Clear();
				}
			}
			return count;
		}

		/// <summary>
		/// Updates only the UVs, is more efficient than calling UpdateChunk in this case.
		/// BUT calls UpdateChunk if necessary!
		/// </summary>
		/// <param name="chunk"></param>
		void UpdateChunkUVsOnly(BloxelChunk chunk) {
#if UNITY_EDITOR
			if (chunk == null) { UnityEngine.Debug.LogError("Chunk must not be null"); }
#endif
			if (!chunk.MeshesExist) { UpdateChunk(chunk, false); return; }
			//if (chunk.rm == null) { return; }
			
			if (chunk.changedBloxelsNum == 0 && chunk.textureSideExtraDataByRelPos.Count == 0) {
				DestroyChunk(chunk);
				return;
			}

			// normal chunk data
			// TODO can be optimized? - in this case do i need temporary data?
			var cp = chunk.Pos * chunkSize;
			GetStandardBloxels(tempBloxels, cp.x, cp.y, cp.z, -1, chunkSizeA1);

			// load additional bloxels data (aka user data)
			for (int i = 0; i < chunk.changedBloxelsNum; ++i) {
				int vi = chunk.changedBloxelsDataPosIndices[i];
				tempBloxels[vi] = chunk.GetChangedBloxelAt(i);
			}

			// change chunk mesh data
			var uvs = chunk.rm.uv;
			int vc = 0; // current vertex count
			int index = (chunkSize + 3) * chunkSizeA2 + 1;
			for (tempBloxelPosRel.y = 0; tempBloxelPosRel.y < chunkSize; ++tempBloxelPosRel.y, index += chunkSizeA2M2) {
				for (tempBloxelPosRel.z = 0; tempBloxelPosRel.z < chunkSize; ++tempBloxelPosRel.z, index += 2) {
					for (tempBloxelPosRel.x = 0; tempBloxelPosRel.x < chunkSize; ++tempBloxelPosRel.x, ++index) {
						tempBloxels[index].ChangeUVs(uvs, index, chunk, ref vc);
					}
				}
			}

			chunk.rm.SetUVs(0, uvs);
			Debug.Log("Update UVs");
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// HELPERS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// public helper methods

		public int GetBloxelRelPosIndex(Position3 pos) {
#if UNITY_EDITOR
			// TODO remove
			if (pos.x < -1 || pos.x > chunkSizeA1 || pos.y < -1 || pos.y > chunkSizeA1 || pos.z < -1 || pos.z > chunkSizeA1) {
				Debug.LogWarning("rel pos doesn't fit " + pos);
			}
#endif
			return (pos.y + 1) * chunkSizeA2P2 + (pos.z + 1) * chunkSizeA2 + (pos.x + 1);
		}

		public int GetBloxelRelPosIndex(int x, int y, int z) {
			return (y + 1) * chunkSizeA2P2 + (z + 1) * chunkSizeA2 + (x + 1);
		}

		public Position3 GetBloxelRelPosByIndex(int index) { // TODO public now
			int y = (index / chunkSizeA2P2) - 1;
			int z = ((index / chunkSizeA2) % chunkSizeA2) - 1;
			int x = (index % chunkSizeA2) - 1;
			return new Position3(x, y, z);
		}

		public BloxelTexture GetTextureByIndex(int index) {
			var has = projectSettings.TexturesByID.TryGetValue(TextureUIDs[index], out var tex) ? tex : null;
			if (!has) { return null; }
			return has;
		}

		public bool TryGetTextureByIndex(int index, out BloxelTexture tex) {
			if (projectSettings.TexturesByID.TryGetValue(TextureUIDs[index], out tex)) { return true; }
			return false;
		}
	}

}