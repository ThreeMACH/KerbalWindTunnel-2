using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Graphing;
using KerbalWindTunnel.Extensions;
using System.Collections.Concurrent;

namespace KerbalWindTunnel.DataGenerators
{
    public class EnvelopeSurf
    {
        public readonly GraphableCollection graphables = new GraphableCollection3();
        public EnvelopePoint[,] EnvelopePoints { get; private set; } = null;
        private float left, right, bottom, top;
        private float wingArea;
        private static readonly ConcurrentDictionary<SurfCoords, EnvelopePoint> cache = new ConcurrentDictionary<SurfCoords, EnvelopePoint>();

        public static (int x, int y)[] resolution = { (10, 10), (40, 120), (80, 180), (160, 360) };

        public EnvelopeSurf()
        {
            float bottom = 0, top = 0, left = 0, right = 0;
            float[,] blank = new float[0, 0];

            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Excess Thrust", ZUnit = "kN", StringFormat = "N0", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark, CMin = 0 });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Level AoA", ZUnit = "°", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Lift/Drag Ratio", ZUnit = "", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Thrust Available", ZUnit = "kN", StringFormat = "N0", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark, CMin = 0 });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Max Lift AoA", ZUnit = "°", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Max Lift", ZUnit = "kN", StringFormat = "N0", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Fuel Economy", ZUnit = "kg/100 km", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Fuel Burn Rate", ZUnit = "kg/s", StringFormat = "F3", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Drag", ZUnit = "kN", StringFormat = "N0", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Lift Slope", ZUnit = "/°", StringFormat = "F3", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Pitch Input", ZUnit = "", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Excess Acceleration", ZUnit = "g", StringFormat = "N2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark, CMin = 0 });
            //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Derivative", ZUnit = "kNm/deg", StringFormat = "F3", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Range", ZUnit = "deg", StringFormat = "F2", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            //graphables.Add(new SurfGraph(blank, left, right, bottom, top) { Name = "Stability Score", ZUnit = "kNm-deg", StringFormat = "F1", ColorScheme = Graphing.Extensions.GradientExtensions.Jet_Dark });
            graphables.Add(new OutlineMask(blank, left, right, bottom, top) { Name = "Envelope Mask", ZUnit = "kN", StringFormat = "N0", color = Color.gray, LineWidth = 2, LineOnly = true, MaskCriteria = (v) => !float.IsNaN(v.z) && !float.IsInfinity(v.z) ? v.z : 0 });
            graphables.Add(new MetaLineGraph(new Vector2[0])              { Name = "Fuel-Optimal Path", StringFormat = "N0", color = Color.black, LineWidth = 3, MetaFields = new string[] { "Climb Angle", "Climb Rate", "Fuel Used", "Time" }, MetaStringFormats = new string[] { "N1", "N0", "N3", "N1" }, MetaUnits = new string[] { "°", "m/s", "units", "s" } });
            graphables.Add(new MetaLineGraph(new Vector2[0])              { Name = "Time-Optimal Path", StringFormat = "N0", color = Color.white, LineWidth = 3, MetaFields = new string[] { "Climb Angle", "Climb Rate", "Time" }, MetaStringFormats = new string[] { "N1", "N0", "N1" }, MetaUnits = new string[] { "°", "m/s", "s" } });

