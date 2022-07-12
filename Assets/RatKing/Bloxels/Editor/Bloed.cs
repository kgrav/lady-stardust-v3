using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;
using RatKing.Bloxels;

namespace RatKing {

	[DefaultExecutionOrder(100)]
	public partial class Bloed : EditorWindow {

		public const float TOOLS_WIDTH = 200f;

		public struct MarkData {
			public Base.Position3 pos;
			public int dir;
			public Color color;
			public MarkData(Base.Position3 pos, int dir, Color color) { this.pos = pos; this.dir = dir; this.color = color; }
			public MarkData(Base.Position3 pos, int dir) { this.pos = pos; this.dir = dir; color = new Color(1f, 0.1f, 0.1f, 0.8f); }
			public MarkData(Base.Position3 pos, Color color) { this.pos = pos; dir = -1; this.color = color; }
			public MarkData(Base.Position3 pos) { this.pos = pos; dir = -1; color = new Color(1f, 0.1f, 0.1f, 0.8f); }
		}

		[System.Serializable]
		public class ColorSetting {
			public Color col;
			Color stdCol;
			public string pref;
			public string display;
			public ColorSetting(Color col, string pref, string display) { this.col = this.stdCol = col; this.pref = pref; this.display = display; }
			public void Load() {
				if (EditorPrefs.HasKey(pref)) {
					col = Base.ColorExtras.GetFromHexa(EditorPrefs.GetString(pref), stdCol);
				}
			}
			public void Save() {
				var str = Base.ColorExtras.RGBToHex(col);
				EditorPrefs.SetString(pref, str);
			}
			public void Reset() {
				col = stdCol;
			}
		}

		[System.Serializable]
		public class NumberSetting {
			public float num;
			float stdNum;
			public string pref;
			public string display;
			public bool isInt;
			public NumberSetting(float num, string pref, string display, bool isInt = false) { this.num = this.stdNum = num; this.pref = pref; this.display = display; this.isInt = isInt; }
			public void Load() {
				if (EditorPrefs.HasKey(pref)) {
					num = isInt ? EditorPrefs.GetInt(pref, (int)stdNum) : EditorPrefs.GetFloat(pref, stdNum);
				}
			}
			public void Save() {
				if (isInt) { EditorPrefs.SetInt(pref, (int)num); }
				else { EditorPrefs.SetFloat(pref, num); }
			}
			public void Reset() {
				num = stdNum;
			}
		}

		public enum OverwritePickInner { No, AlwaysInner, AlwaysOuter, UseCurTemplate };
		public enum GridMode { X, Y, Z }
		public enum Mode { Initialize, Build, Helpers }
		static readonly string[] modeNames = new[] { "Initialize", "Build", "Helpers" };
		static readonly string[] modeNamesOneOnly = new[] { "Initialize" };
		public static readonly char[] shelfNameSeparator = { '_' };
		public static GUISkin skin = null;
		//
		[SerializeField] Texture2D btnRotations = null;
		// ONLY to be used for initialising a new level:
		[SerializeField] BloxelLevelSettings levelSettings = null;
		[SerializeField] BloxelProjectSettings projectSettings = null;
		// serialized fields:
		[SerializeField] bool activated;
		public bool Activated => activated;
		[SerializeField] bool gridVisible;
		public bool IsGridVisible => gridVisible && (curToolIdx < 0 || tools[curToolIdx].AllowsGridMode);
		[SerializeField] public GridMode gridMode = GridMode.Y;
		public GridMode ModeGrid => gridMode;
		[SerializeField] BloxelTemplate curBloxTmp;
		public BloxelTemplate CurBloxTmp => curBloxTmp;
		[SerializeField] int curBloxTexIdx = 1;
		public BloxelTexture CurBloxTex => projectSettings != null && curBloxTexIdx >= 0 ? projectSettings.Textures[curBloxTexIdx] : null;
		public int CurBloxTexIdxOfLevel => BloxelUtility.CurLevel == null ? curBloxTexIdx : BloxelUtility.CurLevel.TextureUIDs.IndexOf(projectSettings != null ? projectSettings.Textures[curBloxTexIdx].ID : "$MISSING");
		Mode curMode = Mode.Build;
		[SerializeField] string curTypeShelf = "";
		[SerializeField] string curTextureShelf = "";
		[SerializeField] bool lockCursorToGrid = true;
		[SerializeField] bool pickCurrentLevelOnly = true;
		[SerializeField] Vector3 curViewPivot;
		[SerializeField] public ColorSetting gridColor = new ColorSetting(Color.green, "BLOED_COLOR_GRID", "Grid Color");
		[SerializeField] public ColorSetting colorMarkerCube = new ColorSetting(Color.yellow, "BLOED_COLOR_MARKER_CUBE", "Marker Wire Cube");
		[SerializeField] public ColorSetting chunkBoundsColor = new ColorSetting(Color.white.WithAlpha(0.1f), "BLOED_COLOR_CHUNKS_BOUNDS", "Chunks Bounds");
		[SerializeField] public ColorSetting colorMarkerSolid = new ColorSetting(Color.yellow.WithAlpha(0.35f), "BLOED_COLOR_MARKER_SOLID", "Marker Solid");
		[SerializeField] public ColorSetting colorMarkerAir = new ColorSetting(Color.red.WithAlpha(0.25f), "BLOED_COLOR_MARKER_AIR", "Marker Air");
		[SerializeField] public ColorSetting colorMarkerTexer = new ColorSetting(Color.white, "BLOED_COLOR_MARKER_TEXER", "Marker Texer");
		[SerializeField] public ColorSetting colorMarkerPicking = new ColorSetting(Color.blue.WithAlpha(0.20f), "BLOED_COLOR_MARKER_PICKING", "Marker Picking");
		[SerializeField] public ColorSetting colorCursor = new ColorSetting(new Color(0f, 1f, 1f, 0.2f), "BLOED_COLOR_CURSOR", "Cursor");
		[SerializeField] public ColorSetting colorCursorCube = new ColorSetting(new Color(0f, 1f, 1f, 0.5f), "BLOED_COLOR_CURSOR_WIRECUBE", "Cursor Wire Cube");
		[SerializeField] public NumberSetting defaultChunkSize = new NumberSetting(8, "BLOED_DEFAULT_CHUNK_SIZE", "Default Chunk Size", true);
		[SerializeField] public NumberSetting pickMaxDistanceNormal = new NumberSetting(200f, "BLOED_NUM_PICK_DIST_NORMAL", "Pick Distance (Free Mode)");
		[SerializeField] public NumberSetting pickMaxDistanceGrid = new NumberSetting(200f, "BLOED_NUM_PICK_DIST_GRID", "Pick Distance (Grid Mode)");
		[SerializeField] public NumberSetting gridExtents = new NumberSetting(5, "BLOED_NUM_GRID_EXTENTS", "Grid Extents", true);
		//
		static GUIStyle wrappedStyle = null;
		Vector2 scrollPos = Vector2.zero;
		float toolsHeight = 100f;
		BloxelTemplate lastBloxTmp = null;
		BloedBloxelTool[] tools = new BloedBloxelTool[] {
			new BloedBloxelToolStandard(),
			new BloedBloxelToolTexer(),
			new BloedBloxelToolCuboider(),
			new BloedBloxelToolCopyAndPaste(),
			new BloedBloxelToolJoists()
		};
		int curToolIdx;
		List<MarkData> curMarked = new List<MarkData>();
		CursorHoverMarker cursorHoverMarker = null;
		bool isPicking;
		static readonly List<MeshCollider> hitMeshColliders = new List<MeshCollider>();
		static readonly List<BloxelChunk> hitChunks = new List<BloxelChunk>();
		static readonly List<GameObject> hitRootGameObjects = new List<GameObject>();
		static Bloed instance;
		Base.Position3? LastPos;
		// used by the tools too:
		public Vector3? CurWorldNormal { get; private set; } = null;
		public Vector3? CurWorldPos { get; private set; } = null;
		public Base.Position3? CurPos { get; private set; } = null;
		public bool IsPressingRMB { get; private set; } = false;
		public BloxelProjectSettings ProjectSettings => (projectSettings != null) ? projectSettings : (projectSettings = Resources.Load<BloxelProjectSettings>("Settings/ProjectSettings"));

