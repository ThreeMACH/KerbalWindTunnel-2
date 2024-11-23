using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public partial class GraphDrawer
    {
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
            LineRenderer renderer = GetComponentInChildren<LineRenderer>();
            renderer.positionCount = values.Length;
            renderer.SetPositions(values);
        }

        protected int DrawLineGraph(ILineGraph lineGraphable, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            if (forceRegenerate || redrawReasons.Key == typeof(ValuesChangedEventArgs))
                GenerateLineGraph();
            LineRenderer renderer = GetComponentInChildren<LineRenderer>();
            if (forceRegenerate || redrawReasons.Key == typeof(ColorChangedEventArgs))
            {
                if (lineGraphable.UseSingleColor)
                    SetLineRendererColors(renderer, lineGraphable.color);
                else
                    SetLineRendererColors(renderer, (Graphable)lineGraphable);
            }
            if (forceRegenerate || redrawReasons.Key == typeof(LineWidthChangedEventArgs))
            {
                UI_Tools.LineWidthManager lineWidthManager = renderer.GetComponent<UI_Tools.LineWidthManager>();
                if (lineWidthManager != null)
                    lineWidthManager.LineWidth = lineGraphable.LineWidth;
            }
            return pass;
        }

        protected virtual int DrawOtherLineGraph(ILineGraph lineGraph, IGrouping<Type, EventArgs> redrawReasons, int pass, bool forceRegenerate = false)
        {
            Debug.LogError("GraphDrawer is not equipped to draw that type of line graph.");
            return pass;
        }

        protected static void SetLineRendererColors(LineRenderer renderer, Color color)
        {
            renderer.colorGradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] { new GradientColorKey(color, 0) },
                alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
            };
            renderer.startColor = renderer.endColor = color;
        }

        protected static void SetLineRendererColors(LineRenderer renderer, Graphable graph)
        {
            if (graph.UseSingleColor)
            {
                SetLineRendererColors(renderer, graph.color);
                return;
            }
            Vector3[] positions = new Vector3[renderer.positionCount];
            if (positions.Length == 0)
                return;
            renderer.GetPositions(positions);
            if (positions.Length == 1)
            {
                Color color = graph.EvaluateColor(positions[0]);
                renderer.colorGradient = new Gradient()
                {
                    colorKeys = new GradientColorKey[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                    alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0), new GradientAlphaKey(color.a, 1) }
                };
                return;
            }
            float totalDistance = 0;
            float[] stepDistances = new float[positions.Length];
            stepDistances[positions.Length - 1] = 0;
            for (int i = positions.Length - 2; i >= 0; i--)
            {
                totalDistance += Vector3.Distance(positions[i], positions[i + 1]);
                stepDistances[i] = totalDistance;
            }
            List<GradientColorKey> colors = new List<GradientColorKey>();
            List<GradientAlphaKey> alphas = new List<GradientAlphaKey>();
            for (int i = 0; i < positions.Length; i++)
            {
                Color color = graph.EvaluateColor(positions[i]);
                if (colors.Count > 2)
                {
                    if ((colors[colors.Count - 2].color == colors[colors.Count - 1].color && colors[colors.Count - 1].color == color) ||
                        (Color.Lerp(colors[colors.Count - 2].color, color, (colors[colors.Count - 1].time - colors[colors.Count - 2].time) / ((1 - stepDistances[i] / totalDistance) - colors[colors.Count - 1].time)) == colors[colors.Count - 1].color))
                    {
                        colors[colors.Count - 1] = new GradientColorKey(color, 1 - stepDistances[i] / totalDistance);
                    }
                    else
                        colors.Add(new GradientColorKey(color, 1 - stepDistances[i] / totalDistance));
                }
                else
                {
                    colors.Add(new GradientColorKey(color, 1 - stepDistances[i] / totalDistance));
                }
                if (alphas.Count > 2)
                {
                    if ((alphas[alphas.Count - 2].alpha == alphas[alphas.Count - 1].alpha && alphas[alphas.Count - 1].alpha == color.a) ||
                        Mathf.Lerp(alphas[alphas.Count - 2].alpha, color.a, (alphas[alphas.Count - 1].time - alphas[alphas.Count - 2].time) / ((1 - stepDistances[i] / totalDistance) - alphas[alphas.Count - 1].time)) == alphas[alphas.Count - 1].alpha)
                    {
                        alphas[alphas.Count - 1] = new GradientAlphaKey(color.a, 1 - stepDistances[i] / totalDistance);
                    }
                    else
                        alphas.Add(new GradientAlphaKey(color.a, 1 - stepDistances[i] / totalDistance));
                }
                else
                    alphas.Add(new GradientAlphaKey(color.a, 1 - stepDistances[i] / totalDistance));
            }
            Gradient gradient = new Gradient
            {
                colorKeys = colors.ToArray(),
                alphaKeys = alphas.ToArray()
            };
            renderer.colorGradient = gradient;
        }
    }
}
