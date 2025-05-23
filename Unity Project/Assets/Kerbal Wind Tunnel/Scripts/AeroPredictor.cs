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
        public virtual float MAC { get; protected set; }

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

        public virtual float GetStaticMargin(Conditions conditions, float AoA, float pitchInput = 0, bool dryTorque = false, float dLift = float.NaN, float baselineLift = float.NaN, float baselineTorque = float.NaN)
        {
            const float aoaDelta = WindTunnelWindow.AoAdelta;
            if (float.IsNaN(dLift))
            {
                if (this is VesselCache.AeroOptimizer.ILiftAoADerivativePredictor liftDerivativePredictor)
                    dLift = liftDerivativePredictor.GetLiftForceMagnitudeAoADerivative(conditions, AoA, pitchInput);
                else
                {
                    dLift = GetLiftForceMagnitude(conditions, AoA + aoaDelta, pitchInput);
                    if (float.IsNaN(baselineLift))
                        dLift -= GetLiftForceMagnitude(conditions, AoA, pitchInput);
                    else
                        dLift -= baselineLift;
                }
            }
            float dTorque = GetAeroTorque(conditions, AoA + aoaDelta, pitchInput, dryTorque).x;
            if (float.IsNaN(baselineTorque))
                dTorque -= GetAeroTorque(conditions, AoA, pitchInput, dryTorque).x;
            else
                dTorque -= baselineTorque;
            return (dTorque / dLift) / MAC;
        }
        
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
            => Quaternion.AngleAxis((AoA * Mathf.Rad2Deg), Vector3.left) * force;
        public static Vector2 ToFlightFrame(Vector2 force, float AoA)
            => Quaternion.AngleAxis(AoA * Mathf.Rad2Deg, Vector3.forward) * force;
        public static Vector3 ToVesselFrame(Vector3 force, float AoA)
            => Quaternion.AngleAxis((-AoA * Mathf.Rad2Deg), Vector3.left) * force;
        public static Vector2 ToVesselFrame(Vector2 force, float AoA)
            => Quaternion.AngleAxis(-AoA * Mathf.Rad2Deg, Vector3.forward) * force;

        public static float GetUsefulThrustMagnitude(Vector3 thrustVector)
        {
            if (thrustVector.z > 0)
                return thrustVector.magnitude;
            return thrustVector.magnitude - thrustVector.z;
        }
        public static float GetUsefulThrustMagnitude(Vector2 thrustVector)
        {
            if (thrustVector.x > 0)
                return thrustVector.magnitude;
            return thrustVector.magnitude - thrustVector.x;
        }

        public static Vector3 InflowVect(float AoA)
        {
            Vector3 vesselForward = Vector3.forward;
            Vector3 vesselUp = Vector3.up;
            return vesselForward * Mathf.Cos(-AoA) + vesselUp * Mathf.Sin(-AoA);
        }

        public void WriteToFile(string directory, string filename, Graphing.GraphIO.FileFormat format)
        {
            if ((format & Graphing.GraphIO.FileFormat.Image) > 0)
                throw new ArgumentException($"Format is not supported. {format}");

            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            string path = Graphing.GraphIO.ValidateFilePath(directory, filename, format);

            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception ex) { Debug.Log($"Unable to delete file:{ex.Message}"); }

            System.Data.DataSet output = WriteToDataSet();

            if (output == null)
            {
                Debug.LogError($"This AeroPredictor does not implement an output method. {this.GetType()}");
                return;
            }

            if ((format & Graphing.GraphIO.FileFormat.Excel) > 0)
                WriteToFileXLS(path, output);
            else if (format == Graphing.GraphIO.FileFormat.CSV)
                WriteToFileCSV(path, output);
            else
            {
                output.Dispose();
                throw new NotImplementedException($"The selected format is not supported: {format}");
            }
            output.Dispose();
        }
        protected virtual System.Data.DataSet WriteToDataSet() => null;
        protected virtual void WriteToFileXLS(string path, System.Data.DataSet data)
        {
            MiniExcelLibs.MiniExcel.SaveAs(path, data, false, configuration: new MiniExcelLibs.OpenXml.OpenXmlConfiguration() { FastMode = true, AutoFilter = false, TableStyles = MiniExcelLibs.OpenXml.TableStyles.None, FreezeColumnCount = 1, FreezeRowCount = 1 });
        }
        protected virtual void WriteToFileCSV(string path, System.Data.DataSet data)
        {
            foreach (System.Data.DataTable table in data.Tables)
                MiniExcelLibs.MiniExcel.SaveAs(path.Insert(path.Length - 4, $"_{table.TableName}"), table, printHeader: false, excelType: MiniExcelLibs.ExcelType.CSV, configuration: new MiniExcelLibs.Csv.CsvConfiguration() { FastMode = true });
        }

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