		//

		[MenuItem("Window/Bloxels/Bloxels Editor")]
		static void Init() {
			var w = GetWindow(typeof(Bloed));
			w.titleContent.text = "Bloed";
			instance = w as Bloed;

			instance.gridColor.Load();
			instance.colorMarkerCube.Load();
			instance.chunkBoundsColor.Load();
			instance.colorMarkerSolid.Load();
			instance.colorMarkerAir.Load();
			instance.colorMarkerTexer.Load();
			instance.colorMarkerPicking.Load();
			instance.colorCursor.Load();
			instance.colorCursorCube.Load();
			//
			instance.defaultChunkSize.Load();
			instance.pickMaxDistanceNormal.Load();
			instance.pickMaxDistanceGrid.Load();
			instance.gridExtents.Load();
		}

		void OnDestroy() {
			instance = null;
		}

		void InitGUI() {
			if (skin == null) { skin = Resources.Load<GUISkin>("Bloed/bloedSkin"); }
			GUI.skin = skin;
		}

		void MakeSureItsInited() {
			InitCursorMarker();
			if (btnRotations == null) { btnRotations = Resources.Load<Texture2D>("Bloed/btnRotations"); }
			Activate(activated);
			ShowGrid(gridVisible);
			if (curBloxTmp == null) { curBloxTmp = ProjectSettings.AirTemplate; }
		}

		void InitCursorMarker() {
			if (cursorHoverMarker != null) {
				if (cursorHoverMarker.curMarkerMaterial != null) { return; }
				else { cursorHoverMarker.Destroy(); cursorHoverMarker = null; }
			}
			if (cursorHoverMarker == null) {
				cursorHoverMarker = new CursorHoverMarker(this);
				cursorHoverMarker.SetMarker(Color.black, ProjectSettings.BoxTemplate.mesh, 0, 0);
			}
		}

		void DestroyCursorMarker() {
			if (cursorHoverMarker == null) { return; }
			cursorHoverMarker.Destroy();
			cursorHoverMarker = null;
		}

		void OnEnable() {
			//Debug.Log("OnEnable bloed");
			MakeSureItsInited();
			EditorSceneManager.sceneOpened -= SceneOpenedCallback;
			EditorSceneManager.sceneOpened += SceneOpenedCallback;
			EditorSceneManager.newSceneCreated -= SceneCreatedCallback;
			EditorSceneManager.newSceneCreated += SceneCreatedCallback;
		}

		void OnDisable() {
			//Debug.Log("OnDisable bloed");
			EditorSceneManager.sceneOpened -= SceneOpenedCallback;
			EditorSceneManager.newSceneCreated -= SceneCreatedCallback;
			SceneView.duringSceneGui -= OnSceneGUI_Bloxels;
			SceneView.beforeSceneGui -= BeforeSceneGUI_Bloxels;
			DestroyCursorMarker();
		}

