using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalWindTunnel.Extensions;

namespace KerbalWindTunnel.VesselCache
{
    public class CharacterizedVessel : AeroPredictor, ILiftAoADerivativePredictor
    {
        public readonly SimulatedVessel vessel;

        public override float Mass => vessel.Mass;

        public override bool ThrustIsConstantWithAoA => vessel.ThrustIsConstantWithAoA;

        public override float Area => vessel.Area;

        public int tolerance = 8;

        public List<(FloatCurve machCurve, FloatCurve liftCurve)> surfaceLift = new List<(FloatCurve machCurve, FloatCurve liftCurve)>();
        public List<(FloatCurve machCurve, FloatCurve dragCurve)> surfaceDrag = new List<(FloatCurve machCurve, FloatCurve dragCurve)>();
        public List<(FloatCurve machCurve, FloatCurve liftCurve)> bodyLift = new List<(FloatCurve machCurve, FloatCurve liftCurve)>();
        public FloatCurve2 bodyDrag;

        private static IComparer<(float, bool)> FloatTupleComparer = Comparer<(float, bool)>.Create((x, y) =>
            x.Item1 < y.Item1 ? -1 :
            x.Item1 > y.Item1 ? 1 :
            x.Item2 == y.Item2 ? 0 :
            x.Item2 ? 1 : -1);

