using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Graphing
{
    /// <summary>
    /// A collection of <see cref="IGraphable"/> items.
    /// </summary>
    public class GraphableCollection : IGraphable, IList<IGraphable>
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public string Name { get; set; } = "";

        private string displayName = "";
        /// <summary>
        /// The display name of the object. Can be different than the <see cref="Name"/> of the object.
        /// </summary>
        public string DisplayName
        {
            get => string.IsNullOrEmpty(displayName) ? Name : displayName;
            set { displayName = value; OnDisplayChanged(new DisplayNameChangedEventArgs(DisplayName)); }
        }
        /// <summary>
        /// The visibility status of the collection.
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set
            {
                bool changed = _visible != value;
                _visible = value;
                if (changed)
                    OnDisplayChanged(new VisibilityChangedEventArgs(value));
            }
        }
        private bool _visible = true;
        /// <summary>
        /// Should the value be displayed on mouseover.
        /// </summary>
        public bool DisplayValue { get; set; } = true;
        /// <summary>
        /// The lower X bound of the object.
        /// </summary>
        public virtual float XMin { get => xMin; set { xMin = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Horizontal, XMin, XMax), false); } }
        protected float xMin;
        /// <summary>
        /// The upper X bound of the object.
        /// </summary>
        public virtual float XMax { get => xMax; set { xMax = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Horizontal, XMin, XMax), false); } }
        protected float xMax;
        /// <summary>
        /// The lower Y bound of the object.
        /// </summary>
        public virtual float YMin { get => yMin; set { yMin = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Vertical, YMin, YMax), false); } }
        protected float yMin;
        /// <summary>
        /// The upper Y bound of the object.
        /// </summary>
        public virtual float YMax { get => yMax; set { yMax = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Vertical, YMin, YMax), false); } }
        protected float yMax;

        protected readonly List<IGraphable> graphs = new List<IGraphable>();

        /// <summary>
        /// An event to be triggered when an object's values change.
        /// </summary>
        public event EventHandler<IValueEventArgs> ValuesChanged;
        /// <summary>
        /// An event to be triggered when an object's display formatting changes.
        /// </summary>
        public event EventHandler<IDisplayEventArgs> DisplayChanged;

        protected bool autoFitAxes = true;
        /// <summary>
        /// When true, reports the actual bounds of the contained objects rather than their self-reported bounds.
        /// </summary>
        public virtual bool AutoFitAxes
        {
            get => autoFitAxes;
            set
            {
                bool changed = autoFitAxes != value;
                autoFitAxes = value;
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            if (graphs[i] is GraphableCollection collection)
                                collection.AutoFitAxes = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                if (changed)
                    RecalculateLimits();
            }
        }

        protected bool ignoreChildEvents = false;

        /// <summary>
        /// The unit for the X axis.
        /// </summary>
        public string XUnit
        {
            get
            {
                return FirstVisibleGraph(this)?.XUnit ?? graphs.FirstOrDefault()?.XUnit ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            graphs[i].XUnit = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisUnitChangedEventArgs(AxisUI.AxisDirection.Horizontal, value));
            }
        }
        /// <summary>
        /// The unit for the Y axis.
        /// </summary>
        public string YUnit
        {

            get
            {
                return FirstVisibleGraph(this)?.YUnit ?? graphs.FirstOrDefault()?.YUnit ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            graphs[i].YUnit = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisUnitChangedEventArgs(AxisUI.AxisDirection.Vertical, value));
            }
        }
        /// <summary>
        /// The name of the X axis.
        /// </summary>
        public string XName
        {
            get
            {
                return FirstVisibleGraph(this)?.XName ?? graphs.FirstOrDefault()?.XName ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            graphs[i].XName = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisNameChangedEventArgs(AxisUI.AxisDirection.Horizontal, value));
            }
        }
        /// <summary>
        /// The name of the Y axis.
        /// </summary>
        public string YName
        {
            get
            {
                return FirstVisibleGraph(this).YName ?? graphs.FirstOrDefault()?.YName ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            graphs[i].YName = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisNameChangedEventArgs(AxisUI.AxisDirection.Vertical, value));
            }
        }

        /// <summary>
        /// Gets or sets a contained <see cref="IGraphable"/> by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IGraphable this[string name]
        {
            get => graphs.FirstOrDefault(g => string.Equals(g.Name, name));
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IGraphable"/>s.
        /// </summary>
        public virtual IEnumerable<IGraphable> Graphables
        {
            get => graphs.ToList();
            set
            {
                lock (this)
                {
                    this.Clear();
                    AddRange(value);
                }
            }
        }

        /// <summary>
        /// Gets the number of <see cref="IGraphable"/>s actually contained in the <see cref="GraphableCollection"/>.
        /// </summary>
        public int Count { get => graphs.Count; }

        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets a contained <see cref="IGraphable"/> by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphable this[int index]
        {
            get => graphs[index];
            set
            {
                if (value == null)
                    return;
                if (value == graphs[index])
                    return;
                if (IsCircularReference(value))
                {
                    UnityEngine.Debug.LogError("Cannot create a circular GraphableCollection.");
                    return;
                }
                if (graphs.Contains(value))
                {
                    UnityEngine.Debug.LogError("Cannot add two of the same graph instance to a collection.");
                    return;
                }
                graphs[index].ValuesChanged -= ValuesChangedSubscriber;
                graphs[index].DisplayChanged -= DisplayChangedSubscriber;
                IGraphable oldGraph = graphs[index];

                graphs[index] = value;
                OnDisplayChanged(new GraphElementRemovedEventArgs(oldGraph), false);

                graphs[index].ValuesChanged += ValuesChangedSubscriber;
                graphs[index].DisplayChanged += DisplayChangedSubscriber;
                OnDisplayChanged(new GraphElementAddedEventArgs(graphs[index]));
            }
        }

        public bool IsCircularReference(IGraphable graph)
        {
            if (graph == this)
                return true;
            if (graph is GraphableCollection collection)
                return collection.Any(g => (g as GraphableCollection)?.IsCircularReference(graph) ?? false);
            return false;
        }

        /// <summary>
        /// Constructs an empty <see cref="GraphableCollection"/>.
        /// </summary>
        public GraphableCollection() { }
        /// <summary>
        /// Constructs a <see cref="GraphableCollection"/> with the provided <see cref="IGraphable"/>.
        /// </summary>
        /// <param name="graph"></param>
        public GraphableCollection(IGraphable graph)
        {
            if (graph == null)
                return;
            graphs.Add(graph);
            graph.ValuesChanged += ValuesChangedSubscriber;
            graph.DisplayChanged += DisplayChangedSubscriber;
            RecalculateLimits();
        }
        /// <summary>
        /// Constructs a <see cref="GraphableCollection"/> with the provided <see cref="IGraphable"/>s.
        /// </summary>
        /// <param name="graphs"></param>
        public GraphableCollection(IEnumerable<IGraphable> graphs)
        {
            IEnumerator<IGraphable> enumerator = graphs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                    continue;
                this.graphs.Add(enumerator.Current);
                enumerator.Current.ValuesChanged += ValuesChangedSubscriber;
                enumerator.Current.DisplayChanged += DisplayChangedSubscriber;
            }
            RecalculateLimits();
        }

        public static IGraphable FirstVisibleGraph(IGraphable graph)
        {
            if (graph == null)
                return null;
            if (!graph.Visible)
                return null;
            if (graph is GraphableCollection collection)
            {
                foreach (IGraphable child in collection)
                {
                    if (!child.Visible)
                        continue;
                    IGraphable result = child;
                    if (child is GraphableCollection childCollection)
                        result = FirstVisibleGraph(childCollection);
                    if (result != null)
                        return result;
                }
                return null;
            }
            return graph;
        }

        public static IColorGraph FirstColorGraph(IGraphable graphable)
        {
            if (graphable is IColorGraph colorGraph)
                return colorGraph;
            if (graphable is GraphableCollection collection)
            {
                foreach (IGraphable child in collection)
                {
                    if (child is IColorGraph childColorGraph)
                        return childColorGraph;
                }
                foreach (IGraphable child in collection)
                {
                    if (child is GraphableCollection childCollection)
                    {
                        colorGraph = FirstColorGraph(childCollection);
                        if (colorGraph != null)
                            return colorGraph;
                    }
                }
            }
            return null;
        }

        public IEnumerable<IGraphable> Flatten()
        {
            IEnumerable<IGraphable> collections = Graphables.Where(g => g is GraphableCollection);
            IEnumerable<IGraphable> graphables = Graphables.Except(collections);
            foreach (IGraphable childCollection in collections)
                graphables = graphables.Union(((GraphableCollection)childCollection).Flatten());
            return graphables;
        }

        /// <summary>
        /// Draws the object on the specified <see cref="UnityEngine.Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        public virtual void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop)
        {
            if (!Visible) return;
            for (int i = 0; i < graphs.Count; i++)
            {
                graphs[i].Draw(ref texture, xLeft, xRight, yBottom, yTop);
            }
        }
        public virtual void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop, float cMin, float cMax)
        {
            if (!Visible) return;
            for (int i = 0; i < graphs.Count; i++)
            {
                if (graphs[i] is SurfGraph surfGraph)
                    surfGraph.Draw(ref texture, xLeft, xRight, yBottom, yTop, cMin, cMax);
                else
                    graphs[i].Draw(ref texture, xLeft, xRight, yBottom, yTop);
            }
        }

        /// <summary>
        /// Recalculates the reported limits of the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <returns></returns>
        public virtual bool RecalculateLimits(bool expandSurfFilter = false)
        {
            float[] oldLimits = new float[] { XMin, XMax, YMin, YMax };
            this.xMin = this.xMax = this.yMin = this.yMax = float.NaN;

            for (int i = 0; i < graphs.Count; i++)
            {
                if (!graphs[i].Visible) continue;
                float xMin, xMax, yMin, yMax;
                if (!autoFitAxes)
                {
                    xMin = graphs[i].XMin;
                    xMax = graphs[i].XMax;
                    yMin = graphs[i].YMin;
                    yMax = graphs[i].YMax;
                }
                else
                {
                    if (graphs[i] is LineGraph lineGraph)
                    {
                        GetLimitsAutoLine(lineGraph, out xMin, out xMax, out yMin, out yMax);
                    }
                    else if (graphs[i] is SurfGraph surfGraph)
                    {
                        GetLimitsAutoSurf(surfGraph, out xMin, out xMax, out yMin, out yMax, expandSurfFilter);
                    }
                    else if (graphs[i] is OutlineMask)
                    {
                        continue;
                        //xMin = this.xMin; xMax = this.xMax; yMin = this.yMin; yMax = this.yMax;
                    }
                    else
                    {
                        xMin = graphs[i].XMin;
                        xMax = graphs[i].XMax;
                        yMin = graphs[i].YMin;
                        yMax = graphs[i].YMax;
                    }
                }
                if (xMin < this.xMin || float.IsNaN(this.xMin)) this.xMin = xMin;
                if (xMax > this.xMax || float.IsNaN(this.xMax)) this.xMax = xMax;
                if (yMin < this.yMin || float.IsNaN(this.yMin)) this.yMin = yMin;
                if (yMax > this.yMax || float.IsNaN(this.yMax)) this.yMax = yMax;
            }
            if (float.IsNaN(this.xMin) || float.IsNaN(this.xMax))
                this.xMin = this.xMax = 0;
            if (float.IsNaN(this.yMin) || float.IsNaN(this.yMax))
                this.yMin = this.yMax = 0;
            bool boundsChanged = false;
            if (!(oldLimits[0] == XMin && oldLimits[1] == XMax))
            {
                OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Horizontal, XMin, XMax), false);
                boundsChanged = true;
            }
            if (!(oldLimits[2] == YMin && oldLimits[3] == YMax))
            {
                OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Vertical, YMin, YMax), false);
                boundsChanged = true;
            }
            return boundsChanged;
        }

        protected static void GetLimitsAutoSurf(SurfGraph surfGraph, out float xMin, out float xMax, out float yMin, out float yMax, bool expandFilter = false)
        {
            float[,] values = surfGraph.Values;
            int width = values.GetUpperBound(0);
            int height = values.GetUpperBound(1);
            bool breakFlag = false;
            int x, y;
            for (x = 0; x <= width; x++)
            {
                for (y = 0; y <= height; y++)
                    if (Graphable.ValidFloat(values[x, y]))
                    {
                        breakFlag = true;
                        break;
                    }
                if (breakFlag) break;
            }
            if (expandFilter)
                x = Math.Max(0, x - 1);
            xMin = (surfGraph.XMax - surfGraph.XMin) / width * x + surfGraph.XMin;

            breakFlag = false;
            for (x = width; x >= 0; x--)
            {
                for (y = 0; y <= height; y++)
                    if (Graphable.ValidFloat(values[x, y]))
                    {
                        breakFlag = true;
                        break;
                    }
                if (breakFlag) break;
            }
            if (expandFilter)
                x = Math.Min(width, x + 1);
            xMax = (surfGraph.XMax - surfGraph.XMin) / width * x + surfGraph.XMin;

            breakFlag = false;
            for (y = 0; y <= height; y++)
            {
                for (x = 0; x <= width; x++)
                    if (Graphable.ValidFloat(values[x, y]))
                    {
                        breakFlag = true;
                        break;
                    }
                if (breakFlag) break;
            }
            if (expandFilter)
                y = Math.Max(0, y - 1);
            yMin = (surfGraph.YMax - surfGraph.YMin) / height * y + surfGraph.YMin;

            breakFlag = false;
            for (y = height; y >= 0; y--)
            {
                for (x = 0; x <= width; x++)
                    if (Graphable.ValidFloat(values[x, y]))
                    {
                        breakFlag = true;
                        break;
                    }
                if (breakFlag) break;
            }
            if (expandFilter)
                y = Math.Min(height, y + 1);
            yMax = (surfGraph.YMax - surfGraph.YMin) / height * y + surfGraph.YMin;

            if (yMin > yMax)
            {
                yMin = surfGraph.YMin;
                yMax = surfGraph.YMax;
            }
            if (xMin > xMax)
            {
                xMin = surfGraph.XMin;
                xMax = surfGraph.XMax;
            }
        }

        /*protected void GetLimitsAutoOutline(OutlineMask outlineMask, out float xMin, out float xMax, out float yMin, out float yMax)
        {

        }*/

        protected static void GetLimitsAutoLine(LineGraph lineGraph, out float xMin, out float xMax, out float yMin, out float yMax)
        {
            UnityEngine.Vector2[] values = lineGraph.Values;
            xMin = float.NaN;
            xMax = float.NaN;
            yMin = float.NaN;
            yMax = float.NaN;

            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (Graphable.ValidFloat(values[i].y) && !float.IsNaN(values[i].x) && !float.IsInfinity(values[i].x))
                {
                    if (float.IsNaN(xMin) || values[i].x < xMin) xMin = values[i].x;
                    if (float.IsNaN(xMax) || values[i].x > xMax) xMax = values[i].x;
                    if (float.IsNaN(yMin) || values[i].y < yMin) yMin = values[i].y;
                    if (float.IsNaN(yMax) || values[i].y > yMax) yMax = values[i].y;
                }
            }
        }

        /// <summary>
        /// Gets a value from the first visible element given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <returns></returns>
        public float ValueAt(float x, float y)
        {
            return ValueAt(x, y, Math.Max(graphs.FindIndex(g => g.Visible), 0));
        }
        /// <summary>
        /// Gets a value from the specified element given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="index">The index of the element to report.</param>
        /// <returns></returns>
        public float ValueAt(float x, float y, int index = 0)
        {
            if (graphs.Count - 1 < index)
                return float.NaN;

            if (graphs[index] is ILineGraph lineGraph)
                return lineGraph.ValueAt(x, y, XMax - XMin, YMax - YMin);
            return graphs[index].ValueAt(x, y);
        }

        /// <summary>
        /// Gets the formatted value from all elements given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        public string GetFormattedValueAt(float x, float y, bool withName = false) => GetFormattedValueAt(x, y, -1, withName);

        /// <summary>
        /// Gets a formatted value from the specified element given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="index">The index of the element to report.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        public string GetFormattedValueAt(float x, float y, int index = -1, bool withName = false)
        {
            if (graphs.Count == 0)
                return "";

            if (index >= 0)
            {
                if (graphs[index] is ILineGraph lineGraph)
                    return lineGraph.GetFormattedValueAt(x, y, XMax - XMin, YMax - YMin, withName);
                return graphs[index].GetFormattedValueAt(x, y, withName);
            }

            if (graphs.Count > 1)
                withName = true;

            string returnValue = "";
            for (int i = 0; i < graphs.Count; i++)
            {
                if (!graphs[i].Visible || !graphs[i].DisplayValue)
                    continue;
                string graphValue;
                if (graphs[i] is ILineGraph lineGraph)
                    graphValue = lineGraph.GetFormattedValueAt(x, y, XMax - XMin, YMax - YMin, withName);
                else
                    graphValue = graphs[i].GetFormattedValueAt(x, y, withName);
                if (graphValue != "" && returnValue != "")
                    returnValue += string.Format("\n{0}", graphValue);
                else
                    returnValue += string.Format("{0}", graphValue);
            }
            if (withName)
            {
                string nameSubstring = GetNameSubstring();
                if (nameSubstring != "")
                    return returnValue.Replace(nameSubstring, "");
            }
            return returnValue;
        }

        private string GetNameSubstring()
        {
            List<IGraphable> visibleGraphs = graphs.Where(g => g.Visible).ToList();
            if (visibleGraphs.Count < 2)
                return "";
            int maxL = visibleGraphs[0].DisplayName.Length;
            int commonL = 0;
            while (commonL < maxL && visibleGraphs[1].DisplayName.StartsWith(visibleGraphs[0].DisplayName.Substring(0, commonL + 1)))
                commonL++;
            string nameSubstring = visibleGraphs[0].DisplayName.Substring(0, commonL);
            if (nameSubstring.EndsWith("("))
                nameSubstring = nameSubstring.Substring(0, nameSubstring.Length - 1);

            for (int i = 2; i < visibleGraphs.Count; i++)
            {
                if (!visibleGraphs[i].DisplayName.StartsWith(nameSubstring))
                    return "";
            }
            return nameSubstring;
        }

        /// <summary>
        /// Invokes the <see cref="ValuesChanged"/> event for this object.
        /// </summary>
        /// <param name="eventArgs">Any relevant <see cref="EventArgs"/>.</param>
        protected virtual void OnValuesChanged(IValueEventArgs eventArgs)
        {
            ValuesChanged?.Invoke(this, eventArgs);
        }
        /// <summary>
        /// Invokes the <see cref="DisplayChanged"/> event for this object.
        /// </summary>
        /// <param name="eventArgs">Any relevant <see cref="EventArgs"/>.</param>
        public virtual void OnDisplayChanged(IDisplayEventArgs eventArgs, bool recalculateLimits = true)
        {
            if (recalculateLimits)
                RecalculateLimits();
            DisplayChanged?.Invoke(this, eventArgs);
        }
        /// <summary>
        /// Sets the visibility of all elements.
        /// </summary>
        /// <param name="visible"></param>
        public void SetVisibility(bool visible)
        {
            lock (this)
            {
                ignoreChildEvents = true;
                try
                {
                    for (int i = graphs.Count - 1; i >= 0; i--)
                        graphs[i].Visible = visible;
                }
                finally { ignoreChildEvents = false; }
            }
        }
        /// <summary>
        /// Sets the visibility of all elements, except for the element specified by name.
        /// </summary>
        /// <param name="visible"></param>
        /// <param name="exception"></param>
        public void SetVisibilityExcept(bool visible, string exception)
        {
            lock (this)
            {
                ignoreChildEvents = true;
                try
                {
                    this.SetVisibility(visible);
                    if (HasGraphNamed(exception))
                        this[exception].Visible = !visible;
                }
                finally { ignoreChildEvents = false; }
            }
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the first occurrence within the entire <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual IGraphable Find(Predicate<IGraphable> predicate)
        {
            return graphs.Find(predicate);
        }
        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual List<IGraphable> FindAll(Predicate<IGraphable> predicate)
        {
            return graphs.FindAll(predicate);
        }
        /// <summary>
        /// Removes all elements from the <see cref="GraphableCollection"/>.
        /// </summary>
        public virtual void Clear()
        {
            for (int i = graphs.Count - 1; i >= 0; i--)
            {
                graphs[i].ValuesChanged -= ValuesChangedSubscriber;
                graphs[i].DisplayChanged -= DisplayChangedSubscriber;
            }
            List<IGraphable> oldGraphs = graphs.ToList();
            graphs.Clear();
            OnDisplayChanged(new GraphElementsRemovedEventArgs(oldGraphs));
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index
        /// of the first occurrence within the entire <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="graphable"></param>
        /// <returns></returns>
        public virtual int IndexOf(IGraphable graphable)
        {
            return graphs.IndexOf(graphable);
        }
        /// <summary>
        /// Adds an object to the end of the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="newGraph"></param>
        public virtual void Add(IGraphable newGraph)
        {
            lock (this)
            {
                if (newGraph == null)
                    return;
                if (IsCircularReference(newGraph))
                {
                    UnityEngine.Debug.LogError("Cannot create a circular GraphableCollection.");
                    return;
                }
                if (graphs.Contains(newGraph))
                {
                    UnityEngine.Debug.LogError("Cannot add two of the same graph instance to a collection.");
                    return;
                }
                graphs.Add(newGraph);
                newGraph.ValuesChanged += ValuesChangedSubscriber;
                newGraph.DisplayChanged += DisplayChangedSubscriber;
                OnDisplayChanged(new GraphElementAddedEventArgs(newGraph));
            }
        }
        /// <summary>
        /// Adds the elements in the specified collection to the end of the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="newGraphs"></param>
        public virtual void AddRange(IEnumerable<IGraphable> newGraphs)
        {
            lock (this)
            {
                IEnumerator<IGraphable> enumerator = newGraphs.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current == null)
                        continue;
                    if (IsCircularReference(enumerator.Current))
                    {
                        UnityEngine.Debug.LogError("Cannot create a circular GraphableCollection.");
                        continue;
                    }
                    graphs.Add(enumerator.Current);
                    enumerator.Current.ValuesChanged += ValuesChangedSubscriber;
                    enumerator.Current.DisplayChanged += DisplayChangedSubscriber;
                }
                OnDisplayChanged(new GraphElementsAddedEventArgs(newGraphs));
            }
        }
        /// <summary>
        /// Inserts an element into the <see cref="GraphableCollection"/> the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newGraph"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public virtual void Insert(int index, IGraphable newGraph)
        {
            lock (this)
            {
                if (newGraph == null)
                    return;
                if (graphs.Contains(newGraph))
                {
                    UnityEngine.Debug.LogError("Cannot add two of the same graph instance to a collection.");
                    return;
                }
                graphs.Insert(index, newGraph);
                newGraph.ValuesChanged += ValuesChangedSubscriber;
                newGraph.DisplayChanged += DisplayChangedSubscriber;
                OnDisplayChanged(new GraphElementAddedEventArgs(newGraph));
            }
        }
        /// <summary>
        /// Removes the first occurrence of the specified object.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public virtual bool Remove(IGraphable graph)
        {
            lock (this)
            {
                bool removed = graphs.Remove(graph);
                if (removed)
                {
                    graph.ValuesChanged -= ValuesChangedSubscriber;
                    graph.DisplayChanged -= DisplayChangedSubscriber;
                    OnDisplayChanged(new GraphElementRemovedEventArgs(graph));
                }
                return removed;
            }
        }
        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public virtual void RemoveAt(int index)
        {
            lock (this)
            {
                IGraphable graphable = graphs[index];
                graphs.RemoveAt(index);
                graphable.ValuesChanged -= ValuesChangedSubscriber;
                graphable.DisplayChanged -= DisplayChangedSubscriber;
                OnDisplayChanged(new GraphElementRemovedEventArgs(graphable));
            }
        }
        /// <summary>
        /// Subscribes to the contained objects' <see cref="IGraphable.ValuesChanged"/> events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected virtual void ValuesChangedSubscriber(object sender, IValueEventArgs eventArgs)
        {
            if (ignoreChildEvents)
                return;

            IValueEventArgs realEventArgs = eventArgs;
            if (realEventArgs is ChildValueChangedEventArgs wrappedEventArgs)
                realEventArgs = wrappedEventArgs.Unwrap();
            if (realEventArgs is ValuesChangedEventArgs)
            {
                OnValuesChanged(new ChildValueChangedEventArgs((IGraphable)sender, eventArgs));
                RecalculateLimits();
            }
        }
        /// <summary>
        /// Subscribes to the contained objects' <see cref="IGraphable.DisplayChanged"/> events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected virtual void DisplayChangedSubscriber(object sender, IDisplayEventArgs eventArgs)
        {
            if (ignoreChildEvents)
                return;

            if (eventArgs is VisibilityChangedEventArgs || (eventArgs is ChildDisplayChangedEventArgs childChangedEvent && childChangedEvent.Unwrap() is VisibilityChangedEventArgs))
            {
                RecalculateLimits();
            }
            IDisplayEventArgs realEventArgs = eventArgs;
            if (realEventArgs is ChildDisplayChangedEventArgs wrappedEventArgs)
                realEventArgs = wrappedEventArgs.Unwrap();
            if (realEventArgs is BoundsChangedEventArgs)
            {
                RecalculateLimits();
                return;
            }
            if (realEventArgs is GraphElementAddedEventArgs addedEvent && IsCircularReference(addedEvent.Graph))
            {
                UnityEngine.Debug.LogError("Cannot create a circular GraphableCollection. Removing problem reference.");
                Remove((IGraphable)sender);
                return;
            }
            if (realEventArgs is GraphElementsAddedEventArgs multiAddEvent && multiAddEvent.Graphs.Any(IsCircularReference))
            {
                UnityEngine.Debug.LogError("Cannot create a circular GraphableCollection. Removing problem reference.");
                Remove((IGraphable)sender);
                return;
            }
            OnDisplayChanged(new ChildDisplayChangedEventArgs((IGraphable)sender, eventArgs));
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        ///   <c>true</c> if the supplied item is in the collection; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(IGraphable item)
        {
            return graphs.Contains(item);
        }

        /// <summary>
        /// Determines whether an element with the given name is in the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <returns>
        ///   <c>true</c> if a graph of the specified name exists in the collection; otherwise, <c>false</c>.
        /// </returns>
        public bool HasGraphNamed(string name)
        {
            return graphs.Any(graph => graph.Name == name);
        }

        /// <summary>
        /// Copies the entire <see cref="GraphableCollection"/> to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void CopyTo(IGraphable[] array, int arrayIndex)
        {
            graphs.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IGraphable> GetEnumerator()
        {
            return Graphables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Graphables.GetEnumerator();
        }

        public void WriteToFileCSV(string path)
            => WriteToFileCSV(path, true);
        public void WriteToFileCSV(string path, bool limitToVisible)
        {
            IEnumerable<IGraphable> graphsToWrite = Flatten();
            if (limitToVisible)
                graphsToWrite = graphsToWrite.Where(Graphable.VisiblePredicate);

            if (CanCombineLineGraphs(graphsToWrite.Where(IsCombinableLineGraph)))
            {
                WriteLineGraphsToCombinedCSV(path, graphsToWrite.Where(IsCombinableLineGraph).Cast<ILineGraph>().ToArray());
                graphsToWrite = graphsToWrite.Where(g => !IsCombinableLineGraph(g));
            }
            else
            {
                graphsToWrite = graphs;
                if (limitToVisible)
                    graphsToWrite = graphsToWrite.Where(Graphable.VisiblePredicate);
            }

            HashSet<string> names = new HashSet<string>();
            
            foreach (IGraphable graph in graphsToWrite)
            {
                string graphSuffix = IO.GraphIO.StripInvalidFileChars(graph.DisplayName.Replace("/", "-").Replace("\\", "-"));
                if (string.IsNullOrEmpty(graphSuffix))
                    graphSuffix = graph.GetType().Name;
                if (names.Contains(graphSuffix))
                {
                    int i = 1;
                    while (names.Contains(string.Concat(graphSuffix, i.ToString())))
                        i++;
                    graphSuffix = string.Concat(graphSuffix, i.ToString());
                }
                names.Add(graphSuffix);
                graph.WriteToFileCSV(path.Insert(path.Length - 4, string.Concat("_", graphSuffix)));
            }
        }

        /// <summary>
        /// Outputs a set of compatible <see cref="LineGraph"/> values to a single file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="lineGraphs">The collection of <see cref="LineGraph"/>s to write in a combined file.</param>
        protected void WriteLineGraphsToCombinedCSV(string path, IList<ILineGraph> lineGraphs)
        {
            int count = lineGraphs.Count;
            string strCsv = "";
            if (lineGraphs[0].XName != "")
                strCsv += string.Format("{0} [{1}]", lineGraphs[0].XName, lineGraphs[0].XUnit != "" ? lineGraphs[0].XUnit : "-");
            else
                strCsv += string.Format("{0}", lineGraphs[0].XUnit != "" ? lineGraphs[0].XUnit : "-");

            for (int i = 0; i < count; i++)
            {
                if (lineGraphs[i].DisplayName != "")
                    strCsv += string.Format(",{0} [{1}]", lineGraphs[i].DisplayName, lineGraphs[i].YUnit != "" ? lineGraphs[i].YUnit : "-");
                else
                    strCsv += string.Format(",{0}", lineGraphs[i].YUnit != "" ? lineGraphs[i].YUnit : "-");
                if (lineGraphs[i] is MetaLineGraph metaLineGraph)
                {
                    for (int m = 0; m <= metaLineGraph.MetaFieldCount; m++)
                        strCsv += "," + (m > metaLineGraph.MetaFields.Length || string.IsNullOrEmpty(metaLineGraph.MetaFields[m]) ? "" : metaLineGraph.MetaFields[m]);
                }
            }

            try
            {
                System.IO.File.AppendAllText(path, strCsv + "\r\n");
            }
            catch (Exception ex) { UnityEngine.Debug.Log(ex.Message); }

            IEnumerator<float> xEnumerator = XValues(lineGraphs[0]).GetEnumerator();
            int j = -1;
            while (xEnumerator.MoveNext())
            {
                j++;
                strCsv = string.Format("{0}", xEnumerator.Current);
                for (int i = 0; i < count; i++)
                {
                    strCsv += string.Format(",{0:" + ((Graphable)lineGraphs[i]).StringFormat.Replace("N", "F") + "}", YValue(lineGraphs[i], j));
                    if (lineGraphs[i] is Line3Graph line3Graph)
                        strCsv += string.Format(",{0:" + ((Graphable)lineGraphs[i]).StringFormat.Replace("N", "F") + "}", line3Graph.Values[j].z);
                    if (lineGraphs[i] is MetaLineGraph metaLineGraph)
                    {
                        for (int m = 0; m <= metaLineGraph.MetaFieldCount; m++)
                        {
                            if (metaLineGraph.MetaStringFormats.Length >= m)
                                strCsv += "," + metaLineGraph.MetaData[m][j].ToString(metaLineGraph.MetaStringFormats[m].Replace("N", "F"));
                            else
                                strCsv += "," + metaLineGraph.MetaData[m][j].ToString();
                        }
                    }
                }
                try
                {
                    System.IO.File.AppendAllText(path, strCsv + "\r\n");
                }
                catch (Exception) { }
            }
#if OUTSIDE_UNITY
            static
#endif
            float YValue(ILineGraph iLineGraph, int index)
            {
                if (iLineGraph is LineGraph lineGraph)
                    return lineGraph.Values[index].y;
                else if (iLineGraph is Line3Graph line3Graph)
                    return line3Graph.Values[index].y;
                throw new InvalidCastException();
            }
        }

        private static bool IsCombinableLineGraph(IGraphable graph)
            => graph is LineGraph || graph is Line3Graph;
        private static IEnumerable<float> XValues(IGraphable graphable)
        {
            if (graphable is LineGraph lineGraph)
                return lineGraph.Values.Select(v => v.x);
            else if (graphable is Line3Graph line3Graph)
                return line3Graph.Values.Select(v => v.x);
            else
                return null;
        }
        private static bool CanCombineLineGraphs(IEnumerable graphs)
        {
            bool moreThanOne = false;
            IEnumerable<float> basisValues = null;
            foreach (IGraphable graph in graphs)
            {
                if (basisValues == null)
                {
                    basisValues = XValues(graph);
                    if (basisValues == null)
                        return false;
                    continue;
                }
                IEnumerable<float> values = XValues(graph);
                // A null value indicates it's not a compatible graph.
                if (values == null)
                    return false;
                // A zero-length value set is ignored.
                if (!values.Any())
                    continue;
                IEnumerator<float> test = values.GetEnumerator();
                IEnumerator<float> basis = basisValues.GetEnumerator();

                while (true)
                {
                    bool testNext = test.MoveNext();
                    bool baseNext = basis.MoveNext();
                    // Make sure they both expire at the same time.
                    // I.e. are the same length.
                    if (testNext != baseNext)
                        return false;
                    // Break if a MoveNext was false.
                    if (!testNext)
                        break;
                    // Compare the values.
                    if (test.Current != basis.Current)
                        return false;
                }
                moreThanOne = true;
            }
            return true && moreThanOne;
        }

        public virtual void WriteToFileXLS(string path, string worksheet)
            => WriteToFileXLS(path, worksheet, true);
        public virtual void WriteToFileXLS(string path, string worksheet, bool limitToVisible)
        {
            IEnumerable<IGraphable> graphsToWrite = Flatten();
            if (limitToVisible)
                graphsToWrite = graphsToWrite.Where(Graphable.VisiblePredicate);

            HashSet<string> names = new HashSet<string>();

            if (CanCombineLineGraphs(graphsToWrite.Where(IsCombinableLineGraph)))
            {
                System.Data.DataTable dataTable = new System.Data.DataTable(worksheet);
                AddLineGraphsToCombinedTable(dataTable, graphsToWrite.Where(IsCombinableLineGraph).Cast<ILineGraph>());
                if (string.IsNullOrWhiteSpace(worksheet))
                    worksheet = DisplayName;
                if (string.IsNullOrWhiteSpace(worksheet))
                    worksheet = IO.GraphIO.defaultSheetName;
                names.Add(worksheet);
                IO.GraphIO.SpreadsheetWriter.Write(path, worksheet, dataTable, IO.GraphIO.defaultOptions);
                graphsToWrite = graphsToWrite.Where(g => !IsCombinableLineGraph(g));
                dataTable.Dispose();
            }
            else
            {
                graphsToWrite = graphs;
                if (limitToVisible)
                    graphsToWrite = graphsToWrite.Where(Graphable.VisiblePredicate);
            }

            foreach (IGraphable graph in graphsToWrite)
            {
                string sheetName = IO.GraphIO.GetValidSheetName(graph);
                if (sheetName.Length + worksheet.Length + 1 >= 31)
                    throw new ArgumentException("Sheet name is too long (max 30 characters)", string.Join("_", worksheet, sheetName));
                sheetName = IO.GraphIO.GetUniqueName(names, sheetName);
                names.Add(sheetName);
                if (!string.IsNullOrEmpty(worksheet))
                    sheetName = string.Join("_", worksheet, sheetName);
                graph.WriteToFileXLS(path, sheetName);
            }
        }

        protected void AddLineGraphsToCombinedTable(System.Data.DataTable table, IEnumerable<ILineGraph> lineGraphs)
        {
            bool includeX = true;
            foreach (ILineGraph lineGraph in lineGraphs)
            {
                lineGraph.WriteToDataTable(table, includeX);
                includeX = false;
            }
        }
    }

    /// <summary>
    /// A collection of <see cref="IGraphable"/> items that may have Z components.
    /// </summary>
    public class GraphableCollection3 : GraphableCollection, IGraphable3
    {
        /// <summary>
        /// The lower Z bound of the object.
        /// </summary>
        public virtual float ZMin { get => zMin; set { zMin = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Depth, ZMin, ZMax), false); } }
        protected float zMin;
        /// <summary>
        /// The upper Z bound of the object.
        /// </summary>
        public virtual float ZMax { get => zMax; set { zMax = value; OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Depth, ZMin, ZMax), false); } }
        protected float zMax;

        /// <summary>
        /// The unit for the Z axis.
        /// </summary>
        public string ZUnit
        {
            get
            {
                return FirstVisibleGraph3(this)?.ZUnit ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            if (graphs[i] is IGraphable3 graphable3)
                                graphable3.ZUnit = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisUnitChangedEventArgs(AxisUI.AxisDirection.Depth, value));
            }
        }
        /// <summary>
        /// The name of the Z axis.
        /// </summary>
        public string ZName
        {
            get
            {
                return FirstVisibleGraph3(this)?.ZName ?? "";
            }
            set
            {
                lock (this)
                {
                    ignoreChildEvents = true;
                    try
                    {
                        for (int i = graphs.Count - 1; i >= 0; i--)
                            if (graphs[i] is IGraphable3 graphable3)
                                graphable3.ZName = value;
                    }
                    finally { ignoreChildEvents = false; }
                }
                OnDisplayChanged(new AxisNameChangedEventArgs(AxisUI.AxisDirection.Depth, value));
            }
        }

        /// <summary>
        /// The <see cref="ColorMap"/> of the dominant <see cref="SurfGraph"/>.
        /// </summary>
        public UnityEngine.Gradient dominantColorMap = Extensions.GradientExtensions.Jet_Dark;
        /// <summary>
        /// The index of the <see cref="SurfGraph"/> whose <see cref="ColorMap"/> is dominant.
        /// </summary>
        public int dominantColorMapIndex = -1;

        /// <summary>
        /// Constructs an empty <see cref="GraphableCollection3"/>.
        /// </summary>
        /// <param name="graphs"></param>
        public GraphableCollection3() : base() { }
        /// <summary>
        /// Constructs a <see cref="GraphableCollection3"/> with the provided <see cref="IGraphable"/>.
        /// </summary>
        /// <param name="graphs"></param>
        public GraphableCollection3(IGraphable graph) : base(graph) { }
        /// <summary>
        /// Constructs a <see cref="GraphableCollection3"/> with the provided <see cref="IGraphable"/>s.
        /// </summary>
        /// <param name="graphs"></param>
        public GraphableCollection3(IEnumerable<IGraphable> graphs) : base(graphs) { }

        /// <summary>
        /// Recalculates the reported limits of the <see cref="GraphableCollection"/>.
        /// </summary>
        /// <returns></returns>
        public override bool RecalculateLimits(bool expandSurfFilter = false)
        {
            float[] oldLimits = new float[] { XMin, XMax, YMin, YMax, ZMin, ZMax };
            xMin = xMax = yMin = yMax = zMin = zMax = float.NaN;
            dominantColorMap = null;

            for (int i = 0; i < graphs.Count; i++)
            {
                if (!graphs[i].Visible) continue;
                if (graphs[i] is IGraphable3 graphable3)
                {
                    float zMin = graphable3.ZMin;
                    float zMax = graphable3.ZMax;
                    if (zMin < this.zMin || float.IsNaN(this.zMin)) this.zMin = zMin;
                    if (zMax > this.zMax || float.IsNaN(this.zMax)) this.zMax = zMax;
                    if (graphable3 is IColorGraph colorGraph && dominantColorMap == null)
                    {
                        dominantColorMap = colorGraph.ColorScheme;
                        dominantColorMapIndex = i;
                    }
                }

                float xMin, xMax, yMin, yMax;
                if (!autoFitAxes)
                {
                    xMin = graphs[i].XMin;
                    xMax = graphs[i].XMax;
                    yMin = graphs[i].YMin;
                    yMax = graphs[i].YMax;
                }
                else
                {
                    if (graphs[i] is LineGraph lineGraph)
                    {
                        GetLimitsAutoLine(lineGraph, out xMin, out xMax, out yMin, out yMax);
                    }
                    else if (graphs[i] is SurfGraph surfGraph)
                    {
                        GetLimitsAutoSurf(surfGraph, out xMin, out xMax, out yMin, out yMax, expandSurfFilter);
                    }
                    else if (graphs[i] is OutlineMask)
                    {
                        continue;
                        //xMin = this.xMin; xMax = this.xMax; yMin = this.yMin; yMax = this.yMax;
                    }
                    else
                    {
                        xMin = graphs[i].XMin;
                        xMax = graphs[i].XMax;
                        yMin = graphs[i].YMin;
                        yMax = graphs[i].YMax;
                    }
                }

                if (xMin < this.xMin || float.IsNaN(this.xMin)) this.xMin = xMin;
                if (xMax > this.xMax || float.IsNaN(this.xMax)) this.xMax = xMax;
                if (yMin < this.yMin || float.IsNaN(this.yMin)) this.yMin = yMin;
                if (yMax > this.yMax || float.IsNaN(this.yMax)) this.yMax = yMax;
            }

            if (dominantColorMap == null)
                dominantColorMap = Extensions.GradientExtensions.Jet_Dark;

            if (float.IsNaN(xMin) || float.IsNaN(xMax))
                xMin = xMax = 0;
            if (float.IsNaN(yMin) || float.IsNaN(yMax))
                yMin = yMax = 0;
            if (float.IsNaN(zMin) || float.IsNaN(zMax))
                zMin = zMax = 0;

            bool result = false;
            if (!(oldLimits[0] == XMin && oldLimits[1] == XMax))
            {
                result = true;
                OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Horizontal, XMin, XMax), false);
            }
            if (!(oldLimits[2] == YMin && oldLimits[3] == YMax))
            {
                result = true;
                OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Vertical, YMin, YMax), false);
            }
            if (!(oldLimits[4] == ZMin && oldLimits[5] == ZMax))
            {
                result = true;
                OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Depth, ZMin, ZMax), false);
            }
            return result;
        }

        public static IGraphable3 FirstVisibleGraph3(IGraphable3 graph)
        {
            if (!graph.Visible)
                return null;
            if (graph is GraphableCollection collection)
            {
                foreach (IGraphable3 child in collection.Where(g=>g is IGraphable3))
                {
                    if (!child.Visible)
                        continue;
                    IGraphable3 result = child;
                    if (child is GraphableCollection3 childCollection)
                        result = FirstVisibleGraph3(childCollection);
                    if (result != null)
                        return result;
                }
                return null;
            }
            return graph;
        }
    }
}
