using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Graphing.Extensions
{
    public static class ColorMapMaterial
    {
        public enum Mode
        {
            Jet,
            Jet_Dark,
            Grayscale,
            Custom
        }
        public enum MapSource
        {
            Even,
            Texture,
            Alpha
        }

        public static void SetMin(this Material material, float min) => material.SetFloat("_Min", min);
        public static void SetMax(this Material material, float max) => material.SetFloat("_Max", max);
        public static void SetRange(this Material material, float min, float max)
        {
            material.SetFloat("_Min", min);
            material.SetFloat("_Max", max);
        }

        public static void SetMode(this Material material, Mode mode)
        {
            switch (mode)
            {
                case Mode.Jet:
                    material.EnableKeyword("_MODE_JET");
                    material.DisableKeyword("_MODE_CUSTOM");
                    material.DisableKeyword("_MODE_JET_DARK");
                    material.DisableKeyword("_MODE_GRAYSCALE");
                    break;
                case Mode.Jet_Dark:
                    material.EnableKeyword("_MODE_JET_DARK");
                    material.DisableKeyword("_MODE_CUSTOM");
                    material.DisableKeyword("_MODE_JET");
                    material.DisableKeyword("_MODE_GRAYSCALE");
                    break;
                case Mode.Grayscale:
                    material.EnableKeyword("_MODE_GRAYSCALE");
                    material.DisableKeyword("_MODE_CUSTOM");
                    material.DisableKeyword("_MODE_JET");
                    material.DisableKeyword("_MODE_JET_DARK");
                    break;
                case Mode.Custom:
                    material.EnableKeyword("_MODE_CUSTOM");
                    material.DisableKeyword("_MODE_JET");
                    material.DisableKeyword("_MODE_JET_DARK");
                    material.DisableKeyword("_MODE_GRAYSCALE");
                    break;

            }
        }

        public static void SetTexture(this Material material, Texture2D texture) => material.SetTexture("_ColorTex", texture);

        public static void SetColorMapSource(this Material material, MapSource source)
        {
            switch (source)
            {
                case MapSource.Even:
                    material.EnableKeyword("_MAPSOURCE_EVEN");
                    material.DisableKeyword("_MAPSOURCE_ALPHA");
                    break;
                case MapSource.Texture:
                    material.DisableKeyword("_MAPSOURCE_EVEN");
                    material.DisableKeyword("_MAPSOURCE_ALPHA");
                    break;
                case MapSource.Alpha:
                    material.EnableKeyword("_MAPSOURCE_ALPHA");
                    material.DisableKeyword("_MAPSOURCE_EVEN");
                    break;
            }
        }

        public static void SetMapTexture(this Material material, Texture2D texture) => material.SetTexture("_ValueMapTex", texture);

        public static void SetStep(this Material material, bool value) => material.SetInt("_Step", value ? 1 : 0);
        public static void SetClip(this Material material, bool value) => material.SetInt("_Clip", value ? 1 : 0);

        public static void SetContourMapSource(this Material material, MapSource source)
        {
            switch (source)
            {
                case MapSource.Even:
                    material.EnableKeyword("_CONTOURMAPSOURCE_EVEN");
                    material.DisableKeyword("_CONTOURMAPSOURCE_ALPHA");
                    break;
                case MapSource.Texture:
                    material.DisableKeyword("_CONTOURMAPSOURCE_EVEN");
                    material.DisableKeyword("_CONTOURMAPSOURCE_ALPHA");
                    break;
                case MapSource.Alpha:
                    material.EnableKeyword("_CONTOURMAPSOURCE_ALPHA");
                    material.DisableKeyword("_CONTOURMAPSOURCE_EVEN");
                    break;
            }
        }

        public static void GenerateTextures(Gradient gradient, ref Texture2D colorTex, ref Texture2D mapTex, Texture2D axisTex)
        {
            SortedSet<float> indices = new SortedSet<float>();
            foreach (var key in gradient.colorKeys)
                indices.Add(key.time);
            foreach (var key in gradient.alphaKeys)
                indices.Add(key.time);

            if (colorTex == null)
                colorTex = new Texture2D(indices.Count, 1, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (mapTex == null)
                mapTex = new Texture2D(indices.Count, 1, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

            colorTex.SetPixels(indices.Select(gradient.Evaluate).ToArray());
            colorTex.Apply();
            mapTex.SetPixels(indices.Select(i => new Color(i, i, i)).ToArray());
            mapTex.Apply();

            if (axisTex == null)
                return;

            const int axisResolution = 2520;    // Divisible by all integers [1,10] and 12, 15, and 20
            int width = axisResolution / GCD(indices.Select(f => (int)(f * axisResolution)));
            axisTex.Resize(width + 1, 1);
            float invWidth = 1f / width;
            for (int i = 0; i <= width; i++)
                axisTex.SetPixel(i, 1, gradient.Evaluate(i * invWidth));
            axisTex.Apply();
        }

        private static int GCD(IEnumerable<int> values)
            => values.Aggregate(0, GCD);

        private static int GCD(int a, int b)
        {
            if (a < 0) a = -a;
            if (b < 0) b = -b;

            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return (a | b);
        }
    }
}
