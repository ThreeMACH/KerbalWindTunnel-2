using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalWindTunnel.VesselCache
{
    public class CharacterizedControlSurface : CharacterizedLiftingSurface
    {
        private static readonly FloatCurve2.DiffSettings settings = new FloatCurve2.DiffSettings(1E-8f, CharacterizedVessel.toleranceF);
        private readonly SimulatedControlSurface controlSurface;
        public readonly CharacterizedPart basePart;
        public FloatCurve2 DeltaDragCoefficientCurve_Pos { get; private set; }
        public FloatCurve2 DeltaDragCoefficientCurve_Neg { get; private set; }

        private readonly SortedSet<(float aoa, bool continuousDerivative)> partAoAKeys = new SortedSet<(float aoa, bool continuousDerivative)>(CharacterizedVessel.FloatTupleComparer);
        private readonly SortedSet<(float mach, bool continuousDerivative)> partMachKeys = new SortedSet<(float mach, bool continuousDerivative)>(CharacterizedVessel.FloatTupleComparer);

        public CharacterizedControlSurface(ModuleControlSurface controlSurface, CharacterizedPart part) : base(controlSurface.part)
        {
            this.controlSurface = SimulatedControlSurface.Borrow(controlSurface, part.simulatedPart);
            simulatedLiftingSurface = this.controlSurface;
            needsDisposing = true;
            basePart = part;
        }
        public CharacterizedControlSurface(SimulatedControlSurface liftingSurface, CharacterizedPart part) : base(liftingSurface)
        {
            controlSurface = liftingSurface;
            basePart = part;
        }

        protected override void CharacterizeDrag()
        {
            base.CharacterizeDrag();

            partAoAKeys.UnionWith(basePart.AoAKeys);
            partMachKeys.UnionWith(basePart.MachKeys);

            if (!basePart.simulatedPart.noDrag && !basePart.simulatedPart.shieldedFromAirstream)
            {

                DeltaDragCoefficientCurve_Pos = FloatCurve2.ComputeCurve(partMachKeys, partAoAKeys, PartDragForcePos, settings);
                DeltaDragCoefficientCurve_Neg = FloatCurve2.ComputeCurve(partMachKeys, partAoAKeys, PartDragForceNeg, settings);

                DeltaDragCoefficientCurve_Pos = FloatCurve2.Subtract(DeltaDragCoefficientCurve_Pos, basePart.DragCoefficientCurve);
                DeltaDragCoefficientCurve_Neg = FloatCurve2.Subtract(DeltaDragCoefficientCurve_Neg, basePart.DragCoefficientCurve);
            }
            else
            {
                DeltaDragCoefficientCurve_Pos = null;
                DeltaDragCoefficientCurve_Neg = null;
            }
        }

        private float PartDragForcePos(float mach, float aoa) => PartDragForce(mach, aoa, 1);
        private float PartDragForceNeg(float mach, float aoa) => PartDragForce(mach, aoa, -1);
        private float PartDragForce(float mach, float aoa, float deflection)
        {
            Vector3 inflow = AeroPredictor.InflowVect(aoa);
            Vector3 drag = controlSurface.GetPartDrag(inflow, mach, deflection, 1);
            return AeroPredictor.GetDragForceMagnitude(drag, aoa);
        }

        protected override void Null()
        {
            base.Null();
            DeltaDragCoefficientCurve_Pos = null;
            DeltaDragCoefficientCurve_Neg = null;
        }
    }
}
