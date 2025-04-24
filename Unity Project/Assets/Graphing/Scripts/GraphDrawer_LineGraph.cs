using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public partial class GraphDrawer
    {
        protected void LineGraphSetup()
        {
            ScreenSpaceLineRenderer lineRenderer = gameObject.AddComponent<ScreenSpaceLineRenderer>();
            lineRenderer.material = lineVertexMaterial;
            lineRenderer.WhitelistCamera(grapher.GetComponentInChildren<Camera>(true));
        }

        protected void GenerateLineGraph()
        {
            Vector3[] values;
            if (graph is LineGraph lineGraph)
            {
                if (lineGraph.Values == null)
                    return;
                values = lineGraph.Values.Select(v2 => lineGraph.Transpose ? new Vector3(v2.y, v2.x, 0) : new Vector3(v2.x, v2.y, 0)).ToArray();
            }
            else// if (graph is Line3Graph line3Graph)
            {
                Line3Graph line3Graph = (Line3Graph)graph;
                if (line3Graph.Values == null)
                    return;
                values = line3Graph.Values.Select(v3 => line3Graph.Transpose ? new Vector3(v3.y, v3.x, -v3.z) : new Vector3(v3.x, v3.y, -v3.z)).ToArray();
            }
            ScreenSpaceLineRenderer lineRenderer = GetComponent<ScreenSpaceLineRenderer>();
            lineRenderer.Points = values;
        }

        protected int DrawLineGraph(ILineGraph lineGraphable, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs))
                GenerateLineGraph();
            ScreenSpaceLineRenderer lineRenderer = GetComponentInChildren<ScreenSpaceLineRenderer>(true);
            if (forceRegenerate || redrawReasons.Key == typeof(ColorChangedEventArgs))
            {
                if (lineGraphable.UseSingleColor)
                    SetLineRendererColor(lineRenderer, lineGraphable.color);
                else
                    SetLineRendererColors(lineRenderer, lineGraphable);
            }
            if (forceRegenerate || redrawReasons.Key == typeof(LineWidthChangedEventArgs))
            {
                float width = lineGraphable.LineWidth;
                lineRenderer.Width = lineGraphable.LineWidth;
                if (width < 10)
                {
                    lineRenderer.CapSegments = 4;
                    lineRenderer.ElbowSegments = 4;
                }
                else if (width < 20)
                {
                    lineRenderer.CapSegments = 6;
                    lineRenderer.ElbowSegments = 6;
                }
                else
                {
                    lineRenderer.CapSegments = 12;
                    lineRenderer.ElbowSegments = 12;
                }
            }
            return pass;
        }

        protected virtual int DrawOtherLineGraph(ILineGraph lineGraph, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            Debug.LogError("GraphDrawer is not equipped to draw that type of line graph.");
            return pass;
        }

        protected static void SetLineRendererColor(ScreenSpaceLineRenderer lineRenderer, Color color)
        {
            lineRenderer.Colors = new Color[] { color };
        }

        protected static void SetLineRendererColors(ScreenSpaceLineRenderer lineRenderer, ILineGraph graph)
        {
            if (graph.UseSingleColor)
            {
                SetLineRendererColor(lineRenderer, graph.color);
                return;
            }

            IEnumerable<Vector3> values;
            if (graph is Line3Graph line3Graph)
                values = line3Graph.Values;
            else
                values = ((LineGraph)graph).Values.Select(v2 => (Vector3)v2);

            lineRenderer.Colors = values.Select(graph.EvaluateColor).ToArray();
        }
    }
}
