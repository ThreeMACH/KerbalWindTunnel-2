using System;
using System.Collections.Generic;
using UI_Tools.Universal_Text;
using UnityEngine;

namespace KerbalWindTunnel.UI
{
    public class SelectionGrid : MonoBehaviour
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to
        [SerializeField]
        private List<string> listItems;
        [SerializeField]
        private List<string> tooltips;
        [SerializeField]
        private List<uint> listFlags;
        [SerializeField]
        private GameObject textPrefab;
        [SerializeField]
        private GameObject positivePrefab;
        [SerializeField]
        private GameObject negativePrefab;
        [SerializeField]
        private GameObject labelsContainer;
        [SerializeField]
        private List<GameObject> flagContainers;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field is never assigned to

        private int numItems;
        private int numFlags;

        private void Start()
        {
            numItems = listItems.Count;
            if (listFlags.Count < numItems)
                throw new ArgumentException("KWT - Selection grid malformatted.");
            numFlags = flagContainers.Count;

            for (int i = 0; i < numItems; i++)
            {
                GameObject label = Instantiate(textPrefab, labelsContainer.transform);
                label.GetComponent<UT_Text>().Text = Localize(listItems[i]);
                label.SetActive(true);

                if (tooltips.Count > i)
                    label.GetComponent<Tooltip>()?.SetTooltip(Localize(tooltips[i]));
                    //label.AddComponent<KSPAssets.KSPedia.DatabaseTooltip>().text = tooltips[i];

                uint flags = listFlags[i];
                for (int j = 0; j < numFlags; j++)
                {
                    GameObject prefab;
                    if (((uint)1 << j & flags) > 0)
                        prefab = positivePrefab;
                    else
                        prefab = negativePrefab;
                    Instantiate(prefab, flagContainers[j].transform).SetActive(true);
                }
            }
        }

        private string Localize(string text)
        {
#if !UNITY_EDITOR
            if (text?.StartsWith("#autoLOC", StringComparison.InvariantCultureIgnoreCase) == true)
                text = KSP.Localization.Localizer.Format(text);
#endif
            return text;
        }
    }
}
