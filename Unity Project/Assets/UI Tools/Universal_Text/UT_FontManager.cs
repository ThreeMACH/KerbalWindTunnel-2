using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UI_Tools.Universal_Text
{
	public static class UT_FontManager
	{
        private static Dictionary<Font, TMP_FontAsset> tmpFonts;
        private static Dictionary<TMP_FontAsset, Font> unityFonts;
        private static HashSet<Font> failedCreates = new HashSet<Font>();

        public static void ReattemptFails() => failedCreates.Clear();

        public static TMP_FontAsset GetTMPFont(Font font)
        {
            if (font == null)
                return null;
            if (tmpFonts == null)
                Initialize();
            if (tmpFonts.TryGetValue(font, out TMP_FontAsset TMPFont))
                return TMPFont;
            if (failedCreates.Contains(font))
                return TMP_Settings.defaultFontAsset;
            TMPFont = null;// TMP_FontAsset.CreateFontAsset(font);
            if (TMPFont == null)
            {
                failedCreates.Add(font);
                return TMP_Settings.defaultFontAsset;
            }
            tmpFonts.Add(font, TMPFont);
            unityFonts.Add(TMPFont, font);
            return TMPFont;
        }
        public static TMP_FontAsset GetTMPFont(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (tmpFonts == null)
                Initialize();
            return tmpFonts.Values.FirstOrDefault(font => string.Equals(font.name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Font GetUnityFont(TMP_FontAsset font)
        {
            if (font == null)
                return null;
            if (unityFonts == null)
                Initialize();
            if (unityFonts.TryGetValue(font, out Font unityFont))
                return unityFont;
            // It would be nice to generate a font at runtime, but that's beyond
            // scope for now...
            return null;
        }
        public static Font GetUnityFont(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (unityFonts == null)
                Initialize();
            return unityFonts.Values.FirstOrDefault(font => string.Equals(font.name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        private static void Initialize()
        {
            tmpFonts = new Dictionary<Font, TMP_FontAsset>();
            unityFonts = new Dictionary<TMP_FontAsset, Font>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
                return;
            Font defaultUnityFont = Resources.FindObjectsOfTypeAll<Font>()?.FirstOrDefault(f => string.Equals(f.name, defaultFont.name, StringComparison.InvariantCultureIgnoreCase));
            if (defaultUnityFont == null)
                return;
            tmpFonts.Add(defaultUnityFont, defaultFont);
            unityFonts.Add(defaultFont, defaultUnityFont);
        }
    }
}

