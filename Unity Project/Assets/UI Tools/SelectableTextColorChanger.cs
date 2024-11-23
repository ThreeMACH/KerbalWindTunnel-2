using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableTextColorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Color normalColor;
    public Color disabledColor;
    public Color pressedColor;
    public Color highlightedColor;

    private Text text;
    private TMPro.TMP_Text tmpText;
    private Selectable sender;
    private bool useTMP;

    private Action<ButtonStatus> setMethod;

    public void SetupColors(Color normalColor, Color disabledColor, Color pressedColor, Color highlightedColor)
    {
        this.normalColor = normalColor;
        this.disabledColor = disabledColor;
        this.pressedColor = pressedColor;
        this.highlightedColor = highlightedColor;
    }

    private void Start()
    {
        text = GetComponentInChildren<Text>();
        tmpText = GetComponentInChildren<TMPro.TMP_Text>();
        if (tmpText != null)
            setMethod = SetTextColorTMP;
        else
            setMethod = SetTextColor;
        sender = GetComponent<Selectable>();
        if ((text == null && tmpText == null) || sender == null)
            Destroy(this);
    }

    private ButtonStatus lastButtonStatus = ButtonStatus.Normal;
    private bool isHighlightDesired = false;
    private bool isPressedDesired = false;

    private void Update()
    {
        ButtonStatus desiredButtonStatus = ButtonStatus.Normal;
        if (!sender.interactable)
            desiredButtonStatus = ButtonStatus.Disabled;
        else
        {
            if (isPressedDesired)
                desiredButtonStatus = ButtonStatus.Pressed;
            else if (isHighlightDesired)
                desiredButtonStatus = ButtonStatus.Highlighted;
        }

        if (desiredButtonStatus != lastButtonStatus)
        {
            lastButtonStatus = desiredButtonStatus;
            setMethod(desiredButtonStatus);
        }
    }

    public void ForceUpdate() => setMethod?.Invoke(lastButtonStatus);

    private void SetTextColorTMP(ButtonStatus buttonStatus)
    {
        switch (buttonStatus)
        {
            case ButtonStatus.Normal:
                tmpText.color = normalColor;
                break;
            case ButtonStatus.Disabled:
                tmpText.color = disabledColor;
                break;
            case ButtonStatus.Pressed:
                tmpText.color = pressedColor;
                break;
            case ButtonStatus.Highlighted:
                tmpText.color = highlightedColor;
                break;
        }
    }
    private void SetTextColor(ButtonStatus buttonStatus)
    {
        switch (buttonStatus)
        {
            case ButtonStatus.Normal:
                text.color = normalColor;
                break;
            case ButtonStatus.Disabled:
                text.color = disabledColor;
                break;
            case ButtonStatus.Pressed:
                text.color = pressedColor;
                break;
            case ButtonStatus.Highlighted:
                text.color = highlightedColor;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHighlightDesired = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressedDesired = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressedDesired = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHighlightDesired = false;
    }

    public enum ButtonStatus
    {
        Normal,
        Disabled,
        Highlighted,
        Pressed
    }
}