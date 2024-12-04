using KerbalWindTunnel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalWindTunnel.VesselCache
{
    public static class CharacterizationExtensions
    {
        public static IEnumerable<float> RoundFloat(IEnumerable<float> values)
            => values.Select(RoundFloat);
        public static IEnumerable<(float, bool)> RoundFloatTuple(IEnumerable<(float, bool)> values)
            => values.Select(RoundFloatTuple);
        public static float RoundFloat(float value) => (float)Math.Round(value, CharacterizedVessel.tolerance);
        public static (float, bool) RoundFloatTuple((float, bool) value) =>
            ((float)Math.Round(value.Item1, CharacterizedVessel.tolerance), value.Item2);
        public static void AndSortedSet<T>(this SortedSet<(T, bool b)> values) =>
            values.RemoveWhere(v => v.b == true && values.Contains((v.Item1, false)));

        public static IEnumerable<(float AoA, bool continuousDerivative)> GetWorldKeys(Vector3 norm, FloatCurve aoaCurve)
        {
            if (norm.x == 0 && norm.z == 0)
                yield break;

            if (norm.sqrMagnitude > 1)
                norm.Normalize();

            float upperLimit = Mathf.Sqrt(norm.x * norm.x + norm.z * norm.z);
            if (upperLimit < 1E-10f)
                upperLimit = 0;

            foreach (Keyframe key in aoaCurve.Curve.keys)
            {
                bool continuousDerivative;
                if (key.time == aoaCurve.minTime)
                    continuousDerivative = key.outTangent == 0;
                else if (key.time == aoaCurve.maxTime)
                    continuousDerivative = key.inTangent == 0;
                else
                    continuousDerivative = key.outTangent == key.inTangent;

                if (key.time <= upperLimit)
                {
                    foreach (float AoA in SinAoAMapping(norm, key.time))
                        yield return (AoA, continuousDerivative);
                }
                else
                {
                    continuousDerivative = aoaCurve.EvaluateDerivative(upperLimit) == 0;
                    foreach (float AoA in SinAoAMapping(norm, upperLimit))
                        yield return (AoA, continuousDerivative);
                    yield break;
                }
            }
        }
        public static IEnumerable<float> GetWorldKeys(Vector3 norm, IEnumerable<float> sinAoAs)
        {
            if (norm.x == 0 && norm.z == 0)
                yield break;

            if (norm.sqrMagnitude > 1)
                norm.Normalize();

            float upperLimit = Mathf.Sqrt(norm.x * norm.x + norm.z * norm.z);
            if (upperLimit < 1E-10f)
                upperLimit = 0;

            foreach (float sinAoA in sinAoAs)
            {
                if (sinAoA <= upperLimit)
                {
                    foreach (float AoA in SinAoAMapping(norm, sinAoA))
                        yield return AoA;
                }
                else
                {
                    foreach (float AoA in SinAoAMapping(norm, upperLimit))
                        yield return AoA;
                    yield break;
                }
            }
        }

        public static IEnumerable<float> SinAoAMapping(Vector3 norm, float localSinAoA)
        {
            //return (norm.z * key.time - Math.Sign(norm.x) * Mathf.Sqrt(Math.Max(x2 * (x2 - key.time * key.time + z2), 0))) / (x2 + z2);
            float x = norm.z, z = norm.y;

            float radical = Math.Max(x * x + z * z - localSinAoA * localSinAoA, 0);
            if (radical == 0)
            {
                yield return Mathf.Atan2(z * localSinAoA, x * localSinAoA);
                yield return Mathf.Atan2(-z * localSinAoA, -x * localSinAoA);
                yield return Mathf.Atan2(z * localSinAoA, -x * localSinAoA);
                yield break;
            }
            radical = Mathf.Sqrt(Math.Max(x * x + z * z - localSinAoA * localSinAoA, 0));

            float xa = x * localSinAoA, za = z * localSinAoA;
            float xRadical = x * radical, zRadical = z * radical;

            float InnerMap(short AoASign, short quadrantSign)
                => Mathf.Atan2(
                    quadrantSign * (za * AoASign - xRadical),
                    quadrantSign * (xa * AoASign + zRadical));
            yield return InnerMap(1, 1);
            yield return InnerMap(-1, 1);
            yield return InnerMap(1, -1);
            yield return InnerMap(-1, -1);
        }
    }
}
