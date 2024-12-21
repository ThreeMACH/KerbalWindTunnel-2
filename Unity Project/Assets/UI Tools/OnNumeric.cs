using UnityEngine;

namespace UI_Tools
{
    public class OnNumeric : MonoBehaviour
    {
        public float trigger;

        public UnityEngine.UI.Button.ButtonClickedEvent OnGreaterThanOrEqual = new UnityEngine.UI.Button.ButtonClickedEvent();
        public UnityEngine.UI.Button.ButtonClickedEvent OnLessThan = new UnityEngine.UI.Button.ButtonClickedEvent();
        public UnityEngine.UI.Toggle.ToggleEvent OnTrigger = new UnityEngine.UI.Toggle.ToggleEvent();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            OnGreaterThanOrEqual.AddListener(BoolTrue);
            OnLessThan.AddListener(BoolFalse);
        }
        private void BoolTrue() => OnTrigger?.Invoke(true);
        private void BoolFalse() => OnTrigger?.Invoke(false);

        public void OnValueAction(float value)
        {
            if (value >= trigger)
                OnGreaterThanOrEqual?.Invoke();
            else
                OnLessThan?.Invoke();
        }
        public void OnValueAction(int value) => OnValueAction((float)value);
    }
}