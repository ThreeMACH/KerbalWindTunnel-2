using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graphing
{
#region Graphable
    public sealed class DisplayNameChangedEventArgs : EventArgs
    {
        public string NewDisplayName { get; }
        public DisplayNameChangedEventArgs(string newDisplayName) => NewDisplayName = newDisplayName;
    }
    public sealed class ColorChangedEventArgs : EventArgs
    {
        public Func<Vector3, float> ColorFunc { get; }
        public Gradient ColorMap { get; }
        public bool UseSingleColor { get; }
        public Color Color { get; }
        public ColorChangedEventArgs(Gradient colorMap, Func<Vector3, float> colorFunc, bool useSingleColor, Color color) { ColorMap = colorMap; ColorFunc = colorFunc; UseSingleColor = useSingleColor; Color = color; }
    }
    public sealed class AxisNameChangedEventArgs : EventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public string AxisName { get; }
        public AxisNameChangedEventArgs(AxisUI.AxisDirection axis, string axisName) { Axis = axis; AxisName = axisName; }
    }
    public sealed class AxisUnitChangedEventArgs : EventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public string Unit { get; }
        public AxisUnitChangedEventArgs(AxisUI.AxisDirection axis, string unit) { Axis = axis; Unit = unit; }
    }
    public sealed class TransposeChangedEventArgs : EventArgs
    {
        public bool Transpose { get; }
        public TransposeChangedEventArgs(bool transpose) => Transpose = transpose;
    }
    public sealed class ValuesChangedEventArgs : EventArgs
    {
        public Array NewValues { get; }
        public (float, float)[] NewBounds { get; }
        public bool BoundsChanged { get; }
        public ValuesChangedEventArgs(Array newValues, (float, float)[] newBounds) : this(newValues, true, newBounds) { }
        public ValuesChangedEventArgs(Array newValues, bool boundsChanged, (float, float)[] newBounds) { NewValues = newValues; BoundsChanged = boundsChanged; NewBounds = newBounds; }
    }
    public sealed class VisibilityChangedEventArgs : EventArgs
    {
        public bool Visible { get; }
        public VisibilityChangedEventArgs(bool visible) => Visible = visible;
    }
    public sealed class BoundsChangedEventArgs : EventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public float Lower { get; }
        public float Upper { get; }
        public BoundsChangedEventArgs(AxisUI.AxisDirection axis, float lower, float upper) { Axis = axis; Lower = lower; Upper = upper; }
    }
#endregion

#region Lines
    public sealed class LineWidthChangedEventArgs : EventArgs
    {
        public float NewWidth { get; }
        public LineWidthChangedEventArgs(float newWidth) => NewWidth = newWidth;
    }
#endregion

#region Outlines
    public sealed class MaskLineOnlyChangedEventArgs : EventArgs
    {
        public bool LineOnly { get; }
        public MaskLineOnlyChangedEventArgs(bool lineOnly) => LineOnly = lineOnly;
    }
    public class MaskCriteriaChangedEventArgs : EventArgs
    {
        public Func<Vector3, float> MaskCriteria { get; }
        public MaskCriteriaChangedEventArgs(Func<Vector3, float> maskCriteria) => MaskCriteria = maskCriteria;
    }
#endregion

#region Collections
    public sealed class ChildChangedEventArgs : EventArgs
    {
        public EventArgs EventArgs { get; }
        public IGraphable OriginalSender { get; }
        public ChildChangedEventArgs(IGraphable sender, EventArgs eventArgs) { OriginalSender = sender; EventArgs = eventArgs; }
        public ChildChangedEventArgs LowestWrapping()
        {
            if (EventArgs is ChildChangedEventArgs nestedEventArgs)
            {
                if (nestedEventArgs.EventArgs is ChildChangedEventArgs)
                    return nestedEventArgs.LowestWrapping();
                else return nestedEventArgs;
            }
            return this;
        }
        public EventArgs Unwrap()
        {
            if (EventArgs is ChildChangedEventArgs nestedEventArgs)
                return nestedEventArgs.Unwrap();
            return EventArgs;
        }
    }
    public sealed class GraphElementRemovedEventArgs : EventArgs
    {
        public IGraphable Graph { get; }
        public GraphElementRemovedEventArgs(IGraphable graph) => Graph = graph;
    }
    public sealed class GraphElementAddedEventArgs : EventArgs
    {
        public IGraphable Graph { get; }
        public GraphElementAddedEventArgs(IGraphable graph) => Graph = graph;
    }
    public sealed class GraphElementsRemovedEventArgs : EventArgs
    {
        public IEnumerable<IGraphable> Graphs { get; }
        public GraphElementsRemovedEventArgs(IEnumerable<IGraphable> graphs) => Graphs = graphs;
    }
    public sealed class GraphElementsAddedEventArgs : EventArgs
    {
        public IEnumerable<IGraphable> Graphs { get; }
        public GraphElementsAddedEventArgs(IEnumerable<IGraphable> graphs) => Graphs = graphs;
    }
#endregion
}