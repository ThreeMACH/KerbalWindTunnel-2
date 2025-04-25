using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UI_Tools.Universal_Text;
using Graphing;
using KerbalWindTunnel.DataGenerators;
using KSP.Localization;

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
        private static readonly string[] velItems = new string[] { "Level Flight AoA", "Max Lift AoA", "Lift/Drag Ratio", "Lift Slope", "Thrust Available", "Drag Force", "Excess Thrust", "Max Lift", "Excess Acceleration", "Pitch Input" };

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
                    envelopeCollection.SetVisibility(false);

                    envelopeCollection[value].Visible = true;
                    if (envelopeCollection.HasGraphNamed("Fuel-Optimal Path"))
                        envelopeCollection["Fuel-Optimal Path"].Visible = ShowFuelOptimalPath;
                    if (envelopeCollection.HasGraphNamed("Time-Optimal Path"))
                        envelopeCollection["Time-Optimal Path"].Visible = ShowTimeOptimalPath;
                    if (envelopeCollection.HasGraphNamed("Envelope Mask"))
                        envelopeCollection["Envelope Mask"].Visible = WindTunnelSettings.ShowEnvelopeMask;
                    // TODO: Make use of WindTunnelSettings.ShowEnvelopeMaskAlways
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

        public bool autoSetAscentTarget = true;

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
        private float _ascentTargetAlt = -1;

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
        private float _ascentTargetSpeed = -1;

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
#if UNITY_EDITOR
            Debug.Log("Drawing placeholder");
            GraphableCollection collection = new GraphableCollection3() {
                    new SurfGraph(new float[0,0], 0, 0, 0, 0),
                    new OutlineMask(new float[0,0], 0, 0, 0, 0, v => v.z - 0.5f),
                    new LineGraph(new float[]{ }, 0, 0) { color = Color.black }
            };
            envelopeGrapher.AddGraphToDefaultAxes(collection);
            
            System.Threading.Tasks.Task.Delay(1000).ContinueWith((_) =>
            {
                ((SurfGraph)collection[0]).SetValues(PlaceholderData.surfPlaceholder, 0, 1, 0, 1);
                ((OutlineMask)collection[1]).SetValues(PlaceholderData.surfPlaceholder, 0, 1, 0, 1);
                ((LineGraph)collection[2]).SetValues(new float[] { 0, 0.8f, 0.8f, 0.8f, 0.8f, 0.3f }, 0, 1);
                Debug.Log("Drew placeholder.");
            });
            return;
