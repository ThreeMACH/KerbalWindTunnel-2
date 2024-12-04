using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UI_Tools.Universal_Text;
using Graphing;
using KerbalWindTunnel.Extensions;
using KerbalWindTunnel.VesselCache;

namespace KerbalWindTunnel
{
    public partial class WindTunnelWindow : MonoBehaviour
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [SerializeField]
        private UT_Dropdown planetDropdown;
        [SerializeField]
        private UT_Dropdown highlightDropdown;
        [SerializeField]
        private UT_InputField highlightSpeedInput;
        [SerializeField]
        private UT_InputField highlightAltitudeInput;
        [SerializeField]
        private UT_InputField highlightAoAInput;
        [SerializeField]
        private Toggle envelopeToggle;
        [SerializeField]
        private Toggle aoaCurveToggle;
        [SerializeField]
        private Toggle velCurveToggle;
        [SerializeField]
        private UT_Dropdown envelopeDropdown;
        [SerializeField]
        private UT_InputField ascentSpeedInput;
        [SerializeField]
        private UT_InputField ascentAltitudeInput;
        [SerializeField]
        private Toggle ascentFuelToggle;
        [SerializeField]
        private Toggle ascentTimeToggle;
        [SerializeField]
        private UI_Tools.ToggleArray aoaToggleArray;
        [SerializeField]
        private UI_Tools.ToggleArray velToggleArray;
        [SerializeField]
        private Toggle aoaWetToggle;
        [SerializeField]
        private Toggle aoaDryToggle;
        [SerializeField]
        private UnityEngine.UI.Button updateVesselButton;
        [SerializeField]
        private UT_Text envelopeInfo;
        [SerializeField]
        private UT_Text aoaCurveInfo;
        [SerializeField]
        private UT_Text velCurveInfo;
        [SerializeField]
        private Graphing.Grapher envelopeGrapher;
        [SerializeField]
        private Graphing.Grapher aoaCurveGrapher;
        [SerializeField]
        private Graphing.Grapher velCurveGrapher;
        [SerializeField]
        private Button settingsButton;
        [SerializeField]
        private Toggle selectOnGraphToggle;
        [SerializeField]
        private Toggle rollUpToggle;
#pragma warning restore IDE0044 // Add readonly modifier

        private Graphing.GraphableCollection envelopeCollection;
        private Graphing.GraphableCollection aoaCollection;
        private Graphing.GraphableCollection velocityCollection;

        public static WindTunnelWindow Instance { get; private set; }

        public bool Minimized
        {
            get => rollUpToggle != null && rollUpToggle.isOn;
            set { if (rollUpToggle != null) rollUpToggle.isOn = value; }
        }

        public bool UseMach
        {
            get;
            set;
        }
        
        public int EnvelopeGraphShown
        {
            get => envelopeDropdown.Value;
            set
            {
                if (envelopeCollection != null)
                {
                    (bool?, bool?, bool?) persistentGraphs = (
                        envelopeCollection["Fuel-Optimal Path"]?.Visible,
                        envelopeCollection["Time-Optimal Path"]?.Visible,
                        envelopeCollection["Envelope Mask"]?.Visible);
                    envelopeCollection.SetVisibility(false);

                    envelopeCollection[value].Visible = true;
                    if (persistentGraphs.Item1 != null)
                        envelopeCollection["Fuel-Optimal Path"].Visible = (bool)persistentGraphs.Item1;
                    if (persistentGraphs.Item2 != null)
                        envelopeCollection["Time-Optimal Path"].Visible = (bool)persistentGraphs.Item2;
                    if (persistentGraphs.Item3 != null)
                        envelopeCollection["Envelope Mask"].Visible = (bool)persistentGraphs.Item3;
                }
                envelopeDropdown.Value = value;
            }
        }

        public bool ShowAoAGraphsWet
        {
            get => _showAoAGraphsWet;
            set
            {
                if (_showAoAGraphsWet == value)
                    return;
                _showAoAGraphsWet = value;
                aoaWetToggle.isOn = value;
                UpdateAoAGraphs();
            }
        }
        private bool _showAoAGraphsWet = true;
        public bool ShowAoAGraphsDry
        {
            get => _showAoAGraphsDry;
            set
            {
                if (_showAoAGraphsDry == value)
                    return;
                _showAoAGraphsDry = value;
                aoaDryToggle.isOn = value;
                UpdateAoAGraphs();
            }
        }
        private bool _showAoAGraphsDry = true;

