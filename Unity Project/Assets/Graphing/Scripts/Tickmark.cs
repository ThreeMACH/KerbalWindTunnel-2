using UnityEngine;
using UnityEngine.UI;

namespace Graphing
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class Tickmark : MonoBehaviour, ILayoutElement
    {
        [SerializeField]
        internal AxisSide side = AxisSide.Left;
        [SerializeField]
        internal int tickThickness = 1;
        [SerializeField]
        internal int tickWidth = 5;
        [SerializeField]
        internal int tickSpacing = 2;

        public float anchorFraction = 0.5f;

        public UI_Tools.Universal_Text.UT_Text Label { get => label; }
        public AxisSide Side { get => side; set { side = value; RedrawTickmark(); } }
        public int TickThickness { get => tickThickness; set { tickThickness = value; RedrawTickmark(); } }
        public int TickWidth { get => tickWidth; set { tickWidth = value; RedrawTickmark(); } }
        public int TickSpacing { get => tickSpacing; set { tickSpacing = value; RedrawTickmark(); } }

        public float FontSize { get => label.FontSize; set { label.EnableAutoSizing = false; label.FontSize = value; } }
        public bool AutoFontSize { get => label.EnableAutoSizing; set => label.EnableAutoSizing = value; }
        public float FontSizeMin { get => label.FontSizeMin; set => label.FontSizeMin = value; }
        public float FontSizeMax { get => label.FontSizeMax; set => label.FontSizeMax = value; }
        public Color FontColor { get => label.color; set => label.color = value; }
        public Color TickColor { get => tick.color; set => tick.color = value; }
        public string Text { get => label.Text; set => label.Text = value; }

#pragma warning disable IDE1006 // Naming Styles
        public RectTransform rectTransform { get => (RectTransform)transform; }
#pragma warning restore IDE1006 // Naming Styles

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never written to
        [SerializeField]
        private UI_Tools.Universal_Text.UT_Text label;
        [SerializeField]
        private UnityEngine.UI.Image tick;
#pragma warning restore CS0649 // Field is never written to
#pragma warning restore IDE0044 // Add readonly modifier

        // Start is called before the first frame update
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start() => RedrawTickmark();

#if UNITY_EDITOR
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        //private void OnValidate() => RedrawTickmark();
#endif

        public void RedrawTickmark()
        {
            Vector2 anchors;
            RectTransform labelTransform = label.TextObject.rectTransform;
            RectTransform tickTransform = tick.rectTransform;
#if UNITY_EDITOR
            if (rectTransform == null || labelTransform == null || tickTransform == null)
                return;
#endif
            switch (side)
            {
                case AxisSide.Bottom:
                    anchors = new Vector2(0.5f, 1);
                    labelTransform.offsetMax = new Vector2(0, -(tickWidth + tickSpacing));
                    labelTransform.offsetMin = new Vector2(0, 0);
                    label.Alignment = TextAnchor.UpperCenter;
                    break;
                case AxisSide.Top:
                    anchors = new Vector2(0.5f, 0);
                    labelTransform.offsetMax = new Vector2(0, 0);
                    labelTransform.offsetMin = new Vector2(0, tickWidth + tickSpacing);
                    label.Alignment = TextAnchor.LowerCenter;
                    break;
                case AxisSide.Right:
                    anchors = new Vector2(0, 0.5f);
                    labelTransform.offsetMax = new Vector2(0, 0);
                    labelTransform.offsetMin = new Vector2(tickWidth + tickSpacing, 0);
                    label.Alignment = TextAnchor.MiddleLeft;
                    break;
                default:
                case AxisSide.Left:
                    anchors = new Vector2(1, 0.5f);
                    labelTransform.offsetMax = new Vector2(-(tickWidth + tickSpacing), 0);
                    labelTransform.offsetMin = new Vector2(0, 0);
                    label.Alignment = TextAnchor.MiddleRight;
                    break;
            }

            tickTransform.anchorMin = anchors;
            tickTransform.anchorMax = anchors;
            tickTransform.pivot = anchors;
            tickTransform.anchoredPosition = new Vector2(-0.1f, -0.1f);// anchoredPosition;

            rectTransform.pivot = anchors;

            if (side == AxisSide.Top || side == AxisSide.Bottom)
            {
                rectTransform.anchorMin = new Vector2(anchorFraction, 0);
                rectTransform.anchorMax = new Vector2(anchorFraction, 1);
                rectTransform.anchoredPosition = new Vector2(0, 0);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(label.LayoutElement.preferredWidth, 9));
                tickTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tickWidth);
                tickTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tickThickness);
            }
            else
            {
                rectTransform.anchorMin = new Vector2(0, anchorFraction);
                rectTransform.anchorMax = new Vector2(1, anchorFraction);
                rectTransform.anchoredPosition = new Vector2(0, 0);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(label.LayoutElement.preferredHeight, 9));
                tickTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tickWidth);
                tickTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tickThickness);
            }
            rectTransform.sizeDelta = Vector2.zero;
        }

        public float minSize = 20;
        private bool isHorizontal;
        public float minWidth => Mathf.Max(minSize, preferredWidth);
        public float preferredWidth { get; private set; }
        public float flexibleWidth => 0;
        public float minHeight => Mathf.Max(minSize, preferredHeight);
        public float preferredHeight { get; private set; }
        public float flexibleHeight => 0;
        public int layoutPriority => 100;
        public void CalculateLayoutInputHorizontal()
        {
            isHorizontal = side == AxisSide.Top || side == AxisSide.Bottom;
            Vector2 preferredSize = label.GetPreferredValues();
            preferredWidth = isHorizontal ? preferredSize.x : preferredSize.x + tickWidth + TickSpacing;
        }
        public void CalculateLayoutInputVertical()
        {
            isHorizontal = side == AxisSide.Top || side == AxisSide.Bottom;
            Vector2 preferredSize = label.GetPreferredValues();
            preferredHeight = isHorizontal ? preferredSize.y + tickWidth + tickSpacing : preferredSize.y;
        }
    }
}