#endif
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;

            highlightAltitudeInput.Text = HighlightAltitude.ToString();
            highlightSpeedInput.Text = HighlightSpeed.ToString();
            highlightAoAInput.Text = HighlightAoA.ToString();

            aoaToggleArray.Items[0].IsOn = true;
            aoaToggleArray.Items[1].IsOn = true;
            velToggleArray.Items[4].IsOn = true;

            highlightManager = gameObject.AddComponent<HighlightManager>();

            InitializePlanetList();
            gAccel = (float)(PhysicsGlobals.GravitationalAcceleration * Planetarium.fetch.Home.GeeASL);
            int homeIndex = planets.FindIndex(x => x.celestialBody == Planetarium.fetch.CurrentMainBody);
            if (homeIndex < 0)
                homeIndex = 0;
            body = planets[homeIndex];
            planetDropdown.Value = homeIndex;

            SetEnvelopeOptions(envelopeItems);

            velocityCollection = velData.graphables;
            aoaCollection = aoaData.graphables;
            envelopeCollection = envelopeData.graphables;
            envelopeDropdown.Value = 0;

            velCurveGrapher.AddGraphToDefaultAxes(velocityCollection);
            aoaCurveGrapher.AddGraphToDefaultAxes(aoaCollection);
            envelopeGrapher.AddGraphToDefaultAxes(envelopeCollection);

            Graphing.Extensions.ColorMapMaterial.SetClip(envelopeGrapher.PrimaryColorAxis.AxisMaterial, true);

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
            else
                highlightSpeedInput.Text = HighlightSpeed.ToString();
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
            else
                highlightAltitudeInput.Text = HighlightAltitude.ToString();
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
            foreach (CBItem body in FlightGlobals.Bodies.Where(x => x.referenceBody == parent && x.atmosphere).Select(x => new CBItem(x, depth)).OrderBy(x => x.semiMajorRadius))
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
                if (result >= 0)
                {
                    _ascentTargetAlt = result;
                    autoSetAscentTarget = false;
                }
                else
                {
                    _ascentTargetAlt = -1;
                    autoSetAscentTarget = true;
                    ascentAltitudeInput.Text = "";
                }
                UpdateAscentTarget();
            }
            else
                ascentAltitudeInput.Text = AscentTargetAltitude >= 0 ? AscentTargetAltitude.ToString() : "";
        }

        // Called in On End Edit of the ascent target entry box
        public void SetAscentTargetSpeed(string speed)
        {
            if (float.TryParse(speed, out float result) && result != _ascentTargetSpeed)
            {
                if (result >= 0)
                {
                    _ascentTargetSpeed = result;
                    autoSetAscentTarget = false;
                }
                else
                {
                    _ascentTargetSpeed = -1;
                    autoSetAscentTarget = true;
                    ascentSpeedInput.Text = "";
                }
                UpdateAscentTarget();
            }
            else
                ascentSpeedInput.Text = AscentTargetSpeed >= 0 ? AscentTargetSpeed.ToString() : "";
        }

        private void EnvelopeGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            clickedPosition = envelopeGrapher.GetGraphCoordinate(clickedPosition);
            HighlightSpeed = clickedPosition.x;
            HighlightAltitude = clickedPosition.y;
            HighlightAoA = Mathf.Round(Mathf.Rad2Deg * SetEnvelopeDetails() * 10) / 10;

            SetCrosshair(velCurveGrapher, HighlightSpeed);
            SetVelDetails();

            SetCrosshair(aoaCurveGrapher, HighlightAoA);
            SetAoADetails();
        }
        private float SetEnvelopeDetails()
        {
            EnvelopePoint pointDetails = new EnvelopePoint(vessel, CelestialBody, HighlightAltitude, HighlightSpeed);
            envelopeInfo.Text = pointDetails.ToString();
            return pointDetails.AoA_level;
        }

        private void AoaCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            HighlightAoA = aoaCurveGrapher.GetGraphCoordinate(clickedPosition).x;
            SetAoADetails();
        }
        private void SetAoADetails()
        {
            AoACurve.AoAPoint pointDetails = new AoACurve.AoAPoint(vessel, CelestialBody, HighlightAltitude, HighlightSpeed, HighlightAoA);
            aoaCurveInfo.Text = pointDetails.ToString();
        }

        private void VelCurveGrapher_GraphClicked(object _, Vector2 clickedPosition)
        {
            HighlightSpeed = velCurveGrapher.GetGraphCoordinate(clickedPosition).x;
            HighlightAoA = Mathf.Round(Mathf.Rad2Deg * SetVelDetails() * 10) / 10;

            SetCrosshair(envelopeGrapher, HighlightSpeed, HighlightAltitude);
            SetEnvelopeDetails();

            SetCrosshair(aoaCurveGrapher, HighlightAoA);
            SetAoADetails();
        }
        private float SetVelDetails()
        {
            VelCurve.VelPoint pointDetails = new VelCurve.VelPoint(vessel, CelestialBody, HighlightAltitude, HighlightSpeed);
            velCurveInfo.Text = pointDetails.ToString();
            return pointDetails.AoA_level;
        }

        private static void SetCrosshair(Grapher grapher, float x, float y)
        {
            if (grapher.CrosshairController == null)
                return;
            
            Vector2 heldPosition = grapher.CrosshairController.NormalizedPosition;
            AxisUI axis = grapher.PrimaryHorizontalAxis;
            if (axis != null && axis.Min != axis.Max)
            {
                heldPosition.x = Mathf.InverseLerp(axis.Min, axis.Max, x);
            }
            axis = grapher.PrimaryVerticalAxis;
            if (axis != null && axis.Min != axis.Max)
            {
                heldPosition.y = Mathf.InverseLerp(axis.Min, axis.Max, y);
            }
            grapher.CrosshairController.SetCrosshairPosition(heldPosition);
        }

        private static void SetCrosshair(Grapher grapher, float value)
        {
            if (grapher.CrosshairController == null)
                return;
            Vector2 heldPosition = grapher.CrosshairController.NormalizedPosition;
            AxisUI horizontalAxis = grapher.PrimaryHorizontalAxis;
            if (horizontalAxis != null && horizontalAxis.Min != horizontalAxis.Max)
            {
                heldPosition.x = Mathf.InverseLerp(horizontalAxis.Min, horizontalAxis.Max, value);
                grapher.CrosshairController.SetCrosshairPosition(heldPosition);
            }
        }

        private Vector2 crosshairsPosition;
        private bool selectingAscentTarget = false;
        private void EnvelopeGrapher_AscentClicked(object _, Vector2 clickedPosition)
        {
            Graphing.AxisUI axis;
            axis = envelopeGrapher.PrimaryHorizontalAxis;
            if (axis != null)
            {
                _ascentTargetSpeed = Mathf.Round(Mathf.Lerp(axis.Min, axis.Max, clickedPosition.x));
                ascentSpeedInput.Text = _ascentTargetSpeed.ToString();
            }
            else
                _ascentTargetSpeed = -1;

            axis = envelopeGrapher.PrimaryVerticalAxis;
            if (axis != null)
            {
                _ascentTargetAlt = Mathf.Round(Mathf.Lerp(axis.Min, axis.Max, clickedPosition.y) / 10) * 10;
                ascentAltitudeInput.Text = _ascentTargetAlt.ToString();
            }
            else
                _ascentTargetAlt = -1;

            envelopeGrapher.CrosshairController?.SetCrosshairPosition(crosshairsPosition);
            selectOnGraphToggle.isOn = false;
            autoSetAscentTarget = false;
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
                {
                    envelopeGrapher.GraphClicked -= EnvelopeGrapher_AscentClicked;
                    envelopeGrapher.GraphClicked += EnvelopeGrapher_GraphClicked;
                }
                selectingAscentTarget = false;
                return;
            }
            crosshairsPosition = envelopeGrapher.CrosshairController.NormalizedPosition;
            envelopeGrapher.GraphClicked += EnvelopeGrapher_AscentClicked;
            envelopeGrapher.GraphClicked -= EnvelopeGrapher_GraphClicked;
            selectingAscentTarget = true;
        }

        #region Reliant on KSP API
        public static float gAccel;// = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        public const float AoAdelta = 0.1f * Mathf.Deg2Rad;
        private CBItem body;// = Planetarium.fetch.CurrentMainBody;
        public CelestialBody CelestialBody { get => body.celestialBody; }