		void OnGUI() {
			InitGUI();

			if (wrappedStyle == null) { wrappedStyle = new GUIStyle("label") { wordWrap = true, richText = true }; }

			var serializedObject = new SerializedObject(this);
			var guiNormalColor = GUI.color;
			EditorGUIUtility.labelWidth = 100f;

			GUILayout.BeginHorizontal();
			GUI.color = activated ? Color.green : Color.red;
			if (GUILayout.Button(activated ? "Bloed is active" : "Bloed is inactive", GUILayout.Height(35f))) {
				Activate(!activated);
			}
			GUI.color = guiNormalColor;
			GUILayout.EndHorizontal();

			var oldMode = curMode;
			var newMode = BloxelUtility.CurLevel == null
					? (Mode)GUILayout.SelectionGrid(0, modeNamesOneOnly, 1, GUILayout.Height(25f))
					: (Mode)GUILayout.SelectionGrid((int)oldMode, modeNames, modeNames.Length, GUILayout.Height(25f));
			if (BloxelUtility.CurLevel != null) { curMode = newMode; }
			if (newMode != oldMode) { scrollPos = Vector2.zero; }

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			GUILayout.Space(10f);

			switch (newMode) {
				case Mode.Initialize: OnGUI_Initalize(guiNormalColor); break;
				case Mode.Build: OnGUI_Build(guiNormalColor); break;
				case Mode.Helpers: OnGUI_Helpers(guiNormalColor); break;
			}

			GUILayout.EndScrollView();

			serializedObject.ApplyModifiedProperties();
		}

		void SceneOpenedCallback(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) {
			BloxelLevel.SetCurrent(null);
			MakeSureItsInited();
			BloedUndoRedo.ResetStacks();
			curMarked.Clear();
		}

		void SceneCreatedCallback(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode) {
			//Debug.Log("scene created " + mode);
			BloxelLevel.SetCurrent(null);
			MakeSureItsInited();
			BloedUndoRedo.ResetStacks();
			curMarked.Clear();
		}

		//

		public void Activate(bool activate) {
			SceneView.duringSceneGui -= OnSceneGUI_Bloxels;
			SceneView.beforeSceneGui -= BeforeSceneGUI_Bloxels;
			if (activate) {
				SceneView.beforeSceneGui += BeforeSceneGUI_Bloxels;
				SceneView.duringSceneGui += OnSceneGUI_Bloxels;
				InitCursorMarker();
			}
			else {
				DestroyCursorMarker();
			}
			Selection.activeGameObject = null;
			activated = activate;
			SceneView.RepaintAll();
		}

		public void ShowGrid(bool showGrid) {
			this.gridVisible = showGrid;
			SceneView.RepaintAll();
		}

		public bool IsGridPositive() {
			var view = SceneView.currentDrawingSceneView;
			if (view == null) { return false; }
			var normal = gridMode == GridMode.X ? BloxelLevel.Current.transform.right : gridMode == GridMode.Y ? BloxelLevel.Current.transform.up : BloxelLevel.Current.transform.forward;
			var plane = new Plane(normal, view.pivot);
			return plane.GetSide(view.camera.transform.position);
		}

		//

		public static bool RaycastSlow(Ray ray, out RaycastHit hit, int layerMask) {
			var origStart = ray.origin;
			var dist = instance != null ? (instance.gridVisible ? instance.pickMaxDistanceGrid.num : instance.pickMaxDistanceNormal.num) : 100f;
			var physicsHas = Physics.Raycast(ray.origin, ray.direction, out var physicsHit, dist, layerMask, QueryTriggerInteraction.Ignore);
			if (!BloxelUtility.TryGetPrefabStage(out var prefabStage)) {
				hit = physicsHit;
				return physicsHas;
			}

			hit = new RaycastHit() { distance = float.MaxValue };
			hitMeshColliders.Clear();
			// PICK ALL
			foreach (var c in prefabStage.prefabContentsRoot.GetComponentsInChildren<MeshCollider>()) {
				if (!c.gameObject.activeInHierarchy) { continue; }
				if (c.sharedMesh == null) { continue; }
				if (RXLookingGlass.IntersectRayMesh(ray, c, out var _)) { hitMeshColliders.Add(c); }
			}
			// CHECK THOSE THAT WERE HIT
			var mouseDir = -ray.direction;
			for (int i = hitMeshColliders.Count - 1; i >= 0; --i) {
				var c = hitMeshColliders[i];
				var m = c.sharedMesh;
				if (RXLookingGlass.IntersectRayMesh(ray, m, c.transform.localToWorldMatrix, out var newHit)
							&& newHit.distance < hit.distance) {
					if (Vector3.Angle(newHit.normal, mouseDir) < 90f) {
						hit = newHit;
					}
					else {
						// wrong side!
						var secondTest = new Ray(newHit.point + ray.direction * 0.001f, ray.direction);
						if (RXLookingGlass.IntersectRayMesh(secondTest, m, c.transform.localToWorldMatrix, out newHit)
								&& Vector3.Angle(newHit.normal, mouseDir) < 90f
								&& Vector3.Distance(newHit.point, origStart) < hit.distance) {
							hit = newHit;
							hit.distance = Vector3.Distance(newHit.point, origStart);
						}
						else {
							hitMeshColliders.RemoveAt(i);
						}
						i = hitMeshColliders.Count;
					}
				}
				else {
					hitMeshColliders.RemoveAt(i);
				}
			}
			if (hit.distance < float.MaxValue && (!physicsHas || hit.distance < physicsHit.distance)) {
				hit.distance = Vector3.Distance(origStart, hit.point);
				return true;
			}
			hit = physicsHit;
			return physicsHas;
		}

