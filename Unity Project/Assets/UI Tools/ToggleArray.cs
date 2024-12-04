using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    [RequireComponent(typeof(LayoutGroup))]
    public class ToggleArray : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        protected GameObject template;
        [SerializeField]
        protected Universal_Text.UT_Text templateTextField;
        [SerializeField]
        public Dropdown.DropdownEvent onChildValueChanged;
        [SerializeField]
        protected List<ToggleArrayItem> items;
        [HideInInspector][SerializeField]
        private List<string> _serialize_labels;
        [HideInInspector][SerializeField]
        private List<Toggle.ToggleEvent> _serialize_actions;
        public IEnumerable<string> Labels
        {
            get => items.Select((t) => t.label);
            set
            {
                Clear();
                foreach (string label in value)
                {
                    Add(label);
                }
            }
        }
        public IEnumerable<bool> States
        {
            get => items.Select((t) => t.Toggle != null && t.Toggle.isOn);
            set
            {
                int i = -1;
                IEnumerator<bool> enumerator = value.GetEnumerator();
                while (enumerator.MoveNext() && i++ < items.Count)
                {
                    if (items[i].Toggle != null)
                        items[i].Toggle.isOn = enumerator.Current;
                }
            }
        }

        public List<ToggleArrayItem> Items { get => items; }

        public bool this[int index]
        {
            get => items[index].Toggle.isOn;
            set => items[index].Toggle.isOn = value;
        }

        public Toggle Add(string item, UnityEngine.Events.UnityAction<bool> action = null)
        {
            ToggleArrayItem newItem = new ToggleArrayItem(item, action);
            items.Add(newItem);
            return SpawnItem(newItem);
        }
        public Toggle Insert(int index, string item, UnityEngine.Events.UnityAction<bool> action = null)
        {
            if (items.Count == 0)
                return Add(item, action);
            ToggleArrayItem newItem = new ToggleArrayItem(item, action);
            items.Insert(index, newItem);
            Toggle child = SpawnItem(newItem);
            if (index == 0)
                child.transform.SetSiblingIndex(items[1].Toggle.transform.GetSiblingIndex() - 1);
            else
                child.transform.SetSiblingIndex(items[index + 1].Toggle.transform.GetSiblingIndex());
            return child;
        }
        public bool Remove(Toggle item)
        {
            ToggleArrayItem itemToDestroy = items.Find(t => t.Toggle == item);
            if (itemToDestroy == null)
                return false;
            GameObject childToDestroy = item.gameObject;
            items.Remove(itemToDestroy);
            if (childToDestroy != null)
                Destroy(childToDestroy);
            return true;
        }
        public bool Remove(string item)
        {
            ToggleArrayItem itemToDestroy = items.Find(t => t.label == item);
            if (itemToDestroy == null)
                return false;
            GameObject childToDestroy = itemToDestroy.Toggle.gameObject;
            items.Remove(itemToDestroy);
            if (childToDestroy != null)
                Destroy(childToDestroy);
            return true;
        }
        public bool RemoveAt(int index)
        {
            if (index >= items.Count)
                return false;
            ToggleArrayItem itemToDestroy = items[index];
            if (itemToDestroy == null)
                return false;
            GameObject childToDestroy = itemToDestroy.Toggle.gameObject;
            items.Remove(itemToDestroy);
            if (childToDestroy != null)
                Destroy(childToDestroy);
            return true;
        }
        public void Clear()
        {
            for (int i = items.Count - 1; i >= 0; i--)
                RemoveAt(i);
            items.Clear();
        }

        protected virtual void Awake()
        {
            foreach (ToggleArrayItem item in items)
                SpawnItem(item);
        }

        protected virtual Toggle SpawnItem(ToggleArrayItem item)
        {
            templateTextField.Text = item.label;    // TODO: Stop editing the prefab.

            Toggle child = Instantiate(template, transform).GetComponent<Toggle>();
            child.onValueChanged.AddListener((bool _) => OnChildValueChanged(child));
            child.gameObject.SetActive(true);
            item.Toggle = child;
            return child;
        }

        private void OnChildValueChanged(Toggle toggle)
        {
            onChildValueChanged?.Invoke(items.FindIndex((t) => t.Toggle == toggle));
        }

        public virtual void OnBeforeSerialize()
        {
            if (items == null)
                return;
            _serialize_labels = new List<string>();
            _serialize_actions = new List<Toggle.ToggleEvent>();
            foreach (ToggleArrayItem item in items)
            {
                _serialize_labels.Add(item.label);
                _serialize_actions.Add(item.action);
            }
            //items = null;
        }

        public virtual void OnAfterDeserialize()
        {
            if (items != null)
                return;
            if (_serialize_labels == null || _serialize_actions == null)
                return;
            List<ToggleArrayItem> _items = new List<ToggleArrayItem>();
            for (int i = 0; i < Math.Min(_serialize_labels.Count, _serialize_actions.Count); i++)
                _items.Add(new ToggleArrayItem(_serialize_labels[i], _serialize_actions[i]));
            _serialize_labels = null;
            _serialize_actions = null;
            items = _items;
        }

        [Serializable]
        public class ToggleArrayEvent : UnityEngine.Events.UnityEvent<int, bool> { }

        [Serializable]
        public class ToggleArrayItem
        {
            [SerializeField]
            public string label;
            [SerializeField]
            public Toggle.ToggleEvent action;
            public Toggle Toggle
            {
                get => _toggle;
                set
                {
                    if (_toggle == value)
                        return;
                    _toggle?.onValueChanged.RemoveListener(OnEvent);
                    _toggle = value;
                    _toggle?.onValueChanged.AddListener(OnEvent);
                }
            }
            private Toggle _toggle;
            private void OnEvent(bool value) => action?.Invoke(value);
            public ToggleArrayItem(string label, UnityEngine.Events.UnityAction<bool> action = null)
            {
                this.label = label;
                if (action != null)
                    this.action.AddListener(action);
            }
            internal ToggleArrayItem(string label, Toggle.ToggleEvent action)
            {
                this.label = label;
                this.action = action;
            }
        }
    }
}