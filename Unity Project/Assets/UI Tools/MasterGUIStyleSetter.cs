using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    public class MasterGUIStyleSetter : MonoBehaviour
    {
        private readonly List<GUIStyleSetter> styleSetters = new List<GUIStyleSetter>();

        public readonly Dictionary<GUIStyle, Sprite> active = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> focused = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> hover = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> normal = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> onActive = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> onFocused = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> onHover = new Dictionary<GUIStyle, Sprite>();
        public readonly Dictionary<GUIStyle, Sprite> onNormal = new Dictionary<GUIStyle, Sprite>();

        // Use this for initialization
        protected virtual void Awake()
        {
            foreach (ScrollRect scrollRect in FindObjectsOfType<ScrollRect>())
                ParseElement(scrollRect);
            foreach (Selectable control in FindObjectsOfType<Selectable>())
                ParseElement(control);
            foreach (MaskableGraphic graphic in FindObjectsOfType<MaskableGraphic>())
            {
                if (!(graphic is Text || graphic is TMPro.TMP_Text))
                    continue;
                GUIStyleSetter existingSetter = graphic.GetComponentInParent<GUIStyleSetter>();
                if (existingSetter != null && existingSetter.skinStyle >= GUIStyleSetter.SkinStyle.button)
                    continue;
                ParseElement(graphic);
            }
        }

        public void SetGlobalSkin(GUISkin skin)
        {
            ParseSkin(skin);
            foreach(GUIStyleSetter styleSetter in styleSetters)
            {
                if (styleSetter.ignoreGlobalStyle)
                    continue;
                styleSetter.SetStyle(skin);
            }
        }

        protected virtual void ParseSkin(GUISkin skin)
        {
            ParseStyle(skin.box);
            ParseStyle(skin.button);
            ParseStyle(skin.horizontalScrollbar);
            ParseStyle(skin.horizontalScrollbarLeftButton);
            ParseStyle(skin.horizontalScrollbarRightButton);
            ParseStyle(skin.horizontalScrollbarThumb);
            ParseStyle(skin.horizontalSlider);
            ParseStyle(skin.horizontalSliderThumb);
            ParseStyle(skin.label);
            ParseStyle(skin.scrollView);
            ParseStyle(skin.textArea);
            ParseStyle(skin.textField);
            ParseStyle(skin.toggle);
            ParseStyle(skin.verticalScrollbar);
            ParseStyle(skin.verticalScrollbarUpButton);
            ParseStyle(skin.verticalScrollbarDownButton);
            ParseStyle(skin.verticalScrollbarThumb);
            ParseStyle(skin.verticalSlider);
            ParseStyle(skin.verticalSliderThumb);
        }

        public virtual void ParseStyle(GUIStyle style)
        {
            if (active.ContainsKey(style))
                Destroy(active[style]);
            if (focused.ContainsKey(style))
                Destroy(focused[style]);
            if (hover.ContainsKey(style))
                Destroy(hover[style]);
            if (normal.ContainsKey(style))
                Destroy(normal[style]);
            if (onActive.ContainsKey(style))
                Destroy(onActive[style]);
            if (onFocused.ContainsKey(style))
                Destroy(onFocused[style]);
            if (onHover.ContainsKey(style))
                Destroy(onHover[style]);
            if (onNormal.ContainsKey(style))
                Destroy(onNormal[style]);
            active[style] = style.active != null ? Sprite.Create(style.active.background, new Rect(Vector2.zero, new Vector2(style.active.background.width, style.active.background.height)), new Vector2(0.5f, 0.5f)) : null;
            focused[style] = style.focused != null ? Sprite.Create(style.focused.background, new Rect(Vector2.zero, new Vector2(style.focused.background.width, style.focused.background.height)), new Vector2(0.5f, 0.5f)) : null;
            hover[style] = style.hover != null ? Sprite.Create(style.hover.background, new Rect(Vector2.zero, new Vector2(style.hover.background.width, style.hover.background.height)), new Vector2(0.5f, 0.5f)) : null;
            normal[style] = style.normal != null ? Sprite.Create(style.normal.background, new Rect(Vector2.zero, new Vector2(style.normal.background.width, style.normal.background.height)), new Vector2(0.5f, 0.5f)) : null;
            onActive[style] = style.onActive != null ? Sprite.Create(style.onActive.background, new Rect(Vector2.zero, new Vector2(style.onActive.background.width, style.onActive.background.height)), new Vector2(0.5f, 0.5f)) : null;
            onFocused[style] = style.onFocused != null ? Sprite.Create(style.onFocused.background, new Rect(Vector2.zero, new Vector2(style.onFocused.background.width, style.onFocused.background.height)), new Vector2(0.5f, 0.5f)) : null;
            onHover[style] = style.onHover != null ? Sprite.Create(style.onHover.background, new Rect(Vector2.zero, new Vector2(style.onHover.background.width, style.onHover.background.height)), new Vector2(0.5f, 0.5f)) : null;
            onNormal[style] = style.onNormal != null ? Sprite.Create(style.onNormal.background, new Rect(Vector2.zero, new Vector2(style.onNormal.background.width, style.onNormal.background.height)), new Vector2(0.5f, 0.5f)) : null;
        }

        protected void ParseElement(Component control)
        {
            if (control.GetComponent<GUIStyleSetter>() != null)
                return;

            GUIStyleSetter styleSetter = null;

            if (control is Button)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.button;
            }
            else if (control is Toggle)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.toggle;
            }
            else if (control is Slider slider)
            {
                styleSetter = slider.gameObject.AddComponent<GUIStyleSetter>();
                GUIStyleSetter subSetter;
                if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
                {
                    styleSetter.skinStyle = GUIStyleSetter.SkinStyle.horizontalSlider;
                    subSetter = slider.handleRect.gameObject.AddComponent<GUIStyleSetter>();
                    subSetter.skinStyle = GUIStyleSetter.SkinStyle.horizontalSliderThumb;
                }
                else
                {
                    styleSetter.skinStyle = GUIStyleSetter.SkinStyle.verticalSlider;
                    subSetter = slider.handleRect.gameObject.AddComponent<GUIStyleSetter>();
                    subSetter.skinStyle = GUIStyleSetter.SkinStyle.verticalSliderThumb;
                }
                subSetter.masterSetter = this;
                styleSetters.Add(subSetter);
            }
            else if (control is Scrollbar scrollbar)
            {
                styleSetter = scrollbar.gameObject.AddComponent<GUIStyleSetter>();
                GUIStyleSetter subSetter;
                if (scrollbar.direction == Scrollbar.Direction.LeftToRight || scrollbar.direction == Scrollbar.Direction.RightToLeft)
                {
                    styleSetter.skinStyle = GUIStyleSetter.SkinStyle.horizontalSlider;
                    subSetter = scrollbar.handleRect.gameObject.AddComponent<GUIStyleSetter>();
                    subSetter.skinStyle = GUIStyleSetter.SkinStyle.horizontalSliderThumb;
                }
                else
                {
                    styleSetter.skinStyle = GUIStyleSetter.SkinStyle.verticalSlider;
                    subSetter = scrollbar.handleRect.gameObject.AddComponent<GUIStyleSetter>();
                    subSetter.skinStyle = GUIStyleSetter.SkinStyle.verticalSliderThumb;
                }
                subSetter.masterSetter = this;
                styleSetters.Add(subSetter);
            }
            else if (control is Dropdown || control is TMPro.TMP_Dropdown)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.button;
            }
            else if (control is InputField || control is TMPro.TMP_InputField)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.textField;
            }
            else if (control is ScrollRect scrollRect)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.textField;
                ParseElement(scrollRect.horizontalScrollbar);
                ParseElement(scrollRect.verticalScrollbar);
            } else if (control is Text || control is TMPro.TMP_Text)
            {
                styleSetter = control.gameObject.AddComponent<GUIStyleSetter>();
                styleSetter.skinStyle = GUIStyleSetter.SkinStyle.label;
            }

            if (styleSetter != null)
            {
                styleSetter.masterSetter = this;
                styleSetters.Add(styleSetter);
            }
        }
    }
}