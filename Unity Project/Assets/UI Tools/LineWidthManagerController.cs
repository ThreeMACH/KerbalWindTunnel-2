using System;
using UnityEngine;

namespace UI_Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class LineWidthManagerController : MonoBehaviour
    {
        public event EventHandler<Vector2> RectTransformDimensionsChanged;
        private RectTransform rectTransform;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        protected virtual void OnRectTransformDimensionsChange()
        {
            if (enabled)
                RectTransformDimensionsChanged?.Invoke(this, rectTransform.rect.size);
        }
        public Vector2 Size { get => rectTransform.rect.size; }
    }
}