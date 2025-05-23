using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Graphing;
using static KerbalWindTunnel.VesselCache.AeroOptimizer;

namespace KerbalWindTunnel.DataGenerators
{
    using LineGraphDefinition = LineGraphDefinition<AoACurve.AoAPoint>;
    public class AoACurve
    {
        public readonly GraphableCollection graphables = new GraphableCollection() { Name = "AoA" };
        public AoAPoint[] AoAPoints { get; private set; }
        public float AverageLiftSlope { get; private set; }
        private static readonly ConcurrentDictionary<(int altitude, int velocity, float aoa), AoAPoint> cache = new ConcurrentDictionary<(int, int, float), AoAPoint>();

        private static Color defaultColor = Color.green;
        private static Color dryColor = Color.yellow;
        private static Func<AoAPoint, Vector2> ToVector(Func<AoAPoint, float> func) => (pt) => new Vector2(pt.AoA * Mathf.Rad2Deg, func(pt));
        public List<GraphDefinition> graphDefinitions = new List<GraphDefinition>
        {
            new LineGraphDefinition("lift_coefSwap", null) { DisplayName = "Lift", Color = defaultColor },
            new LineGraphDefinition("drag_coefSwap", null) { DisplayName = "Drag", Color = defaultColor },
            new LineGraphDefinition("ldRatio", ToVector(p => p.LDRatio)) { DisplayName = "Lift/Drag Ratio", YUnit = "", StringFormat = "F2", Color = defaultColor },
            new LineGraphDefinition("lift_slope_coefSwap", null) { DisplayName = "Lift Slope", StringFormat = "F3", Color = defaultColor },
            new GroupedGraphDefinition<LineGraphDefinition> ("pitchInput",
                new LineGraphDefinition("pitchInput_wet", ToVector(p => p.pitchInput * 100)) { DisplayName = "Pitch Input (Wet)", YName = "Pitch Input", YUnit = "%", StringFormat = "N0", Color = defaultColor },
                new LineGraphDefinition("pitchInput_dry", ToVector(p => p.pitchInput_dry * 100)) { DisplayName = "Pitch Input (Dry)", YName = "Pitch Input", YUnit = "%", StringFormat = "N0", Color = dryColor }
                ) {DisplayName = "Pitch Input", YUnit = "%" },
            /*new GroupedGraphDefinition<LineGraphDefinition> ("staticMargin",
                new LineGraphDefinition("staticMargin_wet", ToVector(p => p.staticMargin * 100)) { DisplayName = "Static Margin (Wet)", YName = "Static Margin", YUnit = "% MAC", StringFormat = "F2", Color = defaultColor },
                new LineGraphDefinition("staticMargin_wet", ToVector(p => p.staticMargin_dry * 100)) { DisplayName = "Static Margin (Dry)", YName = "Static Margin", YUnit = "% MAC", StringFormat = "F2", Color = dryColor }
                ) { DisplayName = "Static Margin", YUnit = "% MAC" },*/
            new GroupedGraphDefinition<LineGraphDefinition> ("torque",
                new LineGraphDefinition("torque_wet", ToVector(p => p.torque)) { DisplayName = "Torque (Wet)", YName = "Torque", YUnit = "kNm", StringFormat = "N0", Color = defaultColor },
                new LineGraphDefinition("torque_dry", ToVector(p => p.torque_dry)) { DisplayName = "Torque (Dry)", YName = "Torque", YUnit = "kNm", StringFormat = "N0", Color = dryColor }
                ) { DisplayName = "Torque", YUnit = "kNm" }
        };

        public void SetCoefficientMode(bool useCoefficients)
        {
            foreach (GraphDefinition graphDef in graphDefinitions.Where(g => g.name.EndsWith("_coefSwap")))
            {
                if (graphDef is LineGraphDefinition lineDef)
                {
                    switch (graphDef.name.Substring(0, graphDef.name.IndexOf("_coefSwap")))
                    {
                        case "lift":
                            lineDef.mappingFunc = useCoefficients ? ToVector(p => p.Coefficient(p.Lift)) : ToVector(p => p.Lift);
                            break;
                        case "drag":
                            lineDef.mappingFunc = useCoefficients ? ToVector(p => p.Coefficient(p.Drag)) : ToVector(p => p.Drag);
                            break;
                        case "lift_slope":
                            lineDef.mappingFunc = useCoefficients ? ToVector(p => p.Coefficient(p.dLift)) : ToVector(p => p.dLift);
                            lineDef.YUnit = useCoefficients ? "/°" : "kN/°";
                            continue;
                        default:
                            continue;
                    }
                    lineDef.YName = useCoefficients ? "Coefficient" : "Force";
                    lineDef.YUnit = useCoefficients ? "" : "kN";
                    lineDef.StringFormat = useCoefficients ? "N0" : "F2";
                }
            }
        }

