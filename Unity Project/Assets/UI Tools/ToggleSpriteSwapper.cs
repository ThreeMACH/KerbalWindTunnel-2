using UnityEngine;
using UnityEngine.UI;

public class ToggleSpriteSwapper : MonoBehaviour
{
    private Toggle toggle;
    private SelectableTextColorChanger textColorChanger;
    public bool setSelectedToo;
    public Sprite normalSprite;
    public Sprite activeSprite;
    public Color normalTextColor;
    public Color activeTextColor;

    public void Setup(Sprite normalSprite, Sprite activeSprite, Color normalTextColor, Color activeTextColor)
    {
        this.normalSprite = normalSprite;
        this.activeSprite = activeSprite;
        this.normalTextColor = normalTextColor;
        this.activeTextColor = activeTextColor;
    }
    public void Setup(Sprite normalSprite, Sprite activeSprite, Color normalTextColor, Color activeTextColor, bool setSelectedToo)
    {
        this.setSelectedToo = setSelectedToo;
        Setup(normalSprite, activeSprite, normalTextColor, activeTextColor);
    }

    private void ForceDeselect(bool _)
    {
        if (!UnityEngine.EventSystems.EventSystem.current.alreadySelecting)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        textColorChanger = GetComponent<SelectableTextColorChanger>();
        if (!setSelectedToo)
            toggle.onValueChanged.AddListener(ForceDeselect);
    }

    private void Start()
    {
        if (toggle == null)
        {
            Destroy(this);
            return;
        }    
        toggle.onValueChanged.AddListener(ReverseToggleModes);
        ReverseToggleModes(toggle.isOn);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(ReverseToggleModes);
        if (!setSelectedToo)
            toggle.onValueChanged.RemoveListener(ForceDeselect);
    }

    void ReverseToggleModes(bool active)
    {
        if (active)
        {
            if (toggle.image != null)
                toggle.image.sprite = activeSprite;
            if (setSelectedToo)
            {
                SpriteState toggleSprite = toggle.spriteState;
                toggleSprite.selectedSprite = activeSprite;
                toggle.spriteState = toggleSprite;
            }
            if (textColorChanger != null)
            {
                textColorChanger.normalColor = activeTextColor;
                textColorChanger.ForceUpdate();
            }
        }
        else
        {
            if (toggle.image != null)
                toggle.image.sprite = normalSprite;
            if (setSelectedToo)
            {
                SpriteState toggleSprite = toggle.spriteState;
                toggleSprite.selectedSprite = normalSprite;
                toggle.spriteState = toggleSprite;
            }
            if (textColorChanger != null)
            {
                textColorChanger.normalColor = normalTextColor;
                textColorChanger.ForceUpdate();
            }
        }
    }
}
