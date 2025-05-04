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
    public class AoACurve
    {
        public readonly GraphableCollection graphables = new GraphableCollection();
        public AoAPoint[] AoAPoints { get; private set; }
        private float wingArea;
        public float AverageLiftSlope { get; private set; }
        private static readonly ConcurrentDictionary<(int altitude, int velocity, float aoa), AoAPoint> cache = new ConcurrentDictionary<(int, int, float), AoAPoint>();

        public AoACurve()
        {
            Vector2[] blank = new Vector2[0];
            graphables.Add(new LineGraph(blank) { Name = "Lift", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Drag", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Lift/Drag Ratio", YUnit = "", StringFormat = "F2", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Lift Slope", YUnit = "/°", StringFormat = "F3", color = Color.green });
            IGraphable[] pitch = new IGraphable[] {
                new LineGraph(blank) { Name = "Pitch Input (Wet)", YUnit = "", StringFormat = "F2", color = Color.green },
                new LineGraph(blank) { Name = "Pitch Input (Dry)", YUnit = "", StringFormat = "F2", color = Color.yellow }
            };
            //graphables.Add(pitch[0]);
            //graphables.Add(pitch[1]);
            graphables.Add(new GraphableCollection(pitch) { Name = "Pitch Input" });
            IGraphable[] torque = new IGraphable[] {
                new LineGraph(blank) { Name = "Torque (Wet)", YUnit = "kNm", StringFormat = "N0", color = Color.green },
                new LineGraph(blank) { Name = "Torque (Dry)", YUnit = "kNm", StringFormat = "N0", color = Color.yellow }
            };
            //graphables.Add(torque[0]);
            //graphables.Add(torque[1]);
            graphables.Add(new GraphableCollection(torque) { Name = "Torque" });

            foreach (var graph in graphables)
            {
                graph.XUnit = "°";
                graph.XName = "Angle of Attack";
                graph.Visible = false;
            }
        }

        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float velocity, float lowerBound, float upperBound, int segments = 50)
        {
            TaskProgressTracker tracker = new TaskProgressTracker();
            Task < ResultsType > task = Task.Run(() => CalculateTask(aeroPredictorToClone, cancellationToken, body, altitude, velocity, lowerBound, upperBound, segments, tracker), cancellationToken);
            tracker.Task = task;
            task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
            return tracker;
        }

        private static ResultsType CalculateTask(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float velocity, float lowerBound, float upperBound, int segments = 50, TaskProgressTracker tracker = null)
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

            return (results, wingArea);
        }
        private void PushResults(Task<ResultsType> data)
        {
            lock (this)
            {
                ResultsType results = data.Result;
                AoAPoints = results.data;
                wingArea = results.wingArea;
                UpdateGraphs();
            }
            Debug.Log("[KWT] Graphs updated - AoA");
        }
        private void RethrowErrors(Task<ResultsType> task)
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
            AverageLiftSlope = AoAPoints.Select(pt => pt.dLift / pt.dynamicPressure).Where(v => !float.IsNaN(v) && !float.IsInfinity(v)).Average();

            Func<AoAPoint, float> scale = (pt) => 1;
            float invArea = 1f / wingArea;
            if (WindTunnelSettings.UseCoefficients)
            {
                scale = (pt) => 1 / pt.dynamicPressure * invArea;
                graphables["Lift"].YName = graphables["Drag"].YName = "Coefficient";
                graphables["Lift"].YUnit = graphables["Drag"].YUnit = "";
                graphables["Lift"].DisplayName = "Lift Coefficient";
                graphables["Drag"].DisplayName = "Drag Coefficient";
                ((LineGraph)graphables["Lift"]).StringFormat = ((LineGraph)graphables["Drag"]).StringFormat = "N2";
            }
            else
            {
                graphables["Lift"].YName = graphables["Drag"].YName = "Force";
                graphables["Lift"].YUnit = graphables["Drag"].YUnit = "kN";
                graphables["Lift"].DisplayName = "Lift";
                graphables["Drag"].DisplayName = "Drag";
                ((LineGraph)graphables["Lift"]).StringFormat = ((LineGraph)graphables["Drag"]).StringFormat = "N0";
            }
            ((LineGraph)graphables["Lift"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.Lift * scale(p))).ToArray());
            ((LineGraph)graphables["Drag"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.Drag * scale(p))).ToArray());
            ((LineGraph)graphables["Lift/Drag Ratio"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.LDRatio)).ToArray());
            ((LineGraph)graphables["Lift Slope"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.dLift / p.dynamicPressure * invArea)).ToArray());
            ((LineGraph)((GraphableCollection)graphables["Pitch Input"])["Pitch Input (Wet)"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.pitchInput)).ToArray());
            ((LineGraph)((GraphableCollection)graphables["Pitch Input"])["Pitch Input (Dry)"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.pitchInput_dry)).ToArray());
            ((LineGraph)((GraphableCollection)graphables["Torque"])["Torque (Wet)"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.torque)).ToArray());
            ((LineGraph)((GraphableCollection)graphables["Torque"])["Torque (Dry)"]).SetValues(AoAPoints.Select(pt => ValuesFunc(pt, p => p.torque_dry)).ToArray());
        }
        private static Vector2 ValuesFunc(AoAPoint point, Func<AoAPoint, float> func)
            => new Vector2(point.AoA * Mathf.Rad2Deg, func(point));

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
            public readonly float torque;
            public readonly float torque_dry;
            public readonly bool completed;
            private readonly float area;

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
                area = vessel.Area;
                completed = true;
            }

            public override string ToString()
            {
                if (WindTunnelSettings.UseCoefficients)
                {
                    float coefMod = 1f / dynamicPressure / area;
                    return String.Format("Altitude:\t{0:N0}m\n" + "Speed:\t{1:N0}m/s\n" + "Mach:\t{6:N2}\n" + "AoA:\t{2:N2}°\n" +
                        "Lift Coefficient:\t{3:N2}\n" + "Drag Coefficient:\t{4:N2}\n" + "Lift/Drag Ratio:\t{5:N2}\n" + "Pitch Input:\t{7:F3}\n" + 
                        "Wing Area:\t{8:F2}",
                        altitude, speed, AoA * Mathf.Rad2Deg,
                        Lift * coefMod, Drag * coefMod, LDRatio, mach, pitchInput,
                        area);
                }
                else
                    return String.Format("Altitude:\t{0:N0}m\n" + "Speed:\t{1:N0}m/s\n" + "Mach:\t{6:N2}\n" + "AoA:\t{2:N2}°\n" +
                            "Lift:\t{3:N0}kN\n" + "Drag:\t{4:N0}kN\n" + "Lift/Drag Ratio:\t{5:N2}\n" + "Pitch Input:\t{7:F3}",
                            altitude, speed, AoA * Mathf.Rad2Deg,
                            Lift, Drag, LDRatio, mach, pitchInput);
            }
        }

        public readonly struct ResultsType
        {
            public readonly AoAPoint[] data;
            public readonly float wingArea;
            public ResultsType(AoAPoint[] data, float wingArea)
            {
                this.data = data;
                this.wingArea = wingArea;
            }
            public static implicit operator (AoAPoint[], float)(ResultsType obj) => (obj.data, obj.wingArea);
            public static implicit operator ResultsType((AoAPoint[] data, float wingArea) obj) => new ResultsType(obj.data, obj.wingArea);
        }
    }
}
