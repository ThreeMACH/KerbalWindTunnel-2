using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Graphing
{
    public class AxisBoundSetter : MonoBehaviour, IDeselectHandler
    {
        private AxisUI parentAxis;
        public AxisUI ParentAxis { get => parentAxis; }
        private int bound;
        public int Bound { get => bound; }
        private UI_Tools.Universal_Text.UT_InputField inputField;
        private UnityEngine.UI.Toggle toggle;
        System.Predicate<float> validEntryPredicate;
        private bool ValidMinPredicate(float value) => value < parentAxis.Max;
        private bool ValidMaxPredicate(float value) => value > parentAxis.Min;
        private Color validColor;
        [SerializeField]
        public Color invalidColor = Color.red;

        private string oldValue;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            inputField = GetComponentInChildren<UI_Tools.Universal_Text.UT_InputField>();
            toggle = GetComponentInChildren<UnityEngine.UI.Toggle>();
            inputField.OnEndEdit.AddListener(OnEndEdit);
        }
        internal void UpdateField()
        {
            inputField.Text = bound < 0 ? parentAxis.Min.ToString() : parentAxis.Max.ToString();
            oldValue = inputField.Text;
        }
        internal void Init(AxisUI axis, int bound)
        {
            parentAxis = axis;
            this.bound = bound;
            UpdateField();
            toggle.SetIsOnWithoutNotify(bound < 0 ? axis.AutoSetMin : axis.AutoSetMax);
            if (bound < 0)
                validEntryPredicate = ValidMinPredicate;
            else
                validEntryPredicate = ValidMaxPredicate;
            validColor = inputField.TextComponent.color;

            RectTransform rectTransform = (RectTransform)transform;
            switch (axis.Side)
            {
                case AxisSide.Bottom:
                    rectTransform.pivot = new Vector2(bound < 0 ? 0 : 1, 1);
                    rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.anchoredPosition = new Vector2(0, -axis.TickWidth);
                    break;
                case AxisSide.Left:
                    rectTransform.pivot = new Vector2(1, bound < 0 ? 0 : 1);
                    rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(-axis.TickWidth, 0);
                    break;
                case AxisSide.Right:
                    rectTransform.pivot = new Vector2(0, bound < 0 ? 0 : 1);
                    rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(axis.TickWidth, 0);
                    break;
                case AxisSide.Top:
                    rectTransform.pivot = new Vector2(bound < 0 ? 0 : 1, 0);
                    rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.anchoredPosition = new Vector2(0, axis.TickWidth);
                    break;
            }
        }
        internal void UpdatePosition()
        {
            RectTransform rectTransform = (RectTransform)transform;
            switch (parentAxis.Side)
            {
                case AxisSide.Bottom:
                    rectTransform.anchoredPosition = new Vector2(0, -parentAxis.TickWidth);
                    break;
                case AxisSide.Left:
                    rectTransform.anchoredPosition = new Vector2(-parentAxis.TickWidth, 0);
                    break;
                case AxisSide.Right:
                    rectTransform.anchoredPosition = new Vector2(parentAxis.TickWidth, 0);
                    break;
                case AxisSide.Top:
                    rectTransform.anchoredPosition = new Vector2(0, parentAxis.TickWidth);
                    break;
            }
        }
        public void ValueChanged(string newValue)
        {
            // TODO: Highlight text red when invalid and return to normal when valid.
            //Debug.Log("ValueChanged: " + newValue);
            if (validEntryPredicate == null)
                return;
            inputField.TextComponent.color =
                (newValue == "" ||
                (float.TryParse(newValue, out float value) && validEntryPredicate(value)))
                ? validColor : invalidColor;
        }

        public void OnEndEdit(string newValue)
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);
            else
                OnDeselect(null);

            if (!float.TryParse(newValue, out float numValue))
                return;
            if (!validEntryPredicate(numValue))
                return;
            if (string.Equals(oldValue, newValue))
                return;
            oldValue = newValue;
            if (bound < 0)
            {
                if (numValue == parentAxis.AutoMin)
                {
                    toggle.isOn = true;
                    return;
                }
                parentAxis.AutoSetMin = false;
                parentAxis.Min = numValue;
            }
            else
            {
                if (numValue == parentAxis.AutoMax)
                {
                    toggle.isOn = true;
                    return;
                }
                parentAxis.AutoSetMax = false;
                parentAxis.Max = numValue;
            }
            toggle.SetIsOnWithoutNotify(false);
        }

        public void BoundsAuto(bool auto)
        {
            if (bound < 0)
                parentAxis.AutoSetMin = auto;
            else
                parentAxis.AutoSetMax = auto;
            if (!auto)
                OnEndEdit(inputField.Text);
            else
                UpdateField();
        }

        public void OnDeselect(BaseEventData _)
            => StartCoroutine(CheckSelectedLater());
        private IEnumerator CheckSelectedLater()
        {
            yield return null;
            DeselectInternal();
        }
        private void DeselectInternal()
        {
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject != null && selectedObject.GetComponentInParent<AxisUI>() == parentAxis)
                return;
            foreach (AxisBoundSetter window in parentAxis.GetComponentsInChildren<AxisBoundSetter>())
                Destroy(window.gameObject);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy() => parentAxis.UnregisterSetterWindow(this);
    }
}