        public bool ShowFuelOptimalPath
        {
            get => _showFuelOptimalPath;
            set
            {
                if (_showFuelOptimalPath == value)
                    return;
                _showFuelOptimalPath = value;
                if (envelopeCollection?["Fuel-Optimal Path"] != null)
                    envelopeCollection["Fuel-Optimal Path"].Visible = value;
                ascentFuelToggle.isOn = value;
            }
        }
        private bool _showFuelOptimalPath = true;
        public bool ShowTimeOptimalPath
        {
            get => _showTimeOptimalPath;
            set
            {
                if (_showTimeOptimalPath == value)
                    return;
                _showTimeOptimalPath = value;
                if (envelopeCollection?["Time-Optimal Path"] != null)
                    envelopeCollection["Time-Optimal Path"].Visible = value;
                ascentTimeToggle.isOn = value;
            }
        }
        private bool _showTimeOptimalPath = true;

        public float AscentTargetAltitude
        {
            get => _ascentTargetAlt;
            set
            {
                if (_ascentTargetAlt == value)
                    return;
                _ascentTargetAlt = value;
                ascentAltitudeInput.Text = value.ToString();
                UpdateAscentTarget();
            }
        }
        private float _ascentTargetAlt;
        public float AscentTargetSpeed
        {
            get => _ascentTargetSpeed;
            set
            {
                if (_ascentTargetSpeed == value)
                    return;
                _ascentTargetSpeed = value;
                ascentSpeedInput.Text = value.ToString();
                UpdateAscentTarget();
            }
        }
        private float _ascentTargetSpeed;

        public int GraphMode
        {
            get => _graphMode;
            set
            {
                if (_graphMode == value)
                    return;
                if (_graphMode < 0 || _graphMode > 2)
                    throw new ArgumentException("Invalid graph mode.");
                _graphMode = value;
                switch (_graphMode)
                {
                    default:
                    case 0:
                        envelopeToggle.isOn = true;
                        break;
                    case 1:
                        aoaCurveToggle.isOn = true;
                        break;
                    case 2:
                        velCurveToggle.isOn = true;
                        break;
                }
                GraphModeChanged();
            }
        }
        private int _graphMode;
        public int HighlightMode
        {
            get => _highlightMode;
            set
            {
                if (_highlightMode == value)
                    return;
                _highlightMode = value;
                highlightDropdown.Value = value;
                UpdateHighlightingMethod();
            }
        }
        private int _highlightMode;
        public float HighlightSpeed
        {
            get => _highlightSpeed;
            set
            {
                if (_highlightSpeed == value)
                    return;
                _highlightSpeed = value;
                highlightSpeedInput.Text = value.ToString();
                UpdateHighlightingMethod();
            }
        }
        private float _highlightSpeed;
        public float HighlightAltitude
        {
            get => _highlightAltitude;
            set
            {
                if (_highlightAltitude == value)
                    return;
                _highlightAltitude = value;
                highlightAltitudeInput.Text = value.ToString();
                UpdateHighlightingMethod();
            }
        }
        private float _highlightAltitude;
        public float HighlightAoA
        {
            get => _highlightAoA;
            set
            {
                if (_highlightAoA == value)
                    return;
                _highlightAoA = value;
                highlightAoAInput.Text = value.ToString();
                UpdateHighlightingMethod();
            }
        }
        private float _highlightAoA;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Start()
        {
            Debug.Log("Window:Start");
            ascentFuelToggle.SetIsOnWithoutNotify(ShowFuelOptimalPath);
            ascentTimeToggle.SetIsOnWithoutNotify(ShowFuelOptimalPath);
            Debug.Log("Drawing placeholder");
            GraphDrawer gd1 = envelopeGrapher.AddGraphToDefaultAxes(new OutlineMask(PlaceholderData.surfPlaceholder, 0, 1, 0, 1, v => v.z - 0.5f));
            GraphDrawer gd2 = envelopeGrapher.AddGraphToDefaultAxes(new SurfGraph(PlaceholderData.surfPlaceholder, 0, 1, 0, 1));
            GraphDrawer gd3 = envelopeGrapher.AddGraphToDefaultAxes(new LineGraph(new float[] { 0, 0.4f, 0.3f, 0.6f, 0.8f, 0.3f }, 0, 1) { color = Color.black });
            Debug.Log("Drew placeholder.");
            //envelopeGrapher.AddGraphToDefaultAxes(new SurfGraph(new float[,] { { 3, 2, 1 }, { 2, 1, 0 }, { 1, 2, 1 }, { 0, 2, 4 } }, 0, 1, 0, 1));

            // Things to actually keep.
            if (Planetarium.fetch != null)
            {
                Instance = this;
                gAccel = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
                body = Planetarium.fetch.CurrentMainBody;
            }
            foreach (var resizer in GetComponentsInChildren<UI_Tools.RenderTextureResizer>())
                resizer.ForceResize();
        }

        public void ExportGraphData()
        {
            switch (GraphMode)
            {
                default:
                case 0:
                    //envelopeCollection.WriteToFile();
                    break;
                case 1:
                    //aoaCollection.WriteToFile();
                    break;
                case 2:
                    //velocityCollection.WriteToFile();
                    break;
            }
        }

