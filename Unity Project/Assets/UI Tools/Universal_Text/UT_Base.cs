using System.Collections.Generic;
using UnityEngine;

namespace UI_Tools.Universal_Text
{
    [DisallowMultipleComponent]
    public abstract class UT_Base : MonoBehaviour
    {
        [SerializeField]
        public Font overrideUnityFont = null;
        [SerializeField]
        public TMPro.TMP_FontAsset overrideTMPFont = null;
        [SerializeField]
        private UT_Mode queuedMode = UT_Mode.Invalid;

        protected virtual Font GetUnityFont(TMPro.TMP_FontAsset font)
            => overrideUnityFont ?? UT_FontManager.GetUnityFont(font);

        protected virtual TMPro.TMP_FontAsset GetTMPFont(Font font)
            => overrideTMPFont ?? UT_FontManager.GetTMPFont(font);

        public abstract void CheckIfValid();

        public UT_Mode Mode
        {
            get
            {
                if (_mode == UT_Mode.Invalid)
                    CheckIfValid();
                return _mode;
            }
            protected set => _mode = value;
        }
        private UT_Mode _mode = UT_Mode.Invalid;

        public static void CreateUTComponents(IEnumerable<GameObject> targets, bool recurseBelowDropdowns = false, bool recurseBelowInputFields = false)
        {
            foreach (GameObject obj in targets)
                CreateUTComponent(obj, true, recurseBelowDropdowns, recurseBelowInputFields);
        }

        private static void CreateUTComponent(GameObject target, bool recursive = true, bool recurseBelowDropdowns = false, bool recurseBelowInputFields = false)
        {
            if ((target.TryGetComponent<UnityEngine.UI.Text>(out _) || target.TryGetComponent<TMPro.TMP_Text>(out _)) &&
                !target.TryGetComponent<UT_Text>(out _))
                target.AddComponent<UT_Text>();
            if (target.TryGetComponent<UnityEngine.UI.Dropdown>(out _) || target.TryGetComponent<TMPro.TMP_Dropdown>(out _))
            {
                if (!target.TryGetComponent<UT_Dropdown>(out _))
                    target.AddComponent<UT_Dropdown>();
                recursive &= recurseBelowDropdowns;
            }
            if (target.TryGetComponent<UnityEngine.UI.InputField>(out _) || target.TryGetComponent<TMPro.TMP_InputField>(out _))
            {
                if (!target.TryGetComponent<UT_InputField>(out _))
                    target.AddComponent<UT_InputField>();
                recursive &= recurseBelowInputFields;
            }
            if (!recursive)
                return;
            for (int index = 0; index < target.transform.childCount; index++)
            {
                CreateUTComponent(target, recursive, recurseBelowDropdowns, recurseBelowInputFields);
            }
        }

        public static void SetMode(GameObject gameObject, UT_Mode mode, bool forceNow = false)
        {
            if (mode == UT_Mode.TMPro && TMPro.TMP_Settings.instance == null)
                throw new System.NullReferenceException("TMP_Settings is not loaded.");
            foreach (UT_Base component in gameObject.GetComponentsInChildren<UT_Base>(true))    // TODO: UT_Base
            {
                component.SetMode(mode, forceNow);
            }
        }
        public static void SetMode(IEnumerable<GameObject> objects, UT_Mode mode, bool forceNow = false)
        {
            if (mode == UT_Mode.TMPro && TMPro.TMP_Settings.instance == null)
                throw new System.NullReferenceException("TMP_Settings is not loaded.");
            foreach (GameObject obj in objects)
                SetMode(obj, mode, forceNow);
        }
        public void SetMode(UT_Mode mode, bool forceNow = false)
        {
            if (mode == UT_Mode.TMPro && TMPro.TMP_Settings.instance == null)
                throw new System.NullReferenceException("TMP_Settings is not loaded.");
            if (mode == Mode)
                return;
            if (mode == UT_Mode.Invalid)
                throw new System.ArgumentException("Desired mode cannot be 'Invalid'");
            if (!forceNow && !gameObject.activeInHierarchy)
            {
                queuedMode = mode;
                return;
            }
            Mode = mode;
            if (mode == UT_Mode.Unity)
                InstantiateUnityObject();
            else
                InstantiateTMPObject();
        }

        protected abstract void InstantiateTMPObject();

        protected abstract void InstantiateUnityObject();

        protected virtual void Awake()
        {
            CheckIfValid();
            if (queuedMode != UT_Mode.Invalid && queuedMode != Mode)
                SetMode(queuedMode);
        }
    }
}