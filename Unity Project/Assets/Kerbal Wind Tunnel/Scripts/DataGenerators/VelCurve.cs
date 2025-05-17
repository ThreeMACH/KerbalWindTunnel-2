using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Graphing;

namespace KerbalWindTunnel.DataGenerators
{
    using LineGraphDefinition = LineGraphDefinition<EnvelopePoint>;
    public class VelCurve
    {
        public readonly GraphableCollection graphables = new GraphableCollection() { Name = "velCollection" };
        public EnvelopePoint[] VelPoints { get; private set; }
        private static readonly ConcurrentDictionary<(int altitude, int velocity), EnvelopePoint> cache = new ConcurrentDictionary<(int, int), EnvelopePoint>();

        private static Color defaultColor = Color.green;
        private static Func<EnvelopePoint, Vector2> ToVector(Func<EnvelopePoint, float> func) => (pt) => new Vector2(pt.speed, func(pt));
        public readonly List<LineGraphDefinition> graphDefinitions = new List<LineGraphDefinition>()
        {
            new LineGraphDefinition("aoa_level", ToVector(p => p.AoA_level * Mathf.Rad2Deg)){ DisplayName = "Level AoA", YName = "Angle", YUnit = "°", StringFormat = "F2", Color = defaultColor },
            new LineGraphDefinition("aoa_max", ToVector(p => p.AoA_max * Mathf.Rad2Deg)) { DisplayName = "Max Lift AoA", YName = "Angle", YUnit = "°", StringFormat = "F2", Color = defaultColor },
            new LineGraphDefinition("ldRatio", ToVector(p => p.LDRatio)) { DisplayName = "Lift/Drag Ratio", YUnit = "-", StringFormat = "F2", Color = defaultColor },
            new LineGraphDefinition("lift_slope_coefSwap", null) { DisplayName = "Lift Slope", StringFormat = "F3", Color = defaultColor },
            new LineGraphDefinition("thrust_available", ToVector(p => p.thrust_available)) { DisplayName = "Thrust Available", YName = "Force", YUnit = "kN", StringFormat = "N0", Color = defaultColor },
            new LineGraphDefinition("thrust_required", ToVector(p => p.thrust_required)) { DisplayName = "Thrust Required", YName = "Force", YUnit = "kN", StringFormat = "N0", Color = defaultColor },
            new LineGraphDefinition("excess_thrust", ToVector(p => p.Thrust_Excess)){ DisplayName = "Excess Thrust", YName = "Force", YUnit = "kN", StringFormat="N0", Color = defaultColor },
            new LineGraphDefinition("liftMax_coefSwap", null) { DisplayName = "Max Lift", Color = defaultColor },
            new LineGraphDefinition("drag_coefSwap", null) { DisplayName = "Drag", Color = defaultColor },
            new LineGraphDefinition("fuel_economy", ToVector(p => p.fuelBurnRate / p.speed * 100 * 1000)) { DisplayName = "Fuel Economy", YUnit = "kg/100 km", StringFormat = "F2", Color = defaultColor, Enabled = false },
            new LineGraphDefinition("fuel_rate", ToVector(p => p.fuelBurnRate)) { DisplayName = "Fuel Burn Rate", YUnit = "kg/s", StringFormat = "F3", Color = defaultColor, Enabled = false },
            new LineGraphDefinition("pitch_input", ToVector(p => p.pitchInput * 100)) { DisplayName = "Pitch Input", YUnit = "%", StringFormat = "N0", Color = defaultColor },
            new LineGraphDefinition("accel_excess", ToVector(p => p.Accel_Excess)) { DisplayName = "Excess Acceleration", YUnit = "g", StringFormat = "N2", Color = defaultColor, Enabled = false }
        };

        public void SetCoefficientMode(bool useCoefficients)
        {
            foreach (GraphDefinition graphDef in graphDefinitions.Where(g => g.name.EndsWith("_coefSwap")))
            {
                if (graphDef is LineGraphDefinition lineDef)
                {
                    switch (graphDef.name.Substring(0, graphDef.name.IndexOf("_coefSwap")))
                    {
                        case "liftMax":
                            lineDef.mappingFunc = useCoefficients ? ToVector(p => p.Coefficient(p.lift_max)) : ToVector(p => p.lift_max);
                            break;
                        case "drag":
                            lineDef.mappingFunc = useCoefficients ? ToVector(p => p.Coefficient(p.drag)) : ToVector(p => p.drag);
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

        public VelCurve()
        {
            SetCoefficientMode(WindTunnelSettings.UseCoefficients);
            foreach (GraphDefinition graphDefinition in graphDefinitions)
            {
                graphDefinition.XUnit = "m/s";
                graphDefinition.XName = "Airspeed";
                graphDefinition.Visible = false;
            }
            graphables.AddRange(graphDefinitions.Where(g => g.Enabled).Select(g => g.Graph));
        }

        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float lowerBound, float upperBound, int segments = 50)
        {
            TaskProgressTracker tracker = new TaskProgressTracker();
            Task<EnvelopePoint[]> task = Task.Run(() => CalculateTask(aeroPredictorToClone, cancellationToken, body, altitude, lowerBound, upperBound, segments, tracker), cancellationToken);
            tracker.Task = task;
            task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
            return tracker;
        }

        private static EnvelopePoint[] CalculateTask(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float lowerBound, float upperBound, int segments = 50, TaskProgressTracker tracker = null)
        {
            // Apply rounding to facilitate caching.
            const int altitudeRound = 10;
            int altitude_ = Mathf.RoundToInt(altitude / altitudeRound) * altitudeRound;
            lowerBound = Mathf.Round(lowerBound);
            upperBound = Mathf.Round(upperBound);
            SortedSet<int> keys = new SortedSet<int>();
            float delta = (upperBound - lowerBound) / segments;
            for (int i = 0; i <= segments; i++)
                keys.Add(Mathf.RoundToInt(lowerBound + delta * i));
            List<int> keysList = keys.ToList();

            // Amend the tracker count in case rounding eliminated any keys.
            tracker?.AmendTotal(keysList.Count);

            float wingArea = aeroPredictorToClone.Area;

            // Fill in the data in a parallel loop.
            EnvelopePoint[] results = new EnvelopePoint[keys.Count];
            try
            {
                Parallel.For<AeroPredictor>(0, keys.Count, new ParallelOptions() { CancellationToken = cancellationToken },
                    aeroPredictorToClone.GetThreadSafeObject,
                    (index, state, predictor) =>
                    {
                        int velocity = keysList[index];
                        if (!cache.TryGetValue((altitude_, velocity), out results[index]))
                        {
                            EnvelopePoint data = new EnvelopePoint(predictor, body, altitude_, velocity);
                            results[index] = data;
                            cache[(altitude_, velocity)] = data;
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
        private void PushResults(Task<EnvelopePoint[]> task)
        {
            lock (this)
            {
                VelPoints = task.Result;
                UpdateGraphs();
            }
            Debug.Log("[KWT] Graphs updated - Velocity");
        }
        private void RethrowErrors(Task<EnvelopePoint[]> task)
        {
            if (task.Status == TaskStatus.Faulted)
            {
                Debug.LogError("[KWT] Wind tunnel task faulted. (Vel)");
                Debug.LogException(task.Exception);
            }
            else if (task.Status == TaskStatus.Canceled)
                Debug.Log("[KWT] Wind tunnel task was canceled. (Vel)");
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
            if (VelPoints == null)
                return;
            foreach (LineGraphDefinition lineGraphDefinition in graphDefinitions)
            {
                lineGraphDefinition.UpdateGraph(VelPoints);
            }
        }
    }
}
