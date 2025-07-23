using Graphing.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UI_Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Graphing
{
    [ExecuteInEditMode]
    public class AxisUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, ILayoutElement, ISerializationCallbackReceiver
    {
        public readonly Axis axis = new Axis();
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never written to
        [SerializeField]
        private RectTransform tickHolder;
        [SerializeField]
        private Image bar;
        [SerializeField]
        private GameObject _tickPrefab;
        [SerializeField]
        private GameObject _axisBoundSetterPrefab;
        [SerializeField]
        private RawImage colorBar;
        [SerializeField]
        private RectTransform axisLabel;
        [SerializeField]
        public Material defaultSurfGraphMaterial;
#pragma warning restore CS0649 // Field is never written to
#pragma warning restore IDE0044 // Add readonly modifier
        [SerializeField]
        private Material axisMaterial;
        private bool materialWasInstantiated = false;

        [SerializeField]
        private bool autoSetMin = true;
        [SerializeField]
        private bool autoSetMax = true;

        public event EventHandler<Axis.AxisBoundsEventArgs> AxisBoundsChangedEvent;

        public static float XMinSelector(GraphDrawer g) => g.Graph.XMin;
        public static float XMaxSelector(GraphDrawer g) => g.Graph.XMax;
        public static float YMinSelector(GraphDrawer g) => g.Graph.YMin;
        public static float YMaxSelector(GraphDrawer g) => g.Graph.YMax;
        public static float ZMinSelector(GraphDrawer g) => (g.Graph as IGraphable3)?.ZMin ?? float.NaN;
        public static float ZMaxSelector(GraphDrawer g) => (g.Graph as IGraphable3)?.ZMax ?? float.NaN;
        public static float CMinSelector(GraphDrawer g) => (g.Graph as IColorGraph)?.CMin ?? (g.Graph as IGraphable3)?.ZMin ?? float.NaN;
        public static float CMaxSelector(GraphDrawer g) => (g.Graph as IColorGraph)?.CMax ?? (g.Graph as IGraphable3)?.ZMax ?? float.NaN;
        public static bool DepthPredicate(GraphDrawer g) => g.Graph is IGraphable3;
        public static bool ColorPredicate(GraphDrawer g) => g.Graph is IColorGraph;
        public static bool VisiblePredicate(GraphDrawer g) => Graphable.VisiblePredicate(g.graph);

        private (Func<GraphDrawer, float> min, Func<GraphDrawer, float> max) boundSelector = (XMinSelector, XMaxSelector);
        private IEnumerable<GraphDrawer> GraphDrawersAffectingBounds
        {
            get
            {
                IEnumerable<GraphDrawer> graphDrawers = attachedGraphDrawers.Where(VisiblePredicate);
                switch (_use)
                {
                    case AxisDirection.Horizontal:
                    case AxisDirection.Vertical:
                        return graphDrawers;
                    case AxisDirection.Depth:
                        graphDrawers = graphDrawers.Where(DepthPredicate);
                        foreach (GraphDrawerCollection drawer in attachedGraphDrawers.Where(VisiblePredicate).Where(d => d is GraphDrawerCollection && !DepthPredicate(d)).Cast<GraphDrawerCollection>())
                            graphDrawers = graphDrawers.Union(drawer.GetFlattenedCollection().Where(VisiblePredicate).Where(DepthPredicate));
                        return graphDrawers;
                    case AxisDirection.Color:
                    case AxisDirection.ColorWithDepth:
                        graphDrawers = graphDrawers.Where(ColorPredicate);
                        foreach (GraphDrawerCollection drawer in attachedGraphDrawers.Where(VisiblePredicate).Where(d => d is GraphDrawerCollection && !ColorPredicate(d)).Cast<GraphDrawerCollection>())
                            graphDrawers = graphDrawers.Union(drawer.GetFlattenedCollection().Where(VisiblePredicate).Where(ColorPredicate));
                        return graphDrawers;
                    default:
                        return Enumerable.Empty<GraphDrawer>();
                }
            }
        }

        private static bool IsaN(float v) => !float.IsNaN(v);
        public float AutoMin
        {
            get
            {
                IEnumerable<float> bounds = GraphDrawersAffectingBounds.Select(boundSelector.min).Where(IsaN);
                if (!bounds.Any())
                    return 0;
                float bound = bounds.Min();
                if (float.IsNegativeInfinity(bound))
                    return float.MinValue;
                if (float.IsPositiveInfinity(bound))
                    return float.MaxValue;
                return bound;
            }
        }

        public float AutoMax
        {
            get
            {
                IEnumerable<float> bounds = GraphDrawersAffectingBounds.Select(boundSelector.max).Where(IsaN);
                if (!bounds.Any())
                    return 0;
                float bound = bounds.Max();
                if (float.IsNegativeInfinity(bound))
                    return float.MinValue;
                if (float.IsPositiveInfinity(bound))
                    return float.MaxValue;
                return bound;
            }
        }

        public bool AutoSetMin
        {
            get => autoSetMin;
            set
            {
                if (autoSetMin == value)
                    return;
                autoSetMin = value;
                axis.AutoRoundMin = value;
                RecalculateBounds();
            }
        }

        public bool AutoSetMax
        {
            get => autoSetMax;
            set
            {
                if (autoSetMax == value)
                    return;
                autoSetMax = value;
                axis.AutoRoundMax = value;
                RecalculateBounds();
            }
        }

        public float Min
        {
            get => axis.Min;
            set
            {
                autoSetMin = false;
                axis.AutoRoundMin = false;
                axis.Min = value;
            }
        }
        public float Max
        {
            get=> axis.Max;
            set
            {
                autoSetMax = false;
                axis.AutoRoundMax = false;
                axis.Max = value;
            }
        }

        public void SetBounds(float min, float max)
        {
            autoSetMin = false;
            autoSetMax = false;
            axis.AutoRoundMin = false;
            axis.AutoRoundMax = false;
            axis.SetBounds(min, max);
        }

        public void AutoSetBounds()
        {
            autoSetMin = true;
            autoSetMax = true;
            axis.AutoRoundMin = true;
            axis.AutoRoundMax = true;
            axis.SetBounds(AutoMin, AutoMax);
        }

        public void RecalculateBounds()
        {
            float max = autoSetMax ? AutoMax : Max;
            float min = autoSetMin ? AutoMin : Min;
            // Ignore auto bounds if they conflict with a manual bound.
            if (max < min)
            {
                if (autoSetMin && !autoSetMax)
                    axis.SetBounds(max, max);
                else if (autoSetMax && !autoSetMin)
                    axis.SetBounds(min, min);
                else if (max == 0 || min == 0)
                    axis.SetBounds(0, 0);
                else
                    axis.SetBounds(max, min);
            }
            else
                axis.SetBounds(min, max);
        }

        [SerializeField]
        private AxisDirection _use = AxisDirection.Undefined;
        public AxisDirection Use
        {
            get => _use;
            set
            {
                _use = value;
                switch (_use)
                {
                    case AxisDirection.Horizontal:
                        boundSelector = (XMinSelector, XMaxSelector);
                        axis.horizontal = true;
                        break;
                    case AxisDirection.Vertical:
                        boundSelector = (YMinSelector, YMaxSelector);
                        axis.horizontal = false;
                        break;
                    case AxisDirection.Depth:
                        boundSelector = (ZMinSelector, ZMaxSelector);
                        axis.horizontal = false;
                        break;
                    case AxisDirection.Color:
                    case AxisDirection.ColorWithDepth:
                        boundSelector = (CMinSelector, CMaxSelector);
                        axis.horizontal = true;
                        break;
                }
                if ((AutoSetMin || AutoSetMax) && GraphDrawersAffectingBounds.Any())
                    RecalculateBounds();
            }
        }

        public enum AxisDirection
        {
            Undefined = 0,
            Horizontal = 1,
            Vertical = 2,
            Depth = 4,
            Color = 8,
            ColorWithDepth = Depth | Color
        }

        public Material AxisMaterial
        {
            get
            {
                if (axisMaterial == null)
                {
                    if (defaultSurfGraphMaterial == null)
                        return null;
                    axisMaterial = Instantiate(defaultSurfGraphMaterial);
                    materialWasInstantiated = true;
                }
                return axisMaterial;
            }
            set
            {
                if (axisMaterial != null)
                {
                    foreach (GraphDrawer.ISurfMaterialUser grapher in attachedGraphDrawers.Where(gd => gd is GraphDrawer.ISurfMaterialUser).Cast<GraphDrawer.ISurfMaterialUser>().Where(g => g.SharedSurfGraphMaterial == axisMaterial))
                        grapher.SurfGraphMaterial = value;
                    if (materialWasInstantiated)
                        Destroy(axisMaterial);
                }
                materialWasInstantiated = false;
                axisMaterial = value;
            }
        }
        public string AxisLabel
        {
            get
            {
                string label = _label ?? "";
                bool showSquareBraces = !string.IsNullOrEmpty(label);
                return string.Format("{0}{1}",
                    _label,
                    string.IsNullOrEmpty(_unit) ? "" :
                        string.Format("{0}{1}{2}",
                            showSquareBraces ? " [" : "",
                            _unit,
                            showSquareBraces ? "]" : ""));
            }
        }

        private string _unit = "-";
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                axisText.Text = AxisLabel;
            }
        }
        private string _label = "";
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                axisText.Text = AxisLabel;
            }
        }

        private UI_Tools.Universal_Text.UT_Text axisText;

        private readonly AxisBoundSetter[] axisBoundSetters = new AxisBoundSetter[2];

        private readonly List<Tickmark> ticks = new List<Tickmark>();

        [SerializeField]
        private AxisSide _side = AxisSide.Left;
        public AxisSide Side { get => _side; set { _side = value; RedrawAxis(); } }

        private Texture2D shaderTex = null;
        private Texture2D shaderMapTex = null;

        private readonly List<GraphDrawer> attachedGraphDrawers = new List<GraphDrawer>();
        public IEnumerable<GraphDrawer> AttachedGraphDrawers { get => attachedGraphDrawers; }

        [SerializeField]
        private int _barThickness = 1;
        public int BarThickness { get => _barThickness; set { _barThickness = value; RedrawAxis(); } }

        [SerializeField]
        private int _tickThickness = 1;
        public int TickThickness { get => _tickThickness; set { _tickThickness = value; RedrawTicks(); } }

        [SerializeField]
        private int _tickWidth = 5;
        public int TickWidth { get => _tickWidth; set { _tickWidth = value; RedrawTicks(); } }

        [SerializeField]
        private int _tickSpacing = 2;
        public int TickSpacing { get => _tickSpacing; set { _tickSpacing = value; RedrawTicks(); } }

        [SerializeField]
        private Color _axisColor = Color.black;
        public Color AxisColor { get => _axisColor; set { _axisColor = value; bar.color = _axisColor; } }

        public Texture2D AxisBackground
        {
            get => colorBar.texture as Texture2D;
            set
            {
                if (value == null)
                {
                    colorBar.texture = value;
                    colorBar.gameObject.SetActive(false);
                }
                else
                {
                    colorBar.texture = value;
                    _rotateBackground = value.height > value.width;
                    colorBar.gameObject.SetActive(true);
                }
            }
        }
        private bool _rotateBackground = false;

        public bool ShowAxisLabel
        {
            get => _showAxisLabel;
            set
            {
                bool changed = _showAxisLabel != value;
                _showAxisLabel = value;
                axisLabel.gameObject.SetActive(value);
                if (changed)
                    RedrawAxis();
            }
        }
        private bool _showAxisLabel = true;

        [SerializeField]
        private Color _tickColor = Color.black;
        public Color TickColor { get => _tickColor; set { _tickColor = value; foreach (Tickmark tick in ticks) tick.TickColor = _tickColor; } }

        [SerializeField]
        private Color _textColor = Color.black;
        public Color TextColor { get => _textColor; set { _textColor = value; axisText.color = value; foreach (Tickmark tick in ticks) tick.FontColor = _textColor; } }

        [SerializeField]
        private float _tickFontSize = 8;
        public float TickFontSize { get => _tickFontSize; set { _tickFontSize = value; _tickAutoFontSize = false; RedrawTicks(); } }

        [SerializeField]
        private float _fontSizeMin = 1;
        public float FontSizeMin { get => _fontSizeMin; set { _fontSizeMin = value; RedrawTicks(); } }

        [SerializeField]
        private float _fontSizeMax = 72;
        public float FontSizeMax { get => _fontSizeMax; set { _fontSizeMax = value; RedrawTicks(); } }

        [SerializeField]
        private bool _tickAutoFontSize = false;
        public bool TickAutoFontSize { get => _tickAutoFontSize; set { _tickAutoFontSize = value; RedrawTicks(); } }

        /// <summary>
        /// The tick mark labels.
        /// </summary>
        public List<string> TickLabels { get => labels; }
        private List<string> labels;
        private List<float> fractions = new List<float> { 0, 1 };
        /// <summary>
        /// The number of tick marks.
        /// </summary>
        public int TickCount { get; private set; }

        public float GetValue(float normalizedValue)
        {
            return Mathf.Lerp(Min, Max, normalizedValue);
        }

        protected virtual void BoundsChanged(object sender, Axis.AxisBoundsEventArgs eventArgs)
        {
            if (sender != axis)
                return;

            SetOriginsAndScales();

            if ((_use & AxisDirection.Color) > 0 && axisMaterial != null)
                axisMaterial.SetRange(Min, Max);

            GenerateTicksAndLabels();
            RedrawTicks();
            AxisBoundsChangedEvent?.Invoke(this, eventArgs);
        }

        private void SetOriginsAndScales()
        {
            foreach (GraphDrawer graphDrawer in attachedGraphDrawers)
                graphDrawer.SetOriginAndScale(_use, Min, 1 / (Max - Min));
        }

        private void GenerateTicksAndLabels()
        {
            if (Min == Max || float.IsNaN(Min) || float.IsNaN(Max) || float.IsInfinity(Min) || float.IsInfinity(Max))
            {
                TickCount = 1;
                labels = new List<string>() { string.Format("{0}", Min), string.Format("{0}", Max) };
                return;
            }

            TickCount = Mathf.RoundToInt((Max - Min) / axis.MajorUnit);

            float minVar = Min / axis.MajorUnit;
            float maxVar = Max / axis.MajorUnit;

            float rangeVar = (Max - Min) / axis.MajorUnit;
            float firstRealTick = Mathf.Ceil(minVar) * axis.MajorUnit;

            labels = new List<string>(TickCount + 2);
            fractions = new List<float>(TickCount + 2);
            for (float f = firstRealTick; f <= Max; f += axis.MajorUnit)
            {
                labels.Add(string.Format("{0}", f));
                fractions.Add((f - Min) / (Max - Min));
            }
            int lastIndex = fractions.Count - 1;
            if (Mathf.Abs(fractions[lastIndex] - 1) <= roundError)
            {
                labels[lastIndex] = string.Format("{0}", Max);
                fractions[lastIndex] = 1;
            }
            else
            {
                labels.Add(string.Format("{0}", Max));
                fractions.Add(1);
            }
            if (Mathf.Abs(fractions[0]) <= roundError)
            {
                labels[0] = string.Format("{0}", Min);
                fractions[0] = 0;
            }
            else
            {
                labels.Insert(0, string.Format("{0}", Min));
                fractions.Insert(0, 0);
            }
            TickCount = labels.Count - 1;
        }
        private const float roundError = 0.05f;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            Use = _use;
            axisText = axisLabel.GetComponent<UI_Tools.Universal_Text.UT_Text>();
            axis.AxisBoundsChangedEvent += BoundsChanged;
            GenerateTicksAndLabels();
            axis.SetBounds(Min, Max);
            RecalculateBounds();
            SetOriginsAndScales();
            
            if ((_use & AxisDirection.Color) > 0 && axisMaterial != null)
                axisMaterial.SetRange(Min, Max);
        }

        // Start is called before the first frame update
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start()
        {
            ticks.Clear();
            foreach (Tickmark tick in GetComponentsInChildren<Tickmark>())
                ticks.Add(tick);
            colorBar.rectTransform.GetComponent<RotatedNodeFillParent>().ForceDisable();
            RedrawAxis();
        }

        public void SetPrimaryController(GraphDrawer graphDrawer)
        {
            if (attachedGraphDrawers.Contains(graphDrawer))
            {
                attachedGraphDrawers.Remove(graphDrawer);
            }
            attachedGraphDrawers.Insert(0, graphDrawer);
            SetUpAxis(graphDrawer.FirstVisibleInHierarchy);
        }

        public bool Contains(GraphDrawer graphDrawer) => attachedGraphDrawers.Contains(graphDrawer);
        public bool Contains(IGraphable graphable) => attachedGraphDrawers.Any(g => g.Graph == graphable);
        public bool AssignGraphsToAxis(IEnumerable<GraphDrawer> graphDrawers)
        {
            bool value = true;
            foreach (GraphDrawer graphDrawer in graphDrawers)
                value &= AssignGraphToAxis(graphDrawer);
            return value;
        }
        public bool AssignGraphToAxis(GraphDrawer graphDrawer)
        {
            if (attachedGraphDrawers.Contains(graphDrawer))
                return false;
            attachedGraphDrawers.Add(graphDrawer);
            if (FirstVisibleGraph() == graphDrawer.FirstVisibleInHierarchy)
            {
                if ((_use & AxisDirection.Color) > 0)
                {
                    IColorGraph firstColorGraph = graphDrawer.FirstColorGraphInHierarchy;
                    if (firstColorGraph != null)
                    {
                        AxisMaterial = InstantiateMaterial(firstColorGraph);
                        materialWasInstantiated = true;
                    }
                }
                SetUpAxis(graphDrawer.FirstVisibleInHierarchy);
            }
            else if (autoSetMin ||  AutoSetMax)
                RecalculateBounds();

            if ((_use & AxisDirection.Color) > 0 && graphDrawer is GraphDrawer.ISurfMaterialUser surfUser)
            {
                surfUser.SurfGraphMaterial = AxisMaterial;
            }

            SetOriginsAndScales();

            graphDrawer.Graph.DisplayChanged += GraphDisplayChangeHandler;
            return true;
        }

        public bool DetachGraphsFromAxis(IEnumerable<GraphDrawer> graphDrawers)
        {
            bool value = true;
            foreach (GraphDrawer graphDrawer in graphDrawers)
                value &= DetachGraphFromAxis(graphDrawer);
            return value;
        }
        public bool DetachGraphFromAxis(GraphDrawer graphDrawer)
        {
            bool resetAxis = false;
            if (FirstVisibleGraph() == graphDrawer.FirstVisibleInHierarchy)
                resetAxis = true;

            if (!attachedGraphDrawers.Remove(graphDrawer))
                return false;
            graphDrawer.Graph.DisplayChanged -= GraphDisplayChangeHandler;
            if (autoSetMin || autoSetMax)
                RecalculateBounds();
            if (resetAxis)
                SetUpAxis(FirstVisibleGraph());
            return true;
        }

        private void GraphDisplayChangeHandler(object sender, IDisplayEventArgs eventArgs)
        {
            if (eventArgs is ChildDisplayChangedEventArgs childArgs)
                eventArgs = childArgs.Unwrap();

            if (eventArgs is TransposeChangedEventArgs)
            {
                if (Use == AxisDirection.Horizontal || Use == AxisDirection.Vertical)
                {
                    RecalculateBounds();
                }
                return;
            }

            if (eventArgs is VisibilityChangedEventArgs)
            {
                SetUpAxis(FirstVisibleGraph());
                return;
            }

            if (eventArgs is BoundsChangedEventArgs boundsChangedEventArgs)
            {
                if ((boundsChangedEventArgs.Axis & Use) == AxisDirection.Undefined)
                    return;
                // Set axis bounds for all axes driven by this drawer.
                if (AutoSetMin || AutoSetMax)
                    RecalculateBounds();
                return;
            }

            if (FirstVisibleGraph() != sender)
                return;

            if (eventArgs is AxisNameChangedEventArgs axisNameChangedEventArgs)
            {
                // Set axis label for all axes driven by drawer.
                if ((axisNameChangedEventArgs.Axis & Use) == AxisDirection.Undefined)
                    return;
                Label = axisNameChangedEventArgs.AxisName;
                return;
            }
            if (eventArgs is AxisUnitChangedEventArgs axisUnitChangedEventArgs)
            {
                // Set axis units for all axes driven by drawer.
                if ((axisUnitChangedEventArgs.Axis & Use) == AxisDirection.Undefined)
                    return;
                Unit = axisUnitChangedEventArgs.Unit;
                return;
            }
        }

        private IGraphable FirstVisibleGraph()
        {
            foreach (IGraphable graph in attachedGraphDrawers.Select(d => d.FirstVisibleInHierarchy))
            {
                if (graph != null)
                    return graph;
            }
            return null;
        }

        /// <summary>
        /// Sets up an axis object based on a given graph drawer.
        /// </summary>
        /// <param name="axis">The axis to set up.</param>
        /// <param name="drawer">The graph drawer to drive its settings.</param>
        public void SetUpAxis(IGraphable graph)
        {
            if (axisText == null)
                axisText = axisLabel.GetComponent<UI_Tools.Universal_Text.UT_Text>();

            if (graph == null)
            {
                Label = "";
                Unit = "-";
                RecalculateBounds();
                return;
            }

            switch (Use)
            {
                case AxisDirection.Horizontal:
                    Label = graph.XName;
                    Unit = graph.XUnit;
                    RecalculateBounds();
                    return;
                case AxisDirection.Vertical:
                    Label = graph.YName;
                    Unit = graph.YUnit;
                    RecalculateBounds();
                    return;
                case AxisDirection.Depth:
                    if (graph is IGraphable3 graphable3)
                    {
                        Label = graphable3.ZName;
                        Unit = graphable3.ZUnit;
                        RecalculateBounds();
                    }
                    return;
                case AxisDirection.Color:
                case AxisDirection.ColorWithDepth:
                    if (graph is IColorGraph colorGraph)
                    {
                        Label = colorGraph.CName;
                        Unit = colorGraph.CUnit;
                    }
                    else if (graph is IGraphable3 _graphable3)
                    {
                        Label = _graphable3.ZName;
                        Unit = _graphable3.ZUnit;
                    }
                    else
                        return;
                    RecalculateBounds();
                    return;
                default:
                    Debug.LogError("The selected axis use is not available: " + Use);
                    return;
            }
        }

        public void SetAxisStyle(AxisStyle style)
        {
            if (style.autoMin != null)
                axis.AutoRoundMin = (bool)style.autoMin;
            if (style.autoMax != null)
                axis.AutoRoundMax = (bool)style.autoMax;
            if (style.barThickness != null)
                this._barThickness = (int)style.barThickness;
            if (style.tickThickness != null)
                this._tickThickness = (int)style.tickThickness;
            if (style.tickWidth != null)
                this._tickWidth = (int)style.tickWidth;
            if (style.tickSpacing != null)
                this._tickSpacing = (int)style.tickSpacing;
            if (style.axisColor != null)
                this._axisColor = (Color)style.axisColor;
            if (style.tickColor != null)
                this._tickColor = (Color)style.tickColor;
            if (style.textColor != null)
                this._textColor = (Color)style.textColor;
            if (style.fontSize != null)
                this._tickFontSize = (float)style.fontSize;
            if (style.fontSizeMin != null)
                this._fontSizeMin = (float)style.fontSizeMin;
            if (style.fontSizeMax != null)
                this._fontSizeMax = (float)style.fontSizeMax;
            if (style.autoFontSize != null)
                this._tickAutoFontSize = (bool)style.autoFontSize;

            RedrawAxis();
        }

        public void RedrawAxis()
        {
            RectTransform barTransform = bar.rectTransform;
            RectTransform ticksTransform = tickHolder;
            RectTransform colorBarTransform = colorBar.rectTransform;
            RectTransform colorBarParentTransform = (RectTransform)colorBarTransform.parent;
            ticksTransform.anchorMin = new Vector2(0, 0);
            ticksTransform.anchorMax = new Vector2(1, 1);
            
            float colorBarRotation = ((_side == AxisSide.Bottom || _side == AxisSide.Top) && _rotateBackground) || ((_side == AxisSide.Left || _side == AxisSide.Right) && !_rotateBackground) ? 90 : 0;

            colorBarTransform.localRotation = Quaternion.Euler(0, 0, colorBarRotation);
            colorBarTransform.GetComponent<RotatedNodeFillParent>().enabled = colorBarRotation != 0;

            float axisTextOffset = _showAxisLabel ? axisText.LayoutElement.preferredHeight : 0;

            switch (_side)
            {
                case AxisSide.Bottom:
                    barTransform.pivot = colorBarParentTransform.pivot = new Vector2(0.5f, 1);
                    barTransform.anchorMin = colorBarParentTransform.anchorMin = new Vector2(0, 1);
                    barTransform.anchorMax = colorBarParentTransform.anchorMax = new Vector2(1, 1);
                    barTransform.offsetMax = barTransform.offsetMin = Vector2.zero;
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _barThickness);
                    colorBarParentTransform.offsetMin = colorBarParentTransform.offsetMax = new Vector2(0, 0);
                    colorBarParentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _tickWidth);
                    colorBarParentTransform.anchoredPosition = new Vector2(0, -1);
                    ticksTransform.offsetMin = Vector2.zero;
                    ticksTransform.offsetMax = new Vector2(0, -_barThickness);
                    axisLabel.localRotation = Quaternion.identity;
                    axisLabel.anchorMin = new Vector2(0, 0);
                    axisLabel.anchorMax = new Vector2(1, 0);
                    axisLabel.pivot = new Vector2(0.5f, 0);
                    break;
                case AxisSide.Top:
                    barTransform.pivot = colorBarParentTransform.pivot = new Vector2(0.5f, 0);
                    barTransform.anchorMin = colorBarParentTransform.anchorMin = new Vector2(0, 0);
                    barTransform.anchorMax = colorBarParentTransform.anchorMax = new Vector2(1, 0);
                    barTransform.offsetMax = barTransform.offsetMin = Vector2.zero;
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _barThickness);
                    colorBarParentTransform.offsetMin = colorBarParentTransform.offsetMax = new Vector2(0, 0);
                    colorBarParentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _tickWidth);
                    colorBarParentTransform.anchoredPosition = new Vector2(0, 1);
                    ticksTransform.offsetMin = new Vector2(0, _barThickness);
                    ticksTransform.offsetMax = Vector2.zero;
                    axisLabel.localRotation = Quaternion.identity;
                    axisLabel.anchorMin = new Vector2(0, 1);
                    axisLabel.anchorMax = new Vector2(1, 1);
                    axisLabel.pivot = new Vector2(0.5f, 1);
                    break;
                case AxisSide.Right:
                    barTransform.pivot = colorBarParentTransform.pivot = new Vector2(0, 0.5f);
                    barTransform.anchorMin = colorBarParentTransform.anchorMin = new Vector2(0, 0);
                    barTransform.anchorMax = colorBarParentTransform.anchorMax = new Vector2(0, 1);
                    barTransform.offsetMax = barTransform.offsetMin = Vector2.zero;
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _barThickness);
                    colorBarParentTransform.offsetMin = colorBarParentTransform.offsetMax = new Vector2(0, 0);
                    colorBarParentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _tickWidth);
                    colorBarParentTransform.anchoredPosition = new Vector2(1, 0);
                    ticksTransform.offsetMin = new Vector2(_barThickness, 0);
                    ticksTransform.offsetMax = Vector2.zero;
                    axisLabel.localRotation = Quaternion.Euler(0, 0, 90);
                    axisLabel.anchorMin = new Vector2(1, 0.5f);
                    axisLabel.anchorMax = new Vector2(1, 0.5f);
                    axisLabel.pivot = new Vector2(0.5f, 0);
                    break;
                default:
                case AxisSide.Left:
                    barTransform.pivot = colorBarParentTransform.pivot = new Vector2(1, 0.5f);
                    barTransform.anchorMin = colorBarParentTransform.anchorMin = new Vector2(1, 0);
                    barTransform.anchorMax = colorBarParentTransform.anchorMax = new Vector2(1, 1);
                    barTransform.offsetMax = barTransform.offsetMin = Vector2.zero;
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _barThickness);
                    colorBarParentTransform.offsetMin = colorBarParentTransform.offsetMax = new Vector2(0, 0);
                    colorBarParentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _tickWidth);
                    colorBarParentTransform.anchoredPosition = new Vector2(-1, 0);
                    ticksTransform.offsetMin = Vector2.zero;
                    ticksTransform.offsetMax = new Vector2(-_barThickness, 0);
                    axisLabel.localRotation = Quaternion.Euler(0, 0, 90);
                    axisLabel.anchorMin = new Vector2(0, 0.5f);
                    axisLabel.anchorMax = new Vector2(0, 0.5f);
                    axisLabel.pivot = new Vector2(0.5f, 1);
                    break;
            }

            RedrawTicks();
        }

        private void RedrawTicks()
        {
#if UNITY_EDITOR
            foreach (Tickmark tick in GetComponentsInChildren<Tickmark>())
                if (!ticks.Contains(tick))
                    ticks.Add(tick);
#endif
            if (ticks.Count > TickCount)
            {
                if (axisBoundSetters[1] != null && TickCount > 0)
                    axisBoundSetters[1].transform.SetParent(ticks[TickCount].transform, false);
                for (int i = ticks.Count - 1; i > TickCount; i--)
                {
#if UNITY_EDITOR
                    DestroyImmediate(ticks[i].gameObject);
#else
                    Destroy(ticks[i].gameObject);
#endif
                    ticks.RemoveAt(i);
                }
            }
            else
            {
                for (int i = ticks.Count; i <= TickCount; i++)
                    ticks.Add(Instantiate(_tickPrefab, tickHolder).GetComponent<Tickmark>());
                if (axisBoundSetters[1] != null && TickCount > 0)
                    axisBoundSetters[1].transform.SetParent(ticks[TickCount].transform, false);
            }
            for (int i = TickCount; i >= 0; i--)
            {
                if (ticks == null)
                    Debug.Log("ticks is null.");
                else if (labels == null)
                    Debug.Log("labels is null.");
                ticks[i].Text = labels[i];
                ticks[i].anchorFraction = fractions[i];
            }

            foreach (Tickmark tick in ticks)
            {
                tick.tickThickness = this._tickThickness;
                tick.tickWidth = this._tickWidth;
                tick.tickSpacing = this._tickSpacing;
                tick.side = this._side;

                tick.TickColor = this._tickColor;
                tick.FontColor = this._textColor;

                if (_tickAutoFontSize)
                {
                    tick.FontSizeMin = this._fontSizeMin;
                    tick.FontSizeMax = this._fontSizeMax;
                    tick.AutoFontSize = true;
                }
                else
                {
                    tick.FontSize = this._tickFontSize;
                }
                tick.RedrawTickmark();
            }
            axisBoundSetters[0]?.UpdatePosition();
            axisBoundSetters[1]?.UpdatePosition();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

#if UNITY_EDITOR
        //private void OnValidate() => RedrawAxis();
        /*{
            if (max < min)
            {
                Debug.LogError("Cannot set maximum lower than minimum.");
                max = min;
            }
            CalculateBounds(min, max, side == AxisSide.Top || side == AxisSide.Bottom, forceBounds);
            if (isActiveAndEnabled)
                StartCoroutine(RedrawNextFrame());
        }
        private IEnumerator RedrawNextFrame()
        {
            yield return new WaitForEndOfFrame();
            RedrawAxis();
        }*/
#endif

        public Material InstantiateMaterial(IGraph graph)
        {
            Material material = Instantiate(defaultSurfGraphMaterial);
            ColorMapMaterial.GenerateTextures(graph.ColorScheme, ref shaderTex, ref shaderMapTex, AxisBackground);
            material.SetTexture(shaderTex);
            material.SetMode(ColorMapMaterial.Mode.Custom);
            material.SetColorMapSource(ColorMapMaterial.MapSource.Texture);
            material.SetMapTexture(shaderMapTex);
            material.SetStep(graph.ColorScheme.mode == GradientMode.Fixed);
            if (AxisBackground != null)
                AxisBackground.filterMode = graph.ColorScheme.mode == GradientMode.Blend ? FilterMode.Bilinear : FilterMode.Point;
            material.SetRange(Min, Max);

            return material;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            AxisBoundSetter window = null;
            if (axisBoundSetters[1] == null && TickCount > 0 && eventData.rawPointerPress.transform.IsChildOf(ticks[TickCount].transform))
                window = axisBoundSetters[1] = SpawnSetterWindow(1, ticks[TickCount].transform);
            else if (axisBoundSetters[0] == null && TickCount > 0 && eventData.rawPointerPress.transform.IsChildOf(ticks[0].transform))
                window = axisBoundSetters[0] = SpawnSetterWindow(-1, ticks[0].transform);
            if (window != null)
            {
                EventSystem.current.SetSelectedGameObject(window.gameObject);
                eventData.Use();
            }
        }
        public AxisBoundSetter SpawnSetterWindow(int bound, Transform parentTransform)
        {
            AxisBoundSetter boundSetter = Instantiate(_axisBoundSetterPrefab, parentTransform).GetComponent<AxisBoundSetter>();
            boundSetter.Init(this, bound);

            return boundSetter;
        }
        internal void UnregisterSetterWindow(AxisBoundSetter boundSetter)
        {
            if (boundSetter == axisBoundSetters[0])
                axisBoundSetters[0] = null;
            else if (boundSetter == axisBoundSetters[1])
                axisBoundSetters[1] = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!(TickCount > 0 && eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(ticks[TickCount].transform)) &&
                !(TickCount > 0 && eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(ticks[0].transform)))
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
        {
            axis.AxisBoundsChangedEvent -= BoundsChanged;
            foreach (GraphDrawer graphDrawer in attachedGraphDrawers)
            {
                graphDrawer.Graph.DisplayChanged -= GraphDisplayChangeHandler;
            }
            if (shaderTex != null)
                Destroy(shaderTex);
            if (shaderMapTex != null) 
                Destroy(shaderMapTex);
            if (materialWasInstantiated && axisMaterial != null)
                Destroy(axisMaterial);
        }

        [SerializeField]
        private float min, max;
        [SerializeField]
        private bool autoRoundMin = true;
        [SerializeField]
        private bool autoRoundMax = true;
        public void OnBeforeSerialize()
        {
            min = axis.Min;
            max = axis.Max;
            autoRoundMin = axis.AutoRoundMin;
            autoRoundMax = axis.AutoRoundMax;
        }

        public void OnAfterDeserialize()
        {
            axis.AutoRoundMin = autoRoundMin;
            axis.AutoRoundMax = autoRoundMax;
            axis.horizontal = _use == AxisDirection.Horizontal || _use == AxisDirection.Color || _use == AxisDirection.ColorWithDepth;
            axis.SetBounds(min, max);
        }

        public float minWidth => minSizeX;

        public float preferredWidth => preferredSizeX;

        public float flexibleWidth => 0;

        public float minHeight => minSizeY;

        public float preferredHeight => preferredSizeY;

        public float flexibleHeight => 0;

        public int layoutPriority => 100;

        private float preferredSizeX, minSizeX;
        private float preferredSizeY, minSizeY;

        public void CalculateLayoutInputHorizontal()
        {
            preferredSizeX = 0;
            minSizeX = 0;
            bool horizontal = _side == AxisSide.Bottom || _side == AxisSide.Top;

            foreach (Tickmark tick in ticks)
            {
                if (horizontal)
                {
                    minSizeX += tick.minWidth;
                    preferredSizeX += tick.preferredWidth;
                }
                else
                {
                    minSizeX = Mathf.Max(tick.minWidth, minSizeX);
                    preferredSizeX = Mathf.Max(tick.preferredWidth, preferredSizeX);
                }
            }

            if (horizontal)
            {
                preferredSizeX = Mathf.Max(preferredSizeX, axisText.LayoutElement.preferredWidth);
            }
            else
            {
                preferredSizeX += _barThickness;
                minSizeX = preferredSizeX;
                if (_showAxisLabel)
                {
                    preferredSizeX += axisText.LayoutElement.preferredHeight;
                    minSizeX += axisText.LayoutElement.preferredHeight;
                }
            }
        }

        public void CalculateLayoutInputVertical()
        {
            preferredSizeY = 0;
            minSizeY = 0;
            bool horizontal = _side == AxisSide.Bottom || _side == AxisSide.Top;

            foreach (Tickmark tick in ticks)
            {
                if (horizontal)
                {
                    minSizeY = Mathf.Max(tick.minHeight, minSizeY);
                    preferredSizeY = Mathf.Max(tick.preferredHeight, preferredSizeY);
                }
                else
                {
                    minSizeY += tick.minHeight;
                    preferredSizeY += tick.preferredHeight;
                }
            }

            if (horizontal)
            {
                preferredSizeY += _barThickness;
                minSizeY = preferredSizeY;
                if (_showAxisLabel)
                {
                    preferredSizeY += axisText.LayoutElement.preferredHeight;
                    minSizeY += axisText.LayoutElement.preferredHeight;
                }
            }
            else
            {
                preferredSizeY = Mathf.Max(preferredSizeY, axisText.LayoutElement.preferredWidth);
            }
        }
    }

    public enum AxisSide
    {
        Left,
        Right,
        Bottom,
        Top
    }
}