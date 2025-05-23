using System.Collections.Generic;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class WindTunnelSettings
    {
        const string popupWindowName = "KWTSettings";

        public static bool UseCharacterized
        {
            get { return Instance.useCharacterized; }
            set
            {
                Instance.useCharacterized = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool useCharacterized = true;

        public static bool UseCoefficients
        {
            get { return Instance.useCoefficients; }
            set
            {
                Instance.useCoefficients = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool useCoefficients = true;

        public static bool DefaultToMach
        {
            get { return Instance.defaultToMach; }
            set
            {
                Instance.defaultToMach = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool defaultToMach;

        public static bool StartMinimized
        {
            get { return Instance.startMinimized; }
            set
            {
                Instance.startMinimized = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool startMinimized;

        public static bool UseSingleColorHighlighting
        {
            get { return Instance.useSingleColorHighlighting; }
            set
            {
                Instance.useSingleColorHighlighting = value;
                settingsChanged = true;
            }
        }

        [Persistent]
        private bool useSingleColorHighlighting = true;

        public static bool HighlightIgnoresLiftingSurfaces
        {
            get { return Instance.highlightIgnoresLiftingSurfaces; }
            set
            {
                Instance.highlightIgnoresLiftingSurfaces = value;
                settingsChanged = true;
            }
        }

        [Persistent]
        private bool highlightIgnoresLiftingSurfaces = false;

        public static bool ShowEnvelopeMask
        {
            get { return Instance.showEnvelopeMask; }
            set
            {
                Instance.showEnvelopeMask = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool showEnvelopeMask = true;

        public static bool ShowEnvelopeMaskAlways
        {
            get { return Instance.showEnvelopeMaskAlways; }
            set
            {
                Instance.showEnvelopeMaskAlways = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool showEnvelopeMaskAlways = false;

        public static bool UseBlizzy
        {
            get { return Instance.useBlizzy; }
            set
            {
                Instance.useBlizzy = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool useBlizzy = false;

        public static bool AutoFitAxes
        {
            get { return Instance.autoFitAxes; }
            set
            {
                Instance.autoFitAxes = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private bool autoFitAxes = true;

        public static int RotationCount
        {
            get => Instance.rotationCount;
            set
            {
                Instance.rotationCount = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        private int rotationCount = 1;

        private static bool settingsChanged = false;
        private static bool settingsLoaded = false;

        public static readonly WindTunnelSettings Instance = new WindTunnelSettings();

        public static void InitializeSettings()
        {
            if (settingsLoaded)
                return;

            Instance.LoadSettingsFromFile();

            settingsLoaded = true;
        }
        private void LoadSettingsFromFile()
        {
            ConfigNode[] settingsNode = GameDatabase.Instance.GetConfigNodes("KerbalWindTunnelSettings");
            if (settingsNode.Length < 1)
            {
                Debug.Log("Kerbal Wind Tunnel Settings file note found.");
                // To trigger creating a settings file.
                settingsChanged = true;
                return;
            }
            ConfigNode.LoadObjectFromConfig(this, settingsNode[0]);
        }

        public static void SaveSettings()
        {
            Instance.SaveSettingsToFile();
        }
        private void SaveSettingsToFile()
        {
            if (!settingsChanged)
                return;

            ConfigNode data = ConfigNode.CreateConfigFromObject(this, 0, new ConfigNode("KerbalWindTunnelSettings"));

            ConfigNode save = new ConfigNode();
            save.AddNode(data);
            save.Save("GameData/WindTunnel/KerbalWindTunnelSettings.cfg");
        }

        public static PopupDialog SpawnDialog(System.Action acceptAction = null, bool invokeOnlyOnChange = false)
        {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>
            {
                new DialogGUIToggle(UseCoefficients, "Lift, Drag as coefficients", b => UseCoefficients = b ),
                //new DialogGUIToggle(DefaultToMach, "Default to speed as Mach", (b) => WindTunnelSettings.DefaultToMach = b), // TODO: Implement this
                new DialogGUIToggle(UseCharacterized, "Use faster vessel characterization",  b => UseCharacterized = b),
                new DialogGUIToggle(StartMinimized, "Start minimized", b => StartMinimized = b),
                new DialogGUIToggle(UseSingleColorHighlighting, "Use simple part highlighting", b => UseSingleColorHighlighting = b),
                new DialogGUIToggle(ShowEnvelopeMask, "Show flight envelope outline on graphs", b => ShowEnvelopeMask = b),
                new DialogGUIToggle(ShowEnvelopeMaskAlways && ShowEnvelopeMask, "Show flight envelope outline even on flight envelope", b => { ShowEnvelopeMaskAlways = b; ShowEnvelopeMask |= b; }),
            };

            dialog.Add(new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                new DialogGUILabel(string.Format("Propeller rotation evaluations: {0}", RotationCount), UISkinManager.defaultSkin.toggle, true),
                new DialogGUISlider(() => Mathf.Log(RotationCount, 2), 0, 4, true, 100, 20, value => RotationCount = (int)Mathf.Pow(2, value))
                ));

            if (ToolbarManager.ToolbarAvailable)
                dialog.Add(new DialogGUIToggle(UseBlizzy, "Use Blizzy's Toolbar", b => UseBlizzy = b));

            dialog.Add(new DialogGUIButton("Accept", () =>
            {
                if (!invokeOnlyOnChange || settingsChanged)
                    acceptAction?.Invoke();
            }, true));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(popupWindowName, "", "Kerbal Wind Tunnel Settings", UISkinManager.defaultSkin, dialog.ToArray()),
                false, UISkinManager.defaultSkin, true);
        }
    }
}
