using System;
using UnityEngine;

namespace KerbalWindTunnel
{
    public abstract class AeroPredictor
    {
        public virtual bool ThreadSafe => false;
        public virtual AeroPredictor GetThreadSafeObject() => ThreadSafe ? this : throw new NotImplementedException();

        public abstract float Mass { get; }
        public abstract bool ThrustIsConstantWithAoA { get; }
        public Vector3 CoM;
        public Vector3 CoM_dry;

        public abstract float Area { get; }

        public virtual Func<double, double> AerodynamicObjectiveFunc(Conditions conditions, float pitchInput, int scalar = 1)
        {
            double AerodynamicObjectiveFuncInternal(double aoa) =>
                GetLiftForceMagnitude(conditions, (float)aoa, pitchInput) * scalar;
            return AerodynamicObjectiveFuncInternal;
        }
        public virtual Func<double, double> LevelFlightObjectiveFunc(Conditions conditions, float offsettingForce, float pitchInput = 0)
        {
            if (ThrustIsConstantWithAoA)
            {
                Vector3 thrustForce = GetThrustForce(conditions);
                double LevelFlightObjectiveFuncInternal_ConstantThrust(double aoa) =>
                    GetLiftForceComponent(GetLiftForce(conditions, (float)aoa, pitchInput) + thrustForce, (float)aoa) - offsettingForce;
                return LevelFlightObjectiveFuncInternal_ConstantThrust;
            }
            else
            {
                double LevelFlightObjectiveFuncInternal(double aoa) =>
                    GetLiftForceComponent(GetLiftForce(conditions, (float)aoa, pitchInput) + GetThrustForce(conditions, (float)aoa), (float)aoa) - offsettingForce;
                return LevelFlightObjectiveFuncInternal;
            }
        }
        public virtual Func<double, double> LevelFlightObjectiveFunc(Conditions conditions, float offsettingForce, Func<float, float> pitchInput)
        {
            if (ThrustIsConstantWithAoA)
            {
                Vector3 thrustForce = GetThrustForce(conditions);
                double LevelFlightObjectiveFuncInternal_ConstantThrust(double aoa) =>
                    GetLiftForceComponent(GetLiftForce(conditions, (float)aoa, pitchInput((float)aoa)) + thrustForce, (float)aoa) - offsettingForce;
                return LevelFlightObjectiveFuncInternal_ConstantThrust;
            }
            else
            {
                double LevelFlightObjectiveFuncInternal(double aoa) =>
                    GetLiftForceComponent(GetLiftForce(conditions, (float)aoa, pitchInput((float)aoa)) + GetThrustForce(conditions, (float)aoa), (float)aoa) - offsettingForce;
                return LevelFlightObjectiveFuncInternal;
            }
        }
        public virtual Func<double, double> PitchInputObjectiveFunc(Conditions conditions, float aoa, bool dryTorque = false)
        {
            double PitchInputObjectiveFuncInternal(double input) =>
                GetAeroTorque(conditions, aoa, (float)input, dryTorque).x;
            return PitchInputObjectiveFuncInternal;
        }

        public abstract Vector3 GetAeroForce(Conditions conditions, float AoA, float pitchInput = 0);

        public virtual Vector3 GetLiftForce(Conditions conditions, float AoA, float pitchInput = 0)
        {
            return GetAeroForce(conditions, AoA, pitchInput);
        }

        public abstract Vector3 GetAeroTorque(Conditions conditions, float AoA, float pitchInput = 0, bool dryTorque = false);
        
        public virtual void GetAeroCombined(Conditions conditions, float AoA, float pitchInput, out Vector3 forces, out Vector3 torques, bool dryTorque = false)
        {
            forces = GetAeroForce(conditions, AoA, pitchInput);
            torques = GetAeroTorque(conditions, AoA, pitchInput);
        }

        public virtual float GetLiftForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            return GetLiftForceComponent(GetLiftForce(conditions, AoA, pitchInput), AoA);
        }
        public static float GetLiftForceComponent(Vector3 force, float AoA)
        {
            return ToFlightFrame(force, AoA).y;
        }

