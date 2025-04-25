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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start()
        {
            GetComponent<UI_Tools.Universal_Text.UT_InputField>().OnEndEdit.AddListener(Unlock);
        }

        public void Setup(string lockID, ControlTypes controlTypes = ControlTypes.KEYBOARDINPUT)
        {
            _lockID = lockID ?? throw new ArgumentNullException("lockID");
            _controlTypes = controlTypes;
        }

        public void OnSelect(BaseEventData eventData)
            => Lock();
        public void Lock()
        {
            if (LockID == null)
                throw new NullReferenceException("lockID is null");
            InputLockManager.SetControlLock(_controlTypes, "KerbalWindTunnel");
        }

        public void OnDeselect(BaseEventData eventData)
            => Unlock();
        public void Unlock()
        {
            if (LockID == null)
                throw new NullReferenceException("lockID is null");
            InputLockManager.RemoveControlLock("KerbalWindTunnel");
        }
        public void Unlock(string _) => Unlock();

        public void OnBeforeSerialize()
        {
            _controlTypesSerialized = (ulong)_controlTypes;
        }

        public void OnAfterDeserialize()
        {
            _controlTypes = (ControlTypes)_controlTypesSerialized;
        }
    }
}
