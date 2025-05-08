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
    public class VelCurve
    {
        public readonly GraphableCollection graphables = new GraphableCollection();
        public EnvelopePoint[] VelPoints { get; private set; }
        private float wingArea;
        private static readonly ConcurrentDictionary<(int altitude, int velocity), EnvelopePoint> cache = new ConcurrentDictionary<(int, int), EnvelopePoint>();

        public VelCurve()
        {
            Vector2[] blank = new Vector2[0];
            graphables.Add(new LineGraph(blank) { Name = "Level AoA", YUnit = "°", StringFormat = "F2", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Max Lift AoA", YUnit = "°", StringFormat = "F2", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Lift/Drag Ratio", YUnit = "-", StringFormat = "F2", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Lift Slope", YUnit = "m^2/°", StringFormat = "F3", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Thrust Available", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Drag", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Excess Thrust", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Max Lift", YName = "Force", YUnit = "kN", StringFormat = "N0", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Excess Acceleration", YUnit = "g", StringFormat = "N2", color = Color.green });
            graphables.Add(new LineGraph(blank) { Name = "Pitch Input", YUnit = "", StringFormat = "F3", color = Color.green });

            foreach (var graph in graphables)
            {
                graph.XUnit = "m/s";
                graph.XName = "Airspeed";
                graph.Visible = false;
            }
        }

        public TaskProgressTracker Calculate(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float lowerBound, float upperBound, int segments = 50)
        {
            TaskProgressTracker tracker = new TaskProgressTracker();
            Task<ResultsType> task = Task.Run(() => CalculateTask(aeroPredictorToClone, cancellationToken, body, altitude, lowerBound, upperBound, segments, tracker), cancellationToken);
            tracker.Task = task;
            task.ContinueWith(PushResults, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(RethrowErrors, TaskContinuationOptions.NotOnRanToCompletion);
            return tracker;
        }

        private static ResultsType CalculateTask(AeroPredictor aeroPredictorToClone, CancellationToken cancellationToken, CelestialBody body, float altitude, float lowerBound, float upperBound, int segments = 50, TaskProgressTracker tracker = null)
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

            return (results, wingArea);
        }
        private void PushResults(Task<ResultsType> data)
        {
            lock (this)
            {
                ResultsType results = data.Result;
                VelPoints = results.data;
                wingArea = results.wingArea;
                UpdateGraphs();
            }
            Debug.Log("[KWT] Graphs updated - Velocity");
        }
        private void RethrowErrors(Task<ResultsType> task)
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
            Func<EnvelopePoint, float> scale = (pt) => 1;
            if (WindTunnelSettings.UseCoefficients)
                scale = (pt) => 1 / pt.dynamicPressure * wingArea;
            ((LineGraph)graphables["Level AoA"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.AoA_level * Mathf.Rad2Deg)).ToArray());
            ((LineGraph)graphables["Level AoA"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.AoA_level * Mathf.Rad2Deg)).ToArray());
            ((LineGraph)graphables["Max Lift AoA"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.AoA_max * Mathf.Rad2Deg)).ToArray());
            ((LineGraph)graphables["Thrust Available"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.thrust_available)).ToArray());
            ((LineGraph)graphables["Lift/Drag Ratio"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.LDRatio)).ToArray());
            ((LineGraph)graphables["Drag"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.drag * scale(p))).ToArray());
            ((LineGraph)graphables["Lift Slope"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.dLift / p.dynamicPressure * wingArea)).ToArray());
            ((LineGraph)graphables["Excess Thrust"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.Thrust_Excess)).ToArray());
            ((LineGraph)graphables["Pitch Input"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.pitchInput)).ToArray());
            ((LineGraph)graphables["Max Lift"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.lift_max * scale(p))).ToArray());
            ((LineGraph)graphables["Excess Acceleration"]).SetValues(VelPoints.Select(pt => ValuesFunc(pt, p => p.Accel_Excess)).ToArray());
        }
        private static Vector2 ValuesFunc(EnvelopePoint point, Func<EnvelopePoint, float> func)
            => new Vector2(point.speed, func(point));

        public readonly struct ResultsType
        {
            public readonly EnvelopePoint[] data;
            public readonly float wingArea;
            public ResultsType(EnvelopePoint[] data, float wingArea)
            {
                this.data = data;
                this.wingArea = wingArea;
            }
            public static implicit operator (EnvelopePoint[], float)(ResultsType obj) => (obj.data, obj.wingArea);
            public static implicit operator ResultsType((EnvelopePoint[] data, float wingArea) obj) => new ResultsType(obj.data, obj.wingArea);
        }
    }
}
