using UnityEngine;
using UnityEngine.EventSystems;

namespace UI_Tools
{
    public class WindowDragHandle : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform windowTransform;
        private Canvas canvas;
        private bool _canvasNull;
        bool dragging;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        private void Awake()
        {
            if (windowTransform == null)
                windowTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            _canvasNull = canvas == null;
            if (windowTransform == null)
                Destroy(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                eventData.pointerPress = eventData.lastPress;
                eventData.eligibleForClick = true;
                eventData.pointerDrag = null;
                return;
            }
            Vector2 delta = !_canvasNull ? eventData.delta / canvas.scaleFactor : eventData.delta;
            windowTransform.anchoredPosition += delta;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = eventData.pointerPress == null;
            if (!dragging)
            {
                eventData.pointerPress = eventData.lastPress;
                return;
            }
        }
    }
}