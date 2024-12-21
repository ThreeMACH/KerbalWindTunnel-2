using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI_Tools
{
    public class GUIStyleSetter : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public MasterGUIStyleSetter masterSetter;
        public static System.Func<Font, TMPro.TMP_FontAsset> TMP_FontAssetFunc;
        private readonly List<Graphic> textElements = new List<Graphic>();
        private bool isReady = false;
        public bool ignoreImageOnSame = false;
        public bool autoIgnoreImageOnSame = true;
        private Selectable selectable;

        public GUIStyle style = null;
        public SkinStyle skinStyle;
        public bool ignoreGlobalStyle = false;

        private Sprite active;
        private Sprite focused;
        private Sprite hover;
        private Sprite normal;
        private Sprite onActive;
        private Sprite onFocused;
        private Sprite onHover;
        private Sprite onNormal;

        public enum SkinStyle
        {
            box,
            label,
            textArea,
            button,
            window,
            horizontalScrollbar,
            horizontalScrollbarLeftButton,
            horizontalScrollbarRightButton,
            horizontalScrollbarThumb,
            horizontalSlider,
            horizontalSliderThumb,
            scrollView,
            textField,
            toggle,
            verticalScrollbar,
            verticalScrollbarUpButton,
            verticalScrollbarDownButton,
            verticalScrollbarThumb,
            verticalSlider,
            verticalSliderThumb
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            if (autoIgnoreImageOnSame)
                ignoreImageOnSame = GetComponent<Button>() != null ||
                    GetComponent<Dropdown>() != null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start()
        {
            isReady = true;
            if (style != null)
                UpdateElements();
        }

        private void UpdateElements()
        {
            textElements.Clear();
            foreach (Component component in GetComponents<Component>())
                ParseElement(component);
#if FALSE
            style.padding;      // 
            style.clipping;     // 
            style.contentOffset;// 
            style.imagePosition;// 
            style.margin;       // 
            style.overflow;     // 
            style.alignment;    // Text
            style.font;         // Text
            style.fontSize;     // Text
            style.fontStyle;    // Text
            style.wordWrap;     // Text
            style.fixedHeight;  // LayoutElement
            style.fixedWidth;   // LayoutElement
            style.border;       // Image
            style.active;       // \StyleSetter Pressed
            style.focused;      // \StyleSetter Selected
            style.hover;        // \StyleSetter Highlighted
            style.normal;       // \StyleSetter
            style.onActive;     // \StyleSetter
            style.onFocused;    // \StyleSetter
            style.onHover;      // \StyleSetter
            style.onNormal;     // \StyleSetter

            GUIStyleState normal = style.normal;
            normal.background;  // Image
            normal.textColor;   // Text
#endif
        }

        private void SetSpritesOn(bool on) => SetSpritesOn(selectable);
        private void SetSpritesOn(bool on, Selectable selectable)
        {
            if (selectable == null)
                return;
            if (on)
            {
                SpriteState spriteState = selectable.spriteState;
                spriteState.pressedSprite = active;
                spriteState.selectedSprite = focused;
                spriteState.highlightedSprite = hover;
                selectable.spriteState = spriteState;
                selectable.image.sprite = normal;
            }
            else
            {
                SpriteState spriteState = selectable.spriteState;
                spriteState.pressedSprite = onActive;
                spriteState.selectedSprite = onFocused;
                spriteState.highlightedSprite = onHover;
                selectable.spriteState = spriteState;
                selectable.image.sprite = onNormal;
            }
        }

        private void ParseElement(Component component)
        {
            if (component == null)
                return;
            GUIStyleSetter styleSetter = component.GetComponent<GUIStyleSetter>();
            if (styleSetter != null && styleSetter != this)
                return;

            if (component is Text text)
            {
                text.font = style.font;
                text.fontStyle = style.fontStyle;
                text.fontSize = style.fontSize;
                text.alignment = style.alignment;
                text.horizontalOverflow = style.wordWrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                text.color = style.normal.textColor;
                textElements.Add(text);
            }
            else if (component is TMPro.TMP_Text tmp_text)
            {
                TMPro.TMP_FontAsset fontAsset = TMP_FontAssetFunc != null ? TMP_FontAssetFunc(style.font) : null;
                tmp_text.font = fontAsset;
                tmp_text.fontStyle = (TMPro.FontStyles)style.fontStyle;
                tmp_text.fontSize = style.fontSize;
                tmp_text.alignment = (TMPro.TextAlignmentOptions)style.alignment;
                tmp_text.enableWordWrapping = style.wordWrap;
                tmp_text.color = style.normal.textColor;
                textElements.Add(tmp_text);
            }
            else if (!ignoreImageOnSame && component is Image image)
            {
                image.sprite = normal;
                image.type = Image.Type.Sliced;
            }
            else if (component is Button button)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                SetSpritesOn(false, button);
                ParseElement(button.GetComponentInChildren<Text>());
                ParseElement(button.GetComponentInChildren<TMPro.TMP_Text>());
            }
            else if (component is Toggle toggle)
            {
                toggle.transition = Selectable.Transition.SpriteSwap;
                if (selectable == null)
                {
                    selectable = toggle;
                    toggle.onValueChanged.AddListener(SetSpritesOn);
                }
                SetSpritesOn(toggle.isOn, toggle);
                toggle.graphic.gameObject.SetActive(false);
                ParseElement(toggle.GetComponentInChildren<Text>());
                ParseElement(toggle.GetComponentInChildren<TMPro.TMP_Text>());
            }
            else if (component is Slider slider)
            {
                Image background = slider.GetComponentInChildren<Image>();
                Image fillBackground = slider.fillRect.GetComponent<Image>();
                if (background != null)
                    background.sprite = normal;
                if (fillBackground != null)
                    fillBackground.sprite = onNormal;
                // Note that the handle has its own style.
            }
            else if (component is Scrollbar scrollbar)
            {
                // The background image is already handled.
                // Note that the handle has its own style.
            }
            else if (component is Dropdown dropdown)
            {
                dropdown.transition = Selectable.Transition.SpriteSwap;
                SetSpritesOn(false, dropdown);
                foreach (Text textItem in dropdown.GetComponentsInChildren<Text>())
                    ParseElement(textItem);
                foreach (TMPro.TMP_Text tmp_textItem in dropdown.GetComponentsInChildren<TMPro.TMP_Text>())
                    ParseElement(tmp_textItem);
            }
            else if (component is InputField inputField)
            {
                // The background image is already handled.
                ParseElement(inputField.textComponent);
                ParseElement(inputField.placeholder);
            }
            else if (component is TMPro.TMP_Dropdown tmp_dropdown)
            {
                tmp_dropdown.transition = Selectable.Transition.SpriteSwap;
                SetSpritesOn(false, tmp_dropdown);
                foreach (Text textItem in tmp_dropdown.GetComponentsInChildren<Text>())
                    ParseElement(textItem);
                foreach (TMPro.TMP_Text tmp_textItem in tmp_dropdown.GetComponentsInChildren<TMPro.TMP_Text>())
                    ParseElement(tmp_textItem);
            }
            else if (component is TMPro.TMP_InputField tmp_inputField)
            {
                // The background image is already handled.
                ParseElement(tmp_inputField.textComponent);
                ParseElement(tmp_inputField.placeholder);
            }
            else if (component is ScrollRect scrollRect)
            {
                // The background image is already handled.
                ParseElement(scrollRect.horizontalScrollbar);
                ParseElement(scrollRect.verticalScrollbar);
                // Note that the handles have their own style.
            }
            else if (component is LayoutElement layoutElement)
            {
                layoutElement.minHeight = layoutElement.preferredWidth = style.fixedHeight;
                layoutElement.minWidth = layoutElement.preferredWidth = style.fixedWidth;
            }
        }

        public void SetStyle(GUISkin skin)
        {
            GUIStyle style;
            switch (skinStyle)
            {
                default:
                case SkinStyle.box:
                    style = skin.box;
                    break;
                case SkinStyle.label:
                    style = skin.label;
                    break;
                case SkinStyle.button:
                    style = skin.button;
                    break;
                case SkinStyle.window:
                    style = skin.window;
                    break;
                case SkinStyle.horizontalScrollbar:
                    style = skin.horizontalScrollbar;
                    break;
                case SkinStyle.horizontalScrollbarLeftButton:
                    style = skin.horizontalScrollbarLeftButton;
                    break;
                case SkinStyle.horizontalScrollbarRightButton:
                    style = skin.horizontalScrollbarRightButton;
                    break;
                case SkinStyle.horizontalScrollbarThumb:
                    style = skin.horizontalScrollbarThumb;
                    break;
                case SkinStyle.horizontalSlider:
                    style = skin.horizontalSlider;
                    break;
                case SkinStyle.horizontalSliderThumb:
                    style = skin.horizontalSliderThumb;
                    break;
                case SkinStyle.scrollView:
                    style = skin.scrollView;
                    break;
                case SkinStyle.textArea:
                    style = skin.textArea;
                    break;
                case SkinStyle.textField:
                    style = skin.textField;
                    break;
                case SkinStyle.toggle:
                    style = skin.toggle;
                    break;
                case SkinStyle.verticalScrollbar:
                    style = skin.verticalScrollbar;
                    break;
                case SkinStyle.verticalScrollbarUpButton:
                    style = skin.verticalScrollbarUpButton;
                    break;
                case SkinStyle.verticalScrollbarDownButton:
                    style = skin.verticalScrollbarDownButton;
                    break;
                case SkinStyle.verticalScrollbarThumb:
                    style = skin.verticalScrollbarThumb;
                    break;
                case SkinStyle.verticalSlider:
                    style = skin.verticalSlider;
                    break;
                case SkinStyle.verticalSliderThumb:
                    style = skin.verticalSliderThumb;
                    break;
            }
            SetStyle(style);
        }
        public void SetStyle(GUISkin skin, SkinStyle skinStyle)
        {
            this.skinStyle = skinStyle;
            SetStyle(skin);
        }
        public void SetStyle(GUIStyle style)
        {
            this.style = style;
            if (masterSetter != null)
            {
                active = masterSetter.active[style];
                focused = masterSetter.focused[style];
                hover = masterSetter.hover[style];
                normal = masterSetter.normal[style];
                onActive = masterSetter.onActive[style];
                onFocused = masterSetter.onFocused[style];
                onHover = masterSetter.onHover[style];
                onNormal = masterSetter.onNormal[style];
            }
            else
            {
                active = style.active != null ? Sprite.Create(style.active.background, new Rect(Vector2.zero, new Vector2(style.active.background.width, style.active.background.height)), new Vector2(0.5f, 0.5f)) : null;
                focused = style.focused != null ? Sprite.Create(style.focused.background, new Rect(Vector2.zero, new Vector2(style.focused.background.width, style.focused.background.height)), new Vector2(0.5f, 0.5f)) : null;
                hover = style.hover != null ? Sprite.Create(style.hover.background, new Rect(Vector2.zero, new Vector2(style.hover.background.width, style.hover.background.height)), new Vector2(0.5f, 0.5f)) : null;
                normal = style.normal != null ? Sprite.Create(style.normal.background, new Rect(Vector2.zero, new Vector2(style.normal.background.width, style.normal.background.height)), new Vector2(0.5f, 0.5f)) : null;
                onActive = style.onActive != null ? Sprite.Create(style.onActive.background, new Rect(Vector2.zero, new Vector2(style.onActive.background.width, style.onActive.background.height)), new Vector2(0.5f, 0.5f)) : null;
                onFocused = style.onFocused != null ? Sprite.Create(style.onFocused.background, new Rect(Vector2.zero, new Vector2(style.onFocused.background.width, style.onFocused.background.height)), new Vector2(0.5f, 0.5f)) : null;
                onHover = style.onHover != null ? Sprite.Create(style.onHover.background, new Rect(Vector2.zero, new Vector2(style.onHover.background.width, style.onHover.background.height)), new Vector2(0.5f, 0.5f)) : null;
                onNormal = style.onNormal != null ? Sprite.Create(style.onNormal.background, new Rect(Vector2.zero, new Vector2(style.onNormal.background.width, style.onNormal.background.height)), new Vector2(0.5f, 0.5f)) : null;
            }
            if (isReady)
                UpdateElements();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (style == null || style.focused == null)
                return;
            foreach (Graphic text in textElements)
                text.color = style.focused.textColor;
            if (transform.parent == null)
                return;
            ISelectHandler nextHandler = transform.parent.GetComponentInParent<ISelectHandler>();
            nextHandler?.OnSelect(eventData);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (style == null || style.normal == null)
                return;
            foreach (Graphic text in textElements)
                text.color = style.normal.textColor;
            if (transform.parent == null)
                return;
            IDeselectHandler nextHandler = transform.parent.GetComponentInParent<IDeselectHandler>();
            nextHandler?.OnDeselect(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (style == null || style.active == null)
                return;
            foreach (Graphic text in textElements)
                text.color = style.active.textColor;
            if (transform.parent == null)
                return;
            IPointerDownHandler nextHandler = transform.parent.GetComponentInParent<IPointerDownHandler>();
            nextHandler?.OnPointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (style == null || style.normal == null)
                return;
            foreach (Graphic text in textElements)
                text.color = style.normal.textColor;
            if (transform.parent == null)
                return;
            IPointerUpHandler nextHandler = transform.parent.GetComponentInParent<IPointerUpHandler>();
            nextHandler?.OnPointerUp(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (transform.parent == null)
                return;
            IPointerClickHandler nextHandler = transform.parent.GetComponentInParent<IPointerClickHandler>();
            nextHandler?.OnPointerClick(eventData);
        }
    }
}