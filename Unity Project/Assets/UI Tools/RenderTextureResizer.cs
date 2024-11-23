using UnityEngine;

namespace UI_Tools
{
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.RawImage))]
    public class RenderTextureResizer : MonoBehaviour
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [SerializeField]
        private UnityEngine.UI.RawImage imageElement;
        [SerializeField]
        private Camera sourceCamera;
        [SerializeField]
        private GameObject scalingGroup;
        [SerializeField]
        public int resolutionFactor = 1;
#pragma warning restore IDE0044 // Add readonly modifier

        private RenderTexture graphTex;
        public RenderTexture GraphTex { get => graphTex; }

#pragma warning disable IDE1006 // Naming Styles
        public RectTransform rectTransform { get => (RectTransform)transform; }
#pragma warning restore IDE1006 // Naming Styles

        // Start is called before the first frame update
        public void Start() => OnRectTransformDimensionsChange();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        private void Awake()
        {
            RectTransform imageTransform = rectTransform;
            if (sourceCamera.targetTexture != null)
            {
                graphTex = sourceCamera.targetTexture;
            }
            else
            {
                float width = Mathf.Max(Mathf.Ceil(imageTransform.rect.width), 16) * resolutionFactor;
                float height = Mathf.Max(Mathf.Ceil(imageTransform.rect.height), 16) * resolutionFactor;

                RenderTextureDescriptor textureDescriptor = new RenderTextureDescriptor(
                    Mathf.CeilToInt(width),
                    Mathf.CeilToInt(height));
                graphTex = new RenderTexture(textureDescriptor);
            }
            imageElement.texture = sourceCamera.targetTexture = graphTex;
        }

        public void OnRectTransformDimensionsChange()
        {
            RectTransform imageTransform = rectTransform;

            float width = Mathf.Max(Mathf.Ceil(imageTransform.rect.width), 16) * resolutionFactor;
            float height = Mathf.Max(Mathf.Ceil(imageTransform.rect.height), 16) * resolutionFactor;

            if (graphTex == null)
            {
                UpdateTexture((int)width, (int)height);
                return;
            }

            if (!enabled)
                return;

            if (Mathf.Abs(width - graphTex.width) / graphTex.width > 0.1f || Mathf.Abs(height - graphTex.height) / graphTex.height > 0.1f)
                UpdateTexture((int)width, (int)height);
        }

        public void ForceResize()
        {
            RectTransform imageTransform = rectTransform;

            float width = Mathf.Max(Mathf.Ceil(imageTransform.rect.width), 16) * resolutionFactor;
            float height = Mathf.Max(Mathf.Ceil(imageTransform.rect.height), 16) * resolutionFactor;
            UpdateTexture((int)width, (int)height);
        }

        private void UpdateTexture(int width, int height)
        {
            if (scalingGroup != null)
                scalingGroup.transform.localScale = new Vector3((float)width / height, 1, 1);

            RenderTextureDescriptor textureDescriptor = new RenderTextureDescriptor(
                    Mathf.CeilToInt(width),
                    Mathf.CeilToInt(height));

            if (graphTex == null && sourceCamera.targetTexture == null)
            {
                graphTex = new RenderTexture(textureDescriptor);
            }
            else
            {
                if (graphTex == null)
                {
                    graphTex = sourceCamera.targetTexture;
                }
                if (graphTex.IsCreated())
                    graphTex.Release();
                //graphTex.width = height;
                //graphTex.height = width;
                graphTex.descriptor = textureDescriptor;
            }

            imageElement.texture = sourceCamera.targetTexture = graphTex;

            sourceCamera.aspect = (float)width / height;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        private void OnDestroy()
            => graphTex?.Release();
    }
}