        public AoACurve()
        {
            SetCoefficientMode(WindTunnelSettings.UseCoefficients);
            foreach (GraphDefinition graphDefinition in graphDefinitions)
            {
                graphDefinition.XUnit = "°";
                graphDefinition.XName = "Angle of Attack";
                graphDefinition.Visible = false;
            }
            graphables.AddRange(graphDefinitions.Where(g => g.Enabled).Select(g => g.Graph));
        }

        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float velocity, float lowerBound, float upperBound, int segments = 50)
        {
            TaskProgressTracker tracker = new TaskProgressTracker();
            Task <AoAPoint[]> task = Task.Run(() => CalculateTask(aeroPredictorToClone, cancellationToken, body, altitude, velocity, lowerBound, upperBound, segments, tracker), cancellationToken);
            tracker.Task = task;
            task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
            return tracker;
        }

        private static AoAPoint[] CalculateTask(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float velocity, float lowerBound, float upperBound, int segments = 50, TaskProgressTracker tracker = null)
        {
            // Apply rounding to facilitate caching.
            const int altitudeRound = 10;
            int altitude_ = Mathf.RoundToInt(altitude / altitudeRound) * altitudeRound;
            int velocity_ = Mathf.RoundToInt(velocity);
            lowerBound = Mathf.Round(lowerBound);
            upperBound = Mathf.Round(upperBound);
            List<float> keysList = new List<float>();
            float delta = (upperBound - lowerBound) / segments;
            for (int i = 0; i <= segments; i++)
                keysList.Add(lowerBound + delta * i);

            // Amend the tracker count in case rounding eliminated any keys.
            tracker?.AmendTotal(keysList.Count);

            float wingArea = aeroPredictorToClone.Area;

            // Fill in the data in a parallel loop.
            AoAPoint[] results = new AoAPoint[keysList.Count];
            try
            {
                Parallel.For<AeroPredictor>(0, keysList.Count, new ParallelOptions() { CancellationToken = cancellationToken },
                    aeroPredictorToClone.GetThreadSafeObject,
                    (index, state, predictor) =>
                    {
                        float aoa = keysList[index];
                        if (!cache.TryGetValue((altitude_, velocity_, aoa), out results[index]))
                        {
                            AoAPoint data = new AoAPoint(predictor, body, altitude_, velocity_, aoa);
                            results[index] = data;
                            cache[(altitude_, velocity_, aoa)] = data;
                        }
                        tracker?.Increment();
                        return predictor;
                    },
                    (predictor) => (predictor as VesselCache.IReleasable)?.Release());
            }
            catch (AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.Flatten().InnerExceptions)
                {
                    Debug.LogException(ex);
                }
                throw aggregateException;
            }

            // Last chance to ditch the results before pushing them to the UI
            cancellationToken.ThrowIfCancellationRequested();

