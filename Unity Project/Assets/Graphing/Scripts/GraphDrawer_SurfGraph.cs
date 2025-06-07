using System;
using System.Linq;
using UnityEngine;
using Graphing.Meshing;

namespace Graphing
{
    public class SurfGraphDrawer : MeshGraphDrawer, GraphDrawer.ISurfMaterialUser, GraphDrawer.ISingleMaterialUser
    {
        static readonly Unity.Profiling.ProfilerMarker s_surfgraphMarker = new Unity.Profiling.ProfilerMarker("GraphDrawer.Draw(SurfGraph)");

        [SerializeField]
        protected Material surfGraphMaterial;
        [SerializeField]
        [HideInInspector]
        private bool surfMaterialIsUnique = false;

        protected SurfGraph surfGraph;

        public override int MaterialSet => 1;

        public Material SurfGraphMaterial
        {
            get
            {
                surfMaterialIsUnique = true;
                surfGraphMaterial = Instantiate(surfGraphMaterial);
                return surfGraphMaterial;
            }
            set => SetSurfMaterialInternal(value);
        }
        public Material SharedSurfGraphMaterial
        {
            get => surfGraphMaterial;
            set => SetSurfMaterialInternal(value);
        }
        void ISurfMaterialUser.SetSurfMaterialInternal(Material value) => SetSurfMaterialInternal(value);
        protected internal virtual void SetSurfMaterialInternal(Material value)
        {
            if (surfMaterialIsUnique)
                Destroy(surfGraphMaterial);
            surfMaterialIsUnique = false;
            surfGraphMaterial = value;
            if (TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material = value;
        }

        protected override void Setup()
        {
            base.Setup();
            surfGraph = (SurfGraph)graph;
            if (mesh == null)
                mesh = new Mesh();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            if (TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material = surfGraphMaterial;

            transform.localEulerAngles = new Vector3(0, 180, 0);
        }

        void ISingleMaterialUser.InitializeMaterial(Material material)
            => InitializeMaterial(material);
        protected internal void InitializeMaterial(Material material)
            => surfGraphMaterial = material;

        protected override int DrawInternal(IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            s_surfgraphMarker.Begin();

            // TODO: Most of this is working, but wrong. E.G. Shouldn't need a new mesh every bounds changed, just shift the mesh position.
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs) || redrawReasons.Key == typeof(BoundsChangedEventArgs))
            {
                if (pass == 0)
                {
                    if (surfGraph.Values.Length > 0)
                    {
                        SurfMeshGeneration.ConstructQuadSurfMesh(mesh, surfGraph.Values, surfGraph.XMin, surfGraph.XMax, surfGraph.YMin, surfGraph.YMax, false);
                        QuadTessellator tessellator = new QuadTessellator(mesh);
                        tessellator.SubdivideForDegeneracy(Mathf.Abs(surfGraph.ZMax - surfGraph.ZMin));
                        mesh.SetVertices(tessellator.Vertices.ToList());
                        mesh.SetIndices(tessellator.Indices.ToList(), MeshTopology.Triangles, 0);
                        mesh.SetUVs(0, tessellator.Coords.ToList());
                        mesh.SetUVs(1, GenerateVertexData(tessellator.Coords, tessellator.Heights, surfGraph.ColorFunc).ToList());
                    }
                    else
                        mesh.Clear();
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
            s_surfgraphMarker.End();
            return pass;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (surfMaterialIsUnique)
                Destroy(surfGraphMaterial);
        }
    }
}
