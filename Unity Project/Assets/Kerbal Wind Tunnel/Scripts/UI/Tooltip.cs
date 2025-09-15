using System;
using System.Collections;
using UI_Tools.Universal_Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KerbalWindTunnel.UI
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        public string tooltip;
        [SerializeField]
        public GameObject tooltipPrefab;
        private GameObject tooltipObject;
        private bool showing = false;
        public void SetTooltip(string value)
        {
            tooltip = value.Replace("\\n", "\n");
            tooltip = tooltip.Replace("\n", Environment.NewLine);
            if (tooltipObject != null)
                tooltipObject.GetComponentInChildren<UT_Text>(true).Text = tooltip;

            if (string.IsNullOrEmpty(tooltip))
            {
                    tooltipObject?.SetActive(false);
            }
            else
            {
                tooltipObject?.SetActive(showing);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            showing = true;
            if (string.IsNullOrEmpty(tooltip))
                return;
            if (tooltipObject == null)
            {
                tooltipObject = tooltipPrefab;
            }
            UT_Text textComponent = tooltipObject.GetComponentInChildren<UT_Text>(true);
            textComponent.CheckIfValid();
            textComponent.Text = tooltip;

            tooltipObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            showing = false;
            tooltipObject?.SetActive(false);
        }

        private void OnDisable()
        {
            if (tooltipObject != null)
            {
                if (showing)
                    tooltipObject.SetActive(false);
                showing = false;
            }
        }
    }
}