        public virtual float GetDragForceMagnitude(Conditions conditions, float AoA, float pitchInput = 0)
        {
            return GetDragForceComponent(GetAeroForce(conditions, AoA, pitchInput), AoA);
        }
        public static float GetDragForceComponent(Vector3 force, float AoA)
        {
            return -ToFlightFrame(force, AoA).z;
        }

        public abstract Vector3 GetThrustForce(Conditions conditions, float AoA);
        public virtual Vector3 GetThrustForce(Conditions conditions) => GetThrustForce(conditions, 0);
        public virtual Vector3 GetThrustForceFlightFrame(Conditions conditions, float AoA)
        {
            return ToFlightFrame(GetThrustForce(conditions, AoA), AoA);
        }

        public virtual Vector2 GetThrustForce2D(Conditions conditions) => GetThrustForce2D(conditions, 0);
        public virtual Vector2 GetThrustForce2D(Conditions conditions, float AoA)
        {
            Vector3 thrustForce = GetThrustForce(conditions, AoA);
            return new Vector2(thrustForce.z, thrustForce.y);
        }
        public virtual Vector2 GetThrustForce2DFlightFrame(Conditions conditions, float AoA)
        {
            Vector3 thrustForce = ToFlightFrame(GetThrustForce(conditions, AoA), AoA);
            return new Vector2(thrustForce.z, thrustForce.y);
        }

        public virtual float GetFuelBurnRate(Conditions conditions) => GetFuelBurnRate(conditions, 0);
        public abstract float GetFuelBurnRate(Conditions conditions, float AoA);

        public static Vector3 ToFlightFrame(Vector3 force, float AoA)
        {
            return Quaternion.AngleAxis((AoA * Mathf.Rad2Deg), Vector3.left) * force;
        }
        public static Vector3 ToVesselFrame(Vector3 force, float AoA)
        {
            return Quaternion.AngleAxis((-AoA * Mathf.Rad2Deg), Vector3.left) * force;
        }

        public static float GetUsefulThrustMagnitude(Vector3 thrustVector)
        {
            Vector2 usefulThrust = new Vector2(Math.Max(thrustVector.z, 0), Math.Max(thrustVector.y, 0));
            if (usefulThrust.x == thrustVector.z && usefulThrust.y == thrustVector.y)
                return usefulThrust.magnitude;
            Vector2 antiThrust = new Vector2(Math.Min(thrustVector.z, 0), Math.Min(thrustVector.y, 0));
            return usefulThrust.magnitude - antiThrust.magnitude;
        }

        public static Vector3 InflowVect(float AoA)
        {
            Vector3 vesselForward = Vector3.forward;
            Vector3 vesselUp = Vector3.up;
            return vesselForward * Mathf.Cos(-AoA) + vesselUp * Mathf.Sin(-AoA);
        }

        //public abstract AeroPredictor Clone();

        public readonly struct Conditions
        {
            public readonly CelestialBody body;
            public readonly float speed;
            public readonly float altitude;
            public readonly float mach;
            public readonly float atmDensity;
            public readonly float atmPressure;
            public readonly float pseudoReDragMult;
            public readonly bool oxygenAvailable;
            public readonly float speedOfSound;
            public readonly float Q;

            public Conditions(CelestialBody body, float speed, float altitude)
            {
                this.body = body;
                this.speed = speed;
                this.altitude = altitude;
                
                lock (body)
                {
                    atmPressure = (float)body.GetPressure(altitude);
                    atmDensity = (float)Extensions.KSPClassExtensions.GetDensity(body, altitude);
                    speedOfSound = (float) body.GetSpeedOfSound(atmPressure, atmDensity);
                    oxygenAvailable = body.atmosphereContainsOxygen;
                }
                mach = speed / speedOfSound;
                
                lock (PhysicsGlobals.DragCurvePseudoReynolds)
                    pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(atmDensity * speed);
                Q = 0.0005f * atmDensity * this.speed * this.speed;
            }
        }
    }
}
