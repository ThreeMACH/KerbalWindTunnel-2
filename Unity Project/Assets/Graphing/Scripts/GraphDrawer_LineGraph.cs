using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public class LineGraphDrawer : GraphDrawer, GraphDrawer.ISingleMaterialUser
    {
        static readonly Unity.Profiling.ProfilerMarker s_lineMarker = new Unity.Profiling.ProfilerMarker("GraphDrawer.Draw(LineGraph)");

        [SerializeField]
        protected Material lineVertexMaterial;

        protected ILineGraph lineGraphable;

        public Material LineVertexMaterial => lineVertexMaterial;

        public override int MaterialSet => 3;

        protected override void Setup()
        {
            lineGraphable = (ILineGraph)graph;
            ScreenSpaceLineRenderer lineRenderer = gameObject.AddComponent<ScreenSpaceLineRenderer>();
            lineRenderer.material = lineVertexMaterial;
            lineRenderer.WhitelistCamera(grapher.GetComponentInChildren<Camera>(true));
        }

        void ISingleMaterialUser.InitializeMaterial(Material material)
            => InitializeMaterial(material);
        protected internal void InitializeMaterial(Material material)
            => lineVertexMaterial = material;

        protected void GenerateLineGraph(ScreenSpaceLineRenderer lineRenderer)
        {
            Vector3[] values;
            if (graph is LineGraph lineGraph)
            {
                if (lineGraph.Values == null)
                    return;
                values = lineGraph.Values.Select(v2 => lineGraph.Transpose ? new Vector3(-v2.y, v2.x, 0) : new Vector3(-v2.x, v2.y, 0)).Where(NotNaN).ToArray();
            }
            else// if (graph is Line3Graph line3Graph)
            {
                Line3Graph line3Graph = (Line3Graph)graph;
                if (line3Graph.Values == null)
                    return;
                values = line3Graph.Values.Select(v3 => line3Graph.Transpose ? new Vector3(-v3.y, v3.x, -v3.z) : new Vector3(-v3.x, v3.y, -v3.z)).Where(NotNaN).ToArray();
            }
            lineRenderer.Points = values;
        }

        protected bool NotNaN(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));

        protected override int DrawInternal(IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            s_lineMarker.Begin();
            ScreenSpaceLineRenderer lineRenderer = GetComponentInChildren<ScreenSpaceLineRenderer>(true);
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs))
                GenerateLineGraph(lineRenderer);
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
            s_lineMarker.End();
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
