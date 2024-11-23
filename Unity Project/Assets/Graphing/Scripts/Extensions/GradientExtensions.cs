using System;
using UnityEngine;

namespace Graphing.Extensions
{
    public static class GradientExtensions
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom
        }

        /// <summary>
        /// Visually similar to MATLAB's Jet color map.
        /// </summary>
        public static readonly Gradient Jet = new Gradient()
        {
            colorKeys = new GradientColorKey[] {
                new GradientColorKey(new Color(0.5f, 1, 1), 0),
                new GradientColorKey(new Color(0.5f, 1, 0.5f), 1f / 3),
                new GradientColorKey(new Color(1, 1, 0.5f), 2f / 3),
                new GradientColorKey(new Color(1, 0.5f, 0.5f), 1)
            },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
        };
        public static Texture2D Jet_Tex
        {
            get
            {
                if (jet == null)
                    jet = Jet.CreateTexture(4, Direction.BottomToTop);
                return jet;
            }
        }
        private static Texture2D jet;
        /// <summary>
        /// Visually similar to MATLAB's Jet color map but with deeper blues.
        /// </summary>
        public static readonly Gradient Jet_Dark = new Gradient()
        {
            colorKeys = new GradientColorKey[] {
                new GradientColorKey(new Color(0.5f, 0.5f, 1), 0),
                new GradientColorKey(new Color(0.5f, 1, 1), 0.25f),
                new GradientColorKey(new Color(0.5f, 1, 0.5f), 0.5f),
                new GradientColorKey(new Color(1, 1, 0.5f), 0.75005f),
                new GradientColorKey(new Color(1, 0.5f, 0.5f), 1)
            },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
        };
        public static Texture2D Jet_Dark_Tex
        {
            get
            {
                if (jet_Dark == null)
                    jet_Dark = Jet_Dark.CreateTexture(5, Direction.LeftToRight);
                return jet_Dark;
            }
        }
        private static Texture2D jet_Dark;
        /// <summary>
        /// Visually similar to MATLAB's Jet color map but with deeper blues.
        /// </summary>
        public static readonly Gradient Grayscale = new Gradient()
        {
            colorKeys = new GradientColorKey[] {
                new GradientColorKey(new Color(0, 0, 1), 0),
                new GradientColorKey(new Color(1, 1, 1), 1)
            },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
        };
        public static Texture2D Grayscale_Tex
        {
            get
            {
                if (grayscale == null)
                    grayscale = Grayscale.CreateTexture(5, Direction.LeftToRight);
                return grayscale;
            }
        }
        private static Texture2D grayscale;

        public static Texture2D CreateTexture(this Gradient gradient, int resolution, Direction direction)
        {
            if (resolution <= 0)
                throw new ArgumentException("Resolution must be positive.");
            Texture2D result;
            if (direction <= Direction.RightToLeft)
                result = new Texture2D(resolution, 1, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            else
                result = new Texture2D(1, resolution, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            if (resolution == 1)
            {
                result.SetPixel(0, 0, Color.Lerp(gradient.Evaluate(0), gradient.Evaluate(1), 0.5f));
                return result;
            }
            Color32[] colors = new Color32[resolution];
            float recipResolution = (float)1 / (resolution - 1);
            if (direction == Direction.LeftToRight || direction == Direction.BottomToTop)
            {
                for (int i = resolution - 1; i >= 0; i--)
                {
                    colors[i] = gradient.Evaluate(i * recipResolution);
                }
            }
            else
            {
                for (int i = resolution - 1; i >= 0; i--)
                {
                    colors[i] = gradient.Evaluate(1 - i * recipResolution);
                }
            }
            result.SetPixels32(colors);
            result.Apply();
            return result;
        }
        /*public static Texture2D CreateTexture(this ColorMap colorMap, int resolution, Direction direction)
        {
            if (resolution <= 0)
                throw new ArgumentException("Resolution must be positive.");
            Texture2D result;
            if (direction <= Direction.RightToLeft)
                result = new Texture2D(resolution, 1, TextureFormat.ARGB32, false);
            else
                result = new Texture2D(1, resolution, TextureFormat.ARGB32, false);
            if (resolution == 1)
            {
                result.SetPixel(0, 0, Color.Lerp(colorMap[0], colorMap[1], 0.5f));
                return result;
            }
            Color32[] colors = new Color32[resolution];
            float recipResolution = (float)1 / (resolution - 1);
            if (direction == Direction.LeftToRight || direction == Direction.BottomToTop)
            {
                for (int i = resolution - 1; i >= 0; i--)
                {
                    colors[i] = colorMap[i * recipResolution];
                }
            }
            else
            {
                for (int i = resolution - 1; i >= 0; i--)
                {
                    colors[i] = colorMap[1 - i * recipResolution];
                }
            }
            result.SetPixels32(colors);
            result.Apply();
            return result;
        }*/
    }
}