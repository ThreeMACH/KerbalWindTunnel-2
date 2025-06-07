using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Graphing.Meshing;
using Graphing.Extensions;

namespace Graphing
{
    public class OutlineGraphDrawer : MeshGraphDrawer, GraphDrawer.IOutlineMaterialUser, GraphDrawer.ISingleMaterialUser
    {
        static readonly Unity.Profiling.ProfilerMarker s_outlineMarker = new Unity.Profiling.ProfilerMarker("GraphDrawer.Draw(Outline)");

        [SerializeField]
        protected Material outlineGraphMaterial;
        [SerializeField]
        [HideInInspector]
        private bool outlineMaterialIsUnique = false;

        protected OutlineMask outlineMask;

        public override int MaterialSet => 2;

        public Material OutlineGraphMaterial
        {
            get
            {
                outlineMaterialIsUnique = true;
                outlineGraphMaterial = Instantiate(outlineGraphMaterial);
                return outlineGraphMaterial;
            }
            set => SetOutlineMaterialInternal(value);
        }
        public Material SharedOutlineGraphMaterial
        {
            get => outlineGraphMaterial;
            set => SetOutlineMaterialInternal(value);
        }
        void IOutlineMaterialUser.SetOutlineMaterialInternal(Material value) => SetOutlineMaterialInternal(value);
        protected internal virtual void SetOutlineMaterialInternal(Material value)
        {
            if (outlineMaterialIsUnique)
                Destroy(outlineGraphMaterial);
            outlineMaterialIsUnique = false;
            outlineGraphMaterial = value;
            if (TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material = value;
        }

        protected override void Setup()
        {
            base.Setup();
            outlineMask = (OutlineMask)graph;
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

        void ISingleMaterialUser.InitializeMaterial(Material material)
            => InitializeMaterial(material);
        protected internal void InitializeMaterial(Material material)
            => outlineGraphMaterial = material;

        protected override int DrawInternal(IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            s_outlineMarker.Begin();
            const float tessTolerance = 1 / 16f;
            // TODO: Most of this is working, but wrong. E.G. Shouldn't need a new mesh every bounds changed, just shift the mesh position.
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs) || redrawReasons.Key == typeof(BoundsChangedEventArgs))
            {
                if (pass == 0)
                {
                    if (outlineMask.Values.Length > 0)
                    {
                        SurfMeshGeneration.ConstructQuadSurfMesh(mesh, outlineMask.Values, outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax, false);
                        QuadTessellator tessellator = new QuadTessellator(mesh);
                        tessellator.SubdivideForDegeneracy(Mathf.Abs(outlineMask.Values.Max() - outlineMask.Values.Min()), tessTolerance);
                        mesh.SetVertices(tessellator.Vertices.ToList());
                        mesh.SetIndices(tessellator.Indices.ToList(), MeshTopology.Triangles, 0);
                        mesh.SetUVs(0, tessellator.Coords.ToList());
                        mesh.SetUVs(1, GenerateVertexData(tessellator.Coords, tessellator.Heights, outlineMask.MaskCriteria).ToList());
                        //mesh.SetUVs(2, GenerateVertexData(tessellator.Coords, tessellator.Heights, v => v.z).ToList());
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
                    //mesh.SetUVs(2, GenerateVertexData(coords, heights, v => v.z).ToList());
                }
                pass = 2;
            }
            if (forceRegenerate || pass != 0 || redrawReasons.Key == typeof(MaskLineOnlyChangedEventArgs))
            {
                // TODO: Implement the filled area through the material.
            }
            if (forceRegenerate || redrawReasons.Key == typeof(ColorChangedEventArgs))
            {
                // Calling a method on the OutlineGraphMaterial property inherently makes the material unique.
                OutlineGraphMaterial.SetColor("_OutlineColor", outlineMask.color);
            }
            if (forceRegenerate || redrawReasons.Key == typeof(LineWidthChangedEventArgs))
            {
                // Calling a method on the OutlineGraphMaterial property inherently makes the material unique.
                OutlineGraphMaterial.SetFloat("_OutlineThickness", outlineMask.LineWidth);
            }
            s_outlineMarker.End();
            return pass;
        }

        public static List<Vector3[]> GenerateOutlines(OutlineMask outlineMask, Mesh mesh = null)
        {
            bool nullMesh = mesh == null;
            if (nullMesh)
                mesh = new Mesh();

            SurfMeshGeneration.ConstructQuadSurfMesh(mesh, outlineMask.Values, outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax, false);
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (outlineMaterialIsUnique)
                Destroy(outlineGraphMaterial);
        }
    }
}
