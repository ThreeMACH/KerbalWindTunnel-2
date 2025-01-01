using System;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            RectTransform imageTransform = rectTransform;
            if (sourceCamera.targetTexture != null)
            {
                graphTex = sourceCamera.targetTexture;
            }
            else
            {
                int width = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.width), 16) * resolutionFactor;
                int height = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.height), 16) * resolutionFactor;

                graphTex = CreateRenderTexture(MakeDescriptor(width, height));
                sourceCamera.targetTexture = graphTex;
            }
            imageElement.texture = graphTex;
        }

        public void OnRectTransformDimensionsChange()
        {
            RectTransform imageTransform = rectTransform;

            int width = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.width), 16) * resolutionFactor;
            int height = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.height), 16) * resolutionFactor;

            if (graphTex == null)
            {
                UpdateTexture(width, height);
                return;
            }

            if (!enabled)
                return;

            if (Mathf.Abs(width - graphTex.width) / graphTex.width > 0.1f || Mathf.Abs(height - graphTex.height) / graphTex.height > 0.1f)
                UpdateTexture(width, height);
        }

        public void ForceResize()
        {
            RectTransform imageTransform = rectTransform;

            int width = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.width), 16) * resolutionFactor;
            int height = Mathf.Max(Mathf.CeilToInt(imageTransform.rect.height), 16) * resolutionFactor;
            UpdateTexture(width, height);
        }

        private void UpdateTexture(int width, int height)
        {
            if (scalingGroup != null)
                scalingGroup.transform.localScale = new Vector3((float)width / height, 1, 1);

            RenderTextureDescriptor textureDescriptor = MakeDescriptor(width, height);

            if (graphTex == null && sourceCamera.targetTexture == null)
            {
                graphTex = CreateRenderTexture(textureDescriptor);
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

        private RenderTextureDescriptor MakeDescriptor(int width, int height)
        {
            return new RenderTextureDescriptor(width, height, RenderTextureFormat.Default, 24, 0);
        }
        private RenderTexture CreateRenderTexture(RenderTextureDescriptor descriptor)
        {
            return new RenderTexture(descriptor) { antiAliasing = Math.Max(QualitySettings.antiAliasing, 1) };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
            => graphTex?.Release();
    }
}