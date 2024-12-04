using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using KerbalWindTunnel.Extensions;

namespace KerbalWindTunnel.VesselCache
{
    public class CharacterizedVessel : AeroPredictor, ILiftAoADerivativePredictor, IDisposable
    {
        public readonly SimulatedVessel vessel;

        public override float Mass => vessel.Mass;

        public override bool ThrustIsConstantWithAoA => vessel.ThrustIsConstantWithAoA;

        public override float Area => vessel.Area;

        public const int tolerance = 6;
        public const float toleranceF = 2E-5f;

        internal readonly List<(FloatCurve machCurve, FloatCurve liftCurve)> surfaceLift = new List<(FloatCurve machCurve, FloatCurve liftCurve)>();
        internal readonly List<(FloatCurve machCurve, FloatCurve dragCurve)> surfaceDrag = new List<(FloatCurve machCurve, FloatCurve dragCurve)>();
        internal readonly List<(FloatCurve machCurve, FloatCurve liftCurve)> bodyLift = new List<(FloatCurve machCurve, FloatCurve liftCurve)>();
        internal FloatCurve2 bodyDrag;

        private readonly List<CharacterizedPart> parts = new List<CharacterizedPart>();
        private readonly List<CharacterizedLiftingSurface> surfaces = new List<CharacterizedLiftingSurface>();

        private Task taskHandle = null;

        public static readonly IComparer<(float, bool)> FloatTupleComparer = Comparer<(float, bool)>.Create((x, y) =>
        {
            int result = x.Item1.CompareTo(y.Item1);
            return result != 0 ? result : y.Item2.CompareTo(x.Item2);   // Order of x and y reversed here so that a True value comes before a False value
        });

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
             */

            /*  Roadmap:
             *  
             *  Control surfaces can incorporate deflection through a FloatCurve2
             *      L = f(M) * f(AoA, defL) * f(q)
             *      D = f(M) * f(AoA, defL) * f(q)
             * 
             *  Rotor Part Collections cannot be characterized as the velocity and AoA of each part
             *  is a function of the vessel's velocity and the part's local velocity.
             *  They will be treated as non-rotating.
             *
             */


            AddCharacterizedComponents(vessel.partCollection);

            void AddCharacterizedComponents(PartCollection collection)
            {
                parts.AddRange(collection.parts.Select(p => new CharacterizedPart(p)));
                surfaces.AddRange(collection.surfaces.Select(s => new CharacterizedLiftingSurface(s)));
                // Todo: Treat controls as their own category.
                surfaces.AddRange(collection.ctrls.Select(c => new CharacterizedLiftingSurface(c)));
                foreach (PartCollection child in collection.partCollections)
                {
                    if (child is RotorPartCollection rotorCollection && rotorCollection.isRotating)
                        Debug.LogWarning($"[Kerbal Wind Tunnel] Tried characterizing a rotating part collection ({collection.parts[0].name}). Parts will be treated as non-rotating.");
                    AddCharacterizedComponents(child);
                }
            }

            Characterize();
        }

        public void Reset()
        {
            lock (this)
            {
                taskHandle = null;
                parts.Clear();
                surfaces.Clear();
                surfaceLift.Clear();
                surfaceDrag.Clear();
                bodyLift.Clear();
                bodyDrag = null;
            }
        }

        private void WaitUntilCharacterized()
        {
            if (taskHandle == null)
                Characterize();
            taskHandle.Wait(new TimeSpan(0, 1, 0));
        }

        public void Characterize()
        {
            lock (this)
            {
                if (taskHandle != null)
                    return;
                Debug.Log("Starting Characterization");

                List<Task> tasks = new List<Task>();
                foreach (CharacterizedPart part in parts)
                    tasks.Add(Task.Run(part.Characterize));
                Task partTask = Task.WhenAll(tasks).ContinueWith(CombineParts);

                tasks.Clear();
                foreach (CharacterizedLiftingSurface surface in surfaces)
                    tasks.Add(Task.Run(surface.Characterize));
                Task surfaceTask = Task.WhenAll(tasks).ContinueWith(CombineSurfaces);

                taskHandle = Task.WhenAll(partTask, surfaceTask);
#if DEBUG
                taskHandle.ContinueWith(t => Debug.Log("Completed Characterization"));
#endif
            }
        }

