using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public class GraphDrawerCollection : GraphDrawer, GraphDrawer.ISurfMaterialUser, GraphDrawer.IOutlineMaterialUser
    {
        static readonly Unity.Profiling.ProfilerMarker s_collectionMarker = new Unity.Profiling.ProfilerMarker("GraphDrawer.Draw(Collection)");

        protected GraphableCollection collection;

        [SerializeField]
        protected Material surfGraphMaterial;
        [SerializeField]
        protected Material outlineGraphMaterial;
        [SerializeField]
        protected Material lineVertexMaterial;
        [SerializeField]
        [HideInInspector]
        private bool surfMaterialIsUnique = false;
        [SerializeField]
        [HideInInspector]
        private bool outlineMaterialIsUnique = false;

        protected List<GraphDrawer> childDrawers = new List<GraphDrawer>();

        public override int MaterialSet => 4;

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

        void ISurfMaterialUser.SetSurfMaterialInternal(Material value) => SetSurfMaterialInternal(value);
        protected internal virtual void SetSurfMaterialInternal(Material value)
        {
            if (surfMaterialIsUnique)
                Destroy(surfGraphMaterial);
            surfMaterialIsUnique = false;
            surfGraphMaterial = value;
            if (childDrawers != null)
                foreach (ISurfMaterialUser graphDrawer in childDrawers.Where(g => g is ISurfMaterialUser).Cast<ISurfMaterialUser>())
                    graphDrawer.SetSurfMaterialInternal(value);
        }

        void IOutlineMaterialUser.SetOutlineMaterialInternal(Material value) => SetOutlineMaterialInternal(value);
        protected internal virtual void SetOutlineMaterialInternal(Material value)
        {
            if (outlineMaterialIsUnique)
                Destroy(outlineGraphMaterial);
            outlineMaterialIsUnique = false;
            outlineGraphMaterial = value;
            if (childDrawers != null)
                foreach (IOutlineMaterialUser graphDrawer in childDrawers.Where(g => g is IOutlineMaterialUser).Cast<IOutlineMaterialUser>())
                    graphDrawer.SetOutlineMaterialInternal(value);
        }

        protected override void Setup()
            => collection = (GraphableCollection)graph;

        internal void InitializeMaterials(Material surfGraphMaterial, Material outlineMaterial, Material lineVertexMaterial)
        {
            this.surfGraphMaterial = surfGraphMaterial;
            this.outlineGraphMaterial = outlineMaterial;
            this.lineVertexMaterial = lineVertexMaterial;
        }

        protected override int DrawInternal(IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            s_collectionMarker.Begin();
            if (forceRegenerate)
            {
                foreach (GraphDrawer child in childDrawers)
                    Destroy(child.gameObject);
                childDrawers.Clear();
                foreach (IGraphable graphable in collection.Graphables)
                    InstantiateChildGraphDrawer(graphable);
                return pass;
            }
            if (redrawReasons.Key == typeof(GraphElementAddedEventArgs) ||
                redrawReasons.Key == typeof(GraphElementRemovedEventArgs) ||
                redrawReasons.Key == typeof(GraphElementsAddedEventArgs) ||
                redrawReasons.Key == typeof(GraphElementsRemovedEventArgs))
            {
                foreach (EventArgs reason in redrawReasons)
                {
                    if (reason is GraphElementAddedEventArgs addedEvent)
                        InstantiateChildGraphDrawer(addedEvent.Graph);
                    else if (reason is GraphElementsAddedEventArgs multiAddEvent)
                    {
                        foreach (IGraphable newGraph in multiAddEvent.Graphs)
                            InstantiateChildGraphDrawer(newGraph);
                    }
                    else if (reason is GraphElementRemovedEventArgs removedEvent)
                        DestroyChildGraphDrawer(removedEvent.Graph);
                    else if (reason is GraphElementsRemovedEventArgs multiRemoveEvent)
                    {
                        foreach (IGraphable oldGraph in multiRemoveEvent.Graphs)
                            DestroyChildGraphDrawer(oldGraph);
                    }
                }
            }
            s_collectionMarker.End();
            return pass;
        }

        private void InstantiateChildGraphDrawer(IGraphable newGraph)
        {
            GraphDrawer childDrawer = grapher.InstantiateGraphDrawer(newGraph, transform, (surfGraphMaterial, outlineGraphMaterial, lineVertexMaterial));
            childDrawer.transform.localRotation = Quaternion.identity;
            childDrawers.Add(childDrawer);
            if (newGraph is IGraphable3 graphable3 && !(newGraph is GraphableCollection))
                ((RectTransform)childDrawer.transform).anchoredPosition3D = new Vector3(0, 0, grapher.ZOffset2D / transform.localScale.z);
        }
        
        private void DestroyChildGraphDrawer(IGraphable removedGraph)
        {
            foreach (GraphDrawer child in childDrawers.Where(drawer => drawer.graph == removedGraph))
                Destroy(child.gameObject);
            childDrawers.RemoveAll(drawer => drawer.graph == removedGraph);
        }

        public override void SetOriginAndScale(AxisUI.AxisDirection axis, float origin, float scale)
        {
            transform.localScale = Vector3.one;
            ((RectTransform)transform).anchoredPosition3D = Vector3.zero;
            foreach (GraphDrawer graphDrawer in childDrawers)
                graphDrawer.SetOriginAndScale(axis, origin, scale);
        }

        public IEnumerable<GraphDrawer> GetFlattenedCollection()
        {
            if (Graph is GraphableCollection)
            {
                IEnumerable<GraphDrawerCollection> collections = childDrawers.Where(d => d.Graph is GraphableCollection).Cast<GraphDrawerCollection>();
                IEnumerable<GraphDrawer> flattenedCollection = childDrawers.Where(d => !(d.Graph is GraphableCollection));
                foreach (GraphDrawerCollection childDrawer in collections)
                    flattenedCollection = flattenedCollection.Union(childDrawer.GetFlattenedCollection());
                return flattenedCollection;
            }
            return Enumerable.Empty<GraphDrawer>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (surfMaterialIsUnique)
                Destroy(surfGraphMaterial);
        }
    }
}
