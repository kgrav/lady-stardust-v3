using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RatKing.Base;

namespace RatKing.Bloxels {

	public struct Bloxel : System.IEquatable<Bloxel> {

		public enum BuildMode { Render, CollisionGame, CollisionEditor }

		public readonly BloxelTemplate template; // bloxel template
		public readonly string templateUID; // bloxel template UID
		public readonly int textureIdx; // texture index
		public Bloxel(BloxelTemplate template, int textureIndex) {
			//Debug.Log("bloxel with " + template.Name + " " + textureIndex);
			this.template = template;
			this.templateUID = template.UID;
			//	this.type = template.Type;
			//	this.templateIndex = -1; // TODO this.type.AllTemplates.IndexOf(template); // is slower
			this.textureIdx = textureIndex;
		}
		//
		public bool IsAir {
			//get { return BloxelTemplates[templateIndex].isAir; }
			get { return template.IsAir; }
		}
		public bool HasFullWallSide(int dir) {
			//int w = BloxelTemplates[templateIndex].walls & (1 << dir);
			//return BloxelTemplates[templateIndex].HasFullWallSide(dir % 6);
			return template.HasFullWallSide(dir % 6);
		}
		public bool HasAirSide(int dir) {
			//return BloxelTemplates[templateIndex].HasAirSide(dir % 6);
			return template.HasAirSide(dir % 6);
		}
		public void Build(BloxelMeshData bmd, int i, BloxelChunk chunk, ref int vc) {
			//BloxelTemplates[templateIndex].Build(tmd, i, ref vc);
			//if (template == null) { Debug.Log("ERROR missing template!? " + vc); return; }
			template.Build(bmd, i, textureIdx, chunk, ref vc, BuildMode.Render);
		}
		public void BuildCollider(BloxelMeshData bmd, int i, BloxelChunk chunk, ref int vc, bool forEditor) {
			//BloxelTemplates[templateIndex].Build(tmd, i, ref vc);
			//if (template == null) { Debug.Log("ERROR missing template!? " + textureIndex); return; }
			if (forEditor) { template.Build(bmd, i, textureIdx, chunk, ref vc, BuildMode.CollisionEditor); }
			else { template.ColliderTemplate.Build(bmd, i, textureIdx, chunk, ref vc, BuildMode.CollisionGame); }
		}
		public void ChangeUVs(Vector2[] uvs, int i, BloxelChunk chunk, ref int vc) {
			template.ChangeUVs(uvs, i, textureIdx, chunk, ref vc);
		}
		//
		public static bool operator ==(Bloxel a, Bloxel b) {
			//return a.templateIndex == b.templateIndex;
			return a.Is(b.template, b.textureIdx); // a.template == b.template && a.textureIndex == b.textureIndex;
		}
		public static bool operator !=(Bloxel a, Bloxel b) {
			//return a.templateIndex != b.templateIndex;
			return !a.Is(b.template, b.textureIdx); // a.template != b.template || a.textureIndex != b.textureIndex;
		}
		public bool Is(BloxelTemplate template, int textureIndex) {
			return this.template == template && (IsAir || this.textureIdx == textureIndex);
		}
		//
		public override bool Equals(object o) { return (o is Bloxel) && (Bloxel)o == this; }
		public override int GetHashCode() { return base.GetHashCode(); }
		//public override string ToString() { return "Bloxel " + templateIndex; }
		public override string ToString() { return "Bloxel " + template.UID + " " + textureIdx; }
		//
		public bool Equals(Bloxel other) { return template == other.template && textureIdx == other.textureIdx; }
	}

}