        private void CombineParts(Task _)
        {
            foreach (var bodyGroup in parts.Where(p => p.LiftMachScalarCurve != null).GroupBy(p => p.LiftMachScalarCurve))
                bodyLift.Add((bodyGroup.Key, KSPClassExtensions.Superposition(bodyGroup.Select(s => s.LiftCoefficientCurve))));
            bodyDrag = FloatCurve2.Superposition(parts.Select(p => p.DragCoefficientCurve));
        }
        private void CombineSurfaces(Task _)
        {
            foreach (var surfGroup in surfaces.Where(s => s.LiftMachScalarCurve != null).GroupBy(s => s.LiftMachScalarCurve))
                surfaceLift.Add((surfGroup.Key, KSPClassExtensions.Superposition(surfGroup.Select(s => s.LiftCoefficientCurve))));
            foreach (var surfGroup in surfaces.Where(s => s.DragMachScalarCurve != null).GroupBy(s => s.DragMachScalarCurve))
                surfaceDrag.Add((surfGroup.Key, KSPClassExtensions.Superposition(surfGroup.Select(s => s.DragCoefficientCurve))));
        }

        public void Dispose()
        {
            foreach (CharacterizedPart part in parts)
                part.Dispose();
            parts.Clear();

            foreach (CharacterizedLiftingSurface surf in surfaces)
                surf.Dispose();
            surfaces.Clear();
        }

        public override float GetPitchInput(Conditions conditions, float AoA, bool dryTorque = false, float guess = float.NaN, float tolerance = 0.0003F)
            => vessel.GetPitchInput(conditions, AoA, dryTorque, guess, tolerance);

        public override Vector3 GetAeroForce(Conditions conditions, float AoA, float pitchInput = 0)
            => ToVesselFrame(-GetDragForceMagnitude(conditions, AoA, pitchInput) * Vector3.forward +
            GetLiftForce(conditions, AoA, pitchInput), AoA);

        public override float GetDragForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            WaitUntilCharacterized();

            float magnitude = 0;

            foreach (var (machCurve, dragCurve) in surfaceDrag)
            {
                float groupMagnitude;
                groupMagnitude = dragCurve.EvaluateThreadSafe(AoA);
                groupMagnitude *= machCurve.EvaluateThreadSafe(conditions.mach);
                magnitude += groupMagnitude;
            }

            magnitude += bodyDrag.Evaluate(conditions.mach, AoA) * conditions.pseudoReDragMult;

            float Q = 0.0005f * conditions.atmDensity * conditions.speed * conditions.speed;
            return magnitude * Q;
        }

        public override float GetLiftForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            WaitUntilCharacterized();

            float magnitude = 0;

            foreach (var (liftMachCurve, liftCurve) in surfaceLift)
            {
                float groupMagnitude;
                groupMagnitude = liftCurve.EvaluateThreadSafe(AoA);
                groupMagnitude *= liftMachCurve.EvaluateThreadSafe(conditions.mach);
                magnitude += groupMagnitude;
            }
            foreach (var (machCurve, liftCurve) in bodyLift)
            {
                float groupMagnitude;
                groupMagnitude = liftCurve.EvaluateThreadSafe(AoA);
                groupMagnitude *= machCurve.EvaluateThreadSafe(conditions.mach);
                magnitude += groupMagnitude;
            }

            return magnitude * conditions.Q;
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
            WaitUntilCharacterized();

            float magnitude = 0;

            foreach (var (machCurve, liftCurve) in surfaceLift)
            {
                float machValue;
                machValue = machCurve.EvaluateThreadSafe(conditions.mach);
                magnitude += liftCurve.EvaluateDerivative(AoA) * machValue;
            }
            foreach (var (machCurve, liftCurve) in bodyLift)
            {
                float machValue;
                machValue = machCurve.EvaluateThreadSafe(conditions.mach);
                magnitude += liftCurve.EvaluateDerivative(AoA) * machValue;
            }

            float Q = 0.0005f * conditions.atmDensity * conditions.speed * conditions.speed;
            return magnitude * Q;
        }
    }
}
