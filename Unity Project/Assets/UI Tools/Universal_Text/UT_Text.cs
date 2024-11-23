using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI_Tools.Universal_Text
{
    [DisallowMultipleComponent]
    public class UT_Text : UT_Base
    {
        private Text unity_text;
        private TMP_Text tmp_text;

        public MaskableGraphic TextObject
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text;
                    case UT_Mode.TMPro:
                        return tmp_text;
                }
                return null;
            }
        }
        public ILayoutElement LayoutElement
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text;
                    case UT_Mode.TMPro:
                        return (ILayoutElement)tmp_text;
                }
                return null;
            }
        }
        public string Text
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.text;
                    case UT_Mode.TMPro:
                        return tmp_text.text;
                }
                return null;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.text = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.text = value;
                        return;
                }
                return;
            }
        }
#pragma warning disable IDE1006 // Naming Styles
        public Color color
#pragma warning restore IDE1006 // Naming Styles
        {
            get => TextObject.color;
            set => TextObject.color = value;
        }
        public TextAnchor Alignment
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.alignment;
                    case UT_Mode.TMPro:
                        switch (tmp_text.alignment)
                        {
                            case TextAlignmentOptions.Baseline:
                            case TextAlignmentOptions.Bottom:
                                return TextAnchor.LowerCenter;
                            case TextAlignmentOptions.BaselineLeft:
                            case TextAlignmentOptions.BaselineFlush:
                            case TextAlignmentOptions.BaselineGeoAligned:
                            case TextAlignmentOptions.BaselineJustified:
                            case TextAlignmentOptions.BottomFlush:
                            case TextAlignmentOptions.BottomGeoAligned:
                            case TextAlignmentOptions.BottomJustified:
                            case TextAlignmentOptions.BottomLeft:
                                return TextAnchor.LowerLeft;
                            case TextAlignmentOptions.BottomRight:
                                return TextAnchor.LowerRight;
                            case TextAlignmentOptions.CenterGeoAligned:
                            case TextAlignmentOptions.Flush:
                            case TextAlignmentOptions.Justified:
                            case TextAlignmentOptions.MidlineFlush:
                            case TextAlignmentOptions.MidlineGeoAligned:
                            case TextAlignmentOptions.MidlineJustified:
                            case TextAlignmentOptions.MidlineLeft:
                            case TextAlignmentOptions.Left:
                            default:
                                return TextAnchor.MiddleLeft;
                            case TextAlignmentOptions.Midline:
                            case TextAlignmentOptions.Center:
                                return TextAnchor.MiddleCenter;
                            case TextAlignmentOptions.MidlineRight:
                            case TextAlignmentOptions.Right:
                                return TextAnchor.MiddleRight;
                            case TextAlignmentOptions.CaplineFlush:
                            case TextAlignmentOptions.CaplineGeoAligned:
                            case TextAlignmentOptions.CaplineJustified:
                            case TextAlignmentOptions.CaplineLeft:
                            case TextAlignmentOptions.TopFlush:
                            case TextAlignmentOptions.TopGeoAligned:
                            case TextAlignmentOptions.TopJustified:
                            case TextAlignmentOptions.TopLeft:
                                return TextAnchor.UpperLeft;
                            case TextAlignmentOptions.Capline:
                            case TextAlignmentOptions.Top:
                                return TextAnchor.UpperCenter;
                            case TextAlignmentOptions.CaplineRight:
                            case TextAlignmentOptions.TopRight:
                                return TextAnchor.UpperRight;
                        }
                }
                return TextAnchor.MiddleCenter;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.alignment = value;
                        return;
                    case UT_Mode.TMPro:
                        switch (value)
                        {
                            case TextAnchor.LowerCenter:
                                tmp_text.alignment = TextAlignmentOptions.Bottom;
                                break;
                            case TextAnchor.LowerLeft:
                                tmp_text.alignment = TextAlignmentOptions.BottomLeft;
                                break;
                            case TextAnchor.LowerRight:
                                tmp_text.alignment = TextAlignmentOptions.BottomRight;
                                break;
                            default:
                            case TextAnchor.MiddleCenter:
                                tmp_text.alignment = TextAlignmentOptions.Center;
                                break;
                            case TextAnchor.MiddleLeft:
                                tmp_text.alignment = TextAlignmentOptions.Left;
                                break;
                            case TextAnchor.MiddleRight:
                                tmp_text.alignment = TextAlignmentOptions.Right;
                                break;
                            case TextAnchor.UpperCenter:
                                tmp_text.alignment = TextAlignmentOptions.Top;
                                break;
                            case TextAnchor.UpperLeft:
                                tmp_text.alignment = TextAlignmentOptions.TopLeft;
                                break;
                            case TextAnchor.UpperRight:
                                tmp_text.alignment = TextAlignmentOptions.TopRight;
                                break;
                        }
                        return;
                }
            }
        }
        public FontStyle FontStyle
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.fontStyle;
                    case UT_Mode.TMPro:
                        switch (tmp_text.fontStyle)
                        {
                            case FontStyles.Bold | FontStyles.Italic:
                                return FontStyle.BoldAndItalic;
                            case FontStyles.Bold:
                                return FontStyle.Bold;
                            case FontStyles.Italic:
                                return FontStyle.Italic;
                            default:
                                return FontStyle.Normal;
                        }
                }
                return FontStyle.Normal;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.fontStyle = value;
                        return;
                    case UT_Mode.TMPro:
                        switch (value)
                        {
                            case FontStyle.Bold:
                                tmp_text.fontStyle = FontStyles.Bold;
                                break;
                            case FontStyle.BoldAndItalic:
                                tmp_text.fontStyle = FontStyles.Bold | FontStyles.Italic;
                                break;
                            case FontStyle.Italic:
                                tmp_text.fontStyle = FontStyles.Italic;
                                break;
                            case FontStyle.Normal:
                                tmp_text.fontStyle = FontStyles.Normal;
                                break;
                        }
                        return;
                }
            }
        }
        public float LineSpacing
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.lineSpacing;
                    case UT_Mode.TMPro:
                        return tmp_text.lineSpacing;
                }
                return 0;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.lineSpacing = Mathf.RoundToInt(value);
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.lineSpacing = value;
                        return;
                }
            }
        }
        public bool EnableAutoSizing
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.resizeTextForBestFit;
                    case UT_Mode.TMPro:
                        return tmp_text.enableAutoSizing;
                }
                return false;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.resizeTextForBestFit = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.enableAutoSizing = value;
                        return;
                }
            }
        }
        public float FontSizeMin
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.resizeTextMinSize;
                    case UT_Mode.TMPro:
                        return tmp_text.fontSizeMin;
                }
                return 0;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.resizeTextMinSize = Mathf.RoundToInt(value);
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.fontSizeMin = value;
                        return;
                }
            }
        }
        public float FontSizeMax
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.resizeTextMaxSize;
                    case UT_Mode.TMPro:
                        return tmp_text.fontSizeMax;
                }
                return 0;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.resizeTextMaxSize = Mathf.RoundToInt(value);
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.fontSizeMax = value;
                        return;
                }
            }
        }
        public float FontSize
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.fontSize;
                    case UT_Mode.TMPro:
                        return tmp_text.fontSize;
                }
                return 0;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.fontSize = Mathf.RoundToInt(value);
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.fontSize = value;
                        return;
                }
            }
        }
        public bool SupportRichText
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_text.supportRichText;
                    case UT_Mode.TMPro:
                        return tmp_text.richText;
                }
                return false;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_text.supportRichText = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_text.richText = value;
                        return;
                }
            }
        }

        protected override void InstantiateTMPObject()
        {
            bool wasActive = gameObject.activeSelf;
            //gameObject.SetActive(true);

            Vector2 sizeDelta = ((RectTransform)transform).sizeDelta;
            string tag = unity_text.tag;
            string text = unity_text.text;
            Color color = unity_text.color;
            TextAnchor alignment = unity_text.alignment;
            FontStyle fontStyle = unity_text.fontStyle;
            float fontSize = unity_text.fontSize;
            float lineSpacing = unity_text.lineSpacing;
            bool resizeTextForBestFit = unity_text.resizeTextForBestFit;
            float resizeTextMaxSize = unity_text.resizeTextMaxSize;
            float resizeTextMinSize = unity_text.resizeTextMinSize;
            bool supportRichText = unity_text.supportRichText;
            Font font = unity_text.font;

            bool isMaskingGraphic = unity_text.isMaskingGraphic;
            bool maskable = unity_text.maskable;
            Material material = unity_text.material;
            bool raycastTarget = unity_text.raycastTarget;

            DestroyImmediate(unity_text);

            tmp_text = gameObject.AddComponent<TextMeshProUGUI>();

            tmp_text.tag = tag;
            tmp_text.text = text;
            tmp_text.color = color;
            switch (alignment)
            {
                case TextAnchor.LowerCenter:
                    tmp_text.alignment = TextAlignmentOptions.Bottom;
                    break;
                case TextAnchor.LowerLeft:
                    tmp_text.alignment = TextAlignmentOptions.BottomLeft;
                    break;
                case TextAnchor.LowerRight:
                    tmp_text.alignment = TextAlignmentOptions.BottomRight;
                    break;
                case TextAnchor.MiddleCenter:
                    tmp_text.alignment = TextAlignmentOptions.Center;
                    break;
                case TextAnchor.MiddleLeft:
                    tmp_text.alignment = TextAlignmentOptions.MidlineLeft;
                    break;
                case TextAnchor.MiddleRight:
                    tmp_text.alignment = TextAlignmentOptions.MidlineRight;
                    break;
                case TextAnchor.UpperCenter:
                    tmp_text.alignment = TextAlignmentOptions.Top;
                    break;
                case TextAnchor.UpperLeft:
                    tmp_text.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case TextAnchor.UpperRight:
                    tmp_text.alignment = TextAlignmentOptions.TopRight;
                    break;
            }
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    tmp_text.fontStyle = FontStyles.Bold;
                    break;
                case FontStyle.BoldAndItalic:
                    tmp_text.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    break;
                case FontStyle.Italic:
                    tmp_text.fontStyle = FontStyles.Italic;
                    break;
                case FontStyle.Normal:
                    tmp_text.fontStyle = FontStyles.Normal;
                    break;
            }
            tmp_text.fontSize = fontSize;
            tmp_text.font = GetTMPFont(font);
            tmp_text.lineSpacing = lineSpacing;
            tmp_text.enableAutoSizing = resizeTextForBestFit;
            tmp_text.fontSizeMin = resizeTextMinSize;
            tmp_text.fontSizeMax = resizeTextMaxSize;
            tmp_text.richText = supportRichText;

            tmp_text.isMaskingGraphic = isMaskingGraphic;
            tmp_text.maskable = maskable;
            tmp_text.material = material;
            tmp_text.raycastTarget = raycastTarget;
            ((RectTransform)transform).sizeDelta = sizeDelta;

            gameObject.SetActive(wasActive);
        }

        protected override void InstantiateUnityObject()
        {
            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(true);

            string tag = unity_text.tag;
            string text = tmp_text.text;
            Color color = tmp_text.color;
            TextAlignmentOptions alignment = tmp_text.alignment;
            FontStyles fontStyle = tmp_text.fontStyle;
            float fontSize = tmp_text.fontSize;
            float lineSpacing = tmp_text.lineSpacing;
            bool resizeTextForBestFit = tmp_text.enableAutoSizing;
            float resizeTextMaxSize = tmp_text.fontSizeMax;
            float resizeTextMinSize = tmp_text.fontSizeMin;
            bool supportRichText = tmp_text.richText;
            TMP_FontAsset font = tmp_text.font;

            bool isMaskingGraphic = tmp_text.isMaskingGraphic;
            bool maskable = tmp_text.maskable;
            Material material = tmp_text.material;
            bool raycastTarget = tmp_text.raycastTarget;

            DestroyImmediate(tmp_text);

            unity_text = gameObject.AddComponent<Text>();

            unity_text.tag = tag;
            unity_text.text = text;
            unity_text.color = color;
            switch (alignment)
            {
                case TextAlignmentOptions.Baseline:
                case TextAlignmentOptions.Bottom:
                    unity_text.alignment = TextAnchor.LowerCenter;
                    break;
                case TextAlignmentOptions.BaselineLeft:
                case TextAlignmentOptions.BaselineFlush:
                case TextAlignmentOptions.BaselineGeoAligned:
                case TextAlignmentOptions.BaselineJustified:
                case TextAlignmentOptions.BottomFlush:
                case TextAlignmentOptions.BottomGeoAligned:
                case TextAlignmentOptions.BottomJustified:
                case TextAlignmentOptions.BottomLeft:
                    unity_text.alignment = TextAnchor.LowerLeft;
                    break;
                case TextAlignmentOptions.BottomRight:
                    unity_text.alignment = TextAnchor.LowerRight;
                    break;
                case TextAlignmentOptions.CenterGeoAligned:
                case TextAlignmentOptions.Flush:
                case TextAlignmentOptions.Justified:
                case TextAlignmentOptions.MidlineFlush:
                case TextAlignmentOptions.MidlineGeoAligned:
                case TextAlignmentOptions.MidlineJustified:
                case TextAlignmentOptions.MidlineLeft:
                case TextAlignmentOptions.Left:
                default:
                    unity_text.alignment = TextAnchor.MiddleLeft;
                    break;
                case TextAlignmentOptions.Midline:
                case TextAlignmentOptions.Center:
                    unity_text.alignment = TextAnchor.MiddleCenter;
                    break;
                case TextAlignmentOptions.MidlineRight:
                case TextAlignmentOptions.Right:
                    unity_text.alignment = TextAnchor.MiddleRight;
                    break;
                case TextAlignmentOptions.CaplineFlush:
                case TextAlignmentOptions.CaplineGeoAligned:
                case TextAlignmentOptions.CaplineJustified:
                case TextAlignmentOptions.CaplineLeft:
                case TextAlignmentOptions.TopFlush:
                case TextAlignmentOptions.TopGeoAligned:
                case TextAlignmentOptions.TopJustified:
                case TextAlignmentOptions.TopLeft:
                    unity_text.alignment = TextAnchor.UpperLeft;
                    break;
                case TextAlignmentOptions.Capline:
                case TextAlignmentOptions.Top:
                    unity_text.alignment = TextAnchor.UpperCenter;
                    break;
                case TextAlignmentOptions.CaplineRight:
                case TextAlignmentOptions.TopRight:
                    unity_text.alignment = TextAnchor.UpperRight;
                    break;
            }
            switch (fontStyle)
            {
                case FontStyles.Bold | FontStyles.Italic:
                    unity_text.fontStyle = FontStyle.BoldAndItalic;
                    break;
                case FontStyles.Bold:
                    unity_text.fontStyle = FontStyle.Bold;
                    break;
                case FontStyles.Italic:
                    unity_text.fontStyle = FontStyle.Italic;
                    break;
                default:
                    unity_text.fontStyle = FontStyle.Normal;
                    break;
            }
            unity_text.fontSize = Mathf.RoundToInt(fontSize);
            unity_text.font = GetUnityFont(font);
            unity_text.lineSpacing = lineSpacing;
            unity_text.resizeTextForBestFit = resizeTextForBestFit;
            unity_text.resizeTextMinSize = Mathf.RoundToInt(resizeTextMinSize);
            unity_text.resizeTextMaxSize = Mathf.RoundToInt(resizeTextMaxSize);
            unity_text.supportRichText = supportRichText;

            unity_text.isMaskingGraphic = isMaskingGraphic;
            unity_text.maskable = maskable;
            unity_text.material = material;
            unity_text.raycastTarget = raycastTarget;

            gameObject.SetActive(wasActive);
        }

        public override void CheckIfValid()
        {
            unity_text = GetComponent<Text>();
            if (unity_text != null)
            {
                Mode = UT_Mode.Unity;
                return;
            }
            tmp_text = GetComponent<TMP_Text>();
            if (tmp_text != null)
            {
                Mode = UT_Mode.TMPro;
                return;
            }
        }
    }
}