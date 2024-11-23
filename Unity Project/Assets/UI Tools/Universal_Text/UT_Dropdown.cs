using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI_Tools.Universal_Text
{
    [DisallowMultipleComponent]
    public class UT_Dropdown : UT_Base
    {
        private Dropdown unity_dropdown;
        private TMP_Dropdown tmp_dropdown;
        private Dropdown.DropdownEvent unity_event;
        private TMP_Dropdown.DropdownEvent tmp_event;

        public Selectable DropdownObject
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_dropdown;
                    case UT_Mode.TMPro:
                        return tmp_dropdown;
                }
                return null;
            }
        }
        public int Value
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_dropdown.@value;
                    case UT_Mode.TMPro:
                        return tmp_dropdown.@value;
                }
                return -1;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_dropdown.@value = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_dropdown.@value = value;
                        return;
                }
            }
        }
        public UnityEngine.Events.UnityEvent<int> OnValueChanged
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_dropdown.onValueChanged;
                    case UT_Mode.TMPro:
                        return tmp_dropdown.onValueChanged;
                }
                return null;
            }
        }
        public List<Dropdown.OptionData> Options
        {
            get
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        return unity_dropdown.options;
                    case UT_Mode.TMPro:
                        return tmp_dropdown.options.Select(o => new Dropdown.OptionData(o.text, o.image)).ToList();
                }
                return null;
            }
            set
            {
                switch (Mode)
                {
                    case UT_Mode.Unity:
                        unity_dropdown.options = value;
                        return;
                    case UT_Mode.TMPro:
                        tmp_dropdown.options = value.Select(o => new TMP_Dropdown.OptionData(o.text, o.image)).ToList();
                        return;
                }
            }
        }

        protected override void InstantiateTMPObject()
        {
            bool wasActive = gameObject.activeSelf;
            //gameObject.SetActive(true);

            Vector2 sizeDelta = ((RectTransform)transform).sizeDelta;
            Image captionImage = unity_dropdown.captionImage;
            Text captionText = unity_dropdown.captionText;
            Image itemImage = unity_dropdown.itemImage;
            Text itemText = unity_dropdown.itemText;
            unity_event = unity_dropdown.onValueChanged;
            List<Dropdown.OptionData> options = unity_dropdown.options;
            RectTransform template = unity_dropdown.template;
            int value = unity_dropdown.value;

            AnimationTriggers animationTriggers = unity_dropdown.animationTriggers;
            ColorBlock colors = unity_dropdown.colors;
            Image image = unity_dropdown.image;
            bool interactable = unity_dropdown.interactable;
            Navigation navigation = unity_dropdown.navigation;
            SpriteState spriteState = unity_dropdown.spriteState;
            string tag = unity_dropdown.tag;
            Graphic targetGraphic = unity_dropdown.targetGraphic;
            Selectable.Transition transition = unity_dropdown.transition;

            UT_Text captionText_UT = null, itemText_UT = null;
            if (captionText != null && !captionText.TryGetComponent(out captionText_UT))
            {
                captionText_UT = captionText.gameObject.AddComponent<UT_Text>();
                captionText_UT.overrideTMPFont = overrideTMPFont;
                captionText_UT.overrideUnityFont = overrideUnityFont;
            }
            captionText_UT?.SetMode(UT_Mode.TMPro, true);

            if (itemText != null && !itemText.TryGetComponent(out itemText_UT))
            {
                itemText_UT = itemText.gameObject.AddComponent<UT_Text>();
                itemText_UT.overrideTMPFont = overrideTMPFont;
                itemText_UT.overrideUnityFont = overrideUnityFont;
            }
            itemText_UT?.SetMode(UT_Mode.TMPro, true);

            gameObject.SetActive(false);

            DestroyImmediate(unity_dropdown);

            tmp_dropdown = gameObject.AddComponent<TMP_Dropdown>();

            tmp_dropdown.animationTriggers = animationTriggers;
            tmp_dropdown.colors = colors;
            tmp_dropdown.image = image;
            tmp_dropdown.interactable = interactable;
            tmp_dropdown.navigation = navigation;
            tmp_dropdown.spriteState = spriteState;
            tmp_dropdown.tag = tag;
            tmp_dropdown.targetGraphic = targetGraphic;
            tmp_dropdown.transition = transition;

            tmp_dropdown.captionImage = captionImage;
            tmp_dropdown.captionText = (TMP_Text)captionText_UT?.TextObject;
            tmp_dropdown.itemImage = itemImage;
            tmp_dropdown.itemText = (TMP_Text)itemText_UT?.TextObject;
            if (tmp_event == null)
                tmp_dropdown.onValueChanged.AddListener(unity_event.Invoke);
            else
                tmp_dropdown.onValueChanged = tmp_event;
            tmp_dropdown.options = options.Select(o => new TMP_Dropdown.OptionData(o.text, o.image)).ToList();
            tmp_dropdown.template = template;
            tmp_dropdown.value = value;
            ((RectTransform)transform).sizeDelta = sizeDelta;

            gameObject.SetActive(wasActive);
        }

        protected override void InstantiateUnityObject()
        {
            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(true);

            Image captionImage = tmp_dropdown.captionImage;
            TMP_Text captionText = tmp_dropdown.captionText;
            Image itemImage = tmp_dropdown.itemImage;
            TMP_Text itemText = tmp_dropdown.itemText;
            tmp_event = tmp_dropdown.onValueChanged;
            List<TMP_Dropdown.OptionData> options = tmp_dropdown.options;
            RectTransform template = tmp_dropdown.template;
            int value = tmp_dropdown.value;

            AnimationTriggers animationTriggers = tmp_dropdown.animationTriggers;
            ColorBlock colors = tmp_dropdown.colors;
            Image image = tmp_dropdown.image;
            bool interactable = tmp_dropdown.interactable;
            Navigation navigation = tmp_dropdown.navigation;
            SpriteState spriteState = tmp_dropdown.spriteState;
            string tag = tmp_dropdown.tag;
            Graphic targetGraphic = tmp_dropdown.targetGraphic;
            Selectable.Transition transition = tmp_dropdown.transition;

            UT_Text captionText_UT = null, itemText_UT = null;
            if (captionText != null && !captionText.TryGetComponent(out captionText_UT))
            {
                captionText_UT = captionText.gameObject.AddComponent<UT_Text>();
                captionText_UT.overrideTMPFont = overrideTMPFont;
                captionText_UT.overrideUnityFont = overrideUnityFont;
            }
            captionText_UT?.SetMode(UT_Mode.Unity, true);

            if (itemText != null && !itemText.TryGetComponent(out itemText_UT))
            {
                itemText_UT = itemText_UT.gameObject.AddComponent<UT_Text>();
                itemText_UT.overrideTMPFont = overrideTMPFont;
                itemText_UT.overrideUnityFont = overrideUnityFont;
            }
            itemText_UT?.SetMode(UT_Mode.Unity, true);

            gameObject.SetActive(false);

            DestroyImmediate(tmp_dropdown);

            unity_dropdown = gameObject.AddComponent<Dropdown>();

            unity_dropdown.animationTriggers = animationTriggers;
            unity_dropdown.colors = colors;
            unity_dropdown.image = image;
            unity_dropdown.interactable = interactable;
            unity_dropdown.navigation = navigation;
            unity_dropdown.spriteState = spriteState;
            unity_dropdown.tag = tag;
            unity_dropdown.targetGraphic = targetGraphic;
            unity_dropdown.transition = transition;

            unity_dropdown.captionImage = captionImage;
            unity_dropdown.captionText = (Text)captionText_UT?.TextObject;
            unity_dropdown.itemImage = itemImage;
            unity_dropdown.itemText = (Text)itemText_UT?.TextObject;
            if (unity_event == null)
                unity_dropdown.onValueChanged.AddListener(tmp_event.Invoke);
            else
                unity_dropdown.onValueChanged = unity_event;
            unity_dropdown.options = options.Select(o => new Dropdown.OptionData(o.text, o.image)).ToList();
            unity_dropdown.template = template;
            unity_dropdown.value = value;

            gameObject.SetActive(wasActive);
        }

        public override void CheckIfValid()
        {
            unity_dropdown = GetComponent<Dropdown>();
            if (unity_dropdown != null)
            {
                Mode = UT_Mode.Unity;
                return;
            }
            tmp_dropdown = GetComponent<TMP_Dropdown>();
            if (tmp_dropdown != null)
            {
                Mode = UT_Mode.TMPro;
                return;
            }
        }

        public void ClearOptions()
        {
            switch (Mode)
            {
                case UT_Mode.Unity:
                    unity_dropdown.ClearOptions();
                    return;
                case UT_Mode.TMPro:
                    tmp_dropdown.ClearOptions();
                    return;
            }
        }

        public void AddOptions(List<string> options)
        {
            switch (Mode)
            {
                case UT_Mode.Unity:
                    unity_dropdown.AddOptions(options);
                    return;
                case UT_Mode.TMPro:
                    tmp_dropdown.AddOptions(options);
                    return;
            }
        }

        public void Hide()
        {
            switch (Mode)
            {
                case UT_Mode.Unity:
                    unity_dropdown.Hide();
                    return;
                case UT_Mode.TMPro:
                    tmp_dropdown.Hide();
                    return;
            }
        }

        public void RefreshShownValue()
        {
            switch (Mode)
            {
                case UT_Mode.Unity:
                    unity_dropdown.RefreshShownValue();
                    return;
                case UT_Mode.TMPro:
                    tmp_dropdown.RefreshShownValue();
                    return;
            }
        }

        public void Show()
        {
            switch (Mode)
            {
                case UT_Mode.Unity:
                    unity_dropdown.Show();
                    return;
                case UT_Mode.TMPro:
                    tmp_dropdown.Show();
                    return;
            }
        }
    }
}