using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalWindTunnel.Extensions
{
    public static class KSPClassExtensions
    {
        /*
        From Trajectories
        Copyright 2014, Youen Toupin
        This method is part of Trajectories, under MIT license.
        StockAeroUtil by atomicfury
        */
        /// <summary>
        /// Gets the air density (rho) for the specified altitude on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature, require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// From StockAeroUtil.cs from Trajectories
        /// </summary>
        /// <param name="body"></param>
        /// <param name="altitude">Altitude above sea level (in meters)</param>
        /// <returns></returns>
        public static double GetDensity(this CelestialBody body, double altitude)
        {
            if (!body.atmosphere)
                return 0;

            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double sunDot = 0.5;
            float sunAxialDot = 0;
            double atmosphereTemperatureOffset = (double)body.latitudeTemperatureBiasCurve.Evaluate(0)
                + (double)body.latitudeTemperatureSunMultCurve.Evaluate(0) * sunDot
                + (double)body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            double temperature = // body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
                body.GetTemperature(altitude)
                + (double)body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;


            return body.GetDensity(pressure, temperature);
        }

        public static FloatCurve MakeFloatCurve(IEnumerable<float> keys, Func<float, float> func, float delta = 0.000001f)
            => MakeFloatCurve(keys.Select(k => (k, false)), func, delta);
        public static FloatCurve MakeFloatCurve(IEnumerable<(float value, bool continuousDerivative)> keys, Func<float, float> func, float delta = 0.000001f)
        {
#if DEBUG
            bool comma = false;
            UnityEngine.Debug.Log("Making FloatCurve:");
            string keyStr = "Keys: ";
            string valStr = "Values: ";
            foreach (var (key, cd) in keys)
            {
                if (comma)
                {
                    keyStr += ", ";
                    valStr += ", ";
                }
                keyStr += key;
                valStr += func(key);
                comma = true;
            }
            UnityEngine.Debug.Log(keyStr);
            UnityEngine.Debug.Log(valStr);
#endif
            float invDelta = 1 / delta;

            FloatCurve curve = new FloatCurve();

            foreach (var (key, continuousDerivative) in keys)
            {
                float value = func(key);
                float inTangent, outTangent;
                inTangent = -(func(key - delta) - value) * invDelta;
                outTangent = continuousDerivative ? inTangent : (func(key + delta) - value) * invDelta;
                curve.Add(key, value, inTangent, outTangent);
            }
            curve.Curve.keys[0].inTangent = 0;
            curve.Curve.keys[curve.Curve.keys.Length - 1].outTangent = 0;
            return curve;
        }

        public static FloatCurve Clone(this FloatCurve inCurve)
            => new FloatCurve(inCurve.Curve.keys);

        public static IEnumerable<float> ExtractTimes(this FloatCurve curve)
            => curve.Curve.keys.Select(k => k.time);

        public static float EvaluateDerivative(this FloatCurve curve, float time)
        {
            if (time <= curve.minTime || time >= curve.maxTime)
                return 0;
            UnityEngine.Keyframe keyframe0 = curve.Curve.keys.Last(k => k.time < time);
            UnityEngine.Keyframe keyframe1 = curve.Curve.keys[curve.Curve.keys.IndexOf(keyframe0) + 1];

            float dt = keyframe1.time - keyframe0.time;
            float t = (time - keyframe0.time) / dt;

            float m0 = keyframe0.outTangent * dt;
            float m1 = keyframe1.inTangent * dt;

            float t2 = t * t;
            float t3 = t2 * t;

            float a = 2 * t3 - 3 * t2 + 1;
            float b = t3 - 2 * t2 + t;
            float c = t3 - t2;
            float d = -2 * t3 + 3 * t2;

            return a * keyframe0.value + b * m0 + c * m1 + d * keyframe1.value;
        }

        public static UnityEngine.Vector3 ProjectOnPlaneSafe(UnityEngine.Vector3 vector, UnityEngine.Vector3 planeNormal)
        {
            return vector - UnityEngine.Vector3.Dot(vector, planeNormal) / planeNormal.sqrMagnitude * planeNormal;
        }
    }

    public class FloatCurveComparer : IEqualityComparer<FloatCurve>
    {
        public static FloatCurveComparer Instance = new FloatCurveComparer();

        public int GetHashCode(FloatCurve curve)
        {
            if (curve == null)
                return HashCode.Combine(curve);
            UnityEngine.AnimationCurve ac = curve.Curve;
            HashCode h = new HashCode();
            h.Add(ac.length);
            foreach (UnityEngine.Keyframe k in ac.keys)
            {
                h.Add(k.time);
                h.Add(k.value);
                h.Add(k.inTangent);
                h.Add(k.outTangent);
            }
            return h.ToHashCode();
        }

        public bool Equals(FloatCurve curve1, FloatCurve curve2)
        {
            if (curve1 == null && curve2 == null)
                return true;
            if (curve1 == null || curve2 == null)
                return false;
            if (ReferenceEquals(curve1, curve2))
                return true;
            if (curve1.Curve.length != curve2.Curve.length)
                return false;
            UnityEngine.AnimationCurve ac1 = curve1.Curve, ac2 = curve2.Curve;
            for (int i = ac1.length - 1; i >= 0; i--)
            {
                UnityEngine.Keyframe k1 = ac1.keys[i], k2 = ac2.keys[i];
                if (k1.time != k2.time ||
                    k1.value != k2.value ||
                    k1.inTangent != k2.inTangent ||
                    k1.outTangent != k2.outTangent)
                    return false;
            }
            return true;
        }
    }

    public class PartDragCurveComparer : IEqualityComparer<DragCubeList>
    {
        public static PartDragCurveComparer Instance = new PartDragCurveComparer();

        public bool Equals(DragCubeList cubes1, DragCubeList cubes2)
        {
            FloatCurveComparer curveComparer = FloatCurveComparer.Instance;

            PhysicsGlobals.SurfaceCurvesList surfCurvesList1 = cubes1.SurfaceCurves, surfCurvesList2 = cubes2.SurfaceCurves;
            if (!curveComparer.Equals(surfCurvesList1.dragCurveTip, surfCurvesList2.dragCurveTip))
                return false;
            if (!curveComparer.Equals(surfCurvesList1.dragCurveSurface, surfCurvesList2.dragCurveSurface))
                return false;
            if (!curveComparer.Equals(surfCurvesList1.dragCurveTail, surfCurvesList2.dragCurveTail))
                return false;
            if (!curveComparer.Equals(surfCurvesList1.dragCurveMultiplier, surfCurvesList2.dragCurveMultiplier))
                return false;

            FloatCurve dragMult1 = cubes1.DragCurveMultiplier, dragMult2 = cubes2.DragCurveMultiplier;
            if (!curveComparer.Equals(dragMult1, dragMult2))
                return false;

            FloatCurve cdPower1 = cubes1.DragCurveCdPower, cdPower2 = cubes2.DragCurveCdPower;
            if (!curveComparer.Equals(cdPower1, cdPower2))
                return false;

            return true;
        }

        public int GetHashCode(DragCubeList cubes)
        {
            HashCode hashCode = new HashCode();
            FloatCurveComparer curveComparer = FloatCurveComparer.Instance;
            PhysicsGlobals.SurfaceCurvesList surfCurvesList = cubes.SurfaceCurves;

            hashCode.Add(surfCurvesList.dragCurveTip, curveComparer);
            hashCode.Add(surfCurvesList.dragCurveSurface, curveComparer);
            hashCode.Add(surfCurvesList.dragCurveTail, curveComparer);
            hashCode.Add(surfCurvesList.dragCurveMultiplier, curveComparer);
            if (!curveComparer.Equals(surfCurvesList.dragCurveMultiplier, cubes.DragCurveMultiplier))
                hashCode.Add(cubes.DragCurveMultiplier);
            hashCode.Add(cubes.DragCurveCdPower);

            return hashCode.ToHashCode();
        }
    }
}