        public void SetHighlightSpeed(string speed)
        {
            if (float.TryParse(speed, out float result) && result != _highlightSpeed)
            {
                _highlightSpeed = result;
                UpdateHighlightingMethod();
            }
        }

        public void SetHighlightAltitude(string altitude)
        {
            if (float.TryParse(altitude, out float result) && result != _highlightAltitude)
            {
                _highlightAltitude = result;
                UpdateHighlightingMethod();
            }
        }

        public void SetHighlightAoA(string AoA)
        {
            if (float.TryParse(AoA, out float result) && result != _highlightAoA)
            {
                _highlightAoA = result;
                UpdateHighlightingMethod();
            }
        }

        public void SetPlanetOptions(IEnumerable<(string name, float upperAlt, float upperSpeed)> options)
        {
            planetDropdown.ClearOptions();
            planetDropdown.AddOptions(options.Select(o => o.name).ToList());
        }

        public void SetEnvelopeOptions(IEnumerable<string> optionNames)
        {
            envelopeDropdown.ClearOptions();
            envelopeDropdown.AddOptions(optionNames.ToList());
        }

        public void SetAscentTargetAltitude(string altitude)
        {
            if (float.TryParse(altitude, out float result) && result != _ascentTargetAlt)
            {
                _ascentTargetAlt = result;
                UpdateAscentTarget();
            }
        }

        public void SetAscentTargetSpeed(string speed)
        {
            if (float.TryParse(speed, out float result) && result != _ascentTargetSpeed)
            {
                _ascentTargetSpeed = result;
                UpdateAscentTarget();
            }
        }