            var e = graphables.GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.XUnit = "m/s";
                e.Current.XName = "Speed";
                e.Current.YUnit = "m";
                e.Current.YName = "Altitude";
                e.Current.Visible = false;
            }
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
            if (aeroPredictorToClone is VesselCache.SimulatedVessel simVessel)
                simVessel.InitMaxAoA(body, (upperBoundAltitude - lowerBoundAltitude) * 0.25f + lowerBoundAltitude);
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
            return (results.To2Dimension(speedSegments + 1), (lowerBoundSpeed, upperBoundSpeed), (lowerBoundAltitude, upperBoundAltitude), aeroPredictorToClone.Area);
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
                wingArea = results.wingArea;
                UpdateGraphs();
            }
        }
        private void RethrowErrors(Task<ResultsType> task)
        {
            if (task.Status == TaskStatus.Faulted)
            {
                Debug.LogError("Wind tunnel task faulted. (Vel)");
                Debug.LogException(task.Exception);
            }
            else if (task.Status == TaskStatus.Canceled)
                Debug.Log("Wind tunnel task was canceled. (Vel)");
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
            float invArea = 1f / wingArea;
            Func<EnvelopePoint, float> scale = (pt) => 1f;
            if (WindTunnelSettings.UseCoefficients)
            {
                scale = (pt) => 1f / pt.dynamicPressure * invArea;
                ((SurfGraph)graphables["Drag"]).ZUnit = "";
                ((SurfGraph)graphables["Drag"]).StringFormat = "F3";
                ((SurfGraph)graphables["Max Lift"]).ZUnit = "";
                ((SurfGraph)graphables["Max Lift"]).StringFormat = "F3";
            }
            else
            {
                ((SurfGraph)graphables["Drag"]).ZUnit = "kN";
                ((SurfGraph)graphables["Drag"]).StringFormat = "N0";
                ((SurfGraph)graphables["Max Lift"]).ZUnit = "kN";
                ((SurfGraph)graphables["Max Lift"]).StringFormat = "N0";
            }

            ((SurfGraph)graphables["Excess Thrust"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.Thrust_excess), left, right, bottom, top);
            ((SurfGraph)graphables["Excess Acceleration"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.Accel_excess), left, right, bottom, top);
            ((SurfGraph)graphables["Thrust Available"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.Thrust_available), left, right, bottom, top);
            ((SurfGraph)graphables["Level AoA"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.AoA_level * Mathf.Rad2Deg), left, right, bottom, top);
            ((SurfGraph)graphables["Max Lift AoA"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.AoA_max * Mathf.Rad2Deg), left, right, bottom, top);
            ((SurfGraph)graphables["Max Lift"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.Lift_max * scale(pt)), left, right, bottom, top);
            ((SurfGraph)graphables["Lift/Drag Ratio"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.LDRatio), left, right, bottom, top);
            ((SurfGraph)graphables["Drag"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.drag * scale(pt)), left, right, bottom, top);
            ((SurfGraph)graphables["Lift Slope"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.dLift / pt.dynamicPressure * invArea), left, right, bottom, top);
            ((SurfGraph)graphables["Pitch Input"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.pitchInput), left, right, bottom, top);
            ((SurfGraph)graphables["Fuel Burn Rate"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.fuelBurnRate), left, right, bottom, top);
            //((SurfGraph)graphables["Stability Derivative"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.stabilityDerivative), left, right, bottom, top);
            //((SurfGraph)graphables["Stability Range"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.stabilityRange), left, right, bottom, top);
            //((SurfGraph)graphables["Stability Score"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.stabilityScore), left, right, bottom, top);

            float[,] economy = EnvelopePoints.SelectToArray(pt => pt.fuelBurnRate / pt.speed * 1000 * 100);
            SurfGraph toModify = (SurfGraph)graphables["Fuel Economy"];
            toModify.SetValues(economy, left, right, bottom, top);

            try
            {
                int stallpt = EnvelopeLine.CoordLocator.GenerateCoordLocators(EnvelopePoints.SelectToArray(pt => pt.Thrust_excess)).First(0, 0, c => c.value >= 0);
                float minEconomy = economy[stallpt, 0] / 3;
                //toModify.ZMax = minEconomy;   // TODO: What was the intent here...
            }
            catch (InvalidOperationException)
            {
                Debug.LogError("The vessel cannot maintain flight at ground level. Fuel Economy graph will be weird.");
            }
            ((OutlineMask)graphables["Envelope Mask"]).SetValues(EnvelopePoints.SelectToArray(pt => pt.Thrust_excess), left, right, bottom, top);
        }

        public void CalculateOptimalLines(CancellationToken cancellationToken)
        {
            Task.Run(() => CalculateOptimalLinesTask((EnvelopePoints, (left, right), (bottom, top), wingArea), cancellationToken));
        }

        private void CalculateOptimalLinesTask(ResultsType results, CancellationToken cancellationToken)
        {
            EnvelopePoint[,] data = results.data;
            (float, float) initialCoords = (10, 0);
            (float, float) exitCoords = (WindTunnelWindow.Instance.AscentTargetSpeed, WindTunnelWindow.Instance.AscentTargetAltitude);
            (float, float, float) speedBounds = (results.speedBounds.left, (results.speedBounds.right - results.speedBounds.left) / (data.GetUpperBound(0) + 1), results.speedBounds.right);
            (float, float, float) altitudeBounds = (results.altitudeBounds.bottom, (results.altitudeBounds.top - results.altitudeBounds.bottom) / (data.GetUpperBound(1) + 1), results.altitudeBounds.top);
            EnvelopeLine.CalculateOptimalLines(exitCoords, initialCoords, speedBounds, altitudeBounds, data, cancellationToken, graphables);
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
            public readonly float wingArea;
            public ResultsType(EnvelopePoint[,] data, (float, float) speedBounds, (float, float) altitudeBounds, float wingArea)
            {
                this.data = data;
                this.speedBounds = speedBounds;
                this.altitudeBounds = altitudeBounds;
                this.wingArea = wingArea;
            }

            public static implicit operator (EnvelopePoint[,], (float, float), (float, float), float)(ResultsType obj) =>
                (obj.data, obj.speedBounds, obj.altitudeBounds, obj.wingArea);
            public static implicit operator ResultsType((EnvelopePoint[,] data, (float, float) speedBounds, (float, float) altitudeBounds, float wingArea) obj) =>
                new ResultsType(obj.data, obj.speedBounds, obj.altitudeBounds, obj.wingArea);
        }
    }
}
