using KerbalWindTunnel.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalWindTunnel.VesselCache
{
    public class CharacterizedLiftingSurface : IDisposable
    {
        private readonly SimulatedLiftingSurface simulatedLiftingSurface;
        private readonly Part part;
        public FloatCurve DragCoefficientCurve { get; private set; }
        public FloatCurve LiftCoefficientCurve { get; private set; }
        public FloatCurve LiftMachScalarCurve { get => simulatedLiftingSurface?.liftMachCurve; }
        public FloatCurve DragMachScalarCurve { get => simulatedLiftingSurface?.dragMachCurve; }
        private readonly SortedSet<(float aoa, bool continuousDerivative)> liftAoAKeys = new SortedSet<(float aoa, bool continuousDerivative)>(CharacterizedVessel.FloatTupleComparer);
        private readonly SortedSet<(float aoa, bool continuousDerivative)> dragAoAKeys = new SortedSet<(float aoa, bool continuousDerivative)>(CharacterizedVessel.FloatTupleComparer);

        public bool IsValid { get => part != null; }
        private bool characterized = false;
        private readonly bool needsDisposing;

        public CharacterizedLiftingSurface(ModuleLiftingSurface liftingSurface, SimulatedPart part)
        {
            simulatedLiftingSurface = SimulatedLiftingSurface.Borrow(liftingSurface, part);
            this.part = liftingSurface.part;
            needsDisposing = true;
        }
        public CharacterizedLiftingSurface(SimulatedLiftingSurface liftingSurface)
        {
            simulatedLiftingSurface = liftingSurface;
            part = liftingSurface.part.part;
            needsDisposing = false;
        }

        private void Reset()
        {
            lock (this)
            {
                characterized = false;
                liftAoAKeys.Clear();
                dragAoAKeys.Clear();
            }
        }

        public void Characterize()
        {
            lock (this)
            {
                if (characterized)
                    return;
                characterized = true;

                if (simulatedLiftingSurface.liftCurve.Curve.keys.Length > 0 || simulatedLiftingSurface.liftCurve.Curve.keys[0].value != 0)
                {
                    float machMag = LiftMachScalarCurve.EvaluateThreadSafe(0), evalPt = 0;
                    if (machMag == 0)
                    {
                        LiftMachScalarCurve.EvaluateThreadSafe(1);
                        evalPt = 1;
                    }
                    float SurfLiftForce(float aoa)
                    {
                        Vector3 inflow = AeroPredictor.InflowVect(aoa);
                        Vector3 lift = simulatedLiftingSurface.GetLift(inflow, evalPt);
                        return AeroPredictor.GetLiftForceMagnitude(lift, aoa) / machMag;
                    }

                    liftAoAKeys.UnionWith(
                        CharacterizationExtensions.RoundFloatTuple(
                            CharacterizationExtensions.GetWorldKeys(simulatedLiftingSurface.liftVector, simulatedLiftingSurface.liftCurve)));
                    CharacterizationExtensions.AndSortedSet(liftAoAKeys);

                    LiftCoefficientCurve = KSPClassExtensions.ComputeFloatCurve(liftAoAKeys, SurfLiftForce, CharacterizedVessel.toleranceF);
                }
                else
                    LiftCoefficientCurve = null;

                if (simulatedLiftingSurface.dragCurve.Curve.keys.Length > 0 || simulatedLiftingSurface.dragCurve.Curve.keys[0].value != 0)
                {
                    float machMag = DragMachScalarCurve.EvaluateThreadSafe(0), evalPt = 0;
                    if (machMag == 0)
                    {
                        DragMachScalarCurve.EvaluateThreadSafe(1);
                        evalPt = 1;
                    }
                    float SurfDragForce(float aoa)
                    {
                        Vector3 inflow = AeroPredictor.InflowVect(aoa);
                        Vector3 drag = simulatedLiftingSurface.GetDrag(inflow, evalPt);
                        return AeroPredictor.GetDragForceMagnitude(drag, aoa) / machMag;
                    }
                    dragAoAKeys.UnionWith(
                        CharacterizationExtensions.RoundFloatTuple(
                            CharacterizationExtensions.GetWorldKeys(simulatedLiftingSurface.liftVector, simulatedLiftingSurface.dragCurve)));
                    CharacterizationExtensions.AndSortedSet(dragAoAKeys);

                    DragCoefficientCurve = KSPClassExtensions.ComputeFloatCurve(dragAoAKeys, SurfDragForce, CharacterizedVessel.toleranceF);
                }
                else
                    DragCoefficientCurve = null;
            }
        }

        public void Dispose()
        {
            if (!needsDisposing)
                return;
            simulatedLiftingSurface?.Release();
        }
    }
}
