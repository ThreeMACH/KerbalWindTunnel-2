using UnityEngine;

namespace UI_Tools
{
    public class OnBoolean : MonoBehaviour
    {
        public UnityEngine.UI.Button.ButtonClickedEvent OnTrue;
        public UnityEngine.UI.Button.ButtonClickedEvent OnFalse;
        
        public void OnBooleanAction(bool value)
        {
            if (value)
                OnTrue?.Invoke();
            else
                OnFalse?.Invoke();
        }
    }
}