using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graphing
{
    public interface IDisplayEventArgs { }
    public interface IValueEventArgs { }

#region Graphable
    public sealed class DisplayNameChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public string NewDisplayName { get; }
        public DisplayNameChangedEventArgs(string newDisplayName) => NewDisplayName = newDisplayName;
    }
    public sealed class ColorChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public Func<Vector3, float> ColorFunc { get; }
        public Gradient ColorMap { get; }
        public bool UseSingleColor { get; }
        public Color Color { get; }
        public ColorChangedEventArgs(Gradient colorMap, Func<Vector3, float> colorFunc, bool useSingleColor, Color color) { ColorMap = colorMap; ColorFunc = colorFunc; UseSingleColor = useSingleColor; Color = color; }
    }
    public sealed class AxisNameChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public string AxisName { get; }
        public AxisNameChangedEventArgs(AxisUI.AxisDirection axis, string axisName) { Axis = axis; AxisName = axisName; }
    }
    public sealed class AxisUnitChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public string Unit { get; }
        public AxisUnitChangedEventArgs(AxisUI.AxisDirection axis, string unit) { Axis = axis; Unit = unit; }
    }
    public sealed class TransposeChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public bool Transpose { get; }
        public TransposeChangedEventArgs(bool transpose) => Transpose = transpose;
    }
    public sealed class ValuesChangedEventArgs : EventArgs, IValueEventArgs
    {
        public Array NewValues { get; }
        public ValuesChangedEventArgs(Array newValues) { NewValues = newValues; }
    }
    public sealed class VisibilityChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public bool Visible { get; }
        public VisibilityChangedEventArgs(bool visible) => Visible = visible;
    }
    public sealed class BoundsChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public AxisUI.AxisDirection Axis { get; }
        public float Lower { get; }
        public float Upper { get; }
        public BoundsChangedEventArgs(AxisUI.AxisDirection axis, float lower, float upper) { Axis = axis; Lower = lower; Upper = upper; }
    }
#endregion

#region Lines
    public sealed class LineWidthChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public float NewWidth { get; }
        public LineWidthChangedEventArgs(float newWidth) => NewWidth = newWidth;
    }
#endregion

#region Outlines
    public sealed class MaskLineOnlyChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public bool LineOnly { get; }
        public MaskLineOnlyChangedEventArgs(bool lineOnly) => LineOnly = lineOnly;
    }
    public class MaskCriteriaChangedEventArgs : EventArgs, IDisplayEventArgs
    {
        public Func<Vector3, float> MaskCriteria { get; }
        public MaskCriteriaChangedEventArgs(Func<Vector3, float> maskCriteria) => MaskCriteria = maskCriteria;
    }
    #endregion

    #region Collections
    public sealed class ChildValueChangedEventArgs : ChildChangedEventArgs<IValueEventArgs>, IValueEventArgs
    {
        public ChildValueChangedEventArgs(IGraphable sender, IValueEventArgs eventArgs) : base(sender, eventArgs) { }
    }
    public sealed class ChildDisplayChangedEventArgs : ChildChangedEventArgs<IDisplayEventArgs>, IDisplayEventArgs
    {
        public ChildDisplayChangedEventArgs(IGraphable sender, IDisplayEventArgs eventArgs) : base(sender, eventArgs) { }
    }
    public abstract class ChildChangedEventArgs<T> : EventArgs
    {
        public T EventArgs { get; }
        public IGraphable OriginalSender { get; }
        public ChildChangedEventArgs(IGraphable sender, T eventArgs) { OriginalSender = sender; EventArgs = eventArgs; }
        public ChildChangedEventArgs<T> LowestWrapping()
        {
            if (EventArgs is ChildChangedEventArgs<T> nestedEventArgs)
            {
                if (nestedEventArgs.EventArgs is ChildChangedEventArgs<T>)
                    return nestedEventArgs.LowestWrapping();
                else return nestedEventArgs;
            }
            return this;
        }
        public T Unwrap()
        {
            if (EventArgs is ChildChangedEventArgs<T> nestedEventArgs)
                return nestedEventArgs.Unwrap();
            return EventArgs;
        }
    }
    public sealed class GraphElementRemovedEventArgs : EventArgs, IDisplayEventArgs
    {
        public IGraphable Graph { get; }
        public GraphElementRemovedEventArgs(IGraphable graph) => Graph = graph;
    }
    public sealed class GraphElementAddedEventArgs : EventArgs, IDisplayEventArgs
    {
        public IGraphable Graph { get; }
        public GraphElementAddedEventArgs(IGraphable graph) => Graph = graph;
    }
    public sealed class GraphElementsRemovedEventArgs : EventArgs, IDisplayEventArgs
    {
        public IEnumerable<IGraphable> Graphs { get; }
        public GraphElementsRemovedEventArgs(IEnumerable<IGraphable> graphs) => Graphs = graphs;
    }
    public sealed class GraphElementsAddedEventArgs : EventArgs, IDisplayEventArgs
    {
        public IEnumerable<IGraphable> Graphs { get; }
        public GraphElementsAddedEventArgs(IEnumerable<IGraphable> graphs) => Graphs = graphs;
    }
#endregion
}