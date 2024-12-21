using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    public class RollUpWindowController : MonoBehaviour
    {
        private Vector2 originalSize;
        private bool rolled;
        public bool Rolled { get => rolled; }
        private ContentSizeFitter contentFitter;
        public RectTransform windowTransform;
        private ContentSizeFitter.FitMode horizontalFitMode;
        private ContentSizeFitter.FitMode verticalFitMode;
        public bool rollHorizontal = true;
        public bool rollVertical = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            contentFitter = windowTransform.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
            {
                contentFitter = windowTransform.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            horizontalFitMode = contentFitter.horizontalFit;
            verticalFitMode = contentFitter.verticalFit;
            rollHorizontal &= horizontalFitMode == ContentSizeFitter.FitMode.Unconstrained;
            rollVertical &= verticalFitMode == ContentSizeFitter.FitMode.Unconstrained;
        }
        public void SetRoll(bool roll)
        {
            if (roll)
                Roll();
            else
                Unroll();
        }
        public void Roll()
        {
            if (!rolled)
                StartCoroutine(RollCoroutine());
        }
        private IEnumerator RollCoroutine()
        {
            yield return null;
            if (rolled)
                yield break;
            originalSize = windowTransform.rect.size;
            Vector2 newsize = new Vector2(LayoutUtility.GetMinWidth(windowTransform), LayoutUtility.GetMinHeight(windowTransform));
            Vector2 move = (originalSize - newsize) * windowTransform.pivot;
            if (rollVertical)
                contentFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            else
                move.y = 0;
            if (rollHorizontal)
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            else
                move.x = 0;
            windowTransform.anchoredPosition += move;
            rolled = true;
        }
        public void Unroll()
        {
            if (rolled)
                StartCoroutine(UnrollCoroutine());
        }
        private IEnumerator UnrollCoroutine()
        {
            //yield return null;
            if (!rolled)
                yield break;
            Vector2 move = (windowTransform.rect.size - originalSize) * windowTransform.pivot;
            contentFitter.verticalFit = verticalFitMode;
            contentFitter.horizontalFit = horizontalFitMode;
            if (rollHorizontal)
                windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
            else
                move.x = 0;
            if (rollVertical)
                windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
            else
                move.y = 0;
            windowTransform.anchoredPosition += move;
            rolled = false;
        }
    }
}