        public CharacterizedVessel(SimulatedVessel vessel)
        {
            this.vessel = vessel;

            /*  How to characterize a vessel:
             *  Lift: 
             *  - Group parts by their lifting surface LiftMach curve if their lift curve is non-zero
             *      - Map their Lift curve local AoA to vessel AoA according to their lift vector
             *      - Treat parts as three lifting surfaces with lift vectors along their primary axes
             *      
             *      L = f(M) * f(AoA) * f(q)
             *  
             *  - For each group, make a FloatCurve of lift coefficient using the union of those
             *    AoA keys within each group.
             *    
             *  
             *  Drag:
             *  - Group lifting surfaces by their DragMach curve if their drag curve is non-zero
             *      - Map their Drag curve local AoA to vessel AoA according to their lift vector
             *      
             *      D = f(M) * f(AoA) * f(q)
             *  
             *  - For each group, make a FloatCurve of drag coefficient using the union of those
             *    AoA keys within each group.
             *  
             *  - Find the union of all Mach keys for every draggy part's Tip, Tail, Surface, Multiplier, and Power.
             *  - Find the vessel AoA equating to 0, 90, 180, and 270 (-90) local AoA using each part's primary axes.
             *      - Round each to the nearest 1E-8. Math.Round(value,8).
             *  - Make a FloatCurve2 of part drag coefficient for those AoAs and -180:15:180, and Machs.
             *  
             *      D = f(M, AoA) * f(q)
             *  
             *  Methods Needed:
             *  - IEqualityComparer<FloatCurve> to use in GroupBy() (or other) to group by LiftMach or DragMach.
             *      - Extensions.KSPClassExtensions.FloatCurveComparer
             *  - Take a part and return its primary axes.
             *      - GetPartAxes(Part) return Vector3[3] of [Forward, Right, Up]
             *  - Take a normvector and a Lift or Drag curve and return the key vessel AoAs.
             *      - GetWorldKeys(Vector3, FloatCurve)
             *  - Make a FloatCurve from a list of keys (and optionally whether the derivative is continuous
             *    at that key), a function, and a list of objects.
             *      - MakeFloatCurve((float, bool), Func<float, float>)
             *  - Make a FloatCurve2 from two lists of keys (and optionally whether the derivative is continuous
             *    at that key), a function, and a list of objects.
             *      - MakeFloatCurve2((float, bool), (float, bool), Func<float, float, float>)
             *  - Collect all the relevant Mach number keys from part and lifting surface drag curves.
            */

            IEnumerable<SimulatedLiftingSurface> surfaces = vessel.partCollection.surfaces.Union(vessel.partCollection.ctrls);
            ILookup<FloatCurve, SimulatedLiftingSurface> liftingSurfs = surfaces.Where(surf => surf.liftCurve.Curve.keys.Length > 1 || surf.liftCurve.Evaluate(0) != 0)
                .ToLookup(surf => surf.liftMachCurve, FloatCurveComparer.Instance);
            ILookup<FloatCurve, SimulatedPart> liftingParts = vessel.partCollection.parts.Where(part => !part.NoBodyLift)
                .ToLookup(part => part.cubes.BodyLiftCurve.liftMachCurve, FloatCurveComparer.Instance);
            ILookup<FloatCurve, SimulatedLiftingSurface> dragSurfaces = surfaces.Where(surf => surf.dragCurve.Curve.keys.Length > 1 || surf.dragCurve.Evaluate(0) != 0)
                .ToLookup(surf => surf.dragMachCurve, FloatCurveComparer.Instance);
            IEnumerable<SimulatedPart> dragParts = vessel.partCollection.parts.Where(p => !(p.noDrag || p.shieldedFromAirstream));

            SortedSet<(float aoa, bool continuousDerivative)> partAoAs = new SortedSet<(float aoa, bool continuousDerivative)>(FloatTupleComparer);
            SortedSet<(float mach, bool continuousDerivative)> partMachs = new SortedSet<(float mach, bool continuousDerivative)>(FloatTupleComparer);
            for (int i = -180; i <= 180; i += 15)
                partAoAs.Add((Mathf.Deg2Rad * i, false));
            foreach (var part in dragParts)
            {
                partAoAs.UnionWith(GetPartAxes(part).SelectMany(v => GetWorldKeys(v, new float[] { 0, 1 })).Select(v => ((float)Math.Round(v, tolerance), false)));
                partMachs.UnionWith(GetDragMachSet(part.cubes));
            }
            foreach (var group in liftingSurfs)
            {
                float machMag = group.Key.Evaluate(0), evalPt = 0;
                if (machMag == 0)
                {
                    group.Key.Evaluate(1);
                    evalPt = 1;
                }
                float SurfLiftForce(float aoa)
                {
                    Vector3 lift = Vector3.zero;
                    Vector3 inflow = InflowVect(aoa);
                    foreach (var surf in group)
                        lift += surf.GetLift(inflow, evalPt);
                    return GetLiftForceMagnitude(lift, aoa) / machMag;
                }
                SortedSet<(float aoa, bool continuousDerivative)> AoAKeys = new SortedSet<(float aoa, bool continuousDerivative)>(FloatTupleComparer);
                AoAKeys.UnionWith(group.SelectMany(surf => GetWorldKeys(surf.liftVector, surf.liftCurve)).Select(v => ((float)Math.Round(v.AoA, tolerance), v.continuousDerivative)));
                
                FloatCurve liftCurve;
                liftCurve = KSPClassExtensions.MakeFloatCurve(AoAKeys.GroupBy(t => t.aoa).Select(g => (g.Key, g.All(t => t.continuousDerivative))), SurfLiftForce);
                surfaceLift.Add((group.Key, liftCurve));
            }
            foreach (var group in liftingParts)
            {
                float machMag = group.Key.Evaluate(0), evalPt = 0;
                if (machMag == 0)
                {
                    group.Key.Evaluate(1);
                    evalPt = 1;
                }
                float PartLiftForce(float aoa)
                {
                    Vector3 lift = Vector3.zero;
                    Vector3 inflow = InflowVect(aoa);
                    foreach (var part in group)
                        lift += part.GetLift(inflow, evalPt);
                    return GetLiftForceMagnitude(lift, aoa) / machMag;
                }
                SortedSet<float> AoAKeys = new SortedSet<float>();
                AoAKeys.UnionWith(group.SelectMany(part => GetPartAxes(part).SelectMany(v => GetWorldKeys(v, new float[] { 0, 1 }))).Select(v => (float)Math.Round(v, tolerance)));
                
                FloatCurve liftCurve;
                liftCurve = KSPClassExtensions.MakeFloatCurve(AoAKeys, PartLiftForce);
                bodyLift.Add((group.Key, liftCurve));
            }
            foreach (var group in dragSurfaces)
            {
                float machMag = group.Key.Evaluate(0), evalPt = 0;
                if (machMag == 0)
                {
                    group.Key.Evaluate(1);
                    evalPt = 1;
                }
                float SurfDragForce(float aoa)
                {
                    Vector3 drag = Vector3.zero;
                    Vector3 inflow = InflowVect(aoa);
                    foreach (var surf in group)
                        drag += surf.GetDrag(inflow, evalPt);
                    return GetDragForceMagnitude(drag, aoa) / machMag;
                }
                SortedSet<(float aoa, bool continuousDerivative)> AoAKeys = new SortedSet<(float aoa, bool continuousDerivative)>(FloatTupleComparer);
                AoAKeys.UnionWith(group.SelectMany(surf => GetWorldKeys(surf.liftVector, surf.dragCurve)).Select(v => ((float)Math.Round(v.AoA, tolerance), v.continuousDerivative)));
                
                FloatCurve dragCurve;
                dragCurve = KSPClassExtensions.MakeFloatCurve(AoAKeys.GroupBy(t => t.aoa).Select(g => (g.Key, g.All(t => t.continuousDerivative))), SurfDragForce);
                surfaceDrag.Add((group.Key, dragCurve));
            }
            float PartDragForce(float mach, float aoa)
            {
                Vector3 drag = Vector3.zero;
                Vector3 inflow = InflowVect(aoa);
                foreach (var part in dragParts)
                    drag += part.GetAero(inflow, 0, 1);
                return GetDragForceMagnitude(drag, aoa);
            }

            //partAoAs = (SortedSet<(float, bool)>)partAoAs.GroupBy(t => t.aoa).Select(g => (g.Key, g.All(t => t.continuousDerivative)));
            //partMachs = (SortedSet<(float, bool)>)partMachs.GroupBy(t => t.mach).Select(g => (g.Key, g.All(t => t.continuousDerivative)));
            bodyDrag = FloatCurve2.MakeFloatCurve2(
                partMachs.GroupBy(t => t.mach).Select(g => (g.Key, g.All(t => t.continuousDerivative))),
                partAoAs.GroupBy(t => t.aoa).Select(g => (g.Key, g.All(t => t.continuousDerivative))),
                PartDragForce);
        }

