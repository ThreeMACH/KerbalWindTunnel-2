using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Tools
{
    public class NonExclusiveToggleArray : ToggleArray, ISerializationCallbackReceiver
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [SerializeField]
        private List<NonExclusiveToggleArrayItem> nonExclusiveItems;
#pragma warning restore IDE0044 // Add readonly modifier
        [HideInInspector][SerializeField]
        private string[] _serialize_allowableToggles;
        [HideInInspector][SerializeField]
        private bool[] _serialize_enforceMutual;
        public List<NonExclusiveToggleArrayItem> NonExclusiveItems { get => nonExclusiveItems; }

        protected override void Awake()
        {
            base.Awake();
            if (nonExclusiveItems == null)
                return;
            int numItems = items.Count;
            for (int i = Mathf.Min(numItems, nonExclusiveItems.Count) - 1; i >= 0; i--)
            {
                NonExclusiveToggle net = items[i].Toggle.GetComponent<NonExclusiveToggle>();
                if (net == null)
                    continue;
                SetNonExclusiveToggle(net, nonExclusiveItems[i].allowableToggles, nonExclusiveItems[i].enforceMutual);
            }
        }

        public void Add(string item, UnityEngine.Events.UnityAction<bool> action = null, IEnumerable<int> allowableToggles = null, bool enforceMutual = false)
        {
            Add(item, action, ToggleSelector(allowableToggles)?.ToList(), enforceMutual);
        }
        public void Add(string item, UnityEngine.Events.UnityAction<bool> action = null, List<Toggle> allowableToggles = null, bool enforceMutual = false)
        {
            Toggle newItem = base.Add(item, action);

            NonExclusiveToggle net = newItem.GetComponent<NonExclusiveToggle>();
            if (net == null)
                return;
            SetNonExclusiveToggle(net, allowableToggles, enforceMutual);
        }
        public void Insert(int index, string item, UnityEngine.Events.UnityAction<bool> action = null, IEnumerable<int> allowableToggles = null, bool enforceMutual = false)
        {
            Insert(index, item, action, ToggleSelector(allowableToggles)?.ToList(), enforceMutual);
        }
        public void Insert(int index, string item, UnityEngine.Events.UnityAction<bool> action = null, List<Toggle> allowableToggles = null, bool enforceMutual = false)
        {
            Toggle newItem = base.Insert(index, item, action);

            NonExclusiveToggle net = newItem.GetComponent<NonExclusiveToggle>();
            if (net == null)
                return;
            SetNonExclusiveToggle(net, allowableToggles, enforceMutual);
        }
        private void SetNonExclusiveToggle(NonExclusiveToggle nonExclusiveToggle, IEnumerable<int> allowableToggles, bool enforceMutual = false)
        {
            SetNonExclusiveToggle(nonExclusiveToggle, ToggleSelector(allowableToggles).ToList(), enforceMutual);
        }
        private void SetNonExclusiveToggle(NonExclusiveToggle nonExclusiveToggle, List<Toggle> allowableToggles, bool enforceMutual = false)
        {
            nonExclusiveToggle.allowableToggles = allowableToggles;
            nonExclusiveToggle.enforceMutual = enforceMutual;
        }
        private IEnumerable<Toggle> ToggleSelector(IEnumerable<int> toggleEnumerable)
            => toggleEnumerable.Where(n => n < items.Count && items[n].Toggle != null).Select(n => items[n].Toggle);

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (nonExclusiveItems == null)
                return;

            _serialize_allowableToggles = new string[nonExclusiveItems.Count];
            _serialize_enforceMutual = new bool[nonExclusiveItems.Count];
            for (int i = 0; i < nonExclusiveItems.Count; i++)
            {
                _serialize_allowableToggles[i] = string.Join(",", nonExclusiveItems[i].allowableToggles.Select(v => v.ToString()));
                _serialize_enforceMutual[i] = nonExclusiveItems[i].enforceMutual;
            }
            //nonExclusiveItems = null;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (nonExclusiveItems != null)
                return;
            if (_serialize_allowableToggles == null || _serialize_enforceMutual == null)
                return;
            List<NonExclusiveToggleArrayItem> _nonExclusiveItems = new List<NonExclusiveToggleArrayItem>();
            for (int i = 0; i < System.Math.Min(_serialize_allowableToggles.Length, _serialize_enforceMutual.Length); i++)
            {
                _nonExclusiveItems.Add(
                    new NonExclusiveToggleArrayItem() { allowableToggles = string.IsNullOrEmpty(_serialize_allowableToggles[i]) ? new List<int>() : _serialize_allowableToggles[i].Split(',').Select(int.Parse).ToList(), enforceMutual = _serialize_enforceMutual[i] });
            }
            _serialize_allowableToggles = null;
            _serialize_enforceMutual = null;
            nonExclusiveItems = _nonExclusiveItems;
        }

        [System.Serializable]
        public class NonExclusiveToggleArrayItem
        {
            [SerializeField]
            public List<int> allowableToggles = new List<int>();
            [SerializeField]
            public bool enforceMutual;
        }
    }
}