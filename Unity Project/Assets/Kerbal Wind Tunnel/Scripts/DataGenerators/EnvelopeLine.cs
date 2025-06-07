using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace KerbalWindTunnel.DataGenerators
{
    public static class EnvelopeLine
    {
        public static async Task CalculateOptimalLines((float speed, float altitude) exitCoords, (float speed, float altitude) initialCoords, (float lower, float step, float upper) speedBounds, (float lower, float step, float upper) altitudeBounds, EnvelopePoint[,] dataArray, CancellationToken cancellationToken, MetaLineGraphDefinition<AscentPathPoint> fuelPath, MetaLineGraphDefinition<AscentPathPoint> timePath)
        {
            Task fuelTask = ProcessOptimalLine(fuelPath, exitCoords, initialCoords, speedBounds, altitudeBounds, fuelToClimb, dataArray, timeToClimb, cancellationToken);
            Task timeTask = ProcessOptimalLine(timePath, exitCoords, initialCoords, speedBounds, altitudeBounds, timeToClimb, dataArray, timeToClimb, cancellationToken);
            await Task.WhenAll(fuelTask, timeTask);
            return;

            // Local methods to keep code clean:
            float timeToClimb(in PathSolverPoint next, in PathSolverPoint current)
            {
                ref EnvelopePoint currentPoint = ref dataArray[next.xi, next.yi];
                ref EnvelopePoint lastPoint = ref dataArray[current.xi, current.yi];
                float power = currentPoint.Specific(currentPoint.Power_Excess) + lastPoint.Specific(lastPoint.Power_Excess);
                if (power == 0)
                    return float.MaxValue;
                float travelTime = Mathf.Abs(2 * (currentPoint.altitude - lastPoint.altitude) / (currentPoint.speed + lastPoint.speed));
                return Mathf.Max(2 * (currentPoint.EnergyHeight - lastPoint.EnergyHeight) / power, travelTime);  // Time cannot be negative, and we assume instantaneous zoom.
            }
            float fuelToClimb(in PathSolverPoint next, in PathSolverPoint current)
            {
                return timeToClimb(next, current) * (dataArray[next.xi, next.yi].fuelBurnRate + dataArray[current.xi, current.yi].fuelBurnRate) * 0.5f;
            }
        }

        private static async Task ProcessOptimalLine(MetaLineGraphDefinition<AscentPathPoint> graphDef, (float speed, float altitude) exitCoords, (float speed, float altitude) initialCoords, (float lower, float step, float upper) speedBounds, (float lower, float step, float upper) altitudeBounds, CostIncreaseFunction costIncreaseFunc, EnvelopePoint[,] data, CostIncreaseFunction timeDifferenceFunc, CancellationToken cancellationToken)
        {
            Task<IList<AscentPathPoint>> task = Task.Run(() => GetOptimalPath(exitCoords, initialCoords, speedBounds, altitudeBounds, costIncreaseFunc, data, timeDifferenceFunc, cancellationToken));
            try
            {
                await task;
            }
            catch (OperationCanceledException) { return; }
            graphDef.UpdateGraph(task.Result);
        }

        private delegate float CostIncreaseFunction(in PathSolverPoint next, in PathSolverPoint current);

        public readonly struct AscentPathPoint
        {
            public readonly float altitude;
            public readonly float speed;
            public readonly float cost;
            public readonly float climbAngle;
            public readonly float climbRate;
            public readonly float time;
            public AscentPathPoint(PathSolverPoint current, EnvelopePoint[,] data)
            {
                ref EnvelopePoint currentPoint = ref data[current.xi, current.yi];

                speed = currentPoint.speed;
                altitude = currentPoint.altitude;
                cost = current.cost;
                time = current.time;
                if (time == 0 || current.Previous == null)
                {
                    climbRate = climbAngle = 0;
                }
                else
                {
                    float prevTime = current.Previous.time;
                    float prevAlt = data[current.Previous.xi, current.Previous.yi].altitude;
                    climbRate = Mathf.Min((altitude - prevAlt) / (time - prevTime), speed);
                    climbAngle = Mathf.Asin(climbRate / speed);
                }
            }
        }
        public readonly struct CoordLocator
        {
            public readonly int x;
            public readonly int y;
            public readonly float value;
            public CoordLocator(int x, int y, float value)
            {
                this.x = x; this.y = y; this.value = value;
            }
            public static CoordLocator[,] GenerateCoordLocators(float[,] values)
            {
                int width = values.GetUpperBound(0);
                int height = values.GetUpperBound(1);
                CoordLocator[,] coordLocators = new CoordLocator[width + 1, height + 1];
                for (int i = 0; i <= width; i++)
                    for (int j = 0; j <= height; j++)
                        coordLocators[i, j] = new CoordLocator(i, j, values[i, j]);
                return coordLocators;
            }
        }

        private static IList<AscentPathPoint> GetOptimalPath((float speed, float altitude) exitCoords, (float speed, float altitude) initialCoords, (float lower, float step, float upper) speedBounds, (float lower, float step, float upper) altitudeBounds, CostIncreaseFunction costIncreaseFunc, EnvelopePoint[,] data, CostIncreaseFunction timeFunc, CancellationToken cancellationToken)
        {
            const int decimation = 5;
            bool costIsTime = costIncreaseFunc == timeFunc;
            BoundsInfo bounds = new BoundsInfo(speedBounds, altitudeBounds);

            Priority_Queue.FastPriorityQueue<PathSolverPoint> queue = new Priority_Queue.FastPriorityQueue<PathSolverPoint>(4 * Mathf.CeilToInt((bounds.speedUpperBound - bounds.speedLowerBound) / bounds.speedStep + (bounds.altitudeUpperBound - bounds.altitudeLowerBound) / bounds.altitudeStep));
            PathSolverPoint[,] network = new PathSolverPoint[bounds.width + 1, bounds.height + 1];

            PathSolverPoint origin = FindValidPoint(PathSolverPoint.Nearest(initialCoords.speed, initialCoords.altitude, bounds), data, bounds);
            PathSolverPoint target = FindValidPoint(PathSolverPoint.Nearest(exitCoords.speed, exitCoords.altitude, bounds), data, bounds);

            if (origin == null)
                throw new Exception("Vessel cannot maintain flight at or below the specified origin altitude.");

            origin.cost = 0;
            origin.time = 0;
            origin.Previous = null;
            network[origin.xi, origin.yi] = origin;
            network[target.xi, target.yi] = target;

            // Implement Dijkstra's Algorithm to find the time to climb to any given node, stopping at the target node.
            // TODO: Consider implementing specific operations in negative Ps areas (moving to a point with lower energy)
            queue.Enqueue(origin, 0);

            while (queue.Count > 0)
            {
                PathSolverPoint current = queue.Dequeue();
                if (current == target)
                    break;
                foreach ((int xi, int yi) in current.GetNeighbors(data))
                {
                    PathSolverPoint neighbor;
                    if (network[xi, yi] != null)
                    {
                        neighbor = network[xi, yi];
                        if (current.cost > neighbor.cost)
                            continue;
                        float cost = current.cost + costIncreaseFunc(neighbor, current);
                        if (cost < neighbor.cost)
                        {
                            neighbor.cost = cost;
                            if (costIsTime)
                                neighbor.time = neighbor.cost;
                            else
                                neighbor.time = current.time + timeFunc(neighbor, current);
                            neighbor.Previous = current;
                            if (queue.Contains(neighbor))
                                queue.UpdatePriority(neighbor, neighbor.cost);
                            else
                                queue.Enqueue(neighbor, neighbor.cost);
                        }
                    }
                    else
                    {
                        neighbor = network[xi, yi] = new PathSolverPoint(xi, yi, bounds);
                        neighbor.cost = current.cost + costIncreaseFunc(neighbor, current);
                        if (costIsTime)
                            neighbor.time = neighbor.cost;
                        else
                            neighbor.time = current.time + timeFunc(neighbor, current);
                        neighbor.Previous = current;
                        queue.Enqueue(neighbor, neighbor.cost);
                    }
                }
            }

            /*new Graphing.SurfGraph(network.SelectToArray(p => p?.cost ?? float.MaxValue), bounds.speedLowerBound, bounds.speedUpperBound, bounds.altitudeLowerBound, bounds.altitudeUpperBound).
                WriteToFile(WindTunnel.graphPath, "costMatrix2");*/

            // If the target node could not be reached, no path is generated.
            if (target.Previous == null)
            {
                Debug.LogWarning($"[KWT] The target point could not be reached. {target.xi * bounds.speedStep + bounds.speedLowerBound}m/s at {target.yi * bounds.altitudeStep + bounds.altitudeLowerBound}m");
                return null;
            }

            // Convert the path into a data structure appropriate for graphing.
            // Also decimate it to mitigate the stairstep appearance.
            AscentPathPoint[] result = new AscentPathPoint[Mathf.CeilToInt(target.Index / decimation) + 1];
            PathSolverPoint point = target;
            while (target != origin && target != null)
            {
                int i = decimation;
                while (i > 0 && target.Previous != null)
                {
                    i--;
                    target = target.Previous;
                }
                point.Previous = target;
                int index = Mathf.CeilToInt(point.Index / decimation) + 1;
                result[index] = new AscentPathPoint(point, data);
                point = target;
            }
            result[0] = new AscentPathPoint(origin, data);

            result.Reverse();
            return result;
        }
        private static PathSolverPoint FindValidPoint((int xi, int yi) target, EnvelopePoint[,] data, BoundsInfo bounds)
        {
            int xi = target.xi, yi = target.yi;
            int upperBound = bounds.width;
            if (yi == 0 && data[xi, yi].Thrust_Excess < 0)
                xi = upperBound;
            while (data[xi, yi].Thrust_Excess < 0)
            {
                xi -= 1;
                if (xi < 0)
                {
                    yi -= 1;
                    if (yi < 0)
                        return null;
                    xi = upperBound;
                }
            }
            return new PathSolverPoint(xi, yi, bounds);
        }
        public class BoundsInfo
        {
            public readonly float speedLowerBound;
            public readonly float speedUpperBound;
            public readonly float speedStep;
            public readonly float altitudeLowerBound;
            public readonly float altitudeUpperBound;
            public readonly float altitudeStep;
            public readonly int width, height;
            public BoundsInfo((float lower, float step, float upper) speedBounds, (float lower, float step, float upper) altitudeBounds) :
                this(speedBounds.lower, speedBounds.upper, speedBounds.step, altitudeBounds.lower, altitudeBounds.upper, altitudeBounds.step)
            { }
            public BoundsInfo(float speedLowerBound, float speedUpperBound, float speedStep, float altitudeLowerBound, float altitudeUpperBound, float altitudeStep)
            {
                this.speedLowerBound = speedLowerBound;
                this.speedUpperBound = speedUpperBound;
                this.speedStep = speedStep;
                this.altitudeLowerBound = altitudeLowerBound;
                this.altitudeUpperBound = altitudeUpperBound;
                this.altitudeStep = altitudeStep;
                width = Mathf.CeilToInt((speedUpperBound - speedLowerBound) / speedStep);
                height = Mathf.CeilToInt((altitudeUpperBound - altitudeLowerBound) / altitudeStep);
            }
        }

        public class PathSolverPoint : Priority_Queue.FastPriorityQueueNode
        {
            public readonly BoundsInfo bounds;
            public readonly int xi, yi;
            public float cost = float.MaxValue;
            public float time = float.MaxValue;
            private PathSolverPoint previous;
            public PathSolverPoint Previous
            {
                get => previous;
                set
                {
                    previous = value;
                    Index = previous?.Index + 1 ?? 0;
                }
            }
            public int Index { get; private set; } = -1;
            public PathSolverPoint(int xi, int yi, BoundsInfo bounds)
            {
                this.xi = xi;
                this.yi = yi;
                this.bounds = bounds;
            }
            public static (int xi, int yi) Nearest(float speed, float altitude, BoundsInfo bounds)
            {
                if (bounds == null)
                    throw new ArgumentNullException(nameof(bounds));
                if (speed < bounds.speedLowerBound || speed > bounds.speedUpperBound)
                    throw new ArgumentOutOfRangeException(nameof(speed));
                if (altitude < bounds.altitudeLowerBound || altitude > bounds.altitudeUpperBound)
                    throw new ArgumentOutOfRangeException(nameof(altitude));
                return (Mathf.RoundToInt((speed - bounds.speedLowerBound) / bounds.speedStep), Mathf.RoundToInt((altitude - bounds.altitudeLowerBound) / bounds.altitudeStep));
            }

            public IEnumerable<(int xi, int yi)> GetNeighbors()
            {
                bool openLeft = xi > 0;
                bool openBelow = yi > 0;
                bool openRight = xi < bounds.width - 1;
                bool openAbove = yi < bounds.height - 1;
                if (openLeft)
                    yield return (xi - 1, yi);
                if (openLeft && openBelow)
                    yield return (xi - 1, yi - 1);
                if (openBelow)
                    yield return (xi, yi - 1);
                if (openBelow && openRight)
                    yield return (xi + 1, yi - 1);
                if (openRight)
                    yield return (xi + 1, yi);
                if (openRight && openAbove)
                    yield return (xi + 1, yi + 1);
                if (openAbove)
                    yield return (xi, yi + 1);
                if (openAbove && openLeft)
                    yield return (xi - 1, yi + 1);
            }
            public IEnumerable<(int xi, int yi)> GetNeighbors(EnvelopePoint[,] data)
            {
#if OUTSIDE_UNITY
                static
#endif
                bool Positive(EnvelopePoint val) => val.Thrust_Excess > 0;
                bool openLeft = xi > 0;
                bool openBelow = yi > 0;
                bool openRight = xi < bounds.width - 1;
                bool openAbove = yi < bounds.height - 1;
                if (openLeft && Positive(data[xi - 1, yi]))
                    yield return (xi - 1, yi);
                if (openLeft && openBelow && Positive(data[xi - 1, yi - 1]))
                    yield return (xi - 1, yi - 1);
                if (openBelow && Positive(data[xi, yi - 1]))
                    yield return (xi, yi - 1);
                if (openBelow && openRight && Positive(data[xi + 1, yi - 1]))
                    yield return (xi + 1, yi - 1);
                if (openRight && Positive(data[xi + 1, yi]))
                    yield return (xi + 1, yi);
                if (openRight && openAbove && Positive(data[xi + 1, yi + 1]))
                    yield return (xi + 1, yi + 1);
                if (openAbove && Positive(data[xi, yi + 1]))
                    yield return (xi, yi + 1);
                if (openAbove && openLeft && Positive(data[xi - 1, yi + 1]))
                    yield return (xi - 1, yi + 1);
            }
        }
    }
}