#if DEBUG
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "DEBUG Only")]
        private void CharacterizedTestDump()
        {
            VesselCache.SimulatedVessel simVessel_test = VesselCache.SimulatedVessel.Borrow(EditorLogic.fetch.ship);
            VesselCache.CharacterizedVessel charVessel = new VesselCache.CharacterizedVessel(simVessel_test);

            AeroPredictor.Conditions conditions = new AeroPredictor.Conditions(CelestialBody, 100, 0);

            Debug.Log("Truth Data");
            Debug.LogFormat("Angle\tLift_true\tLift_Char\tDrag_true\tDrag_Char");
            for (int i = -180; i <= 180; i++)
            {
                float lift_true = simVessel_test.GetLiftForceMagnitude(conditions, Mathf.Deg2Rad * i);
                float lift_char = charVessel.GetLiftForceMagnitude(conditions, Mathf.Deg2Rad * i);
                float drag_true = simVessel_test.GetDragForceMagnitude(conditions, Mathf.Deg2Rad * i);
                float drag_char = charVessel.GetDragForceMagnitude(conditions, Mathf.Deg2Rad * i);

                Debug.LogFormat($"{Mathf.Deg2Rad * i}\t{lift_true}\t{lift_char}\t{drag_true}\t{drag_char}");
            }
            Debug.Log("End data");

            Debug.Log($"{simVessel_test.GetDragForceMagnitude(conditions, Mathf.Deg2Rad * 5)} == {charVessel.GetDragForceMagnitude(conditions, Mathf.Deg2Rad * 5)}");
            Debug.Log("Vessel Lift Curve");
            foreach (var k in charVessel.surfaceLift[0].liftCurve.Curve.keys)
                Debug.LogFormat($"{k.time}\t{k.value}\t{k.inTangent}\t{k.outTangent}");
        }
