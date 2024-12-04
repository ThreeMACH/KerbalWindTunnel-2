using Smooth.Pools;
using System;
using UnityEngine;

namespace KerbalWindTunnel.VesselCache
{
    public class RotorPartCollection : PartCollection
    {
        public Vector3 axis;
        public float angularVelocity;
        public bool isRotating;
        public float fuelConsumption;
        private bool enginesUseVelCurve;
        private bool enginesUseVelCurveISP;

        // Apparently local Mach means nothing, and everything just uses the vessel frame Mach.
        // Similarly for local PseudoReDragMult...

        // Excludes control surfaces that may be deflected.
        public override Vector3 GetAeroForceStatic(Vector3 inflow, AeroPredictor.Conditions conditions, out Vector3 torque, Vector3 torquePoint)
        {
            if (!isRotating)
                return base.GetAeroForceStatic(inflow, conditions, out torque, torquePoint);

            Vector3 aeroForce = Vector3.zero;
            torque = Vector3.zero;
            int rotationCount = WindTunnelSettings.Instance.rotationCount;

            float Q = 0.0005f * conditions.atmDensity;

            // The root part is the rotor hub, so since the rotating mesh is usually cylindrical we
            // only need to evaluate this part once.
            if (!parts[0].shieldedFromAirstream && inflow.sqrMagnitude > 0)
            {
                float localVelFactor = inflow.sqrMagnitude;

                aeroForce += parts[0].GetAero(inflow.normalized, conditions.mach, conditions.pseudoReDragMult, out Vector3 pTorque, origin) * localVelFactor * Q;
                torque += pTorque * localVelFactor * Q;
            }

            for (int r = 0; r < rotationCount; r++)
            {
                Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                Vector3 rTorque = Vector3.zero;
                Vector3 rAeroForce = Vector3.zero;
                // Rotate inflow
                Vector3 rotatedInflow = rotation * inflow;
                // Calculate forces
                for (int i = parts.Count - 1; i >= 1; i--)
                {
                    if (parts[i].shieldedFromAirstream)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (parts[i].transformPosition - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;

                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;

                    rAeroForce += parts[i].GetAero(partInflow, conditions.mach, conditions.pseudoReDragMult, out Vector3 pTorque, origin) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }
                for (int i = surfaces.Count - 1; i >= 0; i--)
                {
                    if (surfaces[i].part.shieldedFromAirstream)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (surfaces[i].part.transformPosition + surfaces[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                        
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    rAeroForce += surfaces[i].GetDrag(partInflow, conditions.mach, out Vector3 pTorque, origin) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }
                for (int i = ctrls.Count - 1; i >= 0; i--)
                {
                    if (ctrls[i].part.shieldedFromAirstream)
                        continue;
                    if (!ctrls[i].ignorePitch)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (ctrls[i].part.transformPosition + ctrls[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                        
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    Vector3 pTorque;
                    if (ctrls[i].ignoresAllControls)
                        rAeroForce += ctrls[i].GetForce(partInflow, conditions.mach, out pTorque, origin) * localVelFactor;
                        // This call chain gets the total force, but does it through a chain that enforces zero deflection.
                        // ctrls[i].GetDrag(partInflow, conditions.mach, conditions.pseudoReDragMult, out pTorque, origin) * localVelFactor;
                    else
                        rAeroForce += ctrls[i].GetForce(partInflow, conditions.mach, conditions.pseudoReDragMult, out pTorque, origin) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }

                rTorque *= Q;
                rAeroForce *= Q;

                for (int i = partCollections.Count - 1; i >= 0; i--)
                {
                    Vector3 partMotion = Vector3.Cross(axis, partCollections[i].origin - this.origin) * angularVelocity;
                    rAeroForce += partCollections[i].GetAeroForceStatic(rotatedInflow + partMotion, conditions, out Vector3 pTorque, origin);
                    rTorque += pTorque;
                }

                // Rotate vectors backwards
                if (r != 0)
                {
                    Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                    rTorque = inverseRotation * rTorque;
                    rAeroForce = inverseRotation * rTorque;
                }

                torque += rTorque;
                aeroForce += rAeroForce;
            }

            aeroForce /= rotationCount;
            torque /= rotationCount;
            torque = Vector3.ProjectOnPlane(torque, axis);
            torque += Vector3.Cross(aeroForce, origin - torquePoint);

            return aeroForce;
        }

        // Includes only control surfaces that may be deflected.
        public override Vector3 GetAeroForceDynamic(Vector3 inflow, AeroPredictor.Conditions conditions, float pitchInput, out Vector3 torque, Vector3 torquePoint)
        {
            if (!isRotating)
                return base.GetAeroForceDynamic(inflow, conditions, pitchInput, out torque, torquePoint);

            Vector3 aeroForce = Vector3.zero;
            torque = Vector3.zero;
            int rotationCount = WindTunnelSettings.Instance.rotationCount;
            float Q = 0.0005f * conditions.atmDensity;

            for (int r = 0; r < rotationCount; r++)
            {
                Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                Vector3 rTorque = Vector3.zero;
                Vector3 rAeroForce = Vector3.zero;
                // Rotate inflow
                Vector3 rotatedInflow = rotation * inflow;
                // Calculate forces
                for (int i = ctrls.Count - 1; i >= 0; i--)
                {
                    if (ctrls[i].part.shieldedFromAirstream)
                        continue;
                    if (ctrls[i].ignorePitch)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (ctrls[i].part.transformPosition + ctrls[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    rAeroForce += ctrls[i].GetForce(partInflow, conditions.mach, pitchInput, conditions.pseudoReDragMult, out Vector3 pTorque, origin) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }

                rTorque *= Q;
                rAeroForce *= Q;

                for (int i = partCollections.Count - 1; i >= 0; i--)
                {
                    Vector3 partMotion = Vector3.Cross(axis, partCollections[i].origin - this.origin) * angularVelocity;
                    rAeroForce += partCollections[i].GetAeroForceDynamic(rotatedInflow + partMotion, conditions, pitchInput, out Vector3 pTorque, origin);
                    rTorque += pTorque;
                }

                // Rotate vectors backwards
                if (r != 0)
                {
                    Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                    rTorque = inverseRotation * rTorque;
                    rAeroForce = inverseRotation * rTorque;
                }

                torque += rTorque;
                aeroForce += rAeroForce;
            }

            aeroForce /= rotationCount;
            torque /= rotationCount;
            torque = Vector3.ProjectOnPlane(torque, axis);
            torque += Vector3.Cross(aeroForce, origin - torquePoint);

            return aeroForce;
        }

        public override Vector3 GetLiftForce(Vector3 inflow, AeroPredictor.Conditions conditions, float pitchInput, out Vector3 torque, Vector3 torquePoint)
        {
            if (!isRotating)
                return base.GetLiftForce(inflow, conditions, pitchInput, out torque, torquePoint);

            Vector3 aeroForce = Vector3.zero;
            torque = Vector3.zero;
            int rotationCount = Math.Abs(angularVelocity) > 0 ? WindTunnelSettings.Instance.rotationCount : 1;

            float Q = 0.0005f * conditions.atmDensity;

            // The root part is the rotor hub, so since the rotating mesh is usually cylindrical we
            // only need to evaluate this part once.
            if (!parts[0].shieldedFromAirstream && inflow.sqrMagnitude > 0)
            {
                float localVelFactor = inflow.sqrMagnitude;
                aeroForce += parts[0].GetAero(inflow.normalized, conditions.mach, conditions.pseudoReDragMult, out Vector3 pTorque, origin) * localVelFactor * Q;
                torque += pTorque * localVelFactor * Q;
            }

            for (int r = 0; r < rotationCount; r++)
            {
                Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                Vector3 rTorque = Vector3.zero;
                Vector3 rAeroForce = Vector3.zero;
                // Rotate inflow
                Vector3 rotatedInflow = rotation * inflow;
                // Calculate forces
                for (int i = parts.Count - 1; i >= 1; i--)
                {
                    if (parts[i].shieldedFromAirstream)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (parts[i].transformPosition - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    rAeroForce += parts[i].GetLift(partInflow, conditions.mach, out Vector3 pTorque, torquePoint) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }
                /*for (int i = surfaces.Count - 1; i >= 0; i--)
                {
                    if (surfaces[i].part.shieldedFromAirstream)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (surfaces[i].part.transformPosition + surfaces[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    rAeroForce += surfaces[i].GetLift(partInflow, conditions.mach, out Vector3 pTorque, torquePoint) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }*/
                
                for (int i = ctrls.Count - 1; i >= 0; i--)
                {
                    if (ctrls[i].part.shieldedFromAirstream)
                        continue;
                    if (ctrls[i].ignoresAllControls && ctrls[i].ignorePitch)
                        continue;
                    Vector3 partMotion = Vector3.Cross(axis, (ctrls[i].part.transformPosition + ctrls[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                    if (partMotion.sqrMagnitude <= 0)
                        continue;
                    Vector3 partInflow = partMotion.normalized;
                    float localVelFactor = partMotion.sqrMagnitude;
                    rAeroForce += ctrls[i].GetLift(partInflow, conditions.mach, pitchInput, out Vector3 pTorque, torquePoint) * localVelFactor;
                    rTorque += pTorque * localVelFactor;
                }

                rTorque *= Q;
                rAeroForce *= Q;

                for (int i = partCollections.Count - 1; i >= 0; i--)
                {
                    Vector3 partMotion = Vector3.Cross(axis, partCollections[i].origin - this.origin) * angularVelocity;
                    rAeroForce += partCollections[i].GetLiftForce(rotatedInflow + partMotion, conditions, pitchInput, out Vector3 pTorque, torquePoint);
                    rTorque += pTorque;
                }

                // Rotate vectors backwards
                if (r != 0)
                {
                    Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                    rTorque = inverseRotation * rTorque;
                    rAeroForce = inverseRotation * rTorque;
                }

                torque += rTorque;
                aeroForce += rAeroForce;
            }
            aeroForce /= rotationCount;
            torque /= rotationCount;
            torque = Vector3.ProjectOnPlane(torque, axis);
            torque += Vector3.Cross(aeroForce, origin - torquePoint);

            return aeroForce;
        }

        public override Vector3 GetThrustForce(Vector3 inflow, AeroPredictor.Conditions conditions)
        {
            return GetThrustForce(inflow, conditions, out _, Vector3.zero);
        }

        public override Vector3 GetThrustForce(Vector3 inflow, AeroPredictor.Conditions conditions, out Vector3 torque, Vector3 torquePoint)
        {
            Vector3 engineThrust;
            Vector3 engineTorque;
            if ((enginesUseVelCurve || enginesUseVelCurveISP) && isRotating)
            {
                engineThrust = Vector3.zero;
                engineTorque = Vector3.zero;
                float Q = 0.0005f * conditions.atmDensity;
                int rotationCount = WindTunnelSettings.Instance.rotationCount;

                for (int r = 0; r < rotationCount; r++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                    Vector3 rTorque = Vector3.zero;
                    Vector3 rThrust = Vector3.zero;
                    // Rotate inflow
                    Vector3 rotatedInflow = rotation * inflow;
                    // Calculate forces
                    for (int i = engines.Count - 1; i >= 0; i--)
                    {
                        Vector3 eThrust = engines[i].GetThrust(conditions.mach, conditions.atmDensity, conditions.atmPressure, conditions.oxygenAvailable);
                        rThrust += eThrust;
                        rTorque += Vector3.Cross(eThrust, engines[i].thrustPoint - origin);
                    }

                    for (int i = partCollections.Count - 1; i >= 0; i--)
                    {
                        Vector3 partMotion = Vector3.Cross(axis, partCollections[i].origin - this.origin) * angularVelocity;
                        rThrust += partCollections[i].GetThrustForce(rotatedInflow + partMotion, conditions, out Vector3 pTorque, origin); ;
                        rTorque += pTorque;
                    }

                    // Rotate vectors backwards
                    if (r != 0)
                    {
                        Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                        rTorque = inverseRotation * rTorque;
                        rThrust = inverseRotation * rTorque;
                    }

                    engineTorque += rTorque;
                    engineThrust += rThrust;
                }

                engineTorque /= rotationCount;
                engineThrust /= rotationCount;
            }
            else if (engines.Count > 0)
                engineThrust = Vector3.Project(base.GetThrustForce(inflow, conditions, out engineTorque, origin), axis);
            else
                engineThrust = engineTorque = Vector3.zero;

            if (isRotating)
            {
                Vector3 propThrust = Vector3.zero;
                Vector3 propTorque = Vector3.zero;
                float Q = 0.0005f * conditions.atmDensity;
                int rotationCount = WindTunnelSettings.Instance.rotationCount;

                for (int r = 0; r < rotationCount; r++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                    Vector3 rTorque = Vector3.zero;
                    Vector3 rAeroForce = Vector3.zero;
                    // Rotate inflow
                    Vector3 rotatedInflow = rotation * inflow;
                    // Calculate forces
                    for (int i = surfaces.Count - 1; i >= 0; i--)
                    {
                        if (surfaces[i].part.shieldedFromAirstream)
                            continue;
                        Vector3 partMotion = Vector3.Cross(axis, (surfaces[i].part.transformPosition + surfaces[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                        if (partMotion.sqrMagnitude <= 0)
                            continue;
                        Vector3 partInflow = partMotion.normalized;
                        float localVelFactor = partMotion.sqrMagnitude;
                        rAeroForce += surfaces[i].GetLift(partInflow, conditions.mach, out Vector3 pTorque, origin) * localVelFactor;
                        rTorque += pTorque * localVelFactor;
                    }
                    for (int i = ctrls.Count - 1; i >= 0; i--)
                    {
                        if (ctrls[i].part.shieldedFromAirstream)
                            continue;
                        if (!ctrls[i].ignoresAllControls || !ctrls[i].ignorePitch)
                            continue;
                        Vector3 partMotion = Vector3.Cross(axis, (ctrls[i].part.transformPosition + ctrls[i].velocityOffset - origin)) * angularVelocity + rotatedInflow;
                        if (partMotion.sqrMagnitude <= 0)
                            continue;
                        Vector3 partInflow = partMotion.normalized;
                        float localVelFactor = partMotion.sqrMagnitude;
                        rAeroForce += ctrls[i].GetLift(partInflow, conditions.mach, out Vector3 pTorque, origin) * localVelFactor;
                        rTorque += pTorque * localVelFactor;
                    }

                    rTorque *= Q;
                    rAeroForce *= Q;

                    // Rotate vectors backwards
                    if (r != 0)
                    {
                        Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                        rTorque = inverseRotation * rTorque;
                        rAeroForce = inverseRotation * rAeroForce;
                    }

                    propTorque += rTorque;
                    propThrust += rAeroForce;
                }

                propTorque /= rotationCount;
                propThrust /= rotationCount;

                torque = Vector3.ProjectOnPlane(engineTorque + propTorque, axis);
                torque += Vector3.Cross(engineThrust + propThrust, origin - torquePoint);
                return engineThrust + propThrust;
            }
            else
            {
                torque = Vector3.ProjectOnPlane(engineTorque, axis);
                torque += Vector3.Cross(engineThrust, origin - torquePoint);
                return engineThrust;
            }
        }

        public override float GetFuelBurnRate(Vector3 inflow, AeroPredictor.Conditions conditions)
        {
            float burnRate;
            if (enginesUseVelCurve && isRotating)
            {
                burnRate = 0;
                int rotationCount = WindTunnelSettings.Instance.rotationCount;
                for (int r = 0; r < rotationCount; r++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(360f / rotationCount * r, axis);
                    //Quaternion inverseRotation = Quaternion.AngleAxis(-360f / rotationCount * r, axis);
                    // Rotate inflow
                    Vector3 rotatedInflow = rotation * inflow;
                    // Calculate burn rate
                    for (int i = engines.Count - 1; i >= 0; i--)
                    {
                        //Vector3 partMotion = Vector3.Cross(axis, (engines[i].thrustPoint - origin)) * angularVelocity + rotatedInflow;
                        burnRate += engines[i].GetFuelBurnRate(conditions.mach, conditions.atmDensity);
                    }

                    for (int i = partCollections.Count - 1; i >= 0; i--)
                    {
                        Vector3 partMotion = Vector3.Cross(axis, partCollections[i].origin - this.origin) * angularVelocity;
                        burnRate += partCollections[i].GetFuelBurnRate(rotatedInflow + partMotion, conditions);
                    }
                }

                burnRate /= rotationCount;
            }
            else
                burnRate = base.GetFuelBurnRate(inflow, conditions);

            // Assumes all rotors are running at max torque.
            // An over-estimation, sure, but the closest I'll get without simulating the aero forces at that point,
            // which requires information this function can't get.
            //
            // Users should be aware, though, and de-tune their rotors to provide only sufficient torque throughout
            // their target flight regime.
            return fuelConsumption + burnRate;
        }

        #region Pool Methods

        private static readonly Pool<RotorPartCollection> pool = new Pool<RotorPartCollection>(Create, Reset);

        private static RotorPartCollection Create()
        {
            return new RotorPartCollection();
        }

        public override void Release()
        {
            lock (pool)
                pool.Release(this);
        }

        private static void Reset(RotorPartCollection obj)
        {
            PartCollection.Reset(obj);
            obj.enginesUseVelCurve = false;
            obj.enginesUseVelCurveISP = false;
        }

        new public static RotorPartCollection Borrow(PartCollection parentCollection, Part originPart)
        {
            RotorPartCollection collection = BorrowWithoutAdding(parentCollection?.parentVessel, originPart);
            collection.parentCollection = parentCollection;
            collection.AddPart(originPart);
            return collection;
        }

        internal static RotorPartCollection DirectBorrow()
        {
            lock (pool)
                return pool.Borrow();
        }
        
        new public static RotorPartCollection Borrow(SimulatedVessel vessel, Part originPart)
        {
            RotorPartCollection collection;
            lock (pool)
                collection = pool.Borrow();
            collection.parentVessel = vessel;

            collection.Init(originPart);

            collection.AddPart(originPart);
            return collection;
        }

        public static RotorPartCollection BorrowWithoutAdding(SimulatedVessel vessel, Part originPart)
        {
            RotorPartCollection collection;
            lock (pool)
                collection = pool.Borrow();
            collection.parentVessel = vessel;

            collection.Init(originPart);
            return collection;
        }

        public override void AddPart(Part part)
        {
            int enginesCount = engines.Count;
            base.AddPart(part);
            int enginesCountPost = engines.Count;
            if (enginesCountPost < enginesCount)
            {
                enginesUseVelCurve = false;
                enginesUseVelCurveISP = false;
            }
            else if (enginesCountPost > enginesCount || (enginesCount == 1 && enginesCountPost == 1))
            {
                enginesUseVelCurve |= engines[enginesCountPost - 1].useVelCurve;
                enginesUseVelCurveISP |= engines[enginesCountPost - 1].useVelCurveIsp;
            }
        }

        private void Init(Part part)
        {
            Expansions.Serenity.ModuleRoboticServoRotor rotorModule = part.FindModuleImplementing<Expansions.Serenity.ModuleRoboticServoRotor>();
            
            Transform rotorTransform = part.FindModelTransform(rotorModule.servoTransformName);

            origin = rotorTransform.position;

            axis = rotorTransform.TransformVector(rotorModule.GetMainAxis());
            if (rotorModule.rotateCounterClockwise ^ rotorModule.inverted)
                axis *= -1;
            angularVelocity = rotorModule.rpmLimit / 60 * 2 * Mathf.PI;
            fuelConsumption = rotorModule.LFPerkN * rotorModule.maxTorque;
            isRotating = Math.Abs(angularVelocity) > 0 && rotorModule.servoMotorIsEngaged;
        }

        protected override void InitClone(PartCollection collection)
        {
            base.InitClone(collection);
            if (collection is RotorPartCollection rotorCollection)
            {
                axis = rotorCollection.axis;
                angularVelocity = rotorCollection.angularVelocity;
                fuelConsumption = rotorCollection.fuelConsumption;
                enginesUseVelCurve = rotorCollection.enginesUseVelCurve;
                enginesUseVelCurveISP = rotorCollection.enginesUseVelCurveISP;
                isRotating = rotorCollection.isRotating;
            }
            else
            {
                // No need to throw any exceptions, we can just have this one be non-rotating.
                axis = Vector3.forward;
                angularVelocity = 0;
                fuelConsumption = 0;
                enginesUseVelCurve = false;
                enginesUseVelCurveISP = false;
                isRotating = false;
            }
        }
        #endregion
    }
}
