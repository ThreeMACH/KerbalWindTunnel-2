using System.Collections.Generic;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class WindTunnelSettings
    {
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
    }

    public partial class WindTunnelWindow
    {
        private PopupDialog settingsDialog;
        private PopupDialog SpawnDialog()
        {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>
            {
                new DialogGUIToggle(WindTunnelSettings.UseCoefficients, "Lift, Drag as coefficients",
                    b => {
                        WindTunnelSettings.UseCoefficients = b;
                        // TODO: Have this update the graphs.
                        //GraphGenerator.UpdateGraphs();
                    }),
                //new DialogGUIToggle(WindTunnelSettings.DefaultToMach, "Default to speed as Mach", (b) => WindTunnelSettings.DefaultToMach = b), // TODO: Implement this
                new DialogGUIToggle(WindTunnelSettings.UseCharacterized, "Use faster vessel characterization",  b=> WindTunnelSettings.UseCharacterized = b),
                new DialogGUIToggle(WindTunnelSettings.StartMinimized, "Start minimized", b => WindTunnelSettings.StartMinimized = b),
                new DialogGUIToggle(WindTunnelSettings.UseSingleColorHighlighting, "Use simple part highlighting", b => WindTunnelSettings.UseSingleColorHighlighting = b),
                new DialogGUIToggle(() => WindTunnelSettings.ShowEnvelopeMask, "Show flight envelope outline on graphs", b => WindTunnelSettings.ShowEnvelopeMask = b),
                new DialogGUIToggle(WindTunnelSettings.ShowEnvelopeMaskAlways && WindTunnelSettings.ShowEnvelopeMask, "Show flight envelope outline even on flight envelope", b => {WindTunnelSettings.ShowEnvelopeMaskAlways = b; WindTunnelSettings.ShowEnvelopeMask |= b; }),
            };


            dialog.Add(new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                new DialogGUILabel(() => string.Format("Propeller rotation evaluations: {0}", WindTunnelSettings.RotationCount), UISkinManager.defaultSkin.toggle, true),
                new DialogGUISlider(() => Mathf.Log(WindTunnelSettings.RotationCount, 2), 0, 4, true, 100, 20, value => WindTunnelSettings.RotationCount = (int)Mathf.Pow(2, value))
                ));

            if (ToolbarManager.ToolbarAvailable)
                dialog.Add(new DialogGUIToggle(WindTunnelSettings.UseBlizzy, "Use Blizzy's Toolbar", b => WindTunnelSettings.UseBlizzy = b));

            dialog.Add(new DialogGUIButton("Accept", () => settingsDialog.Dismiss()));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("KWTSettings", "", "Kerbal Wind Tunnel Settings", UISkinManager.defaultSkin, dialog.ToArray()),
                false, UISkinManager.defaultSkin);
        }
    }
}
