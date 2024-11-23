using System;
using System.Linq;
using UnityEngine;
using Graphing.Meshing;
using System.Collections.Generic;

namespace Graphing
{
    public partial class GraphDrawer
    {
        protected void SurfGraphSetup()
        {
            if (mesh == null)
                mesh = new Mesh();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            Material axisMaterial = grapher.GetComponentsInChildren<AxisUI>().FirstOrDefault(a=>a.Use == AxisUI.AxisDirection.Color && a.Contains(this))?.AxisMaterial ?? Instantiate(surfGraphMaterial);
            ShaderMaterial = axisMaterial;
            transform.localEulerAngles = new Vector3(0, 180, 0);
        }

        protected int DrawSurfGraph(SurfGraph surfGraph, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            // TODO: Most of this is working, but wrong. E.G. Shouldn't need a new mesh every bounds changed, just shift the mesh position.
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs) || redrawReasons.Key == typeof(BoundsChangedEventArgs))
            {
                if (pass == 0)
                {
                    SurfMeshGeneration.ConstructQuadSurfMesh(surfGraph.Values, surfGraph.XMin, surfGraph.XMax, surfGraph.YMin, surfGraph.YMax, mesh, false);
                    QuadTessellator tessellator = new QuadTessellator(mesh);
                    tessellator.SubdivideForDegeneracy();
                    mesh.SetVertices(tessellator.Vertices.ToList());
                    mesh.SetIndices(tessellator.Indices.ToList(), MeshTopology.Triangles, 0);
                    mesh.SetUVs(0, tessellator.Coords.ToList());
                    mesh.SetUVs(1, GenerateVertexData(tessellator.Coords, tessellator.Heights, surfGraph.ColorFunc).ToList());
                }
                pass = 1;
            }
            if (forceRegenerate || pass == 1 || redrawReasons.Key == typeof(ColorChangedEventArgs))
            {
                if (pass != 2)
                {
                    if (pass == 1)
                        SetColorMapProperties(mesh, surfGraph);
                    //_material.SetRange(surfGraph.CMin, surfGraph.CMax);
                    pass = 2;
                }
            }
            return pass;
        }

        protected IEnumerable<Vector4> GenerateVertexData(IEnumerable<Vector4> quadCoords, IEnumerable<Vector4> quadHeights, Func<Vector3, float> dataFunc)
        {
            IEnumerator<Vector4> quadCoordsEnumerator = quadCoords.GetEnumerator();
            IEnumerator<Vector4> quadHeightsEnumerator = quadHeights.GetEnumerator();
            while (quadCoordsEnumerator.MoveNext() && quadHeightsEnumerator.MoveNext())
            {
                yield return Vector4Operator(quadCoordsEnumerator.Current, quadHeightsEnumerator.Current);
            }
            Vector4 Vector4Operator(Vector4 coords, Vector4 heights)
                => new Vector4(
                    dataFunc(new Vector3(coords.x, coords.y, heights.x)),
                    dataFunc(new Vector3(coords.x + coords.z, coords.y, heights.y)),
                    dataFunc(new Vector3(coords.x + coords.z, coords.y + coords.w, heights.z)),
                    dataFunc(new Vector3(coords.x, coords.y + coords.w, heights.w)));
        }
    }
}
