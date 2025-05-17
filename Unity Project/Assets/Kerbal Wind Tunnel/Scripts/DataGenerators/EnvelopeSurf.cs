using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Graphing;
using KerbalWindTunnel.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace KerbalWindTunnel.DataGenerators
{
    using SurfGraphDefinition = SurfGraphDefinition<EnvelopePoint>;
    public class EnvelopeSurf
    {
        private static readonly (float speed, float altitude) ascentOrigin = (30, 0);
        public readonly GraphableCollection graphables = new GraphableCollection3() { Name = "envelopeCollection" };
        public EnvelopePoint[,] EnvelopePoints { get; private set; } = null;
        private float left, right, bottom, top;
        private static readonly ConcurrentDictionary<SurfCoords, EnvelopePoint> cache = new ConcurrentDictionary<SurfCoords, EnvelopePoint>();

        public static (int x, int y)[] resolution = { (10, 10), (40, 120), (80, 180), (160, 360) };

        private const string xName = "Speed", xUnit = "m/s", yName = "Altitude", yUnit = "m";

        public readonly List<GraphDefinition> graphDefinitions = new List<GraphDefinition>
        {
            new SurfGraphDefinition("thrust_excess", p => p.Thrust_Excess) { DisplayName = "Excess Thrust", ZUnit = "kN", StringFormat = "N0", CMin = 0 },
            new SurfGraphDefinition("thrust_available", p => p.thrust_available) { DisplayName = "Thrust Available", ZUnit = "kN", StringFormat = "N0", CMin = 0 },
            new SurfGraphDefinition("power_excess", p => p.Specific(p.Power_Excess)) {DisplayName = "Specific Excess Power", ZUnit = "m/s", StringFormat = "N0", CMin = 0 },
            // TODO: Angle of climb
            new SurfGraphDefinition("aoa_level", p => p.AoA_level * Mathf.Deg2Rad) { DisplayName = "Level AoA", ZUnit = "°", StringFormat = "F2" },
            new SurfGraphDefinition("ldRatio", p => p.LDRatio) { DisplayName = "Lift/Drag Ratio", ZUnit = "-", StringFormat = "F2" },
            new SurfGraphDefinition("lift_slope_coefSwap", null) { DisplayName = "Lift Slope", StringFormat = "F3" },
            new SurfGraphDefinition("drag_coefSwap", null) { DisplayName = "Drag" },
            new SurfGraphDefinition("aoa_max", p => p.AoA_max * Mathf.Deg2Rad) { DisplayName = "Max Lift AoA", ZUnit = "°", StringFormat = "F2" },
            new SurfGraphDefinition("liftMax_coefSwap", null) { DisplayName = "Max Lift" },
            new SurfGraphDefinition("pitch_input", p => p.pitchInput * 100) { DisplayName = "Pitch Input", ZUnit = "%", StringFormat = "N0" },
            // TODO: Static margin
            new SurfGraphDefinition("fuel_economy", p => p.fuelBurnRate / p.speed * 100 * 1000) { DisplayName = "Fuel Economy", ZUnit = "kg/100 km", StringFormat = "F2" },
            new SurfGraphDefinition("fuel_rate", p => p.fuelBurnRate) { DisplayName = "Fuel Burn Rate", ZUnit = "kg/s", StringFormat = "F3" },
            new SurfGraphDefinition("accel_excess", p => p.Accel_Excess) { DisplayName = "Excess Acceleration", ZUnit = "g", StringFormat = "N2", CMin = 0, Enabled = false }
        };
        //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Derivative", ZUnit = "kNm/deg", StringFormat = "F3", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
        //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Range", ZUnit = "deg", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
        //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Score", ZUnit = "kNm-deg", StringFormat = "F1", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
        public readonly OutlineGraphDefinition<EnvelopePoint> envelope = new OutlineGraphDefinition<EnvelopePoint>("envelope", p => p.Thrust_Excess) { DisplayName = "Flight Envelope", ZUnit = "kN", StringFormat = "N0", Color = Color.gray, LineWidth = 2, LineOnly = true, MaskCriteria = (v) => !float.IsNaN(v.z) && !float.IsInfinity(v.z) ? v.z : -1 };
        public readonly MetaLineGraphDefinition<EnvelopeLine.AscentPathPoint> fuelPath = new MetaLineGraphDefinition<EnvelopeLine.AscentPathPoint>("path_fuelOptimal", p => new Vector2(p.speed, p.altitude),
                new Func<EnvelopeLine.AscentPathPoint, float>[] { p => p.climbAngle * Mathf.Rad2Deg, p => p.climbRate, p => p.cost, p => p.time },
                new string[] { "Climb Angle", "Climb Rate", "Fuel Used", "Time" },
                new string[] { "N1", "N0", "N3", "N1" },
                new string[] { "°", "m/s", "units", "s" })
        { DisplayName = "Fuel-Optimal Path", StringFormat = "N0", Color = Color.black, LineWidth = 3 };
        public readonly MetaLineGraphDefinition<EnvelopeLine.AscentPathPoint> timePath = new MetaLineGraphDefinition<EnvelopeLine.AscentPathPoint>("path_timeOptimal", p => new Vector2(p.speed, p.altitude),
                new Func<EnvelopeLine.AscentPathPoint, float>[] { p => p.climbAngle * Mathf.Rad2Deg, p => p.climbRate, p => p.cost },
                new string[] { "Climb Angle", "Climb Rate", "Time" },
                new string[] { "N1", "N0", "N1" },
                new string[] { "°", "m/s", "s" })
        { DisplayName = "Time-Optimal Path", StringFormat = "N0", Color = Color.white, LineWidth = 3 };

        public void SetCoefficientMode(bool useCoefficients)
        {
            foreach (GraphDefinition graphDef in graphDefinitions.Where(g => g.name.EndsWith("_coefSwap")))
            {
                if (graphDef is SurfGraphDefinition surfDef)
                {
                    switch (graphDef.name.Substring(0, graphDef.name.IndexOf("_coefSwap")))
                    {
#if OUTSIDE_UNITY
                        case "liftMax":
                            surfDef.mappingFunc = useCoefficients ? p => p.Coefficient(p.lift_max) : p => p.lift_max;
                            break;
                        case "drag":
                            surfDef.mappingFunc = useCoefficients ? p => p.Coefficient(p.drag) : p => p.drag;
                            break;
                        case "lift_slope":
                            surfDef.mappingFunc = useCoefficients ? p => p.Coefficient(p.dLift) : p => p.dLift;
                            surfDef.ZUnit = useCoefficients ? "/°" : "kN/°";
                            continue;
#endif
                        default:
                            continue;
                    }
                    surfDef.ZName = useCoefficients ? "Coefficient" : "Force";
                    surfDef.ZUnit = useCoefficients ? "" : "kN";
                    surfDef.StringFormat = useCoefficients ? "N0" : "F2";
                }
            }
        }

        public EnvelopeSurf()
        {
            SetCoefficientMode(WindTunnelSettings.UseCoefficients);
            graphDefinitions.Add(envelope);
            graphDefinitions.Add(fuelPath);
            graphDefinitions.Add(timePath);
            foreach (GraphDefinition graphDefinition in graphDefinitions)
            {
                graphDefinition.XName = xName;
                graphDefinition.XUnit = xUnit;
                graphDefinition.YName = yName;
                graphDefinition.YUnit = yUnit;
            }

            graphables.AddRange(graphDefinitions.Where(g => g.Enabled).Select(g => g.Graph));
        }

        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float lowerBoundSpeed, float upperBoundSpeed, float lowerBoundAltitude, float upperBoundAltitude)
        {
            TaskProgressTracker result = Calculate_Internal(aeroPredictorToClone, cancellationToken, body, lowerBoundSpeed, upperBoundSpeed, lowerBoundAltitude, upperBoundAltitude, resolution[0].x, resolution[0].y);
            TaskProgressTracker prevTask = result;
            for (int i = 1; i < resolution.Length; i++)
            {
                TaskProgressTracker tracker = new TaskProgressTracker();
                // prevTask is the Calculate task.
                // Its FollowOn is the PushResults task.
                // We want to wait until after results have been pushed to begin the next batch.
                // This prevents a potential race condition of pushing results.
                Task<ResultsType> task = prevTask.FollowOnTaskTracker.Task.ContinueWith((_) => CalculateTask(aeroPredictorToClone, cancellationToken, body, lowerBoundSpeed, upperBoundSpeed, lowerBoundAltitude, upperBoundAltitude, resolution[i].x, resolution[i].y, tracker), TaskContinuationOptions.OnlyOnRanToCompletion);
                tracker.Task = task;
                prevTask.FollowOnTaskTracker.FollowOnTaskTracker = tracker;
                prevTask = tracker;
                Task pushTask = task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
                tracker.FollowOnTaskTracker = new TaskProgressTracker(pushTask);
                task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
                task.ContinueWith(t => CalculateOptimalLinesTask(t.Result, cancellationToken), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            return result;
        }
        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float lowerBoundSpeed, float upperBoundSpeed, float lowerBoundAltitude, float upperBoundAltitude, int speedSegments, int altitudeSegments)
        {
            TaskProgressTracker result = Calculate_Internal(aeroPredictorToClone, cancellationToken, body, lowerBoundSpeed, lowerBoundAltitude, upperBoundSpeed, upperBoundAltitude, speedSegments, altitudeSegments);
            ((Task<ResultsType>)result.Task).ContinueWith(task => CalculateOptimalLinesTask(task.Result, cancellationToken), TaskContinuationOptions.OnlyOnRanToCompletion);
            return result;
        }
        private TaskProgressTracker Calculate_Internal(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float lowerBoundSpeed, float upperBoundSpeed, float lowerBoundAltitude, float upperBoundAltitude, int speedSegments, int altitudeSegments)
        {
            TaskProgressTracker tracker = new TaskProgressTracker();
            Task<ResultsType> task = Task.Run(() => CalculateTask(aeroPredictorToClone, cancellationToken, body, lowerBoundSpeed, upperBoundSpeed, lowerBoundAltitude, upperBoundAltitude, speedSegments, altitudeSegments, tracker), cancellationToken);
            Task pushTask = task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
            tracker.FollowOnTaskTracker = new TaskProgressTracker(pushTask);
            task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
            return tracker;
        }

        private static ResultsType CalculateTask(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float lowerBoundSpeed, float upperBoundSpeed, float lowerBoundAltitude, float upperBoundAltitude, int speedSegments, int altitudeSegments, TaskProgressTracker progressTracker = null)
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.BeginSample($"EnvelopeSurf.Calculate {resolution.IndexOf((speedSegments, altitudeSegments))}");
#endif
            if (aeroPredictorToClone is VesselCache.SimulatedVessel simVessel && !simVessel.DirectAoAInitialized)
                simVessel.InitMaxAoA();
            EnvelopePoint[] results = new EnvelopePoint[(speedSegments + 1) * (altitudeSegments + 1)];
            float stepSpeed = (upperBoundSpeed - lowerBoundSpeed) / speedSegments;
            float stepAltitude = (upperBoundAltitude - lowerBoundAltitude) / altitudeSegments;

            int cachedCount = 0;

            try
            {
                //OrderablePartitioner<EnvelopePoint> partitioner = Partitioner.Create(primaryProgress, true);
                Parallel.For<AeroPredictor>(0, results.Length, new ParallelOptions() { CancellationToken = cancellationToken },
                    aeroPredictorToClone.GetThreadSafeObject,
                    (index, state, predictor) =>
                    {
                        int x = index % (speedSegments + 1), y = index / (speedSegments + 1);
                        SurfCoords coords = new SurfCoords(x * stepSpeed + lowerBoundSpeed,
                        y * stepAltitude + lowerBoundAltitude);

                        if (!cache.TryGetValue(coords, out EnvelopePoint result))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            result = new EnvelopePoint(predictor, body, y * stepAltitude + lowerBoundAltitude, x * stepSpeed + lowerBoundSpeed);
                            cancellationToken.ThrowIfCancellationRequested();
                            cache[coords] = result;
                        }
                        else
                            Interlocked.Increment(ref cachedCount);
                        results[index] = result;
                        progressTracker?.Increment();
                        return predictor;
                    }, (predictor) => (predictor as VesselCache.IReleasable)?.Release());
            }
            catch (AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.Flatten().InnerExceptions)
                {
                    Debug.LogException(ex);
                }
                throw aggregateException;
            }
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.EndSample();
#endif                
            cancellationToken.ThrowIfCancellationRequested();
            Debug.LogFormat("Wind Tunnel - Data run finished. {0} of {1} ({2:F0}%) retrieved from cache.", cachedCount, results.Length, (float)cachedCount / results.Length * 100);
            return (results.To2Dimension(speedSegments + 1), (lowerBoundSpeed, upperBoundSpeed), (lowerBoundAltitude, upperBoundAltitude));
        }
        private void PushResults(Task<ResultsType> data)
        {
            lock (this)
            {
                ResultsType results = data.Result;
                EnvelopePoints = results.data;
                left = results.speedBounds.left;
                right = results.speedBounds.right;
                bottom = results.altitudeBounds.bottom;
                top = results.altitudeBounds.top;
                UpdateGraphs();
            }
            Debug.Log("[KWT] Graphs updated - Envelope");
        }
        private void RethrowErrors(Task<ResultsType> task)
        {
            if (task.Status == TaskStatus.Faulted)
            {
                Debug.LogError("[KWT] Wind tunnel task faulted. (Env)");
                Debug.LogException(task.Exception);
            }
            else if (task.Status == TaskStatus.Canceled)
                Debug.Log("[KWT] Wind tunnel task was canceled. (Env)");
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
            if (EnvelopePoints == null)
                return;
            foreach (GraphDefinition graph in graphDefinitions.Where(g => g.Enabled))
            {
                if (graph is SurfGraphDefinition surfDefinition)
                    surfDefinition.UpdateGraph(left, right, bottom, top, EnvelopePoints);
                else if (graph is OutlineGraphDefinition<EnvelopePoint> outlineDefinition)
                    outlineDefinition.UpdateGraph(left, right, bottom, top, EnvelopePoints);
            }
        }

        public void CalculateOptimalLines(CancellationToken cancellationToken)
            => Task.Run(() => CalculateOptimalLinesTask((EnvelopePoints, (left, right), (bottom, top)), cancellationToken));

        private void CalculateOptimalLinesTask(ResultsType results, CancellationToken cancellationToken)
        {
            EnvelopePoint[,] data = results.data;
            
            (float speed, float altitude) initialCoords = ascentOrigin;
            (float speed, float altitude) exitCoords = (WindTunnelWindow.Instance.AscentTargetSpeed, WindTunnelWindow.Instance.AscentTargetAltitude);
            (float lower, float step, float upper) speedBounds = (results.speedBounds.left, (results.speedBounds.right - results.speedBounds.left) / (data.GetUpperBound(0) + 1), results.speedBounds.right);
            (float lower, float step, float upper) altitudeBounds = (results.altitudeBounds.bottom, (results.altitudeBounds.top - results.altitudeBounds.bottom) / (data.GetUpperBound(1) + 1), results.altitudeBounds.top);

            if (WindTunnelWindow.Instance.autoSetAscentTarget || exitCoords.speed < 0 || exitCoords.altitude < 0)
            {
                exitCoords = GetMaxSustainableEnergy(data, speedBounds, altitudeBounds);
                WindTunnelWindow.Instance.ProvideAscentTarget(exitCoords);
            }
            EnvelopeLine.CalculateOptimalLines(exitCoords, initialCoords, speedBounds, altitudeBounds, data, cancellationToken, fuelPath, timePath);
        }

        public static (float speed, float altitude) GetMaxSustainableEnergy(EnvelopePoint[,] data, (float lower, float step, float upper) speedBounds, (float lower, float step, float upper) altitudeBounds)
        {
            int speedLength = data.GetUpperBound(0);
            int altLength = data.GetUpperBound(1);
            (float speed, float altitude) result = ascentOrigin;
            int altIndex = 0;
            float g = (float)(WindTunnelWindow.Instance.CelestialBody.GeeASL * PhysicsGlobals.GravitationalAcceleration);
            for (int h = 0; h <= altLength; h++)
            {
                // Below this speed, the energy is less than the current result
                float Vmin = Mathf.Sqrt(2 * g * (altitudeBounds.step * (altIndex - h)) + result.speed * result.speed);

                for (int v = speedLength; v >= 0 && v * speedBounds.step + speedBounds.lower >= Vmin; v--)
                {
                    if (data[v, h].Thrust_Excess >= 0)
                    {
                        altIndex = h;
                        result = (speedBounds.step * v + speedBounds.lower, altitudeBounds.step * h + altitudeBounds.lower);
                        break;
                    }
                }
            }
            return result;
        }

        private readonly struct SurfCoords : IEquatable<SurfCoords>
        {
            public readonly int speed, altitude;

            public SurfCoords(float speed, float altitude)
            {
                this.speed = Mathf.RoundToInt(speed);
                this.altitude = Mathf.RoundToInt(altitude);
            }
            public SurfCoords(EnvelopePoint point) : this(point.speed, point.altitude) { }

            public override bool Equals(object obj)
            {
                if (obj is SurfCoords c)
                    return Equals(c);
                return false;
            }

            public bool Equals(SurfCoords obj)
            {
                return this.speed == obj.speed && this.altitude == obj.altitude;
            }

            public override int GetHashCode()
            {
                // I'm not expecting altitudes over 131 km or speeds over 8 km/s
                // (or negative values for either) so bit-shifting the values in this way
                // should equally weight the two inputs while returning a hash with the
                // same quality as the default uint/int hash
                // (which may or may not be just that number).
                // This means that there will be zero collisions within the expected range.
                return ((((uint)Mathf.RoundToInt(speed)) << 17) | (uint)Mathf.RoundToInt(altitude)).GetHashCode();
            }
        }

        public readonly struct ResultsType
        {
            public readonly EnvelopePoint[,] data;
            public readonly (float left, float right) speedBounds;
            public readonly (float bottom, float top) altitudeBounds;
            public ResultsType(EnvelopePoint[,] data, (float, float) speedBounds, (float, float) altitudeBounds)
            {
                this.data = data;
                this.speedBounds = speedBounds;
                this.altitudeBounds = altitudeBounds;
            }

            public static implicit operator (EnvelopePoint[,], (float, float), (float, float))(ResultsType obj) =>
                (obj.data, obj.speedBounds, obj.altitudeBounds);
            public static implicit operator ResultsType((EnvelopePoint[,] data, (float, float) speedBounds, (float, float) altitudeBounds) obj) =>
                new ResultsType(obj.data, obj.speedBounds, obj.altitudeBounds);
        }
    }
}
