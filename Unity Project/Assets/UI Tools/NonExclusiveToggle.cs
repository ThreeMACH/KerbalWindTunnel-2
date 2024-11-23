using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    [RequireComponent(typeof(Toggle))]
    public class NonExclusiveToggle : MonoBehaviour
    {
        public bool enforceMutual = false;
        public List<Toggle> allowableToggles;
        [SerializeField]
        private NonExclusiveToggleGroup _group;
#pragma warning disable IDE1006 // Naming Styles
        public NonExclusiveToggleGroup group
#pragma warning restore IDE1006 // Naming Styles
        {
            get => _group;
            set
            {
                if (value == _group)
                    return;
                _group?.UnRegisterToggle(this);
                _group = value;
                _group.RegisterToggle(this);
            }
        }
        private Toggle toggle;

        public static explicit operator Toggle(NonExclusiveToggle toggle) => toggle.toggle;

#pragma warning disable IDE1006 // Naming Styles
        public bool isOn { get => toggle.isOn; set => toggle.isOn = value; }
#pragma warning restore IDE1006 // Naming Styles

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        private void Start()
        {
            toggle = GetComponent<Toggle>();
            if (toggle.group != null)
                Debug.LogError("Toggle group must be null.");
            toggle.group = null;
            toggle.onValueChanged.AddListener(OnValueChanged);
            group?.RegisterToggle(this);
        }
        private void OnValueChanged(bool value)
            => group?.NotifyToggleChange(this);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Unity Method>")]
        private void OnDestroy()
        {
            toggle?.onValueChanged.RemoveListener(OnValueChanged);
            group?.UnRegisterToggle(this);
        }
    }
}