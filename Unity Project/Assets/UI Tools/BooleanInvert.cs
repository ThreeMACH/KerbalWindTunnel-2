using UnityEngine;

public class BooleanInvert : MonoBehaviour
{
    public UnityEngine.UI.Toggle.ToggleEvent callback;
    public void OnCallback(bool input) => callback?.Invoke(!input);
}
