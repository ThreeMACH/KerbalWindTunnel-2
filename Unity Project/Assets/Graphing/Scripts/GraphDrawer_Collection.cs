using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public partial class GraphDrawer
    {
        protected List<GraphDrawer> childDrawers;

        protected virtual int DrawCollection(GraphableCollection collection, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            if (forceRegenerate)
            {
                foreach (GraphDrawer child in childDrawers)
                    Destroy(child.gameObject);
                childDrawers.Clear();
                foreach (IGraphable graphable in collection.Graphables)
                {
                    InstantiateChildGraphDrawer(graphable);
                }
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
                    {
                        InstantiateChildGraphDrawer(addedEvent.Graph);
                    }
                    else if (reason is GraphElementsAddedEventArgs multiAddEvent)
                    {
                        foreach (IGraphable newGraph in multiAddEvent.Graphs)
                        {
                            InstantiateChildGraphDrawer(newGraph);
                        }
                    }
                    else if (reason is GraphElementRemovedEventArgs removedEvent)
                    {
                        DestroyChildGraphDrawers(removedEvent.Graph);
                    }
                    else if (reason is GraphElementsRemovedEventArgs multiRemoveEvent)
                    {
                        foreach (IGraphable oldGraph in multiRemoveEvent.Graphs)
                        {
                            DestroyChildGraphDrawers(oldGraph);
                        }
                    }
                }
            }
            return pass;
        }

        private void InstantiateChildGraphDrawer(IGraphable newGraph)
        {
            GraphDrawer childDrawer = Instantiate(grapher.GraphDrawerPrefab, transform).GetComponent<GraphDrawer>();
            childDrawers.Add(childDrawer);
            if (childDrawer.grapher == null)
                childDrawer.grapher = grapher;
            childDrawer.SharedSurfGraphMaterial = SharedSurfGraphMaterial;
            childDrawer.SharedOutlineGraphMaterial = SharedOutlineGraphMaterial;
            childDrawer.SetGraph(newGraph, grapher);
            if (newGraph is IGraphable3 graphable3 && !(newGraph is GraphableCollection))
                ((RectTransform)childDrawer.transform).anchoredPosition3D = new Vector3(0, 0, grapher.ZOffset2D / transform.localScale.z);
        }
        private void DestroyChildGraphDrawers(IGraphable removedGraph)
        {
            foreach (GraphDrawer child in childDrawers.Where(drawer => drawer.graph == removedGraph))
                Destroy(child.gameObject);
            childDrawers.RemoveAll(drawer => drawer.graph == removedGraph);
        }

        public IEnumerable<GraphDrawer> GetFlattenedCollection()
        {
            if (Graph is GraphableCollection)
            {
                IEnumerable<GraphDrawer> collections = childDrawers.Where(d => d.Graph is GraphableCollection);
                IEnumerable<GraphDrawer> flattenedCollection = childDrawers.Where(d => !(d.Graph is GraphableCollection));
                foreach (GraphDrawer childDrawer in collections)
                    flattenedCollection = flattenedCollection.Union(childDrawer.GetFlattenedCollection());
                return flattenedCollection;
            }
            return Enumerable.Empty<GraphDrawer>();
        }
    }
}
