using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace UI_Tools.Universal_Text
{
    [DisallowMultipleComponent]
    public class UT_InputField : UT_Base
    {
        private InputField unity_inputField;
        private TMP_InputField tmp_inputField;
        private InputField.OnChangeEvent unity_changeEvent;
        private InputField.SubmitEvent unity_submitEvent;
        private TMP_InputField.OnChangeEvent tmp_changeEvent;
        private TMP_InputField.SubmitEvent tmp_submitEvent;

        public Selectable InputFieldObject
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField;
                    case UT_Mode.TMPro:
                        return tmp_inputField;
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
                        return unity_inputField.text;
                    case UT_Mode.TMPro:
                        return tmp_inputField.text;
                }
                return null;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_inputField.text = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_inputField.text = value;
                        return;
                }
            }
        }
        public UnityEngine.Events.UnityEvent<string> OnEndEdit
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField.onEndEdit;
                    case UT_Mode.TMPro:
                        return tmp_inputField.onEndEdit;
                }
                return null;
            }
        }
        public UnityEngine.Events.UnityEvent<string> OnValueChanged
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField.onValueChanged;
                    case UT_Mode.TMPro:
                        return tmp_inputField.onValueChanged;
                }
                return null;
            }
        }
        public bool ReadOnly
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField.readOnly;
                    case UT_Mode.TMPro:
                        return tmp_inputField.readOnly;
                }
                return true;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_inputField.readOnly = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_inputField.readOnly = value;
                        return;
                }
            }
        }
        public UT_Text TextComponent
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField.textComponent.GetComponent<UT_Text>();
                    case UT_Mode.TMPro:
                        return tmp_inputField.textComponent.GetComponent<UT_Text>();
                }
                return null;
            }
        }
        public Graphic Placeholder
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_inputField.placeholder;
                    case UT_Mode.TMPro:
                        return tmp_inputField.placeholder;
                }
                return null;
            }
        }

        protected override void InstantiateTMPObject()
        {
            bool wasActive = gameObject.activeSelf;
            //gameObject.SetActive(true);

            //string serializedData = JsonUtility.ToJson(unity_inputField);

            Vector2 sizeDelta = ((RectTransform)transform).sizeDelta;
            char asteriskChar = unity_inputField.asteriskChar;
            float caretBlinkRate = unity_inputField.caretBlinkRate;
            Color caretColor = unity_inputField.caretColor;
            int caretPosition = unity_inputField.caretPosition;
            int caretWidth = unity_inputField.caretWidth;
            int characterLimit = unity_inputField.characterLimit;
            InputField.CharacterValidation characterValidation = unity_inputField.characterValidation;
            InputField.ContentType contentType = unity_inputField.contentType;
            bool customCaretColor = unity_inputField.customCaretColor;
            InputField.InputType inputType = unity_inputField.inputType;
            TouchScreenKeyboardType keyboardType = unity_inputField.keyboardType;
            InputField.LineType lineType = unity_inputField.lineType;
            unity_submitEvent = unity_inputField.onEndEdit;
            InputField.OnValidateInput onValidateInput = unity_inputField.onValidateInput;
            unity_changeEvent = unity_inputField.onValueChanged;
            Graphic placeholder = unity_inputField.placeholder;
            bool readOnly = unity_inputField.readOnly;
            int selectionAnchorPosition = unity_inputField.selectionAnchorPosition;
            Color selectionColor = unity_inputField.selectionColor;
            int selectionFocusPosition = unity_inputField.selectionFocusPosition;
            bool shouldHideMobileInput = unity_inputField.shouldHideMobileInput;
            string text = unity_inputField.text;
            Text textComponent = unity_inputField.textComponent;

            AnimationTriggers animationTriggers = unity_inputField.animationTriggers;
            ColorBlock colors = unity_inputField.colors;
            Image image = unity_inputField.image;
            bool interactable = unity_inputField.interactable;
            Navigation navigation = unity_inputField.navigation;
            SpriteState spriteState = unity_inputField.spriteState;
            string tag = unity_inputField.tag;
            Graphic targetGraphic = unity_inputField.targetGraphic;
            Selectable.Transition transition = unity_inputField.transition;

            GameObject textArea = GetComponentsInChildren<RectMask2D>()?.FirstOrDefault(c => c.gameObject.name == "Text Area")?.gameObject;
            RectTransform textArea_transform = (RectTransform)(textArea?.transform);
            if (textArea == null)
            {
                textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D))
                {
                    layer = gameObject.layer
                };
                textArea_transform = (RectTransform)textArea.transform;
                textArea_transform.SetParent(transform, false);
                textArea_transform.anchorMin = new Vector2(0, 0);
                textArea_transform.anchorMax = new Vector2(1, 1);
                textArea_transform.offsetMin = new Vector2(10, 6);
                textArea_transform.offsetMax = new Vector2(-10, -7);
            }

            UT_Text placeholder_UT = null;
            if (placeholder != null && !placeholder.TryGetComponent(out placeholder_UT))
            {
                placeholder_UT = placeholder.gameObject.AddComponent<UT_Text>();
                placeholder_UT.overrideTMPFont = overrideTMPFont;
                placeholder_UT.overrideUnityFont = overrideUnityFont;
            }
            placeholder_UT?.SetMode(UT_Mode.TMPro, true);
            placeholder_UT?.transform.SetParent(textArea_transform, true);

            UT_Text text_UT = null;
            if (textComponent != null && !textComponent.TryGetComponent(out text_UT))
            {
                text_UT = textComponent.gameObject.AddComponent<UT_Text>();
                text_UT.overrideTMPFont = overrideTMPFont;
                text_UT.overrideUnityFont = overrideUnityFont;
            }
            text_UT?.SetMode(UT_Mode.TMPro, true);
            text_UT?.transform.SetParent(textArea_transform, true);

            GameObject caret = GetComponentsInChildren<LayoutElement>().FirstOrDefault(element => element.gameObject.name.EndsWith("Input Caret"))?.gameObject;

            gameObject.SetActive(false);

            if (caret != null)
                DestroyImmediate(caret);
            DestroyImmediate(unity_inputField);

            tmp_inputField = gameObject.AddComponent<TMP_InputField>();

            tmp_inputField.animationTriggers = animationTriggers;
            tmp_inputField.colors = colors;
            tmp_inputField.image = image;
            tmp_inputField.interactable = interactable;
            tmp_inputField.navigation = navigation;
            tmp_inputField.spriteState = spriteState;
            tmp_inputField.tag = tag;
            tmp_inputField.targetGraphic = targetGraphic;
            tmp_inputField.transition = transition;

            //JsonUtility.FromJsonOverwrite(serializedData, tmp_inputField);

            tmp_inputField.textComponent = (TMP_Text)text_UT?.TextObject;
            tmp_inputField.placeholder = placeholder_UT?.TextObject;

            if (tmp_submitEvent == null)
                tmp_inputField.onEndEdit.AddListener(unity_submitEvent.Invoke);
            else
                tmp_inputField.onEndEdit = tmp_submitEvent;
            if (onValidateInput != null)
                tmp_inputField.onValidateInput = onValidateInput.Invoke;
            if (tmp_changeEvent == null)
                tmp_inputField.onValueChanged.AddListener(unity_changeEvent.Invoke);
            else
                tmp_inputField.onValueChanged = tmp_changeEvent;
            tmp_inputField.asteriskChar = asteriskChar;
            tmp_inputField.caretBlinkRate = caretBlinkRate;
            tmp_inputField.caretColor = caretColor;
            tmp_inputField.caretWidth = caretWidth;
            tmp_inputField.characterLimit = characterLimit;
            switch (characterValidation)
            {
                default:
                case InputField.CharacterValidation.None:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.None;
                    break;
                case InputField.CharacterValidation.Integer:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
                    break;
                case InputField.CharacterValidation.Decimal:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
                    break;
                case InputField.CharacterValidation.Alphanumeric:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
                    break;
                case InputField.CharacterValidation.Name:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.Name;
                    break;
                case InputField.CharacterValidation.EmailAddress:
                    tmp_inputField.characterValidation = TMP_InputField.CharacterValidation.EmailAddress;
                    break;
            }
            tmp_inputField.textViewport = textArea_transform;
            tmp_inputField.contentType = (TMP_InputField.ContentType)contentType;
            tmp_inputField.customCaretColor = customCaretColor;
            tmp_inputField.inputType = (TMP_InputField.InputType)inputType;
            tmp_inputField.keyboardType = keyboardType;
            tmp_inputField.lineType = (TMP_InputField.LineType)lineType;
            tmp_inputField.readOnly = readOnly;
            //tmp_inputField.selectionAnchorPosition = selectionAnchorPosition;//
            tmp_inputField.selectionColor = selectionColor;
            //tmp_inputField.selectionFocusPosition = selectionFocusPosition;//
            tmp_inputField.shouldHideMobileInput = shouldHideMobileInput;
            //tmp_inputField.caretPosition = caretPosition;//
            tmp_inputField.text = text;
            ((RectTransform)transform).sizeDelta = sizeDelta;

            gameObject.SetActive(wasActive);
        }

        protected override void InstantiateUnityObject()
        {
            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(true);

            char asteriskChar = tmp_inputField.asteriskChar;
            float caretBlinkRate = tmp_inputField.caretBlinkRate;
            Color caretColor = tmp_inputField.caretColor;
            int caretPosition = tmp_inputField.caretPosition;
            int caretWidth = tmp_inputField.caretWidth;
            int characterLimit = tmp_inputField.characterLimit;
            TMP_InputField.CharacterValidation characterValidation = tmp_inputField.characterValidation;
            TMP_InputField.ContentType contentType = tmp_inputField.contentType;
            bool customCaretColor = tmp_inputField.customCaretColor;
            TMP_InputField.InputType inputType = tmp_inputField.inputType;
            TouchScreenKeyboardType keyboardType = tmp_inputField.keyboardType;
            TMP_InputField.LineType lineType = tmp_inputField.lineType;
            tmp_submitEvent = tmp_inputField.onEndEdit;
            TMP_InputField.OnValidateInput onValidateInput = tmp_inputField.onValidateInput;
            tmp_changeEvent = tmp_inputField.onValueChanged;
            Graphic placeholder = tmp_inputField.placeholder;
            bool readOnly = tmp_inputField.readOnly;
            int selectionAnchorPosition = tmp_inputField.selectionAnchorPosition;
            Color selectionColor = tmp_inputField.selectionColor;
            int selectionFocusPosition = tmp_inputField.selectionFocusPosition;
            bool shouldHideMobileInput = tmp_inputField.shouldHideMobileInput;
            string text = tmp_inputField.text;
            TMP_Text textComponent = tmp_inputField.textComponent;

            AnimationTriggers animationTriggers = tmp_inputField.animationTriggers;
            ColorBlock colors = tmp_inputField.colors;
            Image image = tmp_inputField.image;
            bool interactable = tmp_inputField.interactable;
            Navigation navigation = tmp_inputField.navigation;
            SpriteState spriteState = tmp_inputField.spriteState;
            string tag = tmp_inputField.tag;
            Graphic targetGraphic = tmp_inputField.targetGraphic;
            Selectable.Transition transition = tmp_inputField.transition;

            Transform textArea_transform = GetComponentInChildren<RectMask2D>(true)?.transform;
            GameObject caret = textArea_transform?.GetComponentInChildren<TMP_SelectionCaret>(true)?.gameObject;
            if (textArea_transform?.parent == transform)
            {
                for (int i = textArea_transform.childCount - 1; i >= 0; i--)
                    textArea_transform.GetChild(0).SetParent(transform, true);  // Always popping the first element preserves the order.
            }

            UT_Text placeholder_UT = null;
            if (placeholder != null && !placeholder.TryGetComponent(out placeholder_UT))
            {
                placeholder_UT = placeholder.gameObject.AddComponent<UT_Text>();
                placeholder_UT.overrideTMPFont = overrideTMPFont;
                placeholder_UT.overrideUnityFont = overrideUnityFont;
            }
            placeholder_UT?.SetMode(UT_Mode.Unity, true);

            UT_Text text_UT = null;
            if (textComponent != null && !textComponent.TryGetComponent(out text_UT))
            {
                text_UT = textComponent.gameObject.AddComponent<UT_Text>();
                placeholder_UT.overrideTMPFont = overrideTMPFont;
                text_UT.overrideUnityFont = overrideUnityFont;
            }
            text_UT?.SetMode(UT_Mode.Unity, true);

            gameObject.SetActive(false);

            DestroyImmediate(tmp_inputField);
            if (caret != null)
                DestroyImmediate(caret);

            unity_inputField = gameObject.AddComponent<InputField>();

            unity_inputField.animationTriggers = animationTriggers;
            unity_inputField.colors = colors;
            unity_inputField.image = image;
            unity_inputField.interactable = interactable;
            unity_inputField.navigation = navigation;
            unity_inputField.spriteState = spriteState;
            unity_inputField.tag = tag;
            unity_inputField.targetGraphic = targetGraphic;
            unity_inputField.transition = transition;

            unity_inputField.textComponent = (Text)text_UT?.TextObject;
            unity_inputField.placeholder = placeholder_UT?.TextObject;
            unity_inputField.text = text;
            if (unity_submitEvent == null)
                unity_inputField.onEndEdit.AddListener(tmp_submitEvent.Invoke);
            else
                unity_inputField.onEndEdit = unity_submitEvent;
            if (onValidateInput != null)
                unity_inputField.onValidateInput = onValidateInput.Invoke;
            if (unity_changeEvent == null)
                unity_inputField.onValueChanged.AddListener(tmp_changeEvent.Invoke);
            else
                unity_inputField.onValueChanged = unity_changeEvent;
            unity_inputField.asteriskChar = asteriskChar;
            unity_inputField.caretBlinkRate = caretBlinkRate;
            unity_inputField.caretColor = caretColor;
            unity_inputField.caretWidth = caretWidth;
            unity_inputField.characterLimit = characterLimit;
            switch (characterValidation)
            {
                default:
                case TMP_InputField.CharacterValidation.None:
                    unity_inputField.characterValidation = InputField.CharacterValidation.None;
                    break;
                case TMP_InputField.CharacterValidation.Integer:
                    unity_inputField.characterValidation = InputField.CharacterValidation.Integer;
                    break;
                case TMP_InputField.CharacterValidation.Decimal:
                    unity_inputField.characterValidation = InputField.CharacterValidation.Decimal;
                    break;
                case TMP_InputField.CharacterValidation.Alphanumeric:
                    unity_inputField.characterValidation = InputField.CharacterValidation.Alphanumeric;
                    break;
                case TMP_InputField.CharacterValidation.Name:
                    unity_inputField.characterValidation = InputField.CharacterValidation.Name;
                    break;
                case TMP_InputField.CharacterValidation.EmailAddress:
                    unity_inputField.characterValidation = InputField.CharacterValidation.EmailAddress;
                    break;
            }
            unity_inputField.contentType = (InputField.ContentType)contentType;
            unity_inputField.customCaretColor = customCaretColor;
            unity_inputField.inputType = (InputField.InputType)inputType;
            unity_inputField.keyboardType = keyboardType;
            unity_inputField.lineType = (InputField.LineType)lineType;
            unity_inputField.readOnly = readOnly;
            //unity_inputField.selectionAnchorPosition = selectionAnchorPosition;
            unity_inputField.selectionColor = selectionColor;
            //unity_inputField.selectionFocusPosition = selectionFocusPosition;
            unity_inputField.shouldHideMobileInput = shouldHideMobileInput;
            //unity_inputField.caretPosition = caretPosition;

            gameObject.SetActive(wasActive);
        }

        public override void CheckIfValid()
        {
            unity_inputField = GetComponent<InputField>();
            if (unity_inputField != null)
            {
                Mode = UT_Mode.Unity;
                return;
            }
            tmp_inputField = GetComponent<TMP_InputField>();
            if (tmp_inputField != null)
            {
                Mode = UT_Mode.TMPro;
                return;
            }
        }
    }
}