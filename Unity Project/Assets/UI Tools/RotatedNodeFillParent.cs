using UnityEngine;

namespace UI_Tools
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class RotatedNodeFillParent : MonoBehaviour
    {
        private RectTransform rectTransform;

        public void ForceDisable()
        {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
            OnDisable();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            OnRectTransformDimensionsChange();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!enabled)
                return;
            if (!rectTransform)
            {
                // OnRectTransformDimensionsChange might be called before Awake - let's ignore this call as we will update during Awake anyway.
                return;
            }
            if (!transform.parent)
            {
                return;
            }
            RectTransform parentTransform = transform.parent.GetComponent<RectTransform>();
            if (!parentTransform)
            {
                return;
            }
            float aspectRatio = parentTransform.rect.size.x / parentTransform.rect.size.y;
            float halfAspectRatio = aspectRatio / 2.0f;
            float halfAspectRatioInvert = (1.0f / aspectRatio) / 2.0f;
            rectTransform.anchorMin = new Vector2(0.5f - halfAspectRatioInvert, 0.5f - halfAspectRatio);
            rectTransform.anchorMax = new Vector2(0.5f + halfAspectRatioInvert, 0.5f + halfAspectRatio);
            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        private void OnDisable()
        {
            if (!rectTransform)
            {
                // OnRectTransformDimensionsChange might be called before Awake - let's ignore this call as we will update during Awake anyway.
                return;
            }
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnEnable() => OnRectTransformDimensionsChange();
    }
}