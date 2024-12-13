using System.Collections.Generic;
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

        private static ApplicationLauncherButton appButton = null;
        internal static IButton blizzyToolbarButton = null;
        private int guiId;
        private bool appLauncherEventSet = false;
        public const string texPath = "GameData/WindTunnel/Textures/";
        public const string graphPath = "GameData/WindTunnel/PluginData";
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

        Gradient dragMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.red, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1) } };
        Gradient liftMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.green, 0), new GradientColorKey(Color.green, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1) } };
        Gradient drag_liftMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(Color.green, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 0.5f), new GradientAlphaKey(1, 1) } };

        /*
        public void UpdateHighlighting(HighlightMode highlightMode, CelestialBody body, float altitude, float speed, float aoa)
        {
            this.highlightMode = highlightMode;
            this.body = body;
            this.altitude = altitude;
            this.speed = speed;
            this.aoa = aoa;

            ClearPartHighlighting();

            if (highlightMode == HighlightMode.Off)
                return;

            GenerateHighlightingData(EditorLogic.fetch.ship, body, altitude, speed, aoa);

            int count = highlightingData.Length;
            float min, max;
            Func<PartAeroData, float> highlightValueFunc;
            switch (highlightMode)
            {
                case HighlightMode.Lift:
                    highlightValueFunc = (p) => p.lift;
                    break;
                case HighlightMode.DragOverMass:
                    highlightValueFunc = (p) => p.drag / p.mass;
                    break;
                case HighlightMode.DragOverLift:
                    highlightValueFunc = (p) => p.lift / p.drag;
                    break;
                case HighlightMode.Drag:
                default:
                    highlightValueFunc = (p) => p.drag;
                    break;
            }
            float[] highlightingDataResolved = highlightingData.Select(highlightValueFunc).ToArray();
            min = highlightingDataResolved.Where(f => !float.IsNaN(f) && !float.IsInfinity(f)).Min();
            max = highlightingDataResolved.Where(f => !float.IsNaN(f) && !float.IsInfinity(f)).Max();
            for (int i = 0; i < count; i++)
            {
                float value = (highlightingDataResolved[i] - min) / (max - min);
                HighlightPart(EditorLogic.fetch.ship.parts[i], value);
            }
        }

        private void HighlightPart(Part part, float value)
        {
            highlightedParts.Add(part);

            part.SetHighlightType(Part.HighlightType.AlwaysOn);
            Gradient colorMap = Graphing.Extensions.GradientExtensions.Jet;
            if (WindTunnelSettings.UseSingleColorHighlighting)
                switch (highlightMode)
                {
                    case HighlightMode.Lift:
                        colorMap = liftMap;
                        break;
                    case HighlightMode.DragOverLift:
                        colorMap = drag_liftMap;
                        break;
                    case HighlightMode.Drag:
                    case HighlightMode.DragOverMass:
                    default:
                        colorMap = dragMap;
                        break;
                }
            part.SetHighlightColor(colorMap.Evaluate(value));
            part.SetHighlight(true, false);
        }

        private void ClearPartHighlighting()
        {
            for (int i = highlightedParts.Count - 1; i >= 0; i--)
            {
                highlightedParts[i].SetHighlightDefault();
            }
            highlightedParts.Clear();
        }

        private void GenerateHighlightingData(ShipConstruct ship, CelestialBody body, float altitude, float speed, float aoa)
        {
            float mach, atmDensity;
            lock (body)
            {
                atmDensity = (float)Extensions.KSPClassExtensions.GetDensity(body, altitude);
                mach = speed / (float)body.GetSpeedOfSound(body.GetPressure(altitude), atmDensity);
            }

            int count = ship.parts.Count;
            highlightingData = new PartAeroData[count];

            Vector3 inflow = AeroPredictor.InflowVect(aoa);

            float pseudoReDragMult;
            lock (PhysicsGlobals.DragCurvePseudoReynolds)
                pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(atmDensity * speed);

            for (int i = 0; i < count; i++)
            {
                if (WindTunnelSettings.HighlightIgnoresLiftingSurfaces && ship.parts[i].HasModuleImplementing<ModuleLiftingSurface>())
                {
                    highlightingData[i] = new PartAeroData(0, 0, ship.parts[i].mass);
                    continue;
                }

                VesselCache.SimulatedPart simPart = VesselCache.SimulatedPart.Borrow(ship.parts[i], null);
                Vector3 partForce = simPart.GetAero(inflow, mach, pseudoReDragMult);

                ModuleLiftingSurface liftingSurface = ship.parts[i].FindModuleImplementing<ModuleLiftingSurface>();
                if (liftingSurface != null)
                {
                    VesselCache.SimulatedLiftingSurface simLiftSurf = VesselCache.SimulatedLiftingSurface.Borrow(liftingSurface, simPart);
                    partForce += simLiftSurf.GetForce(inflow, mach);
                    simLiftSurf.Release();
                }
                simPart.Release();
                //Vector3 partForce = highlightingVessel.parts[i].GetAero(inflow, mach, pseudoReDragMult);
                //Vector3 partForce = StockAeroUtil.SimAeroForce(body, new ShipConstruct("test", "", new List<Part>() { EditorLogic.fetch.ship.parts[i] }), inflow * speed, altitude);
                partForce = AeroPredictor.ToFlightFrame(partForce, aoa);  // (Quaternion.AngleAxis((aoa * 180 / Mathf.PI), Vector3.left) * partForce);

                highlightingData[i] = new PartAeroData(Math.Abs(partForce.z), Math.Abs(partForce.y), ship.parts[i].mass);
            }
        }

        private void OnEditorShipModified(ShipConstruct ship)
        {
            UpdateHighlighting(this.highlightMode, this.body, this.altitude, this.speed, this.aoa);
        }*/

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

            //// GameEvents.onEditorShipModified.Add(OnEditorShipModified);
            if (!ActivateBlizzyToolBar())
            {
                //log.debug("Registering GameEvents.");
                appLauncherEventSet = true;
                GameEvents.onGUIApplicationLauncherReady.Add(OnGuiApplicationLauncherReady);
            }
            guiId = GUIUtility.GetControlID(FocusType.Passive);
            
            //window.UseMach = WindTunnelSettings.DefaultToMach;
            //window.Minimized = WindTunnelSettings.StartMinimized;
        }

        internal bool ActivateBlizzyToolBar()
        {
            try
            {
                if (!WindTunnelSettings.UseBlizzy) return false;
                if (!ToolbarManager.ToolbarAvailable) return false;
                if (HighLogic.LoadedScene != GameScenes.EDITOR && HighLogic.LoadedScene != GameScenes.FLIGHT) return true;
                blizzyToolbarButton = ToolbarManager.Instance.add("ReCoupler", "ReCoupler");
                blizzyToolbarButton.TexturePath = iconPath_blizzy_off;
                blizzyToolbarButton.ToolTip = "ReCoupler";
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
            if (window == null)
                OnButtonTrue();
            else
                OnButtonFalse();
        }

        public void OnButtonTrue()
        {
            appButton?.SetTexture(icon_on);
            if (blizzyToolbarButton != null)
                blizzyToolbarButton.TexturePath = iconPath_blizzy;

            if (window != null)
                window.gameObject.SetActive(true);
            else
                window = Instantiate(AssetLoader.WindTunnelAssetLoader.WindowPrefab, MainCanvasUtil.MainCanvas.transform).GetComponent<WindTunnelWindow>();
        }
        public void OnButtonFalse()
        {
            appButton?.SetTexture(icon);
            if (blizzyToolbarButton != null)
                blizzyToolbarButton.TexturePath = iconPath_blizzy_off;

            if (window == null)
                return;
            window.gameObject.SetActive(false);
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

            //// ClearPartHighlighting();

            WindTunnelSettings.SaveSettings();

            //log.debug("Unregistering GameEvents.");
            //// GameEvents.onEditorShipModified?.Remove(OnEditorShipModified);
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