using System.Linq;
using UnityEngine;
using TMPro;

namespace UI_Tools.Universal_Text
{
    public static class UT_Settings
    {
        public static UT_Mode preferredMode = UT_Mode.Unity;
        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.Load("Fonts/Calibri SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
                }
                return _defaultFont;
            }
            set
            {
                _defaultFont = value;
            }
        }
        private static TMP_FontAsset _defaultFont;

        //public static void SetMode(UT_Mode mode)
        //{
        //    foreach (UT_Base UT_obj in Object.FindObjectsOfType<UT_Base>().OrderBy(SortOrder))
        //        UT_obj.SetMode(mode);
        //}
        private static int SortOrder(UT_Base obj)
        {
            if (typeof(UT_Text).IsAssignableFrom(obj.GetType()))
                return 1;
            return 0;
        }
    }
    public enum UT_Mode
    {
        Invalid = -1,
        Unity = 0,
        TMPro = 1
    }
}