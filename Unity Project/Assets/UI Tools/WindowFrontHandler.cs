using UnityEngine;
using UnityEngine.EventSystems;

namespace UI_Tools
{
    public class WindowFrontHandler : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField]
        public Transform windowTransform;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            if (windowTransform == null)
                windowTransform = transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            windowTransform.SetAsLastSibling();
        }
    }
}