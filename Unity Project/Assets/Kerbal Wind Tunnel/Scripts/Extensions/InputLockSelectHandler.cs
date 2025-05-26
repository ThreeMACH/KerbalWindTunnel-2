using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KerbalWindTunnel.Extensions
{
    public class InputLockSelectHandler : MonoBehaviour, ISelectHandler, IDeselectHandler, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string _lockID = null;
        public string LockID { get => _lockID; }

        private ControlTypes _controlTypes = ControlTypes.None;
        public ControlTypes ControlTypes { get => _controlTypes; set => _controlTypes = value; }

        [SerializeField]
        private ulong _controlTypesSerialized;

        private UnityEngine.Events.UnityEvent<string> endEvent;
        private bool locked = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start()
        {
            endEvent = GetComponent<UI_Tools.Universal_Text.UT_InputField>()?.OnEndEdit;
            if (endEvent == null)
                endEvent = GetComponent<TMPro.TMP_InputField>()?.onEndEdit;
            if (endEvent == null)
                endEvent = GetComponent<UnityEngine.UI.InputField>()?.onEndEdit;
            if (endEvent == null)
            {
                Destroy(this);
                return;
            }
            endEvent.AddListener(UnlockAndDeselect);
        }

        public void Setup(string lockID, ControlTypes controlTypes = ControlTypes.KEYBOARDINPUT)
        {
            _lockID = lockID ?? throw new ArgumentNullException(nameof(lockID));
            _controlTypes = controlTypes;
        }

        public void OnSelect(BaseEventData eventData)
            => Lock();
        public void Lock()
        {
            if (locked)
                return;
            if (LockID == null)
                throw new NullReferenceException("LockID is null");
            InputLockManager.SetControlLock(_controlTypes, LockID);
            locked = true;
        }

        public void OnDeselect(BaseEventData eventData)
            => Unlock();
        public void Unlock()
        {
            if (!locked)
                return;
            if (LockID == null)
                throw new NullReferenceException("lockID is null");
            InputLockManager.RemoveControlLock(LockID);
            locked = false;
        }
        public void UnlockAndDeselect(string _)
        {
            Unlock();
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnBeforeSerialize()
        {
            _controlTypesSerialized = (ulong)_controlTypes;
        }

        public void OnAfterDeserialize()
        {
            _controlTypes = (ControlTypes)_controlTypesSerialized;
        }

        private void OnDestroy()
        {
            if (locked)
                Unlock();
            endEvent?.RemoveListener(UnlockAndDeselect);
        }
    }
}
