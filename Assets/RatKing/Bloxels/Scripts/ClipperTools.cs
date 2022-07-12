//#define DEBUG_LOG
//#define DEBUG_DISPLAY_LINES

using UnityEngine;
using System.Collections.Generic;

using ClipperLib;
using ClipperPath = System.Collections.Generic.List<ClipperLib.IntPoint>;
using ClipperPaths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace RatKing.Bloxels {

	static public class ClipperTools{ 

		static readonly int clipperResolution = 1000;
		//
		static float clipperResolutionInverse = 1f / clipperResolution;

		// POINT TO POINT TRANSFORM HELPERS

		public static float Vec3TimesVec3(Vector3 v, Vector3 d) { return v.x * d.x + v.y * d.y + v.z * d.z; }

		// vec3 -> int point
		delegate IntPoint GetIntPointFromVec3(Vector3 v, int i);
		static IntPoint GetIntPointFromVec3XZ(Vector3 v, int i) { return new IntPoint(Mathf.RoundToInt(i * v.x), Mathf.RoundToInt(i * v.z)); }
		static IntPoint GetIntPointFromVec3XY(Vector3 v, int i) { return new IntPoint(Mathf.RoundToInt(i * v.x), Mathf.RoundToInt(i * v.y)); }
		static IntPoint GetIntPointFromVec3YZ(Vector3 v, int i) { return new IntPoint(Mathf.RoundToInt(i * v.y), Mathf.RoundToInt(i * v.z)); }
		static readonly GetIntPointFromVec3[] GetIntPointFromVec3Delegates = new GetIntPointFromVec3[] { GetIntPointFromVec3XZ, GetIntPointFromVec3XY, GetIntPointFromVec3YZ, GetIntPointFromVec3XZ, GetIntPointFromVec3XY, GetIntPointFromVec3YZ };

		// public enum FaceDir { Top, Back, Right, Bottom, Front, Left }

		// vec2 -> vec3
		public delegate Vector3 GetVec3FromVec2WidthDepth(float depth, Vector2 v);
		public delegate Vector3 GetVec3FromVec2(Vector2 v);
		public static readonly GetVec3FromVec2WidthDepth[] GetVec3FromVec2Delegates = new GetVec3FromVec2WidthDepth[] {
			(d, v) => new Vector3(-v.x, 0f, v.y),
			(d, v) => new Vector3(v.x, v.y, 0f),
			(d, v) => new Vector3(0f, v.x, v.y),
			(d, v) => new Vector3(v.x, d, v.y),
			(d, v) => new Vector3(-v.x, v.y, d),
			(d, v) => new Vector3(d, -v.x, v.y) };
		public static readonly GetVec3FromVec2[] GetVec3FromVec2DelegatesCentered = new GetVec3FromVec2[] {
			v => new Vector3(v.x, 0.5f, v.y),
			v => new Vector3(v.x, v.y, -0.5f),
			v => new Vector3(-0.5f, v.x, v.y),
			v => new Vector3(v.x, -0.5f, v.y),
			v => new Vector3(v.x, v.y, 0.5f),
			v => new Vector3(0.5f, v.x, v.y) };
		public static readonly GetVec3FromVec2[] GetVec3FromVec2DelegatesCenteredInverse = new GetVec3FromVec2[] {
			v => new Vector3(v.y, 0.5f, v.x), // <>
			v => new Vector3(v.y, v.x, -0.5f), // <>
			v => new Vector3(-0.5f, v.y, v.x), // <>
			v => new Vector3(v.x, -0.5f, v.y),
			v => new Vector3(v.x, v.y, 0.5f),
			v => new Vector3(0.5f, v.x, v.y) };
		 // TODO: ^ correct, merge
		 
		// vec3 -> vec2
		public delegate Vector2 GetVec2FromVec3(Vector3 v);
		public static readonly GetVec2FromVec3[] GetVec2FromVec3Delegates = new GetVec2FromVec3[] {
			v => new Vector2(v.x, v.z),
			v => new Vector2(v.x, v.y),
			v => new Vector2(-v.z, v.y),
			v => new Vector2(v.x, -v.z),
			v => new Vector2(-v.x, v.y),
			v => new Vector2(v.z, v.y) };

		//

		static readonly ClipperPaths fullWallPaths = new List<List<IntPoint>>(new[] { new List<IntPoint>(new[] {
			new IntPoint(-clipperResolution / 2, -clipperResolution / 2),
			new IntPoint(clipperResolution / 2, -clipperResolution / 2),
			new IntPoint(clipperResolution / 2, clipperResolution / 2),
			new IntPoint(-clipperResolution / 2, clipperResolution / 2) }) });
		static readonly Clipper clipper = new Clipper();

		//

		/// <summary>
		/// Create a clipper face for a bloxel
		/// </summary>
		/// <param name="mesh">Unity Mesh to check/edit</param>
		/// <param name="faceDir">Direction which to check/edit</param>
		/// <returns>A path to use for further editing</returns>
		public static ClipperPaths CreateBloxelFace(Mesh mesh, BloxelUtility.FaceDir faceDir) {
			Vector3 dir = BloxelUtility.faceDirVectors[(int)faceDir];
			var GetIntPoint = GetIntPointFromVec3Delegates[(int)faceDir];
			var origVertices = mesh.vertices;
			var origTriangles = mesh.triangles;

			// collect triangles in the specific direction only
			var tris = new List<PointTriangle>();
			var triCount = origTriangles.Length;
			for (int i3 = 0; i3 < triCount; i3 += 3) {
				if (Vec3TimesVec3(origVertices[origTriangles[i3]], dir) > 0.499f &&
					Vec3TimesVec3(origVertices[origTriangles[i3+1]], dir) > 0.499f &&
					Vec3TimesVec3(origVertices[origTriangles[i3+2]], dir) > 0.499f) {
					tris.Add(new PointTriangle(origTriangles[i3], origTriangles[i3+1], origTriangles[i3+2]));
				}
			}
			triCount = tris.Count;
			if (tris.Count == 0) {
				return null;
			}

			// create 'polys', ie. lists of triangles that are connected
			var polys = CreatePointTrianglePolys(tris);

			// create NEW mesh data, split vertices for individual polygons
			// TODO: normals (needed? should be dir anyway...), uvs (needed?), etc
			List<Vector3> newVertices;
			List<int> newTriangles;
			CreateNewMeshData(polys, origVertices, out newVertices, out newTriangles);

			// collect all edges which are not shared by 2 triangles, as these are the outlines
			// then create polygons (lists of indices) out of these outline edges - can be holes too!
			var polysFromOutline = CreatePolygonsOutOfEdges(tris);

			var intPoints = newVertices.ConvertAll(nv => GetIntPoint(nv, clipperResolution));
			var clipPath = PreparePolysForClipper(polysFromOutline, intPoints);

#if DEBUG_LOG
			Debug.Log(mesh.name + " " + faceDir + " DONE. found " + triCount + " triangle(s), " + polys.Count + " poly(s), " + polysFromOutline.Count + " poly(s) from outline, " + clipPath.Count + " path(s) in clipper");
#endif
#if DEBUG_DISPLAY_LINES
			for (int p = 0; p < clipPath.Count; ++p) {
				var c = clipPath[p].Count;
				for (int i = 0; i < c; ++i) {
					Debug.DrawLine(
						new Vector3(clipPath[p][i].X * clipperResolutionInverse, 0.35f, clipPath[p][i].Y * clipperResolutionInverse),
						new Vector3(clipPath[p][(i+1)%c].X * clipperResolutionInverse, 0.35f, clipPath[p][(i+1)%c].Y * clipperResolutionInverse),
						Color.red, 30f);
				}
			}
#endif
			if (clipPath.Count == 0) {
				return null;
			}

			return clipPath;
		}

		/// <summary>
		/// create 'polys', ie. lists of triangles that are connected
		/// </summary>
		/// <param name="tris">list of triangle data</param>
		/// <param name="triCount"></param>
		/// <returns>returns the polys</returns>
		static List<List<PointTriangle>> CreatePointTrianglePolys(List<PointTriangle> tris) {
			int triCount = tris.Count;
			var polys = new List<List<PointTriangle>>(2);
			var tmpTris = new PointTriangle[triCount];
			tris.CopyTo(tmpTris);
			while (tris.Count > 0) {
				var poly = new List<PointTriangle>(1);
				var toCheck = new List<PointTriangle>(1);
				poly.Add(tris[0]);
				toCheck.Add(tris[0]);
				tris.RemoveAt(0);
				while (toCheck.Count > 0) {
					for (int i = tris.Count - 1; i >= 0; --i) {
						if (toCheck[0].SharesExactlyTwoPointsWith(tris[i])) {
							poly.Add(tris[i]);
							toCheck.Add(tris[i]);
							tris.RemoveAt(i);
						}
					}
					toCheck.RemoveAt(0);
				}
				polys.Add(poly);
			}
			tris.AddRange(tmpTris);
			return polys;
		}

		/// <summary>
		/// Create NEW mesh data, split vertices for individual polygons
		/// TODO: normals (needed? should be dir), uvs (needed?), etc
		/// </summary>
		/// <param name="polys"></param>
		/// <param name="polyCount"></param>
		/// <param name="origVertices"></param>
		/// <param name="newVertices"></param>
		/// <param name="newTriangles"></param>
		static void CreateNewMeshData(List<List<PointTriangle>> polys, Vector3[] origVertices, out List<Vector3> newVertices, out List<int> newTriangles) {
			int polyCount = polys.Count;
			newVertices = new List<Vector3>(origVertices.Length);
			newTriangles = new List<int>(origVertices.Length * 3);
			var vsCount = 0;
			for (int i = 0; i < polyCount; ++i) {
				var vs_orig = new List<int>(polys[i].Count / 2); // this list holds the original vertex indices
				var vs_new = new List<int>(polys[i].Count / 2); // this list holds the new vertex indices
				for (int j = 0; j < polys[i].Count; ++j) {
					for (int p = 0; p < 3; ++p) {
						var index = vs_orig.IndexOf(polys[i][j].GetP(p));
						if (index < 0) {
							index = vs_orig.Count;
							vs_orig.Add(polys[i][j].GetP(p));
							vs_new.Add(vsCount + index);
						}
						polys[i][j].SetP(p, vsCount + index);
					}
				}
				// split vertices shared by the same polygon, but with non-adjacent triangles
				// very complicated :(
				for (int v = vs_new.Count - 1; v >= 0; --v) {
					var trisSameVert = new HashSet<PointTriangle>();
					// get triangles that share the vertex
					for (int j = polys[i].Count - 1; j >= 0; --j) {
						if (polys[i][j].HasP(vs_new[v])) {
							trisSameVert.Add(polys[i][j]);
						}
					}
					// check which triangles are adjacent
					var trisSameVertNonAdj = new List<HashSet<PointTriangle>>(6);
					for (var tsv = trisSameVert.GetEnumerator(); tsv.MoveNext(); ) {
						var put = new List<int>(1);
						for (int k = trisSameVertNonAdj.Count - 1; k >= 0; --k) {
							for (var tsvna = trisSameVertNonAdj[k].GetEnumerator(); tsvna.MoveNext();) {
								if (tsv.Current.SharesExactlyTwoPointsWith(tsvna.Current)) {
									trisSameVertNonAdj[k].Add(tsv.Current);
									put.Add(k);
									break;
								}
							}
						}
						if (put.Count == 0) {
							trisSameVertNonAdj.Add(new HashSet<PointTriangle>());
							trisSameVertNonAdj[trisSameVertNonAdj.Count - 1].Add(tsv.Current);
						}
						else if (put.Count > 1) {
							for (int j = put.Count - 1; j >= 1; --j) {
								trisSameVertNonAdj[put[0]].UnionWith(trisSameVertNonAdj[put[j]]);
								trisSameVertNonAdj.RemoveAt(put[j]);
							}
						}
					}
					// create new vertices for non-adjacent triangles that share points
					for (int j = trisSameVertNonAdj.Count - 1; j >= 1; --j) {
						// create a new vertex
						var index = vs_new.Count;
						vs_orig.Add(vs_orig[v]); // add the same vertex (pos) again
						vs_new.Add(vs_new[v]);
						for (var tsvna = trisSameVertNonAdj[j].GetEnumerator(); tsvna.MoveNext();) {
								tsvna.Current.SetP(tsvna.Current.GetPIndexByValue(vs_new[v]), vsCount + index);
						}
					}
				}
				// add to triangle list of mesh
				for (int j = 0; j < polys[i].Count; ++j) {
					newTriangles.Add(polys[i][j].GetP(0));
					newTriangles.Add(polys[i][j].GetP(1));
					newTriangles.Add(polys[i][j].GetP(2));
				}
				newVertices.AddRange(vs_orig.ConvertAll(v => origVertices[v]));
				vsCount += vs_orig.Count;
			}
#if DEBUG_DISPLAY_LINES
			foreach (var v in newVertices) {
				Debug.DrawRay(v, Random.onUnitSphere * 0.1f, Color.green, 10f);
			}
			for (int t = 0; t < newTriangles.Count; t += 3) {
				Debug.DrawLine(newVertices[newTriangles[t + 0]], newVertices[newTriangles[t + 1]], Color.yellow, 10f);
				Debug.DrawLine(newVertices[newTriangles[t + 1]], newVertices[newTriangles[t + 2]], Color.yellow, 10f);
				Debug.DrawLine(newVertices[newTriangles[t + 2]], newVertices[newTriangles[t + 0]], Color.yellow, 10f);
			}
#endif
		}

		/// <summary>
		/// collect all edges which are not shared by 2 triangles, as these are the outlines
		/// then create polygons (lists of indices) out of these outline edges - can be holes too!
		/// </summary>
		/// <param name="tris"></param>
		/// <param name="triCount"></param>
		/// <returns>a list polys represented by point indices</returns>
		static List<List<int>> CreatePolygonsOutOfEdges(List<PointTriangle> tris) {
			int triCount = tris.Count;
			// collect ALL edges
			var edges = new List<PointEdge>(triCount);
			for (int i = 0; i < triCount; ++i) {
				for (int e = 0; e < 3; ++e) {
					edges.Add(tris[i].e[e]);
				}
			}
			var edgeCount = edges.Count;

			// collect all edges which are not shared by 2 triangles, as these are the outlines
			// then create polygons (lists of indices) out of these outline edges - can be holes too!
			var tmpOutlineEdges = new List<PointEdge>(edgeCount / 2);
			for (int i = 0; i < edgeCount; ++i) {
				if (edges[i].isOutline) {
					for (int j = i + 1; j < edgeCount; ++j) {
						if ((edges[i].p1 == edges[j].p2 && edges[i].p2 == edges[j].p1) || (edges[i].p1 == edges[j].p1 && edges[i].p2 == edges[j].p2)) {
							edges[i].isOutline = false;
							edges[j].isOutline = false;
							break;
						}
					}
				}
				if (edges[i].isOutline) {
					tmpOutlineEdges.Add(edges[i]);
				}
			}
			var polysFromOutline = new List<List<int>>();
			while (tmpOutlineEdges.Count > 0) {
				var poly = new List<int>(6);
				var start = tmpOutlineEdges[0].p1;
				poly.Add(start);
				var cur = tmpOutlineEdges[0].p2;
				tmpOutlineEdges.RemoveAt(0);
				do {
					for (int i = tmpOutlineEdges.Count - 1; i >= 0; --i) {
						if (tmpOutlineEdges[i].p1 == cur) {
							poly.Add(cur);
							cur = tmpOutlineEdges[i].p2;
							tmpOutlineEdges.RemoveAt(i);
							if (cur == start) {
								break;
							}
						}
					}
				} while (cur != start);
				polysFromOutline.Add(poly);
			}

			return polysFromOutline;
		}

		/// <summary>
		/// Feed it with outline polys, get clipper paths
		/// </summary>
		/// <param name="outlinePolys"></param>
		/// <returns></returns>
		static ClipperPaths PreparePolysForClipper(List<List<int>> outlinePolys, List<IntPoint> vertices) {
			var clipPath = new ClipperPaths(outlinePolys.Count);
			for (int i = 0; i < outlinePolys.Count; ++i) {
				var c = outlinePolys[i].Count;
				clipPath.Add(new ClipperPath(c));
				for (int j = 0; j < c; ++j) {
					clipPath[i].Add(vertices[outlinePolys[i][j]]);
				}
			}
			return clipPath;
		}

		// CLIPPING TWO POLYS

		/// <summary>
		/// Takes two polys and checks if they clip
		/// </summary>
		static public bool ClipTwoPolys(ClipperPaths A, ClipperPaths B, out List<int> resTris, out List<Vector2> resPoints) {
			resTris = null;
			resPoints = null;

			var done = false;
			if (B == null || B.Count == 0) {
				Debug.Log("Second Mesh is empty - result should be full first Mesh.");
				// TODO: fill out?
				done = true;
			}
			if (A == null || A.Count == 0) {
				Debug.Log("First Mesh is empty - final result should be air!");
				// TODO: fill out?
				done = true;
			}
			if (done) {
				return false;
			}

			// clipper in Difference mode creates new polygon data
			Clipper clipper = new Clipper();
			clipper.StrictlySimple = true;
			ClipperPaths clipResult = new ClipperPaths();
			clipper.AddPaths(A, PolyType.ptSubject, true);
			clipper.AddPaths(B, PolyType.ptClip, true);
			clipper.Execute(ClipType.ctDifference, clipResult, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
			clipResult = Clipper.CleanPolygons(clipResult);
			if (clipResult.Count == 0) {
#if DEBUG_LOG
				Debug.Log("<Clipper> Aborting mesh generation: result is air!");
#endif
				return false; // no polys!
			}

			// now do the whole process of triangulating the clipper data via LibTess
			// and analyzing the outlines. in the end, we will have new polys data
			// which we can feed poly2tri, which usually does nicer triangulating than LibTess
			// (especially no superthin triangles, mostly)
			List<PointTriangle> tris;
			Vector3[] vertices;
			TriangulateViaLibTess(clipResult, out tris, out vertices);
			var polys = CreatePointTrianglePolys(tris);
			List<Vector3> newVertices;
			List<int> newTriangles;
			CreateNewMeshData(polys, vertices, out newVertices, out newTriangles);
			var polysFromOutline = CreatePolygonsOutOfEdges(tris);
			var intVertices = newVertices.ConvertAll(nv => new IntPoint(Mathf.RoundToInt(nv.x * clipperResolution), Mathf.RoundToInt(nv.z * clipperResolution)));
			var clipPath = PreparePolysForClipper(polysFromOutline, intVertices);

			// create the clipper polygons
			var clipPolys = new List<ConvertPolygon>();
			var clipHoles = new List<ConvertPolygon>();
			for (int p = 0; p < clipPath.Count; ++p) {
				var cp = new ConvertPolygon(clipPath[p], clipperResolutionInverse);
				if (cp.isHole) {
					clipHoles.Add(cp);
				}
				else {
					clipPolys.Add(cp);
				}
			}

			// connect holes with their respective polys
			for (int i = clipHoles.Count - 1; i >= 0; --i) {
				ConvertPolygon enclosing = null;
				float area = float.MaxValue;
				for (int j = clipPolys.Count - 1; j >= 0; --j) {
					if (clipPolys[j].area < area && clipHoles[i].IsInsideOf(clipPolys[j])) {
						enclosing = clipPolys[j];
						area = clipPolys[j].area;
					}
				}
				if (enclosing == null) {
					Debug.Log("Error! Malfunctioning hole on the lose!");
				}
				else {
					enclosing.AddHole(clipHoles[i]);
				}
			}
			
			TriangulateViaPoly2Tri(clipPolys, out resTris, out resPoints);

#if DEBUG_LOG
			Debug.Log("<color=yellow>=> Clipping result: " + clipPolys.Count + " poly(s) and " + clipHoles.Count + " hole(s), resulting in " + (resTris.Count / 3) + " triangle(s) with " + resPoints.Count + " vertices</color>");
#endif
			return true; // TODO!
		}

		/// <summary>
		/// makes triangles out of a poly
		/// </summary>
		/// <param name="clipPoly"></param>
		static void TriangulateViaLibTess(ClipperPaths clipPaths, out List<PointTriangle> resTris, out Vector3[] resVertices) {
			// create LibTessDotNet data -> triangulation
			var tess = new LibTessDotNet.Tess();
			//tess.NoEmptyPolygons = true;
			var c = clipPaths.Count;
			for (int i = 0; i < c; ++i) {
				var pc = clipPaths[i].Count;
				var contour = new LibTessDotNet.ContourVertex[pc];
				for (int j = 0; j < pc; ++j) {
					contour[j].Position = new LibTessDotNet.Vec3 { X = clipPaths[i][j].X * clipperResolutionInverse, Y = clipPaths[i][j].Y * clipperResolutionInverse };
				}
				tess.AddContour(contour, LibTessDotNet.ContourOrientation.Original);
			}
			tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
			resTris = new List<PointTriangle>(tess.ElementCount); // TODO: can be array, not list
			var tc3 = tess.ElementCount * 3;
			for (int i3 = 0; i3 < tc3; i3 += 3) {
				resTris.Add(new PointTriangle(tess.Elements[i3], tess.Elements[i3 + 1], tess.Elements[i3 + 2]));
			}
			resVertices = new Vector3[tess.VertexCount];
			for (int i = 0; i < tess.VertexCount; ++i) {
				var p = tess.Vertices[i].Position;
				resVertices[i] = new Vector3(p.X, 0f, p.Y);
			}
#if DEBUG_LOG
			Debug.Log("<LibTessDotNet> " + tess.ElementCount + " triangle(s) generated");
#endif
#if DEBUG_DISPLAY_LINES
			var rp = Vector3.zero; // new Vector3(Random.Range(-3f,3f),0f,Random.Range(-3f,3f));
			for (int i3 = 0; i3 < tc3; i3 += 3) {
				var v0 = tess.Vertices[tess.Elements[i3]].Position;
				var v1 = tess.Vertices[tess.Elements[i3 + 1]].Position;
				var v2 = tess.Vertices[tess.Elements[i3 + 2]].Position;
				Debug.DrawLine(rp + new Vector3(v0.X, -0.5f, v0.Y), rp + new Vector3(v1.X, -0.5f, v1.Y), Color.white, 40f);
				Debug.DrawLine(rp + new Vector3(v1.X, -0.5f, v1.Y), rp + new Vector3(v2.X, -0.5f, v2.Y), Color.white, 40f);
				Debug.DrawLine(rp + new Vector3(v2.X, -0.5f, v2.Y), rp + new Vector3(v0.X, -0.5f, v0.Y), Color.white, 40f);
			}
#endif
		}

		/// <summary>
		/// makes triangles out of a poly
		/// </summary>
		/// <param name="clipPoly"></param>
		static void TriangulateViaPoly2Tri(List<ConvertPolygon> clipPolys, out List<int> resTris, out List<Vector2> resPoints) {
#if DEBUG_LOG
			string s = "<Poly2Tri> polys/points created: ";
#endif
			int sumTrisOverall = 0;
			resTris = new List<int>();
			resPoints = new List<Vector2>();

			for (int i = clipPolys.Count - 1; i >= 0; --i) {
				var clipPoly = clipPolys[i];
				// create poly2tri data -> triangulation
				var p2tPS = new Poly2Tri.PolygonSet();
				var p2tPoints = new List<Poly2Tri.PolygonPoint>(clipPoly.points.Count);
				for (var p = clipPoly.points.GetEnumerator(); p.MoveNext(); ) {
					p2tPoints.Add(new Poly2Tri.PolygonPoint(p.Current.X, p.Current.Y));
				}
				var p2tP = new Poly2Tri.Polygon(p2tPoints);
				// add holes if necessary
				if (clipPoly.holes != null) {
#if DEBUG_LOG
					Debug.Log("---> has " + clipPoly.holes.Count + " hole(s)");
#endif
					//// TODO TODO TODO TODO ??
					for (int j = clipPoly.holes.Count - 1; j >= 0; --j) {
						var p2tHolePoints = new List<Poly2Tri.PolygonPoint>(clipPoly.points.Count);
						for (var p = clipPoly.holes[j].points.GetEnumerator(); p.MoveNext();) {
							p2tHolePoints.Add(new Poly2Tri.PolygonPoint(p.Current.X, p.Current.Y));
						}
						var p2tHole = new Poly2Tri.Polygon(p2tHolePoints);
						p2tP.AddHole(p2tHole);
					}
				}
				p2tPS.Add(p2tP);
				// make the triangles
				Poly2Tri.P2T.Triangulate(p2tPS);
				// now put the result into the lists
				var sumTriangles = 0;
				for (var p_iter = p2tPS.Polygons.GetEnumerator(); p_iter.MoveNext(); ) {
					var poly = p_iter.Current;
					// a) get points
					for (var t_iter = poly.Triangles.GetEnumerator(); t_iter.MoveNext();) {
						var tri = t_iter.Current;
						if (tri.Points[0].tempIndex < 0) { tri.Points[0].tempIndex = resPoints.Count; resPoints.Add(new Vector2(tri.Points[0].Xf, tri.Points[0].Yf)); }
						if (tri.Points[1].tempIndex < 0) { tri.Points[1].tempIndex = resPoints.Count; resPoints.Add(new Vector2(tri.Points[1].Xf, tri.Points[1].Yf)); }
						if (tri.Points[2].tempIndex < 0) { tri.Points[2].tempIndex = resPoints.Count; resPoints.Add(new Vector2(tri.Points[2].Xf, tri.Points[2].Yf)); }
					}
					// b) get triangles
					for (var t_iter = poly.Triangles.GetEnumerator(); t_iter.MoveNext();) {
						var tri = t_iter.Current;
						resTris.Add(tri.Points[0].tempIndex);
						resTris.Add(tri.Points[1].tempIndex);
						resTris.Add(tri.Points[2].tempIndex);
					}
					sumTriangles += poly.Triangles.Count * 3;
				}
#if DEBUG_LOG
				s += "(" + (sumTriangles / 3) + "/" + resPoints.Count + ") ";
#endif
				sumTrisOverall += sumTriangles;
			}
#if DEBUG_LOG
			Debug.Log(s + "-> " + (sumTrisOverall / 3) + " triangle(s) overall");
#endif
#if DEBUG_DISPLAY_LINES
			var rp = Vector3.zero; // new Vector3(Random.Range(-3f,3f),0f,Random.Range(-3f,3f));
			for (int t = 0; t < resTris.Count; t += 3) {
				int t0 = resTris[t+0], t1 = resTris[t+1], t2 = resTris[t+2];
				Debug.DrawLine(rp + new Vector3(resPoints[t0].x, -0.9f, resPoints[t0].y), rp + new Vector3(resPoints[t1].x, -0.9f, resPoints[t1].y), Color.cyan, 40f);
				Debug.DrawLine(rp + new Vector3(resPoints[t1].x, -0.9f, resPoints[t1].y), rp + new Vector3(resPoints[t2].x, -0.9f, resPoints[t2].y), Color.cyan, 40f);
				Debug.DrawLine(rp + new Vector3(resPoints[t2].x, -0.9f, resPoints[t2].y), rp + new Vector3(resPoints[t0].x, -0.9f, resPoints[t0].y), Color.cyan, 40f);
			}
#endif
		}

		/// <summary>
		/// Tests if a certain polygon path is a full wall by using Clipper
		/// which might be a bit overkill. Don't use this during runtime.
		/// </summary>
		/// <param name="paths"></param>
		/// <returns></returns>
		static public bool IsFullBloxelWall(ClipperPaths paths) {
			// clipper in Difference mode creates new polygon data
			clipper.Clear();
			clipper.StrictlySimple = true;
			ClipperPaths clipResult = new ClipperPaths();
			clipper.AddPaths(fullWallPaths, PolyType.ptSubject, true);
			clipper.AddPaths(paths, PolyType.ptClip, true);
			clipper.Execute(ClipType.ctDifference, clipResult, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
			clipResult = Clipper.CleanPolygons(clipResult);
			return clipResult.Count == 0;
		}

		// HELPER CLASSES

		/// <summary>
		/// Contains two point indices of an edge
		/// </summary>
		class PointEdge {
			public int p1;
			public int p2;
			public PointTriangle pt;
			public bool isOutline = true; // used for polygon creation
			public PointEdge(int p1, int p2, PointTriangle pt = null) { this.p1 = p1; this.p2 = p2; this.pt = pt; }
		}

		/// <summary>
		/// Contains all infos needed for a triangle, ie. three PointEdges
		/// </summary>
		class PointTriangle {
			public PointEdge[] e = new PointEdge[3];
			public PointTriangle(int p1, int p2, int p3) {
				e[0] = new PointEdge(p1, p2, this);
				e[1] = new PointEdge(p2, p3, this);
				e[2] = new PointEdge(p3, p1, this);
			}
			public bool SharesExactlyTwoPointsWith(PointTriangle o) {
				var t = 0;
				if (e[0].p1 == o.e[0].p1 || e[1].p1 == o.e[0].p1 || e[2].p1 == o.e[0].p1) t++;
				if (e[0].p1 == o.e[1].p1 || e[1].p1 == o.e[1].p1 || e[2].p1 == o.e[1].p1) t++;
				if (e[0].p1 == o.e[2].p1 || e[1].p1 == o.e[2].p1 || e[2].p1 == o.e[2].p1) t++;
				return t == 2;
			}
			public void SetP(int index, int value) {
				switch (index) {
					case 0: e[0].p1 = e[2].p2 = value; break;
					case 1: e[1].p1 = e[0].p2 = value; break;
					case 2: e[2].p1 = e[1].p2 = value; break;
				}
			}
			public int GetP(int index) {
				return e[index].p1;
			}
			public bool HasP(int value) {
				return e[0].p1 == value || e[1].p1 == value || e[2].p1 == value;
			}
			public int GetPIndexByValue(int value) {
				return
					e[0].p1 == value ? 0 :
					e[1].p1 == value ? 1 :
					e[2].p1 == value ? 2 : -1;
			}
		}

		/// <summary>
		/// A temporary helper class for polygons that need to get triangulated
		/// </summary>
		class ConvertPolygon {
			public List<IntPoint> intPoints;
			public List<Poly2Tri.Point2D> points;
			public float area;
			public bool isHole;
			public List<ConvertPolygon> holes;
			public ConvertPolygon(List<IntPoint> points, float inverseFactor) {
				var c = points.Count;
				intPoints = points;
				this.points = new List<Poly2Tri.Point2D>(c);
				long sum = 0;
				for (int i = 0; i < c; ++i) {
					this.points.Add(new Poly2Tri.Point2D(points[i].X * inverseFactor, points[i].Y * inverseFactor));
					// from http://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
					// sum < 0 means it's a hole
					sum += (points[(i+1)%c].X - points[i].X) * (points[(i+1)%c].Y + points[i].Y);
				}
				area = Mathf.Abs(sum * inverseFactor * 0.5f);
				isHole = sum >= 0;
				if (isHole) {
					points.Reverse();
				}
			}
			public bool IsInsideOf(ConvertPolygon o) {
				return Poly2Tri.PolygonUtil.PolygonContainsPolygon(o.points, null, points, null, false);
			}
			public void AddHole(ConvertPolygon o) {
				if (isHole || !o.isHole) {
					Debug.Log("Trying to add a hole to a hole");
					return;
				}
				if (holes == null) {
					holes = new List<ConvertPolygon>();
				}
				holes.Add(o);
			}
			public bool PointOverlaps() {
				var checkPoints = new List<IntPoint>(intPoints);
				if (holes != null) {
					for (int i = holes.Count - 1; i >= 0; --i) {
						checkPoints.AddRange(holes[i].intPoints);
					}
				}
				for (int i = checkPoints.Count - 1; i >= 0; --i) {
					for (int j = i - 1; j >= 0; --j) {
						if (checkPoints[i].X == checkPoints[j].X && checkPoints[i].Y == checkPoints[j].Y) {
							return true;
						}
					}
				}
				return false;
			}
		}
	}

}