using UnityEditor;
using UnityEngine;
using RatKing.Bloxels;

namespace RatKing {

	public class CursorHoverMarker {
		public Vector3 position;
		public Vector3 localScale;
		public int layer = LayerMask.NameToLayer("Default");
		public Material curMarkerMaterial;
		public Material cursorMaterial;
		public Mesh curMarkerMesh;
		public bool showPivotCursor = true;
		//
		Quaternion curMarkerRotation = Quaternion.identity;
		Mesh cursorMesh;
		Bloed bloed;

		//

		public CursorHoverMarker(Bloed bloed) {

			Material CreateTransparentMaterial() {
				if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null) {
					var mat = new Material(Shader.Find("Standard"));
					mat.SetOverrideTag("RenderType", "Transparent");
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					mat.SetInt("_ZWrite", 0);
					mat.DisableKeyword("_ALPHATEST_ON");
					mat.DisableKeyword("_ALPHABLEND_ON");
					mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					mat.EnableKeyword("_EMISSION");
					mat.renderQueue = 3000;
					return mat;
				}
				else {
					var mat = Material.Instantiate(UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.defaultParticleMaterial);
					mat.mainTexture = null;
					return mat;
				}
			}

			curMarkerMaterial = CreateTransparentMaterial();
			cursorMesh = Resources.Load<Mesh>("Bloed/Cursor3D");
			
			cursorMaterial = CreateTransparentMaterial();
			cursorMaterial.mainTexture = null;
			cursorMaterial.color = bloed.colorCursor.col;

			if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null) {
				Camera.onPreCull -= DrawWithCamera;
				Camera.onPreCull += DrawWithCamera;
			}
			else {
				UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
				UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			}
			this.bloed = bloed;
		}

		public void Destroy() {
			if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null) { Camera.onPreCull -= DrawWithCamera; }
			else { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering; }
		}

		public void SetMarker(Color color, Mesh mesh, int dir, int rot) {
			curMarkerMaterial.color = color;
			curMarkerMesh = mesh;
			curMarkerRotation = BloxelUtility.GetRotation(dir, rot);
		}

		void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera) {
			DrawWithCamera(camera);
		}

		void DrawWithCamera(Camera camera) {
			if (bloed == null || !bloed.Activated) {
				Camera.onPreCull -= DrawWithCamera;
				return;
			}
			//
			if (SceneView.currentDrawingSceneView == null || camera != SceneView.currentDrawingSceneView.camera) { return; }
			if (BloxelLevel.Current == null) { return; }
			var matrix = BloxelLevel.Current.transform.localToWorldMatrix * Matrix4x4.TRS(position, curMarkerRotation, localScale);
			Draw(curMarkerMesh, curMarkerMaterial, camera, matrix);
			if (showPivotCursor) {
				var cursorMatrix = Matrix4x4.TRS(SceneView.currentDrawingSceneView.pivot, Quaternion.identity, Vector3.one * 0.25f);
				Draw(cursorMesh, cursorMaterial, camera, cursorMatrix);
			}
		}

		void Draw(Mesh mesh, Material material, Camera camera, Matrix4x4 matrix) {
			if (mesh != null && material != null) {
				Graphics.DrawMesh(mesh, matrix, material, layer, camera);
			}
		}
	}

}