        public static IEnumerable<(float mach, bool continuousDerivative)> GetDragMachSet(DragCubeList cubes)
        {
            var surfCurves = cubes.SurfaceCurves;
            IEnumerable<Keyframe> machs =
                surfCurves.dragCurveTip.Curve.keys.Concat(
                surfCurves.dragCurveSurface.Curve.keys).Concat(
                surfCurves.dragCurveTail.Curve.keys).Concat(
                cubes.DragCurveMultiplier.Curve.keys).Concat(
                cubes.DragCurveCdPower.Curve.keys);

            if (!FloatCurveComparer.Instance.Equals(surfCurves.dragCurveMultiplier, cubes.DragCurveMultiplier))
                machs = machs.Concat(surfCurves.dragCurveMultiplier.Curve.keys);

            return machs.GroupBy(k => k.time).Select(g => (g.Key, g.All(k => k.outTangent == k.inTangent)));
        }

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
                    foreach (float AoA in SinAoAMapping(norm, key.time).Distinct())
                        yield return (AoA, continuousDerivative);
                }
                else
                {
                    continuousDerivative = aoaCurve.EvaluateDerivative(upperLimit) == 0;
                    foreach (float AoA in SinAoAMapping(norm, upperLimit).Distinct())
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
                    foreach (float AoA in SinAoAMapping(norm, sinAoA).Distinct())
                        yield return AoA;
                }
                else
                {
                    foreach (float AoA in SinAoAMapping(norm, upperLimit).Distinct())
                        yield return AoA;
                    yield break;
                }
            }
        }

        public static IEnumerable<float> SinAoAMapping(Vector3 norm, float localSinAoA)
        {
            //return (norm.z * key.time - Math.Sign(norm.x) * Mathf.Sqrt(Math.Max(x2 * (x2 - key.time * key.time + z2), 0))) / (x2 + z2);
            float x = norm.x, z = norm.z;

            float radical = Math.Max(x * x + z * z - localSinAoA * localSinAoA, 0);
            if (radical == 0)
            {
                yield return Mathf.Atan2(z * localSinAoA, x * localSinAoA);
                yield return Mathf.Atan2(-z * localSinAoA, -x * localSinAoA);
                yield break;
            }
            radical = Mathf.Sqrt(Math.Max(x * x + z * z - localSinAoA * localSinAoA, 0));

            float xa = x * localSinAoA, za = z * localSinAoA;
            float xRadical = x * radical, zRadical = z * radical;

            float InnerMap(int AoASign, int quadrantSign)
                => Mathf.Atan2(
                    quadrantSign * (za * AoASign - xRadical),
                    quadrantSign * (xa * AoASign + zRadical));
            yield return InnerMap(1, 1);
            yield return InnerMap(-1, 1);
            yield return InnerMap(1, -1);
            yield return InnerMap(-1, -1);
        }

        public static Vector3[] GetPartAxes(SimulatedPart part)
        {
            Quaternion partToVessel = part.partToVessel;
            return new Vector3[] { partToVessel * Vector3.forward, partToVessel * Vector3.right, partToVessel * Vector3.up };
        }

        /*        private static FloatCurve2 Characterize(Func<float, float, float> func, Dictionary<float, bool> machs, List<float> AoAs)
                {
                    List<float> sortedMachs = machs.Keys.ToList();
                    sortedMachs.Sort();

                    float[,] values = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machInTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machOutTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] AoAInTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] AoAOutTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machIn_AoAInTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machIn_AoAOutTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machOut_AoAInTangents = new float[sortedMachs.Count, AoAs.Count];
                    float[,] machOut_AoAOutTangents = new float[sortedMachs.Count, AoAs.Count];

                    for (int m = 0; m <= sortedMachs.Count - 1; m++)
                    {
                        for (int a = 0; a <= AoAs.Count - 1; a++)
                        {
                            float value = values[m,a] = func(sortedMachs[m], AoAs[a]);

                            AoAOutTangents[m, a] = (func(sortedMachs[m], AoAs[a] + delta_AoA) - value) / delta_AoA;
                            AoAInTangents[m, a] = (func(sortedMachs[m], AoAs[a] - delta_AoA) - value) / -delta_AoA;

                            if (m < sortedMachs.Count - 1)
                            {
                                machOutTangents[m, a] = (func(sortedMachs[m] + delta_mach, AoAs[a]) - value) / delta_mach;
                                machOut_AoAInTangents[m, a] = (func(sortedMachs[m] + delta_mach, AoAs[a] - delta_AoA) - value) / (delta_mach * -delta_AoA);
                                machOut_AoAOutTangents[m, a] = (func(sortedMachs[m] + delta_mach, AoAs[a] + delta_AoA) - value) / (delta_mach * delta_AoA);
                            }
                            if (m == sortedMachs.Count - 1 || !(machs[sortedMachs[m]]))
                            {
                                machInTangents[m, a] = (func(sortedMachs[m] - delta_mach, AoAs[a]) - value) / -delta_mach;
                                machIn_AoAInTangents[m, a] = (func(sortedMachs[m] - delta_mach, AoAs[a] - delta_AoA) - value) / (-delta_mach * -delta_AoA);
                                machIn_AoAOutTangents[m, a] = (func(sortedMachs[m] - delta_mach, AoAs[a] + delta_AoA) - value) / (-delta_mach * delta_AoA);
                            }
                            if (machs[sortedMachs[m]])
                            {
                                machInTangents[m, a] = machOutTangents[m, a];
                                machIn_AoAInTangents[m, a] = machOut_AoAInTangents[m, a];
                                machIn_AoAOutTangents[m, a] = machOut_AoAOutTangents[m, a];
                            }
                        }
                    }

                    return new FloatCurve2(sortedMachs.ToArray(), AoAs.ToArray(), values, machInTangents, machOutTangents, AoAInTangents, AoAOutTangents, machIn_AoAInTangents, machIn_AoAOutTangents, machOut_AoAInTangents, machOut_AoAOutTangents);
                }*/

        /*private static Dictionary<float, bool> GetLiftMachs(PartCollection collection, Dictionary<float, bool> machs = null)
        {
            if (machs == null)
                machs = new Dictionary<float, bool>();

            foreach (SimulatedLiftingSurface surf in collection.surfaces)
            {
                foreach (var key in surf.liftMachCurve.Curve.keys)
                    if (!machs.ContainsKey(key.time))
                        machs.Add(key.time, key.inTangent == key.outTangent);
                    else
                        machs[key.time] &= key.inTangent == key.outTangent;
            }
            foreach (SimulatedControlSurface ctrl in collection.ctrls)
            {
                foreach (var key in ctrl.liftMachCurve.Curve.keys)
                    if (!machs.ContainsKey(key.time))
                        machs.Add(key.time, key.inTangent == key.outTangent);
                    else
                        machs[key.time] &= key.inTangent == key.outTangent;
            }

            foreach (SimulatedPart part in collection.parts)
            {
                foreach (var key in part.cubes.BodyLiftCurve.liftMachCurve.Curve.keys)
                    if (!machs.ContainsKey(key.time))
                        machs.Add(key.time, key.inTangent == key.outTangent);
                    else
                        machs[key.time] &= key.inTangent == key.outTangent;
            }
            foreach (PartCollection subCollection in collection.partCollections)
                GetLiftMachs(subCollection, machs);
            
            return machs;
        }
        */

        public override float GetPitchInput(Conditions conditions, float AoA, bool dryTorque = false, float guess = float.NaN, float tolerance = 0.0003F)
            => vessel.GetPitchInput(conditions, AoA, dryTorque, guess, tolerance);

        public override Vector3 GetAeroForce(Conditions conditions, float AoA, float pitchInput = 0)
            => ToVesselFrame(-GetDragForceMagnitude(conditions, AoA, pitchInput) * Vector3.forward +
            GetLiftForce(conditions, AoA, pitchInput), AoA);

        public override float GetDragForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            float magnitude = 0;

            foreach (var (machCurve, dragCurve) in surfaceDrag)
            {
                float groupMagnitude;
                lock (dragCurve)
                    groupMagnitude = dragCurve.Evaluate(AoA);
                lock (machCurve)
                    groupMagnitude *= machCurve.Evaluate(conditions.mach);
                magnitude += groupMagnitude;
            }

            magnitude += bodyDrag.Evaluate(conditions.mach, AoA) * conditions.pseudoReDragMult;

            float Q = 0.0005f * conditions.atmDensity * conditions.speed * conditions.speed;
            return magnitude * Q;
        }

        public override float GetLiftForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            float magnitude = 0;

            foreach (var (machCurve, liftCurve) in surfaceLift)
            {
                float groupMagnitude;
                lock (liftCurve)
                    groupMagnitude = liftCurve.Evaluate(AoA);
                lock (machCurve)
                    groupMagnitude *= machCurve.Evaluate(conditions.mach);
                magnitude += groupMagnitude;
            }
            foreach (var (machCurve, liftCurve) in bodyLift)
            {
                float groupMagnitude;
                lock (liftCurve)
                    groupMagnitude = liftCurve.Evaluate(AoA);
                lock (machCurve)
                    groupMagnitude *= machCurve.Evaluate(conditions.mach);
                magnitude += groupMagnitude;
            }

            float Q = 0.0005f * conditions.atmDensity * conditions.speed * conditions.speed;
            return magnitude * Q;
        }

        public override Vector3 GetLiftForce(Conditions conditions, float AoA, float pitchInput = 0)
            => ToVesselFrame(GetLiftForceMagnitude(conditions, AoA, pitchInput) * Vector3.up, AoA);

        public override Vector3 GetAeroTorque(Conditions conditions, float AoA, float pitchInput = 0, bool dryTorque = false)
            => vessel.GetAeroTorque(conditions, AoA, pitchInput, dryTorque);

        public override Vector3 GetThrustForce(Conditions conditions, float AoA)
            => vessel.GetThrustForce(conditions, AoA);

        public override float GetFuelBurnRate(Conditions conditions, float AoA)
            => vessel.GetFuelBurnRate(conditions, AoA);

        public float GetLiftForceMagnitudeAoADerivative(Conditions conditions, float AoA, float pitchInput = 0)
        {
            float magnitude = 0;

            foreach (var (machCurve, liftCurve) in surfaceLift)
            {
                float machValue;
                lock (machCurve)
                    machValue = machCurve.Evaluate(conditions.mach);
                magnitude += liftCurve.EvaluateDerivative(AoA) * machValue;
            }
            foreach (var (machCurve, liftCurve) in bodyLift)
            {
                float machValue;
                lock (machCurve)
                    machValue = machCurve.Evaluate(conditions.mach);
                magnitude += liftCurve.EvaluateDerivative(AoA) * machValue;
            }

            float Q = 0.0005f * conditions.atmDensity * conditions.speed * conditions.speed;
            return magnitude * Q;
        }
    }
}
