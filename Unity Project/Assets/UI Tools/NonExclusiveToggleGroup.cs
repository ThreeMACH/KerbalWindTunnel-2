using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    public class NonExclusiveToggleGroup : MonoBehaviour
    {
        public bool allowSwitchOff;
        private readonly List<NonExclusiveToggle> ActiveToggles = new List<NonExclusiveToggle>();
        private NonExclusiveToggle lastToggle;
        public void NotifyToggleChange(NonExclusiveToggle toggle)
        {
            if (toggle == null || toggle.group != this)
                return;

            if (toggle.isOn)
            {
                if (ActiveToggles.Contains(toggle))
                    return;
                for (int i = ActiveToggles.Count - 1; i >= 0; i--)
                {
                    NonExclusiveToggle activeToggle = ActiveToggles[i];
                    if (activeToggle == toggle)
                        continue;
                    if ((toggle.allowableToggles != null && !toggle.allowableToggles.Contains((Toggle)activeToggle)) ||
                        ((toggle.enforceMutual || activeToggle.enforceMutual) && (activeToggle.allowableToggles != null && !activeToggle.allowableToggles.Contains((Toggle)toggle))))
                        activeToggle.isOn = false;
                }
                ActiveToggles.Add(toggle);
            }
            else if (ActiveToggles.Contains(toggle))
            {
                if (allowSwitchOff || ActiveToggles.Count > 1)
                {
                    lastToggle = toggle;
                    ActiveToggles.Remove(toggle);
                }
                else
                {
                    if (lastToggle != null && lastToggle != toggle)
                    {
                        lastToggle.isOn = true;
                        lastToggle = toggle;
                        ActiveToggles.Remove(toggle);
                    }
                    else
                        toggle.isOn = true;
                }
            }
        }
        public void RegisterToggle(NonExclusiveToggle toggle)
        {
            if (toggle.isOn)
                NotifyToggleChange(toggle);
        }
        public void UnRegisterToggle(NonExclusiveToggle toggle)
        {
            if (ActiveToggles.Contains(toggle))
                ActiveToggles.Remove(toggle);
        }
    }
}