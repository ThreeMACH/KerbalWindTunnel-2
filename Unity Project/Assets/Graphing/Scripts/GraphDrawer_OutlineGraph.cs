using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Graphing.Meshing;
using Graphing.Extensions;

namespace Graphing
{
    public partial class GraphDrawer
    {
        protected void OutlineGraphSetup()
        {
            if (mesh == null)
                mesh = new Mesh();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            if (TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material = outlineGraphMaterial;
            ignoreZScalePos = true;
            Vector3 scale = transform.localScale;
            scale.z = 0;
            transform.localScale = scale;
            transform.localEulerAngles = new Vector3(0, 180, 0);
        }

        protected int DrawOutlineGraph(OutlineMask outlineMask, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            const float tessTolerance = 1 / 16f;
            // TODO: Most of this is working, but wrong. E.G. Shouldn't need a new mesh every bounds changed, just shift the mesh position.
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs) || redrawReasons.Key == typeof(BoundsChangedEventArgs))
            {
                if (pass == 0)
                {
                    if (outlineMask.Values.Length > 0)
                    {
                        SurfMeshGeneration.ConstructQuadSurfMesh(outlineMask.Values, outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax, mesh, false);
                        QuadTessellator tessellator = new QuadTessellator(mesh);
                        tessellator.SubdivideForDegeneracy(Mathf.Abs(outlineMask.Values.Max() - outlineMask.Values.Min()), tessTolerance);
                        mesh.SetVertices(tessellator.Vertices.ToList());
                        mesh.SetIndices(tessellator.Indices.ToList(), MeshTopology.Triangles, 0);
                        mesh.SetUVs(0, tessellator.Coords.ToList());
                        //mesh.SetUVs(2, GenerateVertexData(tessellator.Coords, tessellator.Heights, v => v.z).ToList());
                        mesh.SetUVs(1, GenerateVertexData(tessellator.Coords, tessellator.Heights, outlineMask.MaskCriteria).ToList());
                    }
                    else
                        mesh.Clear();
                }
                pass = 1;
            }
            if (forceRegenerate || pass == 1 || redrawReasons.Key == typeof(MaskCriteriaChangedEventArgs))
            {
                if (pass != 2)
                {
                    List<Vector4> coords = new List<Vector4>(), heights = new List<Vector4>();
                    mesh.GetUVs(0, coords);
                    mesh.GetUVs(1, heights);
                    //mesh.SetUVs(1, GenerateVertexData(coords, heights, outlineMask.MaskCriteria).ToList());
                }
                pass = 2;
            }
            if (forceRegenerate || pass != 0 || redrawReasons.Key == typeof(MaskLineOnlyChangedEventArgs))
            {
                // TODO: Implement the filled area.
            }
            if (forceRegenerate || redrawReasons.Key == typeof(ColorChangedEventArgs))
            {
                // TODO: Does the material need to be made unique if these values are different than shared?
                outlineGraphMaterial.SetColor("_OutlineColor", outlineMask.color);
            }
            if (forceRegenerate || redrawReasons.Key == typeof(LineWidthChangedEventArgs))
            {
                outlineGraphMaterial.SetFloat("_OutlineThickness", outlineMask.LineWidth);
            }
            return pass;
        }

        public static List<Vector3[]> GenerateOutlines(OutlineMask outlineMask, Mesh mesh = null)
        {
            bool nullMesh = mesh == null;
            if (nullMesh)
                mesh = new Mesh();

            SurfMeshGeneration.ConstructQuadSurfMesh(outlineMask.Values, outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax, mesh, false);
            //EnhancedSubMesh enhancedMesh = new EnhancedSubMesh(mesh);
            //enhancedMesh.SubdivideQuadMesh(subdivisionLevel);
            //enhancedMesh.UpdateMesh();
            //MeshExtensions.QuadToTris4(mesh);
            List<Vector3[]> lines = Extensions.MeshIntersectionOutline.GetMeshIntersectionOutline(mesh, outlineMask.MaskCriteria);

            /*mesh.Clear();
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Color> vertexColorMap = new List<Color>();
            bool red = false;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                vertices.Add(lines[i][lines[i].Length - 1]);
                vertexColorMap.Add(!red ? UnityEngine.Color.blue : UnityEngine.Color.red);
                red = !red;
                for (int v = lines[i].Length - 2; v >= 0; v--)
                {
                    vertices.Add(lines[i][v]);
                    vertexColorMap.Add(!red ? UnityEngine.Color.blue : UnityEngine.Color.red);
                    red = !red;
                    indices.Add(vertices.Count - 2);
                    indices.Add(vertices.Count - 1);
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.SetColors(vertexColorMap);*/

            //LineRenderer[] renderers = GetComponentsInChildren<LineRenderer>().Where(r => r.gameObject != gameObject).ToArray();

            if (nullMesh)
                Destroy(mesh);

            return lines;
        }
    }
}
