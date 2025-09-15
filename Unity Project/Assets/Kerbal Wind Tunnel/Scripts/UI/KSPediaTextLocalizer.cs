using KSP.Localization;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalWindTunnel.AssetLoader
{
    public class KSPediaTextLocalizer : MonoBehaviour
    {
#if !UNITY_EDITOR
        // Start is called before the first frame update
        private void Start()
        {
            foreach (Text textElem in GetComponentsInChildren<Text>(true))
            {
                if (textElem?.text?.StartsWith("#autoLOC", StringComparison.InvariantCultureIgnoreCase) == true)
                    textElem.text = Localizer.Format(textElem.text);
            }
            foreach (TMPro.TMP_Text tmpElem in GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmpElem?.text?.StartsWith("#autoLOC", StringComparison.InvariantCultureIgnoreCase) == true)
                    tmpElem.text = Localizer.Format(tmpElem.text);
            }
        }
#endif
    }
}