		public bool MousecastChunks(Event evt, out RaycastHit hit, out BloxelChunk chunk, bool forceAll = false) {
			var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
			var origStart = ray.origin;
			hit = new RaycastHit() { distance = float.MaxValue };
			hitChunks.Clear();
			chunk = null;
			if (!forceAll && pickCurrentLevelOnly) {
				// PICK CURRENT
				foreach (var c in BloxelLevel.Current.Chunks) {
					if (c.Value.mc != null) { if (RXLookingGlass.IntersectRayMesh(ray, c.Value.mc, out var _)) { hitChunks.Add(c.Value); } }
					else if (c.Value.mf != null) { if(RXLookingGlass.IntersectRayMesh(ray, c.Value.mf, out var _)) { hitChunks.Add(c.Value); } }
				}
			}
			else {
				if (BloxelUtility.TryGetPrefabStage(out var prefabStage)) {
					hitRootGameObjects.Clear();
					prefabStage.scene.GetRootGameObjects(hitRootGameObjects);
					foreach (var rgo in hitRootGameObjects) { hitChunks.AddRange(rgo.GetComponentsInChildren<BloxelChunk>()); }
				}
				else {
					hitChunks.AddRange(GameObject.FindObjectsOfType<BloxelChunk>());
				}
				// PICK ALL
				for (int i = hitChunks.Count - 1; i >= 0; --i) {
					var c = hitChunks[i];
					if (c.gameObject.activeInHierarchy) {
						if (c.mc != null) { if (RXLookingGlass.IntersectRayMesh(ray, c.mc, out var _)) { continue; } }
						else if (c.mf != null) { if (RXLookingGlass.IntersectRayMesh(ray, c.mf, out var _)) { continue; } }
					}
					hitChunks.RemoveAt(i);
				}
			}
			// CHECK THOSE THAT WERE HIT
			var mouseDir = -ray.direction;
			for (int i = hitChunks.Count - 1; i >= 0; --i) {
				var c = hitChunks[i];
				var m = c.mc != null ? c.mc.sharedMesh : c.mf.sharedMesh;
				if (RXLookingGlass.IntersectRayMesh(ray, m, c.transform.localToWorldMatrix, out var newHit)
							&& newHit.distance < hit.distance) {
					if (Vector3.Angle(newHit.normal, mouseDir) < 90f) {
						hit = newHit;
						chunk = c;
					}
					else {
						// wrong side!
						var secondTest = new Ray(newHit.point + ray.direction * 0.001f, ray.direction);
						if (RXLookingGlass.IntersectRayMesh(secondTest, m, c.transform.localToWorldMatrix, out newHit)
								&& Vector3.Angle(newHit.normal, mouseDir) < 90f
								&& Vector3.Distance(newHit.point, origStart) < hit.distance) {
							hit = newHit;
							hit.distance = Vector3.Distance(newHit.point, origStart);
							chunk = c;
						}
						else {
							hitChunks.RemoveAt(i);
						}
						i = hitChunks.Count;
					}
				}
				else {
					hitChunks.RemoveAt(i);
				}
			}
			if (hit.distance < float.MaxValue) {
				hit.distance = Vector3.Distance(origStart, hit.point);
				return hit.distance < pickMaxDistanceNormal.num;
			}
			return false;
		}

