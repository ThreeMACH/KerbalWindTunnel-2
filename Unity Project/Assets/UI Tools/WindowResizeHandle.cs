using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI_Tools
{
    public class WindowResizeHandle : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public enum ResizeHandlePoint
        {
            BottomRight,
            BottomLeft,
            TopLeft,
            TopRight,
            Right,
            Bottom,
            Left,
            Top
        }
        public RectTransform windowTransform;
        public ResizeHandlePoint handleLocation;
        private Canvas canvas;
        private bool _canvasNull;
        private Vector2 originalPivot;
        private Vector2 minSize;
        private Vector2 originalSize;
        private bool isDragging;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Update()
        {
            if (isDragging && Input.GetMouseButtonDown(1))
                CancelDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = !_canvasNull ? eventData.delta / canvas.scaleFactor : eventData.delta;
            Vector2 windowPosition = (Vector2)windowTransform.position - (Vector2)canvas.transform.position + canvas.pixelRect.size / 2;
            if (windowTransform.pivot.x == 1)
            {
                if (eventData.position.x > -minSize.x + windowPosition.x)
                    delta.x = windowTransform.rect.size.x - minSize.x;
                delta.x = -delta.x;
            }
            else if (windowTransform.pivot.x == 0)
            {
                if (eventData.position.x < minSize.x + windowPosition.x)
                    delta.x = minSize.x - windowTransform.rect.size.x;
            }
            else
                delta.x = 0;
            if (windowTransform.pivot.y == 1)
            {
                if (eventData.position.y > -minSize.y + windowPosition.y)
                    delta.y = windowTransform.rect.size.y - minSize.y;
                delta.y = -delta.y;
            }
            else if (windowTransform.pivot.y == 0)
            {
                if (eventData.position.y < minSize.y + windowPosition.y)
                    delta.y = minSize.y - windowTransform.rect.size.y;
            }
            else
                delta.y = 0;
            Vector2 size = windowTransform.rect.size + delta;
            windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(minSize.x, size.x));
            windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(minSize.y, size.y));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                eventData.pointerDrag = null;
                return;
            }
            isDragging = true;

            canvas = GetComponentInParent<Canvas>();
            _canvasNull = canvas == null;
            float minHeight = LayoutUtility.GetMinHeight(windowTransform), minWidth = LayoutUtility.GetMinWidth(windowTransform);
            minSize = new Vector2(minWidth, minHeight);
            originalSize = windowTransform.rect.size;
            originalPivot = windowTransform.pivot;
            Vector2 pivot;
            switch (handleLocation)
            {
                case ResizeHandlePoint.BottomRight:
                    pivot = new Vector2(0, 1);
                    break;
                case ResizeHandlePoint.BottomLeft:
                    pivot = new Vector2(1, 1);
                    break;
                case ResizeHandlePoint.TopLeft:
                    pivot = new Vector2(1, 0);
                    break;
                case ResizeHandlePoint.TopRight:
                    pivot = new Vector2(0, 0);
                    break;
                case ResizeHandlePoint.Right:
                    pivot = new Vector2(0, 0.5f);
                    break;
                case ResizeHandlePoint.Bottom:
                    pivot = new Vector2(0.5f, 1);
                    break;
                case ResizeHandlePoint.Left:
                    pivot = new Vector2(1, 0.5f);
                    break;
                case ResizeHandlePoint.Top:
                    pivot = new Vector2(0.5f, 0);
                    break;
                default:
                    throw new System.NotImplementedException("[WindowResizeHandle] Invalid handle location.");
            }
            SetPivot(windowTransform, pivot);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            SetPivot(windowTransform, originalPivot);
        }

        public static void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            if (rectTransform == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 scale = rectTransform.localScale;
            Vector2 deltaPivot = rectTransform.pivot - pivot;
            Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x * scale.x, deltaPivot.y * size.y * scale.y);
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition -= deltaPosition;
        }

        public void CancelDrag()
        {
            if (!isDragging)
                return;
            windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
            windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
            ExecuteEvents.endDragHandler(this, new PointerEventData(EventSystem.current));
        }
    }
}