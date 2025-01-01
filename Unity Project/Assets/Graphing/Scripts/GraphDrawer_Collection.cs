using System;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public partial class GraphDrawer
    {
        protected virtual int DrawCollection(GraphableCollection collection, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            if (forceRegenerate)
            {
                foreach (GraphDrawer childDrawer in GetComponentsInChildren<GraphDrawer>().Where(drawer => drawer != this))
                    Destroy(childDrawer.gameObject);
                foreach (IGraphable graphable in collection.Graphables)
                {
                    GraphDrawer childDrawer = Instantiate(grapher.GraphDrawerPrefab, transform).GetComponent<GraphDrawer>();
                    if (childDrawer.grapher == null)
                        childDrawer.grapher = grapher;
                    childDrawer.SetGraph(graphable, grapher);
                    if (graphable is IGraphable3 graphable3 && !(graphable is GraphableCollection))
                        ((RectTransform)childDrawer.transform).anchoredPosition3D = new Vector3(0, 0, grapher.ZOffset2D / transform.localScale.z);
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
                        GraphDrawer childDrawer = Instantiate(grapher.GraphDrawerPrefab, transform).GetComponent<GraphDrawer>();
                        if (childDrawer.grapher == null)
                            childDrawer.grapher = this.grapher;
                        childDrawer.SetGraph(addedEvent.Graph, grapher);
                    }
                    else if (reason is GraphElementRemovedEventArgs removedEvent)
                    {
                        foreach (GraphDrawer drawer in GetComponentsInChildren<GraphDrawer>().Where(drawer => drawer.graph == removedEvent.Graph))
                            Destroy(drawer.gameObject);
                    }
                    else if (reason is GraphElementsAddedEventArgs multiAddEvent)
                    {
                        foreach (IGraphable newGraph in multiAddEvent.Graphs)
                        {
                            GraphDrawer childDrawer = Instantiate(grapher.GraphDrawerPrefab, transform).GetComponent<GraphDrawer>();
                            if (childDrawer.grapher == null)
                                childDrawer.grapher = this.grapher;
                            childDrawer.SetGraph(newGraph, grapher);
                        }
                    }
                    else if (reason is GraphElementsRemovedEventArgs multiRemoveEvent)
                    {
                        foreach (IGraphable oldGraph in multiRemoveEvent.Graphs)
                        {
                            foreach (GraphDrawer drawer in GetComponentsInChildren<GraphDrawer>().Where(drawer => drawer.graph == oldGraph))
                                Destroy(drawer.gameObject);
                        }
                    }
                }
            }
            return pass;
        }
    }
}
