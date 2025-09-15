using System;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalWindTunnel.UI
{
    public class TooltipMover : MonoBehaviour
    {
        public Vector2 offset = new Vector2(10, 10);
        
        private void Update()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasTransform = (RectTransform)canvas.transform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, Input.mousePosition, canvas.worldCamera, out Vector2 worldPosition);

            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.position = canvasTransform.transform.TransformPoint(worldPosition + offset);

            RectTransform parentTransform = (RectTransform)transform.parent;

            ILayoutElement layoutElement = GetComponent<ILayoutElement>();
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Math.Min(layoutElement.preferredWidth + 5, parentTransform.rect.width - rectTransform.anchoredPosition.x - 15));
        }
    }
}