		public static void MarkSceneDirty(bool resetPhysics = false, bool delayed = false) {
			if (Application.isPlaying) { return; }

			void Do() {
				if (BloxelLevel.Current == null) {
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
				}
				else if (BloxelUtility.TryGetPrefabStage(out var prefStage)) {
					if (resetPhysics) {
						// TODO this is a hack, because apparently MarkSceneDirty removes the ability to do PhysicsScene.Raycast - might get fixed someday
						var lvl = BloxelLevel.Current;
						EditorSceneManager.MarkSceneDirty(prefStage.scene);
						AssetDatabase.SaveAssets();
						BloxelLevel.SetCurrentDirectly(lvl);
						if (instance != null) { instance.Repaint(); }
					}
					else {
						EditorSceneManager.MarkSceneDirty(prefStage.scene);
					}
				}
				else {
					EditorSceneManager.MarkSceneDirty(BloxelLevel.Current.gameObject.scene);
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

		bool TryPickLevel(Event evt) {
			if (!evt.shift || IsPressingRMB || !evt.isMouse || evt.type != EventType.MouseDown || evt.button != 0) { return false; }
			if (!MousecastChunks(evt, out var _, out var chunk, true)) { return false; }
			if (chunk.lvl != BloxelLevel.Current) {
				BloxelLevel.SetCurrent(chunk.lvl);
				MarkSceneDirty();
				Repaint();
			}
			evt.Use();
			return true;
		}

		void BeforeSceneGUI_Bloxels(SceneView view) {
			// input - hotkeys
			Hotkeys_Bloxels(Event.current, view);
		}

		void OnSceneGUI_Bloxels(SceneView view) {
			Event evt = Event.current;

			if (Application.isPlaying || BloxelLevel.Current == null) { return; }
			if (PrefabUtility.IsPartOfPrefabInstance(BloxelLevel.Current)) {
				TryPickLevel(evt);
				return;
			}

			InitGUI();

			// more grid stuff
			if (IsGridVisible && lockCursorToGrid) {
				var movement = view.pivot - curViewPivot;
				switch (gridMode) {
					case GridMode.X: curViewPivot += movement - Vector3.Project(movement, BloxelLevel.Current.transform.right); break; // curViewPivot = new Vector3(curViewPivot.x, view.pivot.y, view.pivot.z); break;
					case GridMode.Y: curViewPivot += movement - Vector3.Project(movement, BloxelLevel.Current.transform.up); break; // curViewPivot = new Vector3(view.pivot.x, curViewPivot.y, view.pivot.z); break;
					case GridMode.Z: curViewPivot += movement - Vector3.Project(movement, BloxelLevel.Current.transform.forward); break; // curViewPivot = new Vector3(view.pivot.x, view.pivot.y, curViewPivot.z); break;
				}
			}
			else {
				var wp = Vec3InvTrPos(view.pivot);
				curViewPivot = new Vector3(Mathf.Floor(wp.x), Mathf.Floor(wp.y), Mathf.Floor(wp.z));
				curViewPivot = Vec3TrPos(curViewPivot + new Vector3(0.5f, 0.5f, 0.5f));
			}

			var curTool = tools[curToolIdx];
			if (curBloxTmp == null && BloxelLevel.CurrentSettings != null) { curBloxTmp = ProjectSettings.AirTemplate; }
			var toolsRect = new Rect(5f, 5f, TOOLS_WIDTH, toolsHeight + 5f);

			// STUFF

			var handlesColor = Handles.color;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

			// draw a wire cube of the current position
			if (CurPos != null && curTool.AllowsWireCubes) {
				Handles.color = colorMarkerCube.col;
				Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
				Handles.DrawWireCube(CurPos.Value.ToVector() + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 1.1f);
				Handles.matrix = Matrix4x4.identity;
				Handles.color = handlesColor;
			}

			// draw the grid
			var gridPos3 = Base.Position3.FlooredVector(Vec3InvTrPos(view.pivot));
			var gridOffset = IsGridPositive() ? 0f : 1f;
			if (IsGridVisible) { DrawGrid(gridPos3, (int)gridExtents.num, gridOffset, gridColor.col); }

			if (curTool.AllowsWireCubes) {
				Handles.color = colorCursorCube.col;
				// and a cube at the position of the pivot
				Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
				Handles.DrawWireCube(gridPos3.ToVector() + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 1.05f);
				Handles.matrix = Matrix4x4.identity;
				Handles.color = handlesColor;
			}

			if (chunkBoundsColor.col.a > 0f) { DrawChunksBounds(chunkBoundsColor.col); }

			DrawMarkedPositions();

			if (BloxelLevel.CurrentSettings == null) { return; }

			if (evt.type != EventType.Repaint && evt.type != EventType.Layout) { CurWorldPos = null; CurPos = null; }

			// don't allow using a bloxel tool in certain circumstances
			var allowClick = false;
			if (!view.camera.pixelRect.Contains(evt.mousePosition)) { CurWorldNormal = null; CurWorldPos = null; CurPos = null; }
			else if (toolsRect.Contains(evt.mousePosition)) { CurWorldNormal = null; CurWorldPos = null; CurPos = null; }
			else if (UnityEditor.Tools.current == Tool.View) { CurWorldNormal = null; CurWorldPos = null; CurPos = null; } // hand tool, moves cam view while pressing LMB
			else { allowClick = true; }

			// right mouse button pressed -> can place bloxel at camera pivot
			if (evt.isMouse && evt.button == 1) {
				if (evt.type == EventType.MouseDown) { IsPressingRMB = true; }
				else if (evt.type == EventType.MouseUp) { IsPressingRMB = false; }
			}

			var wasPicking = isPicking;
			isPicking = (curTool.AllowsChangingSelectedTemplate || curTool.AllowsChangingSelectedTexture) && evt.control;

			//bool isInner = !gridVisible && (isPicking || curBloxTmp.IsAir || !curTool.AllowsChangingSelectedTemplate);
			bool isInner = isPicking || (!IsGridVisible && (curBloxTmp.IsAir || !curTool.AllowsChangingSelectedTemplate));
			if (!IsGridVisible && curTool.OverwritePickInner != OverwritePickInner.No) {
				isInner = curTool.OverwritePickInner == OverwritePickInner.UseCurTemplate
					? curBloxTmp.IsAir
					: curTool.OverwritePickInner == OverwritePickInner.AlwaysInner;
			}

			if (TryPickLevel(evt)) {
			}
			else if (allowClick) {
				if (IsPressingRMB && !isPicking && (curTool.AllowsPivotPlaceMode && evt.shift)) {
					CurPos = gridPos3;
				}
				else if (IsGridVisible && !isPicking) {
					// GRID MODE
					var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
					var normal = gridMode == GridMode.X ? BloxelLevel.Current.transform.right : gridMode == GridMode.Y ? BloxelLevel.Current.transform.up : BloxelLevel.Current.transform.forward;
					if (new Plane(normal, Vec3TrPos(gridPos3.ToVector() + normal * gridOffset)).Raycast(ray, out var enter)) {
						if (enter < pickMaxDistanceGrid.num) {
							CurWorldNormal = normal;
							CurWorldPos = Vec3InvTrPos(ray.GetPoint(enter));
							CurPos = Base.Position3.FlooredVector(Vec3InvTrPos(ray.GetPoint(enter - 0.01f)));
						}
					}
					if (CurPos != null) { DrawGrid(CurPos.Value, 1, gridOffset, colorMarkerSolid.col); }
				}
				else {
					// RAYCAST MODE
					if (evt.type != EventType.Repaint && evt.type != EventType.Layout && curBloxTmp != null) {
						if (MousecastChunks(evt, out var hit, out var _)) {
							CurWorldNormal = hit.normal;
							CurWorldPos = Vec3InvTrPos(hit.point);
							CurPos = Base.Position3.FlooredVector(Vec3InvTrPos(hit.point + hit.normal * (isInner ? -0.01f : 0.01f)));
						}
					}
				
					//Debug.Log(CurPos);
				}
			}

			// HOVER / CLICK

			curTool.DrawHandles(this);
					
			//Debug.Log(activated + " " + (curTool != null) + " " + (curPos != null) + " " + (curBloxTmp != null) + " " + (cursorHoverMarker != null) + " " + !isPicking);
			//if (activated && curTool != null && CurPos != null && curBloxTmp != null) {
			if (curTool != null && CurPos != null && curBloxTmp != null) {
				InitCursorMarker();
				cursorHoverMarker.position = CurPos.Value.ToVector() + new Vector3(0.5f, 0.5f, 0.5f);
				if (isPicking) {
					cursorHoverMarker.localScale = Vector3.one * 1.05f;
					cursorHoverMarker.SetMarker(colorMarkerPicking.col, ProjectSettings.BoxTemplate.mesh, 0, 0);
				}
				else {
					curTool.SetMarkerMesh(this, cursorHoverMarker);
				}

				if (allowClick && !isPicking) {
					curTool.OnClick(this, evt);
					if (evt.isKey || evt.isMouse || evt.isScrollWheel) { GUI.FocusControl(null); }
				}
			}
			else if (cursorHoverMarker != null) {
				cursorHoverMarker.curMarkerMesh = null;
			}
			
			curBloxTexIdx = Mathf.Clamp(curBloxTexIdx, 1, ProjectSettings.Textures.Count - 1);

			// BEGIN GUI

			Handles.BeginGUI();
			GUILayout.BeginArea(toolsRect, "", "tool back");

			var topLabel = "";

			if (curBloxTmp != null && BloxelLevel.CurrentSettings != null && ProjectSettings.Textures != null && ProjectSettings.Textures.Count > 0) {
				topLabel += "<color=#00ff00>" + curBloxTmp.ID + "\n";
				topLabel += "" + ProjectSettings.Textures[curBloxTexIdx].ID + "</color>\n";
			}
			if (CurPos != null && BloxelLevel.Current != null && BloxelLevel.Current.Chunks != null) {
				var bloxel = BloxelLevel.Current.GetBloxel(CurPos.Value);
				topLabel += "Hovered Pos: <b>" + CurPos + "</b>\n";
				topLabel += "> " + bloxel.templateUID + "\n";
				topLabel += "> " + BloxelLevel.Current.TextureUIDs[bloxel.textureIdx];
			}
			else {
				topLabel += "\n<i>NO BLOXEL HOVERED</i>\n";
			}

			GUILayout.Label(topLabel);

			GUILayout.Space(3f);

			GUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			if (GUILayout.Button(new GUIContent("<", "Previous Tool"))) { curToolIdx = (curToolIdx - 1 + tools.Length) % tools.Length; }
			GUILayout.Label("<b>" + (curToolIdx + 1) + "/" + tools.Length + "</b>", "label centered");
			if (GUILayout.Button(new GUIContent(">", "Next Tool"))) { curToolIdx = (curToolIdx + 1 + tools.Length) % tools.Length; }
			GUILayout.Space(5f);
			GUILayout.EndHorizontal();

			curTool.OnSceneGUI(this, view);

			GUILayout.Space(5f);

			var guiNormalColor = GUI.color;
			var guiBtnColor = GUI.color = Color.white.WithAlpha(0.75f);

			// choose direction and rotation
			if (curTool.AllowsChangingSelectedTemplate && curBloxTmp != null && curBloxTmp.Type != null && curBloxTmp.Type.Templates.Count > 1) {
				var sides = (TOOLS_WIDTH - 120f) * 0.5f;
				for (int idx = 0, d = 0; d < 6; ++d) { // directions
					var idName = curBloxTmp.Type.ID + "_" + d + "_";
					var showRow = false;
					for (int r = 0; r < 4; ++r) { // check rotations
						if (projectSettings.TemplatesByUID.ContainsKey(idName + r)) { showRow = true; break; }
					}
					if (showRow) {
						GUILayout.BeginHorizontal();
						GUILayout.Space(sides);
						for (int r = 0; r < 4; ++r) { // rotations
							var rect = GUILayoutUtility.GetAspectRect(1f);
							if (projectSettings.TemplatesByUID.TryGetValue(idName + r, out var template)) {
								if (curBloxTmp.Dir == d && curBloxTmp.Rot == r) { GUI.color = Color.red; }
								if (GUI.Button(rect, GUIContent.none)) {
									curBloxTmp = template;
								}
								GUI.DrawTextureWithTexCoords(rect, btnRotations, new Rect(r / 4f, 1f - (d + 1) / 6f, 1f / 4f, 1f / 6f));
								if (curBloxTmp.Dir == d && curBloxTmp.Rot == r) { GUI.color = guiBtnColor; }
								++idx;
							}
							else {
								GUI.Box(rect, GUIContent.none);
							}
						}
						GUILayout.Space(sides);
						GUILayout.EndHorizontal();
					}
				}
			}
		
			GUI.color = guiNormalColor;


			if (evt.type == EventType.Repaint) { toolsHeight = GUILayoutUtility.GetLastRect().yMax; }

			GUILayout.EndArea();

			int ID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

			// event handling

			if (evt.type == EventType.Layout) {
				// don't allow to activate
				HandleUtility.AddDefaultControl(ID);
			}

			Handles.EndGUI();

			// END GUI

			// picking
			if (isPicking && CurPos != null && !evt.alt && evt.type == EventType.MouseDown) {
				if (curTool.AllowsChangingSelectedTemplate && evt.button == 0) {
					// LMB click = pick template
					curBloxTmp = BloxelLevel.Current.GetBloxel(CurPos.Value).template;
					Repaint();
				}
				else if (curTool.AllowsChangingSelectedTexture && evt.button == 1) {
					// RMB click = pick texture
					curBloxTexIdx = projectSettings.TextureIndicesByID[BloxelLevel.Current.GetTextureByIndex(BloxelLevel.Current.GetBloxel(CurPos.Value).textureIdx).ID];
					Repaint();
				}
			}

			// input - hotkeys

			RepaintIfNeeded(evt, view);

			if (evt.type != EventType.Repaint && evt.type != EventType.Layout) {
				LastPos = CurPos;
			}

			if (lockCursorToGrid && IsGridVisible) {
				view.pivot = curViewPivot;
			}

			if (isPicking != wasPicking) { evt.type = EventType.MouseMove; }
		}

		void DrawGrid(Base.Position3 point, int add, float gridOffset, Color color) {
			var handlesColor = Handles.color;
			var gp = point.ToVector();
			Handles.color = color;
			Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
			switch (gridMode) {
				case GridMode.X:
					for (int i = -add; i <= add + 1; ++i) {
						Handles.DrawLine(gp + new Vector3(gridOffset, i, -add - 1), gp + new Vector3(gridOffset, i, add + 2));
						Handles.DrawLine(gp + new Vector3(gridOffset, -add - 1, i), gp + new Vector3(gridOffset, add + 2, i));
					}
					break;
				case GridMode.Y:
					for (int i = -add; i <= add + 1; ++i) {
						Handles.DrawLine(gp + new Vector3(i, gridOffset, -add - 1), gp + new Vector3(i, gridOffset, add + 2));
						Handles.DrawLine(gp + new Vector3(-add - 1, gridOffset, i), gp + new Vector3(add + 2, gridOffset, i));
					}
					break;
				case GridMode.Z:
					for (int i = -add; i <= add + 1; ++i) {
						Handles.DrawLine(gp + new Vector3(i, -add - 1, gridOffset), gp + new Vector3(i, add + 2, gridOffset));
						Handles.DrawLine(gp + new Vector3(-add - 1, i, gridOffset), gp + new Vector3(add + 2, i, gridOffset));
					}
					break;
			}
			//Handles.matrix = Matrix4x4.identity;
			Handles.color = handlesColor;
		}

		void DrawChunksBounds(Color color) {
			var cur = BloxelLevel.Current;
			if (cur == null) { return; }
			Handles.color = color;
			foreach (var c in cur.Chunks.Values) {
				Handles.matrix = c.transform.localToWorldMatrix;
				Handles.DrawWireCube(Vector3.one * cur.chunkSize * 0.5f, Vector3.one * cur.chunkSize);
			}
		}

		void DrawMarkedPositions() {
			var handlesColor = Handles.color;
			Handles.matrix = BloxelLevel.Current.transform.localToWorldMatrix;
			foreach (var marked in curMarked) {
				Handles.color = marked.color;
				Handles.DrawWireCube(marked.pos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 1.08f);
				if (marked.dir == 6) {
					//	Handles.DrawWireCube(marked.pos.ToVector() + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 0.7f); // Vector3.one * 0.7f);
					for (int i = 0; i < 6; ++i) {
						Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(marked.pos, i, 0.7f, 0.7f), Color.white.WithAlpha(0.15f), Color.white);
					}
				}
				else if (marked.dir >= 0 && marked.dir < 6) {
					Handles.DrawSolidRectangleWithOutline(BloedBloxelToolTexer.GetBloxelSide(marked.pos, marked.dir, 1f, 1.05f), Color.white.WithAlpha(0.25f), Color.white);
				}
			}
			Handles.matrix = Matrix4x4.identity;
			Handles.color = handlesColor;
		}

		void RepaintIfNeeded(Event evt, SceneView view) {
			if (!activated || !UnityEditorInternal.InternalEditorUtility.isApplicationActive) { return; }

			if (evt.type == EventType.MouseDrag && view != null && (tools[curToolIdx].RepaintSceneOnSubBloxelMouseDrag || !LastPos.Equals(CurPos))) {
				view.Repaint();
				return;
			}
		}

		void Hotkeys_Bloxels(Event evt, SceneView view) {
			// https://answers.unity.com/questions/1254217/a-way-to-detect-when-the-editor-application-is-foc.html
			if (!activated || !UnityEditorInternal.InternalEditorUtility.isApplicationActive) { return; }

			if (evt.type != EventType.KeyDown) { return; }

			tools[curToolIdx].Hotkeys(this, evt, view);

			if (evt.type != EventType.KeyDown) { return; } // event could be used
			
			// undo/ redo
			if (evt.keyCode == KeyCode.Z) {
				evt.Use();
				BloedUndoRedo.Undo();
			}
			else if (evt.keyCode == KeyCode.Y) {
				evt.Use();
				BloedUndoRedo.Redo();
			}
			
			// focus
			if (evt.keyCode == KeyCode.F && view != null && BloxelLevel.Current != null) {
				evt.Use();
				if (evt.shift) { view.pivot = Vec3TrPos(new Vector3(0.5f, 0.5f, 0.5f)); }
				else if (CurPos != null) { view.pivot = Vec3TrPos(CurPos.Value.ToVector() + new Vector3(0.5f, 0.5f, 0.5f)); }
			}

			// grid
			else if (evt.keyCode == KeyCode.G) {
				evt.Use();
				if (evt.shift) {
					if (!gridVisible) { ShowGrid(true); }
					var newGM = ((int)gridMode + 1) % 3;
					gridMode = (GridMode)newGM;
				}
				else {
					ShowGrid(!gridVisible);
				}
				Repaint();
			}
			else if (evt.keyCode == KeyCode.Space) {
				if (IsGridVisible && lockCursorToGrid) {
					evt.Use();
					switch (gridMode) {
						case GridMode.X: curViewPivot += BloxelLevel.Current.transform.TransformVector(Vector3.right * (evt.shift ? -1f : 1f)); break;
						case GridMode.Y: curViewPivot += BloxelLevel.Current.transform.TransformVector(Vector3.up * (evt.shift ? -1f : 1f)); break;
						case GridMode.Z: curViewPivot += BloxelLevel.Current.transform.TransformVector(Vector3.forward * (evt.shift ? -1f : 1f)); break;
					}
					//Repaint();
				}
			}

			// bloxel quick access
			else if (evt.keyCode == KeyCode.Alpha1) {
				evt.Use();
				if (curBloxTmp == ProjectSettings.AirTemplate && lastBloxTmp != null) { curBloxTmp = lastBloxTmp; }
				else { if (curBloxTmp != ProjectSettings.BoxTemplate) lastBloxTmp = curBloxTmp; curBloxTmp = ProjectSettings.AirTemplate; }
				Repaint();
				SceneView.RepaintAll();
			}
			else if (evt.keyCode == KeyCode.Alpha2) {
				evt.Use();
				if (curBloxTmp == ProjectSettings.BoxTemplate && lastBloxTmp != null) { curBloxTmp = lastBloxTmp; }
				else { if (curBloxTmp != ProjectSettings.AirTemplate) lastBloxTmp = curBloxTmp; curBloxTmp = ProjectSettings.BoxTemplate; }
				Repaint();
				SceneView.RepaintAll();
			}
			else if (evt.keyCode == KeyCode.Tab) {
				var curBloxType = curBloxTmp.Type;
				if (curBloxType != null && curBloxType.Templates != null && curBloxType.Templates.Count > 1) {
					var idx = curBloxType.Templates.IndexOf(curBloxTmp);
					var dir = evt.shift ? -1 : 1;
					idx = (curBloxType.Templates.Count + idx + dir) % curBloxType.Templates.Count;
					curBloxTmp = curBloxType.Templates[idx];
					Repaint();
					view.Repaint();
					SceneView.RepaintAll();
				}
				evt.Use();
				UnfocusToolbar(view);
			}
		}

		public void UnfocusToolbar(SceneView view) {
			GUI.FocusControl(null);
			// TODO: this is super hacky, there should be a better way!
#if UNITY_2021_OR_NEWER
			var mp = Event.current.mousePosition;
			// from: https://forum.unity.com/threads/mouse-position-in-scene-view.250399/
			var style = (GUIStyle) "GV Gizmo DropDown";
			var ribbon = style.CalcSize( view.titleContent );
			var sv_correctSize = view.position.size;
			sv_correctSize.y -= ribbon.y; //exclude this nasty ribbon
			mp.y += sv_correctSize.y;

			EditorApplication.delayCall += () => {
				var newEvt = new Event() {
					type = EventType.MouseDown, // EventType.MouseUp,
					button = 1,
					keyCode = KeyCode.Tab,
					pointerType = PointerType.Mouse,
					delta = Vector2.zero,
					clickCount = 1,
					mousePosition = mp
				};
				view.SendEvent(newEvt);
			};
#endif
		}

		//

		public static Vector3 Vec3TrPos(float x, float y, float z) {
			return BloxelLevel.Current.transform.TransformPoint(new Vector3(x, y, z));
		}
		public static Vector3 Vec3TrPos(Vector3 v) {
			return BloxelLevel.Current.transform.TransformPoint(v);
		}
		public static Vector3 Vec3InvTrPos(float x, float y, float z) {
			return BloxelLevel.Current.transform.InverseTransformPoint(new Vector3(x, y, z));
		}
		public static Vector3 Vec3InvTrPos(Vector3 v) {
			return BloxelLevel.Current.transform.InverseTransformPoint(v);
		}
		public static Vector3 Vec3TrDir(float x, float y, float z) {
			return BloxelLevel.Current.transform.TransformDirection(new Vector3(x, y, z));
		}
		public static Vector3 Vec3TrDir(Vector3 v) {
			return BloxelLevel.Current.transform.TransformDirection(v);
		}
		public static Vector3 Vec3InvTrDir(float x, float y, float z) {
			return BloxelLevel.Current.transform.InverseTransformDirection(new Vector3(x, y, z));
		}
		public static Vector3 Vec3InvTrDir(Vector3 v) {
			return BloxelLevel.Current.transform.InverseTransformDirection(v);
		}
		public static Vector3 Vec3TrVec(float x, float y, float z) {
			return BloxelLevel.Current.transform.TransformVector(new Vector3(x, y, z));
		}
		public static Vector3 Vec3TrVec(Vector3 v) {
			return BloxelLevel.Current.transform.TransformVector(v);
		}
		public static Vector3 Vec3InvTrVec(float x, float y, float z) {
			return BloxelLevel.Current.transform.InverseTransformVector(new Vector3(x, y, z));
		}
		public static Vector3 Vec3InvTrVec(Vector3 v) {
			return BloxelLevel.Current.transform.InverseTransformVector(v);
		}
	}

}