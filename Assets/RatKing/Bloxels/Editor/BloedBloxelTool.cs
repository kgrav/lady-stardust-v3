using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RatKing.Bloxels;

namespace RatKing {

	public interface BloedBloxelTool {

		bool AllowsPivotPlaceMode { get; set; }
		bool AllowsGridMode { get; set; }
		bool AllowsWireCubes { get; set; }
		bool AllowsChangingSelectedTemplate { get; set; }
		bool AllowsChangingSelectedTexture { get; set; }
		Bloed.OverwritePickInner OverwritePickInner { get; set; }
		bool RepaintSceneOnSubBloxelMouseDrag { get; set; }

		//

		void OnClick(Bloed bloed, Event evt);
		
		// return the last y
		void OnSceneGUI(Bloed bloed, SceneView view);

		void DrawHandles(Bloed bloed);

		void Hotkeys(Bloed bloed, Event evt, SceneView view);

		void SetMarkerMesh(Bloed bloed, CursorHoverMarker cursorHoverMarker);
	}

}