            return results;
        }
        private void PushResults(Task<AoAPoint[]> task)
        {
            lock (this)
            {
                AoAPoints = task.Result;
                UpdateGraphs();
            }
            Debug.Log("[KWT] Graphs updated - AoA");
        }
        private void RethrowErrors(Task<AoAPoint[]> task)
        {
            if (task.Status == TaskStatus.Faulted)
            {
                Debug.LogError("[KWT] Wind tunnel task faulted. (AoA)");
                Debug.LogException(task.Exception);
            }
            else if (task.Status == TaskStatus.Canceled)
                Debug.Log("[KWT] Wind tunnel task was canceled. (AoA)");
        }

        public static void Clear(Task task = null)
        {
            if (task == null)
                cache.Clear();
            else
                task.ContinueWith(ClearContinuation);
        }
        private static void ClearContinuation(Task _) => cache.Clear();

        public void UpdateGraphs()
        {
            if (AoAPoints == null)
                return;

            AverageLiftSlope = AoAPoints.Select(pt => pt.dLift / pt.dynamicPressure).Where(v => !float.IsNaN(v) && !float.IsInfinity(v)).Average();

            foreach (GraphDefinition graphDefinition in graphDefinitions)
            {
                if (graphDefinition is LineGraphDefinition lineGraph)
                    lineGraph.UpdateGraph(AoAPoints);
                else if (graphDefinition is GroupedGraphDefinition<LineGraphDefinition> groupedGraphDefinition)
                    foreach (var lineGraphChild in groupedGraphDefinition)
                        lineGraphChild.UpdateGraph(AoAPoints);
            }
        }

        public readonly struct AoAPoint
        {
            public readonly float Lift;
            public readonly float Drag;
            public readonly float LDRatio;
            public readonly float altitude;
            public readonly float speed;
            public readonly float AoA;
            public readonly float dynamicPressure;
            public readonly float dLift;
            public readonly float mach;
            public readonly float pitchInput;
            public readonly float pitchInput_dry;
            public readonly float staticMargin;
            public readonly float staticMargin_dry;
            public readonly float torque;
            public readonly float torque_dry;
            public readonly bool completed;
            private readonly float wingArea;

            public float Coefficient(float force) => force / (dynamicPressure * wingArea);

            public AoAPoint(AeroPredictor vessel, CelestialBody body, float altitude, float speed, float AoA)
            {
                this.altitude = altitude;
                this.speed = speed;
                AeroPredictor.Conditions conditions = new AeroPredictor.Conditions(body, speed, altitude);
                this.AoA = AoA;
                this.mach = conditions.mach;
                this.dynamicPressure = 0.0005f * conditions.atmDensity * speed * speed;
                this.pitchInput = vessel.FindStablePitchInput(conditions, AoA);
                this.pitchInput_dry = vessel.FindStablePitchInput(conditions, AoA, dryTorque: true);
                Vector3 force = AeroPredictor.ToFlightFrame(vessel.GetAeroForce(conditions, AoA, pitchInput), AoA);
                torque = vessel.GetAeroTorque(conditions, AoA).x;
                torque_dry = vessel.GetAeroTorque(conditions, AoA, 0, true).x;
                Lift = force.y;
                Drag = -force.z;
                LDRatio = Math.Abs(Lift / Drag);
                if (vessel is ILiftAoADerivativePredictor derivativePredictor)
                    dLift = derivativePredictor.GetLiftForceMagnitudeAoADerivative(conditions, AoA, pitchInput) * Mathf.Deg2Rad; // Deg2Rad = 1/Rad2Deg
                else
                    dLift = (vessel.GetLiftForceMagnitude(conditions, AoA + WindTunnelWindow.AoAdelta, pitchInput) - Lift) /
                        (WindTunnelWindow.AoAdelta * Mathf.Rad2Deg);
                /*staticMargin = vessel.GetStaticMargin(conditions, AoA, pitchInput, dLift: dLift, baselineTorque: torque);
                staticMargin_dry = vessel.GetStaticMargin(conditions, AoA, pitchInput, dryTorque: true, dLift: dLift, baselineTorque: torque_dry);*/
                wingArea = vessel.Area;
                completed = true;
            }

            public override string ToString()
            {
                if (WindTunnelSettings.UseCoefficients)
                {
                    float coefMod = Coefficient(1);
                    return string.Format("Altitude:\t{0:N0}m\n" + "Speed:\t{1:N0}m/s\n" + "Mach:\t{6:N2}\n" + "AoA:\t{2:N2}°\n" +
                        "Lift Coefficient:\t{3:N2}\n" + "Drag Coefficient:\t{4:N2}\n" + "Lift/Drag Ratio:\t{5:N2}\n" + "Pitch Input:\t{7:F3}\n" + 
                        "Wing Area:\t{8:F2}",
                        altitude, speed, AoA * Mathf.Rad2Deg,
                        Lift * coefMod, Drag * coefMod, LDRatio, mach, pitchInput,
                        wingArea);
                }
                else
                    return string.Format("Altitude:\t{0:N0}m\n" + "Speed:\t{1:N0}m/s\n" + "Mach:\t{6:N2}\n" + "AoA:\t{2:N2}°\n" +
                            "Lift:\t{3:N0}kN\n" + "Drag:\t{4:N0}kN\n" + "Lift/Drag Ratio:\t{5:N2}\n" + "Pitch Input:\t{7:F3}",
                            altitude, speed, AoA * Mathf.Rad2Deg,
                            Lift, Drag, LDRatio, mach, pitchInput);
            }
        }
    }
}
