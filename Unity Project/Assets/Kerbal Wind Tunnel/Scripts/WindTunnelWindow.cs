using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UI_Tools.Universal_Text;
using Graphing;
using KerbalWindTunnel.DataGenerators;

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
        private GameObject highlightSpeedGroup;
        [SerializeField]
        private UT_InputField highlightAltitudeInput;
        [SerializeField]
        private GameObject highlightAltitudeGroup;
        [SerializeField]
        private UT_InputField highlightAoAInput;
        [SerializeField]
        private GameObject highlightAoAGroup;
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

        private GraphableCollection envelopeCollection;
        private GraphableCollection aoaCollection;
        private GraphableCollection velocityCollection;

        private readonly VelCurve velData = new VelCurve();
        private readonly AoACurve aoaData = new AoACurve();
        private readonly EnvelopeSurf envelopeData = new EnvelopeSurf();

        private TaskProgressTracker taskTracker_vel;
        private TaskProgressTracker taskTracker_aoa;
        private TaskProgressTracker taskTracker_surf;
        private System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();

        private static readonly string[] envelopeItems = new string[] { "Excess Thrust", "Level Flight AoA", "Lift/Drag Ratio", "Thrust Available", "Max Lift AoA", "Max Lift Force", "Fuel Economy", "Fuel Burn Rate", "Drag Force", "Lift Slope", "Pitch Input", "Excess Acceleration" };
        private static readonly string[] aoaItems = new string[] { "Lift Force", "Drag Force", "Lift/Drag Ratio", "Lift Slope", "Pitch Input", "Pitching Torque" };
        private static readonly string[] velItems = new string[] { "Level Flight AoA", "Max Lift AoA", "Lift/Drag Ratio", "Lift Slope", "Thrust Available", "Drag Force", "Excess Thrust", "Max Lift", "Excess Accleration", "Pitch Input" };

        private HighlightManager highlightManager;

        public static WindTunnelWindow Instance { get; private set; }

        private AeroPredictor vessel = null;

        // Only used externally. Rolling up is implemented by the prefab.
        public bool Minimized
        {
            get => rollUpToggle?.isOn ?? false;
            set { if (rollUpToggle != null) rollUpToggle.isOn = value; }
        }
        
        // Set from the On Value Changed of the envelope mode dropdown.
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

        // Set by the 'Wet' AoA Graphs Toggle
        public bool ShowAoAGraphsWet
        {
            get => _showAoAGraphsWet;
            set
            {
                if (_showAoAGraphsWet == value)
                    return;
                _showAoAGraphsWet = value;
                aoaWetToggle.isOn = value;
                UpdateAoAWetDry();
            }
        }
        private bool _showAoAGraphsWet = true;

        // Set by the 'Dry' AoA Graphs Toggle
        public bool ShowAoAGraphsDry
        {
            get => _showAoAGraphsDry;
            set
            {
                if (_showAoAGraphsDry == value)
                    return;
                _showAoAGraphsDry = value;
                aoaDryToggle.isOn = value;
                UpdateAoAWetDry();
            }
        }
        private bool _showAoAGraphsDry = true;

        // Set by the 'Fuel-optimal path' toggle
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

        // Set by the 'Time-optimal path' toggle
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
            // Setter only used externally.
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
            // Setter only used externally.
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

        // Set by each of the Graph Mode toggles
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
                UpdateInputVisibility();
                GraphModeChanged();
            }
        }
        private int _graphMode;

        // Set by the highlight mode dropdown
        public int HighlightMode
        {
            get => _highlightMode;
            set
            {
                if (_highlightMode == value)
                    return;
                _highlightMode = value;
                highlightDropdown.Value = value;
                UpdateInputVisibility();
                UpdateHighlightingMethod();
            }
        }
        private int _highlightMode;

        public float HighlightSpeed
        {
            get => _highlightSpeed;
            set
            {
                value = Mathf.Round(value);
                if (_highlightSpeed == value)
                    return;
                _highlightSpeed = value;
                highlightSpeedInput.Text = value.ToString();
                if (GraphMode == 1)
                    RefreshData();
                UpdateHighlightingMethod();
            }
        }
        private float _highlightSpeed = 30;

        public float HighlightAltitude
        {
            get => _highlightAltitude;
            set
            {
                const int altitudeRound = 10;
                value = Mathf.Round(value / altitudeRound) * altitudeRound;
                if (_highlightAltitude == value)
                    return;
                _highlightAltitude = value;
                highlightAltitudeInput.Text = value.ToString();
                if (GraphMode > 0)
                    RefreshData();
                UpdateHighlightingMethod();
            }
        }
        private float _highlightAltitude;

        public float HighlightAoA
        {
            get => _highlightAoA;
            // Setter only used externally.
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

        private void UpdateInputVisibility()
        {
            bool altitude, speed, aoa;
            altitude = speed = aoa = HighlightMode > 0;
            altitude |= GraphMode >= 1;
            speed |= GraphMode == 1;
            highlightAoAGroup.SetActive(aoa);
            highlightAltitudeGroup.SetActive(altitude);
            highlightSpeedGroup.SetActive(speed);
        }

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
            highlightAltitudeInput.Text = HighlightAltitude.ToString();
            highlightSpeedInput.Text = HighlightSpeed.ToString();
            highlightAoAInput.Text = HighlightAoA.ToString();

            if (!HighLogic.LoadedSceneIsEditor)
                return;
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;
            highlightManager = gameObject.AddComponent<HighlightManager>();

            InitializePlanetList();
            gAccel = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
            int homeIndex = planets.FindIndex(x => x.celestialBody == Planetarium.fetch.CurrentMainBody);
            if (homeIndex < 0)
                homeIndex = 0;
            body = planets[homeIndex];
            planetDropdown.Value = homeIndex;

            SetEnvelopeOptions(envelopeItems);

            velocityCollection = velData.graphables;
            aoaCollection = aoaData.graphables;
            envelopeCollection = envelopeData.graphables;

            velCurveGrapher.AddGraph(velocityCollection);
            aoaCurveGrapher.AddGraph(aoaCollection);
            envelopeGrapher.AddGraph(envelopeCollection);


            foreach (var resizer in GetComponentsInChildren<UI_Tools.RenderTextureResizer>())
                resizer.ForceResize();
        }

        // Called when the Export button is clicked
        // TODO: Set directory
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

        // Called by On End Edit of the highlight speed entry field
        public void SetHighlightSpeed(string speed)
        {
            if (float.TryParse(speed, out float result) && result != _highlightSpeed)
            {
                _highlightSpeed = result;
                if (GraphMode == 1)
                    RefreshData();
                UpdateHighlightingMethod();
            }
        }

        // Called by On End Edit of the highlight altitude entry field
        public void SetHighlightAltitude(string altitude)
        {
            if (float.TryParse(altitude, out float result) && result != _highlightAltitude)
            {
                _highlightAltitude = result;
                if (GraphMode > 0)
                    RefreshData();
                UpdateHighlightingMethod();
            }
        }

        // Called by On End Edit of the highlight AoA entry field
        public void SetHighlightAoA(string AoA)
        {
            if (float.TryParse(AoA, out float result) && result != _highlightAoA)
            {
                _highlightAoA = result;
                UpdateHighlightingMethod();
            }
        }

        public readonly List<CBItem> planets = new List<CBItem>();

        private void InitializePlanetList()
        {
            planets.Clear();
            CelestialBody starCB = FlightGlobals.Bodies.FirstOrDefault(x => x.referenceBody == x);
            if (starCB == null)
                return;
            ParseCelestialBodies(starCB);
            SetPlanetOptions(planets);
        }
        private void ParseCelestialBodies(CelestialBody parent, int depth = 0)
        {
            foreach (CBItem body in FlightGlobals.Bodies.Where(x => x.referenceBody == parent).Select(x => new CBItem(x, depth)).OrderBy(x => x.semiMajorRadius))
            {
                if (body.parent == null)
                    continue;
                planets.Add(body);
                ParseCelestialBodies(body.celestialBody, depth + 1);
            }
        }
        public void SetPlanetOptions(IEnumerable<CBItem> options)
        {
            planetDropdown.ClearOptions();
            planetDropdown.AddOptions(options.Select(o => o.name).ToList());
        }

        public void SetEnvelopeOptions(IEnumerable<string> optionNames)
        {
            envelopeDropdown.ClearOptions();
            envelopeDropdown.AddOptions(optionNames.ToList());
        }

        // Called in On End Edit of the ascent target entry box
        public void SetAscentTargetAltitude(string altitude)
        {
            if (float.TryParse(altitude, out float result) && result != _ascentTargetAlt)
            {
                _ascentTargetAlt = result;
                UpdateAscentTarget();
            }
        }

        // Called in On End Edit of the ascent target entry box
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
            clickedPosition = envelopeGrapher.GetGraphCoordinate(clickedPosition);
            HighlightSpeed = clickedPosition.x;
            HighlightAltitude = clickedPosition.y;
            // Todo: Incorporate AoA
        }

        private void AoaCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            aoaCurveInfo.Text = aoaCurveGrapher.GetDisplayValue(clickedPosition);
            HighlightAoA = aoaCurveGrapher.GetGraphCoordinate(clickedPosition).x;
        }

        private void VelCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            velCurveInfo.Text = velCurveGrapher.GetDisplayValue(clickedPosition);
            HighlightSpeed = velCurveGrapher.GetGraphCoordinate(clickedPosition).x;
            // Todo: Incorporate AoA
        }

        private Vector2 crosshairsPosition;
        private bool selectingAscentTarget = false;
        private void EnvelopeGrapher_AscentClicked(object _, Vector2 clickedPosition)
        {
            Graphing.AxisUI axis;
            axis = envelopeGrapher.PrimaryHorizontalAxis;
            if (axis != null)
                _ascentTargetSpeed = axis.Min + (axis.Max - axis.Min) * clickedPosition.x;
            axis = envelopeGrapher.PrimaryVerticalAxis;
            if (axis != null)
                _ascentTargetAlt = axis.Min + (axis.Max - axis.Min) * clickedPosition.y;
            envelopeGrapher.GetComponentInChildren<Graphing.UI.CrosshairController>().SetCrosshairPosition(crosshairsPosition);
            EnvelopeGrapher_GraphClicked(this, crosshairsPosition);
            selectOnGraphToggle.isOn = false;
            UpdateAscentTarget();
        }

        // Called when the 'Select on Graph' button is clicked
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
            crosshairsPosition = envelopeGrapher.GetComponentInChildren<Graphing.UI.CrosshairController>().NormalizedPosition;
            envelopeGrapher.GraphClicked += EnvelopeGrapher_AscentClicked;
            selectingAscentTarget = true;
        }

        #region Reliant on KSP API
        public static float gAccel;// = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        public const float AoAdelta = 0.1f * Mathf.Deg2Rad;
        private CBItem body;// = Planetarium.fetch.CurrentMainBody;
        private float _targetAltitude = 17700;
        public float TargetAltitude
        {
            get { return _targetAltitude; }
            set
            {
                _targetAltitude = value;
                ascentAltitudeInput.Text = value.ToString("F0");
            }
        }
        private float _targetSpeed = 1410;
        public float TargetSpeed
        {
            get { return _targetSpeed; }
            set
            {
                _targetSpeed = value;
                ascentSpeedInput.Text = value.ToString("F1");
            }
        }

        private void RefreshData()
        {
            if (vessel == null)
                return;
            Cancel();
            {
            }
        }

        // Called by the Planet selection dropdown
        public void PlanetSelected(int item)
        {
            body = planets[item];
            RefreshData();
            if (HighlightMode > 0)
                UpdateHighlightingMethod();
        }

        // Called when the highlighting mode or conditions are changed
        private void UpdateHighlightingMethod()
        {
            highlightManager?.UpdateHighlighting((HighlightManager.HighlightMode)HighlightMode, body.celestialBody, HighlightAltitude, HighlightSpeed, HighlightAoA);
        }

        // Called when the Graph Mode is changed by a toggle or externally.
        private void GraphModeChanged() => RefreshData();

        // Called whenever the target ascent altitude or velocity is changed.
        private void UpdateAscentTarget()
        {
            envelopeData.CalculateOptimalLines(cancellationTokenSource.Token);
        }

        // Called whenever one of the AoA graph selection toggles *changes*.
        public void AoAGraphToggleEvent(int index)
        {
            bool value = aoaToggleArray[index];
            aoaCollection[index].Visible = value;
        }

        // Called whenever one of the Velocity graph selection toggles *changes*.
        public void VelGraphToggleEvent(int index)
        {
            bool value = velToggleArray[index];
            velocityCollection[index].Visible = value;
        }

        // TODO: Should be used to disable any auto-updates.
        // The window is only set to not enabled when the 'X' is clicked.
        public void OnDisable()
        {
            WindTunnel.Instance?.SetButtonOff();
            WindTunnel.appButton?.SetFalse(false);
        }
        public void OnEnable()
        {
            WindTunnel.Instance?.SetButtonOn();
        }

        // Called by the Update Vessel button
        public void UpdateVessel()
        {
            if (vessel != null)
            {
                if (vessel is VesselCache.IReleasable releasable)
                    releasable.Release();
                if (vessel is IDisposable disposable)
                    disposable.Dispose();
                vessel = null;
            }

            updateVesselButton.interactable = false;
        }

        private void Cancel()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                cancellationTokenSource = new System.Threading.CancellationTokenSource();
            }
        }

        // Called when the value of 'Wet' or 'Dry' toggle changes.
        public void UpdateAoAWetDry()
        {
            bool wet = ShowAoAGraphsWet;
            bool dry = ShowAoAGraphsDry;
            foreach (var graph in aoaCollection)
            {
                if (graph is GraphableCollection collection)
                {
                    if (collection.Count < 2)
                        continue;
                    collection[0].Visible = wet;
                    collection[1].Visible = dry;
                }
            }
        }

        // Called by the 'Settings' button being clicked.
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
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            envelopeGrapher.GraphClicked -= EnvelopeGrapher_GraphClicked;
            aoaCurveGrapher.GraphClicked -= AoaCurveGrapher_GraphClicked;
            velCurveGrapher.GraphClicked -= VelCurveGrapher_GraphClicked;
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            GameEvents.onPartActionUIDismiss.Remove(OnPartActionUIDismiss);
            if (selectingAscentTarget)
                envelopeGrapher.GraphClicked -= EnvelopeGrapher_AscentClicked;
        }

        public struct CBItem
        {
            public CBItem(CelestialBody celestialBody, int depth = 0)
            {
                this.celestialBody = celestialBody;
                if (celestialBody.referenceBody != celestialBody)
                    semiMajorRadius = (float)celestialBody.orbit.semiMajorAxis;
                else
                    semiMajorRadius = (float)celestialBody.Radius;
                this.depth = depth;
                name = this.celestialBody.bodyName;
                if (celestialBody.referenceBody == celestialBody)
                    parent = null;
                else
                    parent = celestialBody.referenceBody;

                switch (name.ToLower())
                {
                    case "laythe":
                    default:
                    case "kerbin":
                        upperAlt = 25000;
                        upperSpeed = 2500;
                        break;
                    case "eve":
                        upperAlt = 35000;
                        upperSpeed = 3500;
                        break;
                    case "duna":
                        upperAlt = 10000;
                        upperSpeed = 1000;
                        break;
                    /*case "laythe":
                        upperAlt = 20000;
                        maxSpeed = 2000;
                        break;*/
                    case "jool":
                        upperAlt = 200000;
                        upperSpeed = 7000;
                        break;
                }
            }

            public readonly CelestialBody celestialBody;
            public int depth;
            public readonly string name;
            public string NameFormatted { get => new string(' ', depth * 4) + name; }
            public readonly float semiMajorRadius;
            public readonly CelestialBody parent;
            public float upperAlt;
            public float upperSpeed;

            public static explicit operator CelestialBody(CBItem body) => body.celestialBody;
        }
        #endregion
    }
}