        private void EnvelopeGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            envelopeInfo.Text = envelopeGrapher.GetDisplayValue(clickedPosition);
        }

        private void AoaCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            aoaCurveInfo.Text = aoaCurveGrapher.GetDisplayValue(clickedPosition);
        }

        private void VelCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            velCurveInfo.Text = velCurveGrapher.GetDisplayValue(clickedPosition);
        }

        private Vector2 oldPosition;
        private bool selectingAscentTarget = false;
        private void EnvelopeGrapher_AscentClicked(object _, Vector2 clickedPosition)
        {
            AxisUI axis;
            axis = envelopeGrapher.PrimaryHorizontalAxis;
            if (axis != null)
                _ascentTargetSpeed = axis.Min + (axis.Max - axis.Min) * clickedPosition.x;
            axis = envelopeGrapher.PrimaryVerticalAxis;
            if (axis != null)
                _ascentTargetAlt = axis.Min + (axis.Max - axis.Min) * clickedPosition.y;
            envelopeGrapher.GetComponentInChildren<Graphing.UI.CrosshairController>().SetCrosshairPosition(oldPosition);
            EnvelopeGrapher_GraphClicked(this, oldPosition);
            selectOnGraphToggle.isOn = false;
            UpdateAscentTarget();
        }

        public void SelectAscentOnGraph(bool value)
        {
            if (value && selectingAscentTarget)
                return;
            if (!value)
            {
                if (selectingAscentTarget)
                    envelopeGrapher.GraphClicked -= EnvelopeGrapher_AscentClicked;
                selectingAscentTarget = false;
                return;
            }
            oldPosition = envelopeGrapher.GetComponentInChildren<Graphing.UI.CrosshairController>().NormalizedPosition;
            envelopeGrapher.GraphClicked += EnvelopeGrapher_AscentClicked;
            selectingAscentTarget = true;
        }

        #region Reliant on KSP API
        public static float gAccel;// = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        public const float AoAdelta = 0.1f * Mathf.Deg2Rad;
        private AeroPredictor vessel = null;
        private CelestialBody body;// = Planetarium.fetch.CurrentMainBody;
        private float _targetAltitude = 17700;
        private string targetAltitudeStr = "17700";
        public float TargetAltitude
        {
            get { return _targetAltitude; }
            set
            {
                _targetAltitude = value;
                targetAltitudeStr = value.ToString("F0");
            }
        }
        private float _targetSpeed = 1410;
        private string targetSpeedStr = "1410";
        public float TargetSpeed
        {
            get { return _targetSpeed; }
            set
            {
                _targetSpeed = value;
                targetSpeedStr = value.ToString("F1");
            }
        }

        public AeroPredictor CommonPredictor { get => this.vessel; }
        public AeroPredictor GetAeroPredictor()
        {
            AeroPredictor vesselCache = VesselCache.SimulatedVessel.Borrow(EditorLogic.fetch.ship);
            if (WindTunnelSettings.Instance.useCharacterized)
                vesselCache = new VesselCache.CharacterizedVessel((VesselCache.SimulatedVessel)vesselCache);
            AeroPredictor.Conditions testConditions = new AeroPredictor.Conditions(this.body, 100, 0);
            Debug.Log("Normal:");
            Debug.Log("Lift: " + vesselCache.GetLiftForceMagnitude(testConditions, 1) + "    Drag: " + vesselCache.GetDragForceMagnitude(testConditions, 1));
            Debug.Log("Characterized");
            Debug.Log("Lift: " + vesselCache.GetLiftForceMagnitude(testConditions, 1) + "    Drag: " + vesselCache.GetDragForceMagnitude(testConditions, 1));
            return vesselCache;
        }

        public void PlanetSelected(int item)
        {
        }

        private void UpdateHighlightingMethod()
        {
        }

        private void GraphModeChanged()
        {
        }

        private void UpdateAscentTarget()
        {
        }

        public void AoAGraphToggleEvent(int index)
        {
        }

        public void VelGraphToggleEvent(int index)
        {
        }

        public void UpdateVessel()
        {


            updateVesselButton.interactable = false;
        }
        public void UpdateAoAGraphs()
        {/*
            UISkinDef skin = UISkinManager.defaultSkin;
            void SaveSprite(Sprite sprite, string name)
            {
                if (sprite == null)
                    return;
                Texture2D tex = sprite.texture;
                byte[] texBytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes("GameData/WindTunnel/PluginData/" + name, texBytes);
            }
            void SaveStyle(UIStyle style, string name)
            {
                if (style.active != null)
                {
                    SaveSprite(style.active.background, name + "_active");
                    Debug.Log(name + "_active " + style.active.textColor);
                }
                if (style.disabled != null)
                {
                    SaveSprite(style.disabled.background, name + "_disabled");
                    Debug.Log(name + "_disabled " + style.active.textColor);
                }
                if (style.highlight != null)
                {
                    SaveSprite(style.highlight.background, name + "_highlight");
                    Debug.Log(name + "_highlight " + style.active.textColor);
                }
                if (style.normal != null)
                {
                    SaveSprite(style.normal.background, name + "_normal");
                    Debug.Log(name + "_normal " + style.active.textColor);
                }
            }
            SaveStyle(skin.box, "box");
            SaveStyle(skin.button, "button");
            SaveStyle(skin.horizontalScrollbar, "horizontalScrollbar");
            SaveStyle(skin.horizontalScrollbarLeftButton, "horizontalScrollbarLeftButton");
            SaveStyle(skin.horizontalScrollbarRightButton, "horizontalScrollbarRightButton");
            SaveStyle(skin.horizontalScrollbarThumb, "horizontalScrollbarThumb");
            SaveStyle(skin.horizontalSlider, "horizontalSlider");
            SaveStyle(skin.horizontalSliderThumb, "horizontalSliderThumb");
            SaveStyle(skin.label, "label");
            SaveStyle(skin.scrollView, "scrollView");
            SaveStyle(skin.textArea, "textArea");
            SaveStyle(skin.textField, "textField");
            SaveStyle(skin.toggle, "toggle");
            SaveStyle(skin.verticalScrollbar, "verticalScrollbar");
            SaveStyle(skin.verticalScrollbarUpButton, "verticalScrollbarUpButton");
            SaveStyle(skin.verticalScrollbarDownButton, "verticalScrollbarDownButton");
            SaveStyle(skin.verticalScrollbarThumb, "verticalScrollbarThumb");
            SaveStyle(skin.verticalSlider, "verticalSlider");
            SaveStyle(skin.verticalSliderThumb, "verticalSliderThumb");
            SaveStyle(skin.window, "window");*/
        }
        public void ToggleSettingsWindow()
        {
        }

        private void OnEditorShipModified(ShipConstruct _) => updateVesselButton.interactable = true;
        private void OnPartActionUIDismiss(Part _) => updateVesselButton.interactable = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void Awake()
        {
            envelopeGrapher.GraphClicked += EnvelopeGrapher_GraphClicked;
            aoaCurveGrapher.GraphClicked += AoaCurveGrapher_GraphClicked;
            velCurveGrapher.GraphClicked += VelCurveGrapher_GraphClicked;
            GameEvents.onEditorShipModified.Add(OnEditorShipModified);
            GameEvents.onPartActionUIDismiss.Add(OnPartActionUIDismiss);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
        {
            envelopeGrapher.GraphClicked -= EnvelopeGrapher_GraphClicked;
            aoaCurveGrapher.GraphClicked -= AoaCurveGrapher_GraphClicked;
            velCurveGrapher.GraphClicked -= VelCurveGrapher_GraphClicked;
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            GameEvents.onPartActionUIDismiss.Remove(OnPartActionUIDismiss);
            if (selectingAscentTarget)
                envelopeGrapher.GraphClicked -= EnvelopeGrapher_AscentClicked;
        }
        #endregion
    }
}