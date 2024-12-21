using UnityEngine;

namespace UI_Tools
{
    public sealed class LineWidthManager : MonoBehaviour
    {
        [SerializeField]
        private float lineWidth = 1;
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                if (value == lineWidth)
                    return;
                lineWidth = value;
                UpdateLineRenderer(this, controller.Size);
            }
        }
        private LineRenderer lineRenderer;
        private LineWidthManagerController controller;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        // Start is called before the first frame update
        void Start()
        {
            if (lineRenderer == null)
            {
                Destroy(this);
                return;
            }
            controller = FindController(transform);
            if (controller == null)
            {
                Destroy(this);
                return;
            }
            controller.RectTransformDimensionsChanged += UpdateLineRenderer;
            UpdateLineRenderer(this, controller.Size);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
        {
            if (controller != null)
                controller.RectTransformDimensionsChanged -= UpdateLineRenderer;
        }
        private void UpdateLineRenderer(object _, Vector2 controllingRect)
            => lineRenderer.widthMultiplier = lineWidth / controllingRect.y;

        private static LineWidthManagerController FindController(Transform transform)
        {
            LineWidthManagerController result = transform.GetComponentInChildren<LineWidthManagerController>();
            if (result != null)
                return result;
            if (transform.parent != null)
                return FindController(transform.parent);
            return null;
        }
    }
}