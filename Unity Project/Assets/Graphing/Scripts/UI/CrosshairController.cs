using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Graphing.UI
{
    public class CrosshairController : UnityEngine.UI.Graphic, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
    {
        private Grapher grapher;
        private UnityEngine.UI.Image verticalCrosshair;
        private UnityEngine.UI.Image horizontalCrosshair;
        private UnityEngine.UI.Image heldVerticalCrosshair;
        private UnityEngine.UI.Image heldHorizontalCrosshair;
        private UI_Tools.Universal_Text.UT_Text label;
        //private RectTransform rectTransform;
#pragma warning disable IDE0044 // Add readonly modifier
        [SerializeField]
        private RectTransform verticalRectTransform;
        [SerializeField]
        private RectTransform horizontalRectTransform;
        [SerializeField]
        private RectTransform heldVerticalRectTransform;
        [SerializeField]
        private RectTransform heldHorizontalRectTransform;
        [SerializeField]
        private RectTransform labelRectTransform;
#pragma warning restore IDE0044 // Add readonly modifier
        public bool hasVertical = true;
        public bool hasHorizontal = true;
        public bool hasLabel = true;
        private bool showing = false;
        public bool enableClick = true;
        [SerializeField]
        private bool holdClickedPosition = true;
        public bool HoldClickedPosition { get => holdClickedPosition; set { holdClickedPosition = value; if (!holdClickedPosition) SetHoldChildrenActive(false); } }

        public event EventHandler<Vector2> OnClick;

        public Vector2 NormalizedPosition { get; private set; } = -Vector2.one;
        public bool HoldingPosition { get => heldHorizontalRectTransform.gameObject.activeSelf || heldVerticalRectTransform.gameObject.activeSelf; }

        public bool ShowVertical
        {
            get => hasVertical;
            set => hasVertical = value && verticalRectTransform != null;
        }
        public bool ShowHorizontal
        {
            get => hasHorizontal;
            set => hasHorizontal = value && horizontalRectTransform != null;
        }
        public bool ShowLabel
        {
            get => hasLabel;
            set => hasLabel = value && labelRectTransform != null;
        }
        public override Color color { get => base.color; set { CrosshairColor = value; LabelColor = value; } }

        [SerializeField]
        private Color crosshairColor = Color.black;
        public Color CrosshairColor
        {
            get => crosshairColor;
            set
            {
                crosshairColor = value;
                if (verticalCrosshair != null)
                    verticalCrosshair.color = value;
                if (horizontalCrosshair != null)
                    horizontalCrosshair.color = value;
            }
        }

        public bool IsShowing => showing;
        public bool IsHeld { get; protected set; } = false;

        [SerializeField]
        private Color heldCrosshairColor = Color.white;
        public Color HeldCrosshairColor
        {
            get => heldCrosshairColor;
            set
            {
                heldCrosshairColor = value;
                if (heldVerticalCrosshair != null)
                    heldVerticalCrosshair.color = value;
                if (heldHorizontalCrosshair != null)
                    heldHorizontalCrosshair.color = value;
            }
        }
        public Color LabelColor { get => label.color; set => label.color = value; }
        public float FontSize { get => label.FontSize; set { label.EnableAutoSizing = false; label.FontSize = value; } }
        public bool AutoFontSize { get => label.EnableAutoSizing; set => label.EnableAutoSizing = value; }
        public float FontSizeMin { get => label.FontSizeMin; set => label.FontSizeMin = value; }
        public float FontSizeMax { get => label.FontSizeMax; set => label.FontSizeMax = value; }

        public string LabelText
        {
            get => label.Text;
            set
            {
                label.Text = value;
                labelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(label.LayoutElement.preferredWidth, rectTransform.rect.width * 0.5f));
                labelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(label.LayoutElement.preferredHeight, rectTransform.rect.height * 0.5f));
            }
        }

        protected override void Awake()
        {
            base.Awake();
            //rectTransform = GetComponent<RectTransform>();
            grapher = GetComponentInParent<Grapher>();
            verticalCrosshair = verticalRectTransform.GetComponent<UnityEngine.UI.Image>();
            horizontalCrosshair = horizontalRectTransform.GetComponent<UnityEngine.UI.Image>();
            heldVerticalCrosshair = heldVerticalRectTransform.GetComponent<UnityEngine.UI.Image>();
            heldHorizontalCrosshair = heldHorizontalRectTransform.GetComponent<UnityEngine.UI.Image>();
            label = labelRectTransform.GetComponent<UI_Tools.Universal_Text.UT_Text>();
            SetChildrenActive(false);
            SetHoldChildrenActive(false);
        }
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            labelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
            labelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
            verticalRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1);
            horizontalRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1);
            heldVerticalRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1);
            heldHorizontalRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1);
            label.color = color;
            verticalCrosshair.color = crosshairColor;
            horizontalCrosshair.color = crosshairColor;
            heldVerticalCrosshair.color = heldCrosshairColor;
            heldHorizontalCrosshair.color = heldCrosshairColor;
        }

        public Vector2 GetMouseRelativePosition()
        {
            return Rect.PointToNormalized(rectTransform.rect, (Vector2)Input.mousePosition - GetRectTransformPixelPosition(rectTransform, canvas).position);
        }

        // This is needed to account for the difference in origin between Screenspace Camera and Screenspace Overlay Canvases.
        public static Rect GetRectTransformPixelPosition(RectTransform rectTransform, Canvas canvas)
            => new Rect(
                (Vector2)rectTransform.position - (Vector2)canvas.transform.position + canvas.pixelRect.size / 2,
                rectTransform.rect.size);

        // Update is called once per frame
        protected void Update()
        {
            if (!showing)
                return;
            Vector2 normalizedPosition = GetMouseRelativePosition();
            Vector2 labelSize = labelRectTransform.rect.size / rectTransform.rect.size;

            Vector2 labelOffset = new Vector2(1, 0);// Vector2.zero;
            if (hasLabel)
            {
                LabelText = grapher.GetDisplayValue(normalizedPosition);

                if (labelRectTransform.gameObject.activeSelf && string.IsNullOrEmpty(LabelText))
                    labelRectTransform.gameObject.SetActive(false);
                else if (hasLabel && !labelRectTransform.gameObject.activeSelf && !string.IsNullOrEmpty(LabelText))
                    labelRectTransform.gameObject.SetActive(true);

                if (labelRectTransform.gameObject.activeSelf)
                {
                    if (normalizedPosition.y + labelSize.y > 1)
                    {
                        labelOffset.y -= labelRectTransform.rect.height;
                        if (normalizedPosition.x - labelSize.x >= 0)
                            labelOffset.x -= labelRectTransform.rect.width;
                    }
                    else if (normalizedPosition.x + labelSize.x > 1)
                        labelOffset.x -= labelRectTransform.rect.width + 2 * labelOffset.x;
                }
            }

            if (hasHorizontal)
            {
                horizontalRectTransform.anchorMin = new Vector2(0, normalizedPosition.y);
                horizontalRectTransform.anchorMax = new Vector2(1, normalizedPosition.y);
            }
            if (hasVertical)
            {
                verticalRectTransform.anchorMin = new Vector2(normalizedPosition.x, 0);
                verticalRectTransform.anchorMax = new Vector2(normalizedPosition.x, 1);
            }

            if (hasLabel)
            {
                labelRectTransform.anchorMin = labelRectTransform.anchorMax = normalizedPosition;
                labelRectTransform.anchoredPosition = labelOffset;
            }
        }

        private void SetChildrenActive(bool active)
        {
            labelRectTransform?.gameObject.SetActive(active && hasLabel);
            verticalRectTransform?.gameObject.SetActive(active && hasVertical);
            horizontalRectTransform?.gameObject.SetActive(active && hasHorizontal);
        }
        private void SetHoldChildrenActive(bool active)
        {
            heldVerticalRectTransform?.gameObject.SetActive(active && hasVertical);
            heldHorizontalRectTransform?.gameObject.SetActive(active && hasHorizontal);
            IsHeld = active;
        }
        public void Clear() => SetHoldChildrenActive(false);
        public void SetCrosshairPosition(float normalizedX, float normalizedY) => SetCrosshairPosition(new Vector2(normalizedX, normalizedY));
        public void SetCrosshairPosition(Vector2 normalizedPosition)
        {
            SetHoldChildrenActive(true);
            if (hasHorizontal)
            {
                heldHorizontalRectTransform.anchorMin = new Vector2(0, normalizedPosition.y);
                heldHorizontalRectTransform.anchorMax = new Vector2(1, normalizedPosition.y);
            }
            if (hasVertical)
            {
                heldVerticalRectTransform.anchorMin = new Vector2(normalizedPosition.x, 0);
                heldVerticalRectTransform.anchorMax = new Vector2(normalizedPosition.x, 1);
            }
            NormalizedPosition = normalizedPosition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetChildrenActive(true);
            showing = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetChildrenActive(false);
            showing = false;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                Clear();
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            eventData.Use();
            Vector2 clickedPosition = GetMouseRelativePosition();
            SetCrosshairPosition(clickedPosition);
            OnClick?.Invoke(this, clickedPosition);
        }
        // COULDDO: Could implement dragging to set new axis bounds...
        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh) => vh.Clear();
    }
}