using UnityEngine;
using KSP.UI.Screens;

namespace KerbalWindTunnel
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class WindTunnel : MonoBehaviour
    {
        public static WindTunnel Instance;

        public enum HighlightMode
        {
            Off = 0,
            Drag = 1,
            Lift = 2,
            DragOverMass = 3,
            DragOverLift = 4
        }

        WindTunnelWindow window;

        internal static ApplicationLauncherButton appButton = null;
        internal static IButton blizzyToolbarButton = null;
        private int guiId;
        private bool appLauncherEventSet = false;
        public const string texPath = "GameData/WindTunnel/Textures/";
#if OUTSIDE_UNITY
        public const string graphPath = "GameData/WindTunnel/Output";
#else
        public const string graphPath = "TestOutput";
#endif
        private const string iconPath = "KWT_Icon_on.png";
        private const string iconPath_off = "KWT_Icon.png";
        private const string iconPath_blizzy = "KWT_Icon_blizzy_on.png";
        private const string iconPath_blizzy_off = "KWT_Icon_blizzy.png";
        internal const string iconPath_settings = "KWT_settings.png";
        internal const string iconPath_save = "KWT_saveIcon.png";

        private Texture2D icon_on; // = new Texture2D(38, 38, TextureFormat.ARGB32, false);
        private Texture2D icon; // = new Texture2D(38, 38, TextureFormat.ARGB32, false);
        private Texture2D icon_blizzy_on; // = new Texture2D(24, 24, TextureFormat.ARGB32, false);
        private Texture2D icon_blizzy; // = new Texture2D(24, 24, TextureFormat.ARGB32, false);

        private static MiniExcelWrapper miniExcelWrapper;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            WindTunnelSettings.InitializeSettings();

            if (Instance)
                Destroy(Instance);
            Instance = this;

            icon = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            icon.LoadImage(System.IO.File.ReadAllBytes(texPath + iconPath_off));
            icon_on = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            icon_on.LoadImage(System.IO.File.ReadAllBytes(texPath + iconPath));
            icon_blizzy = new Texture2D(24, 24, TextureFormat.ARGB32, false);
            icon_blizzy.LoadImage(System.IO.File.ReadAllBytes(texPath + iconPath_blizzy_off));
            icon_blizzy_on = new Texture2D(24, 24, TextureFormat.ARGB32, false);
            icon_blizzy_on.LoadImage(System.IO.File.ReadAllBytes(texPath + iconPath_blizzy));

            if (!ActivateBlizzyToolBar())
            {
                appLauncherEventSet = true;
                GameEvents.onGUIApplicationLauncherReady.Add(OnGuiApplicationLauncherReady);
            }
            guiId = GUIUtility.GetControlID(FocusType.Passive);

            if (miniExcelWrapper == null)
                miniExcelWrapper = new MiniExcelWrapper();
            Graphing.IO.GraphIO.SpreadsheetWriter = miniExcelWrapper;
        }

        internal bool ActivateBlizzyToolBar()
        {
            try
            {
                if (!WindTunnelSettings.UseBlizzy) return false;
                if (!ToolbarManager.ToolbarAvailable) return false;
                if (HighLogic.LoadedScene != GameScenes.EDITOR && HighLogic.LoadedScene != GameScenes.FLIGHT) return true;
                blizzyToolbarButton = ToolbarManager.Instance.add("KerbalWindTunnel", "KerbalWindTunnel");
                blizzyToolbarButton.TexturePath = iconPath_blizzy_off;
                blizzyToolbarButton.ToolTip = KSP.Localization.Localizer.Format("#autoLOC_KWT116");
                blizzyToolbarButton.Visible = true;
                blizzyToolbarButton.OnClick += (e) =>
                {
                    ButtonToggle();
                };
                return true;
            }
            catch
            {
                // Blizzy Toolbar instantiation error.  ignore.
                return false;
            }
        }

        private void OnGuiApplicationLauncherReady()
        {
            appButton = ApplicationLauncher.Instance.AddModApplication(
                OnButtonTrue,
                OnButtonFalse,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
                icon);
        }

        public void CloseWindow()
        {
            if (appButton != null)
                appButton.SetFalse();
            else
                OnButtonFalse();
        }

        public void ButtonToggle()
        {
            if (!(window?.isActiveAndEnabled) ?? false)
                OnButtonTrue();
            else
                OnButtonFalse();
        }

        public void OnButtonTrue()
        {
            if (window != null)
                window.gameObject.SetActive(true);
            else
            {
                window = Instantiate(AssetLoader.WindTunnelAssetLoader.WindowPrefab, MainCanvasUtil.MainCanvas.transform).GetComponent<WindTunnelWindow>();
                window.Minimized = WindTunnelSettings.StartMinimized;
            }
        }
        internal void SetButtonOn()
        {
            appButton?.SetTexture(icon_on);
            if (blizzyToolbarButton != null)
                blizzyToolbarButton.TexturePath = iconPath_blizzy;
        }
        public void OnButtonFalse()
        {
            if (window == null)
                return;
            window.gameObject.SetActive(false);
        }
        internal void SetButtonOff()
        {
            appButton?.SetTexture(icon);
            if (blizzyToolbarButton != null)
                blizzyToolbarButton.TexturePath = iconPath_blizzy_off;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
        {
            if (window != null)
                Destroy(window);
            Destroy(icon);
            Destroy(icon_on);
            Destroy(icon_blizzy);
            Destroy(icon_blizzy_on);

            WindTunnelSettings.SaveSettings();

            if (appLauncherEventSet)
                GameEvents.onGUIApplicationLauncherReady?.Remove(OnGuiApplicationLauncherReady);
            if (appButton != null)
                ApplicationLauncher.Instance?.RemoveModApplication(appButton);
            blizzyToolbarButton?.Destroy();

            if (window != null)
                Destroy(window.gameObject);
        }
    }
}