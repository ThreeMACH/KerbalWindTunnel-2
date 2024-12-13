using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class HighlightManager : MonoBehaviour
    {
        public enum HighlightMode
        {
            Off = 0,
            Drag = 1,
            Lift = 2,
            DragOverMass = 3,
            DragOverLift = 4
        }

        private PartAeroData[] highlightingData;
        private readonly List<Part> highlightedParts = new List<Part>();

        public static readonly Gradient dragMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.red, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1) } };
        public static readonly Gradient liftMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.green, 0), new GradientColorKey(Color.green, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1) } };
        public static readonly Gradient drag_liftMap = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.green, 0), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.red, 1) }, alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 0.5f), new GradientAlphaKey(1, 1) } };

        // TODO: Add ability to change mode without recalculating. Listen for vessel modified.
        public void UpdateHighlighting(HighlightMode highlightMode, CelestialBody body, float altitude, float speed, float aoa)
        {
            ClearPartHighlighting();

            if (highlightMode == HighlightMode.Off)
                return;

            GenerateHighlightingData(EditorLogic.fetch.ship, body, altitude, speed, aoa);

            int count = highlightingData.Length;
            float min, max;
            Func<PartAeroData, float> highlightValueFunc;
            Gradient colorMap;
            switch (highlightMode)
            {
                case HighlightMode.Lift:
                    highlightValueFunc = (p) => p.lift;
                    colorMap = liftMap;
                    break;
                case HighlightMode.DragOverMass:
                    highlightValueFunc = (p) => p.drag / p.mass;
                    colorMap = dragMap;
                    break;
                case HighlightMode.DragOverLift:
                    highlightValueFunc = (p) => p.lift / p.drag;
                    colorMap = drag_liftMap;
                    break;
                case HighlightMode.Drag:
                default:
                    highlightValueFunc = (p) => p.drag;
                    colorMap = dragMap;
                    break;
            }
            if (!WindTunnelSettings.UseSingleColorHighlighting)
                colorMap = Graphing.Extensions.GradientExtensions.Jet;
            float[] highlightingDataResolved = highlightingData.Select(highlightValueFunc).ToArray();
            min = highlightingDataResolved.Where(f => !float.IsNaN(f) && !float.IsInfinity(f)).Min();
            max = highlightingDataResolved.Where(f => !float.IsNaN(f) && !float.IsInfinity(f)).Max();

            for (int i = 0; i < count; i++)
            {
                float value = (highlightingDataResolved[i] - min) / (max - min);
                HighlightPart(EditorLogic.fetch.ship.parts[i], colorMap.Evaluate(value));
            }
        }

        private void HighlightPart(Part part, Color color)
        {
            highlightedParts.Add(part);

            part.SetHighlightType(Part.HighlightType.AlwaysOn);
            part.SetHighlightColor(color);
            part.SetHighlight(true, false);
        }

        private void ClearPartHighlighting()
        {
            for (int i = highlightedParts.Count - 1; i >= 0; i--)
                highlightedParts[i]?.SetHighlightDefault();
            highlightedParts.Clear();
        }

        private void GenerateHighlightingData(ShipConstruct ship, CelestialBody body, float altitude, float speed, float aoa)
        {
            float mach, atmDensity;
            lock (body)
            {
                atmDensity = (float)Extensions.KSPClassExtensions.GetDensity(body, altitude);
                mach = speed / (float)body.GetSpeedOfSound(body.GetPressure(altitude), atmDensity);
            }

            int count = ship.parts.Count;
            highlightingData = new PartAeroData[count];

            Vector3 inflow = AeroPredictor.InflowVect(aoa);

            float pseudoReDragMult;
            lock (PhysicsGlobals.DragCurvePseudoReynolds)
                pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(atmDensity * speed);

            for (int i = 0; i < count; i++)
            {
                if (WindTunnelSettings.HighlightIgnoresLiftingSurfaces && ship.parts[i].HasModuleImplementing<ModuleLiftingSurface>())
                {
                    highlightingData[i] = new PartAeroData(ship.parts[i], 0, 0);
                    continue;
                }

                VesselCache.SimulatedPart simPart = VesselCache.SimulatedPart.Borrow(ship.parts[i], null);
                Vector3 partForce = simPart.GetAero(inflow, mach, pseudoReDragMult);

                ModuleLiftingSurface liftingSurface = ship.parts[i].FindModuleImplementing<ModuleLiftingSurface>();
                if (liftingSurface != null)
                {
                    VesselCache.SimulatedLiftingSurface simLiftSurf = VesselCache.SimulatedLiftingSurface.Borrow(liftingSurface, simPart);
                    partForce += simLiftSurf.GetForce(inflow, mach);
                    simLiftSurf.Release();
                }
                simPart.Release();
                //Vector3 partForce = highlightingVessel.parts[i].GetAero(inflow, mach, pseudoReDragMult);
                //Vector3 partForce = StockAeroUtil.SimAeroForce(body, new ShipConstruct("test", "", new List<Part>() { EditorLogic.fetch.ship.parts[i] }), inflow * speed, altitude);
                partForce = AeroPredictor.ToFlightFrame(partForce, aoa);  // (Quaternion.AngleAxis((aoa * 180 / Mathf.PI), Vector3.left) * partForce);

                highlightingData[i] = new PartAeroData(ship.parts[i], Math.Abs(partForce.z), Math.Abs(partForce.y));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDestroy()
        {
            ClearPartHighlighting();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity Method")]
        private void OnDisable()
        {
            ClearPartHighlighting();
        }

        public readonly struct PartAeroData
        {
            public readonly float drag;
            public readonly float lift;
            public readonly float mass;
            public readonly Part part;

            public PartAeroData(Part part, float drag, float lift)
            {
                this.part = part;
                this.drag = drag;
                this.lift = lift;
                mass = part.mass + part.GetResourceMass();
            }
        }
    }
}