#endif

        private void RefreshData()
        {
#if DEBUG
            Debug.Log("[KWT] Refreshing.");
#endif
            if (vessel == null)
                return;
            Cancel();

            float minX, maxX;
            switch (GraphMode)
            {
                case 0:
                    float minY, maxY;
                    minX = envelopeGrapher.PrimaryHorizontalAxis.AutoSetMin ? 0 : envelopeGrapher.PrimaryHorizontalAxis.Min;
                    maxX = envelopeGrapher.PrimaryHorizontalAxis.AutoSetMax ? body.upperSpeed : envelopeGrapher.PrimaryHorizontalAxis.Max;
                    minY = envelopeGrapher.PrimaryVerticalAxis.AutoSetMin ? 0 : envelopeGrapher.PrimaryVerticalAxis.Min;
                    maxY = envelopeGrapher.PrimaryVerticalAxis.AutoSetMax ? body.upperAlt : envelopeGrapher.PrimaryVerticalAxis.Max;
                    taskTracker_surf = envelopeData.Calculate(vessel, cancellationTokenSource.Token, CelestialBody, minX, maxX, minY, maxY);
                    break;
                case 1:
                    minX = aoaCurveGrapher.PrimaryHorizontalAxis.AutoSetMin ? -20 : aoaCurveGrapher.PrimaryHorizontalAxis.Min;
                    maxX = aoaCurveGrapher.PrimaryHorizontalAxis.AutoSetMax ? 20 : aoaCurveGrapher.PrimaryHorizontalAxis.Max;
                    taskTracker_aoa = aoaData.Calculate(vessel, cancellationTokenSource.Token, CelestialBody, HighlightAltitude, HighlightSpeed, minX * Mathf.Deg2Rad, maxX * Mathf.Deg2Rad);
                    break;
                case 2:
                    minX = Mathf.Min(0, velCurveGrapher.PrimaryHorizontalAxis.Min);
                    maxX = Mathf.Max(body.upperSpeed, velCurveGrapher.PrimaryHorizontalAxis.Max);
                    taskTracker_vel = velData.Calculate(vessel, cancellationTokenSource.Token, CelestialBody, HighlightAltitude, minX, maxX);
                    break;
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
            highlightManager?.UpdateHighlighting((HighlightManager.HighlightMode)HighlightMode, CelestialBody, HighlightAltitude, HighlightSpeed, HighlightAoA);
        }

        // Called when the Graph Mode is changed by a toggle or externally.
        private void GraphModeChanged() => RefreshData();

        // Called whenever the target ascent altitude or velocity is changed.
        private void UpdateAscentTarget()
        {
            envelopeData.CalculateOptimalLines(cancellationTokenSource.Token);
        }
        internal void ProvideAscentTarget((float speed, float altitude) maxSustainableEnergy)
        {
            if (_ascentTargetSpeed < 0)
                _ascentTargetSpeed = maxSustainableEnergy.speed;
            if (_ascentTargetAlt < 0)
                _ascentTargetAlt = maxSustainableEnergy.altitude;
            // TODO: Set the text fields to these numbers. Can't do it here since this will be called from a worker thread.
        }

        // Called whenever one of the AoA graph selection toggles *changes*.
        public void AoAGraphToggleEvent(int index)
        {
            bool value = aoaToggleArray[index];
            if (aoaCollection == null)
                return;
            aoaCollection[index].Visible = value;
        }

        // Called whenever one of the Velocity graph selection toggles *changes*.
        public void VelGraphToggleEvent(int index)
        {
            bool value = velToggleArray[index];
            if (velocityCollection == null)
                return;
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

            vessel = VesselCache.SimulatedVessel.Borrow(EditorLogic.fetch.ship);
            if (WindTunnelSettings.Instance.useCharacterized && vessel is VesselCache.SimulatedVessel simVessel)
            {
                vessel = new VesselCache.CharacterizedVessel(simVessel);
            }

            Cancel();

            VelCurve.Clear(taskTracker_vel?.LastFollowOnTask);
            AoACurve.Clear(taskTracker_aoa?.LastFollowOnTask);
            EnvelopeSurf.Clear(taskTracker_surf?.LastFollowOnTask);

            RefreshData();

            //updateVesselButton.interactable = false;
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
            if (aoaCollection == null)
                return;
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
            if (settingsDialog == null)
                settingsDialog = SpawnDialog();
            else
            {
                settingsDialog.Dismiss();
                settingsDialog = null;
            }
        }

        private void OnEditorShipModified(ShipConstruct _)
        {
            updateVesselButton.interactable = true;
            // TODO: Move this to the highlight manager.
            if (HighlightMode > 0 && (highlightManager?.isActiveAndEnabled ?? false))
                UpdateHighlightingMethod();
        }

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
                name = Localizer.Format("<<1>>", celestialBody.displayName);
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