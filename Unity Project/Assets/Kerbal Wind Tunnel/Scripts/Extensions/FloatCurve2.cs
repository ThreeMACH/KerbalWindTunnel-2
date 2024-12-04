using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class FloatCurve2
    {
        public readonly float[] xKeys;
        public readonly float[] yKeys;
        public readonly Keyframe2[,] values;

        private readonly Dictionary<(int, int), float[]> coeffCache;
        private readonly Dictionary<(int, int), int> coeffCacheHash;

        public (int, int) Size { get => (xKeys.Length, yKeys.Length); }
        public int Length { get { return values.Length; } }

        public int GetUpperBound(int dimension) { return dimension == 0 ? xKeys.Length : yKeys.Length; }

        public FloatCurve2(IEnumerable<float> xKeys, IEnumerable<float> yKeys)
        {
            this.xKeys = xKeys.ToArray();
            this.yKeys = yKeys.ToArray();

            values = new Keyframe2[this.xKeys.Length, this.yKeys.Length];
            Array.Sort(this.xKeys);
            Array.Sort(this.yKeys);
            coeffCache = new Dictionary<(int, int), float[]>();
            coeffCacheHash = new Dictionary<(int, int), int>();
        }

        public FloatCurve2(IEnumerable<float> xKeys, IEnumerable<float> yKeys, float[,] values) : this(xKeys, yKeys)
        {
            int xLength = this.xKeys.Length;
            int yLength = this.yKeys.Length;

            if ((values.GetUpperBound(0) + 1 != xLength) || (values.GetUpperBound(1) + 1 != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "values");

            for (int i = xLength - 2; i >= 1; i--)
            {
                float xDiff = this.xKeys[i + 1] - this.xKeys[i - 1];
                for (int j = yLength - 2; j >= 1; j--)
                {
                    this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                        (values[i + 1, j] - values[i - 1, j]) / xDiff,
                        (values[i, j + 1] - values[i, j - 1]) / (this.yKeys[j + 1] - this.yKeys[j - 1]),
                        (values[i + 1, j + 1] - values[i + 1, j - 1] - values[i - 1, j + 1] + values[i - 1, j - 1]) / (xDiff * (this.yKeys[j + 1] - this.yKeys[j - 1])));
                }
            }
            for (int i = xLength - 2; i >= 1; i--)
            {
                float xDiff = this.xKeys[i + 1] - this.xKeys[i - 1];
                float yDiff0 = this.yKeys[1] - this.yKeys[0];
                float yDiff1 = this.yKeys[yLength - 1] - this.yKeys[yLength - 2];

                int j = 0;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (values[i + 1, j] - values[i - 1, j]) / (this.xKeys[i + 1] - this.xKeys[i - 1]),
                    (-3 * values[i, j] + 3 * values[i, j + 1] - this.values[i, j + 1].dDy_in) / 2,
                    (values[i + 1, j + 1] - values[i + 1, j] - values[i - 1, j + 1] + values[i - 1, j]) / (xDiff * yDiff0));

                j = yLength - 1;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (values[i + 1, j] - values[i - 1, j]) / (this.xKeys[i + 1] - this.xKeys[i - 1]),
                    (-3 * values[i, j] - 3 * values[i, j - 1] + this.values[i, j - 1].dDy_in) / 2,
                    (values[i + 1, j] - values[i + 1, j - 1] - values[i - 1, j] + values[i - 1, j - 1]) / (xDiff * yDiff1));
            }
            for (int j = yLength - 2; j >= 1; j--)
            {
                float yDiff = this.yKeys[j + 1] - this.yKeys[j - 1];
                float xDiff0 = this.xKeys[1] - this.xKeys[0];
                float xDiff1 = this.xKeys[xLength - 1] - this.xKeys[xLength - 2];

                int i = 0;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] + 3 * values[i + 1, j] - this.values[i + 1, j].dDx_in) / 2,
                    (values[i, j + 1] - values[i, j - 1]) / yDiff,
                    (values[i + 1, j + 1] - values[i + 1, j - 1] - values[i, j + 1] + values[i, j - 1]) / (xDiff0 * yDiff));

                i = xLength - 1;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] - 3 * values[i - 1, j] + this.values[i - 1, j].dDx_in) / 2,
                    (values[i, j + 1] - values[i, j - 1]) / yDiff,
                    (values[i, j + 1] - values[i, j - 1] - values[i - 1, j + 1] + values[i - 1, j - 1]) / (xDiff1 * yDiff));
            }
            {   // Braces to keep this int i and j confined to allow consisten variable nomenclature.
                int i, j;
                i = j = 0;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] + 3 * values[i + 1, j] - this.values[i + 1, j].dDx_in) / 2,
                    (-3 * values[i, j] + 3 * values[i, j + 1] - this.values[i, j + 1].dDy_in) / 2,
                    (values[i + 1, j + 1] - values[i + 1, j] - values[i, j + 1] + values[i, j]) / ((this.xKeys[i + 1] - this.xKeys[i]) * (this.yKeys[j + 1] - this.yKeys[j])));

                j = yLength - 1;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] + 3 * values[i + 1, j] - this.values[i + 1, j].dDx_in) / 2,
                    (-3 * values[i, j] - 3 * values[i, j - 1] + this.values[i, j - 1].dDy_in) / 2,
                    (values[i + 1, j] - values[i + 1, j - 1] - values[i, j] + values[i, j - 1]) / ((this.xKeys[i + 1] - this.xKeys[i]) * (this.yKeys[j] - this.yKeys[j - 1])));

                i = xLength - 1;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] - 3 * values[i - 1, j] + this.values[i - 1, j].dDx_in) / 2,
                    (-3 * values[i, j] - 3 * values[i, j - 1] + this.values[i, j - 1].dDy_in) / 2,
                    (values[i, j] - values[i, j - 1] - values[i - 1, j] + values[i - 1, j - 1]) / ((this.xKeys[i] - this.xKeys[i - 1]) * (this.yKeys[j] - this.yKeys[j - 1])));


                j = 0;
                this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j],
                    (-3 * values[i, j] - 3 * values[i - 1, j] + this.values[i - 1, j].dDx_in) / 2,
                    (-3 * values[i, j] + 3 * values[i, j + 1] - this.values[i, j + 1].dDy_in) / 2,
                    (values[i, j + 1] - values[i, j] - values[i - 1, j + 1] + values[i - 1, j]) / ((this.xKeys[i] - this.xKeys[i - 1]) * (this.yKeys[j + 1] - this.yKeys[j])));
            }
        }

        public FloatCurve2(IEnumerable<float> xKeys, IEnumerable<float> yKeys, float[,] values, float[,] xPartialTangents, float[,] yPartialTangents, float[,] mixedTangents) : this(xKeys, yKeys)
        {
            int xLength = this.xKeys.Length - 1;
            int yLength = this.yKeys.Length - 1;

            if ((values.GetUpperBound(0) != xLength) || (values.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "values");
            if ((xPartialTangents.GetUpperBound(0) != xLength) || (xPartialTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xPartialTangents");
            if ((yPartialTangents.GetUpperBound(0) != xLength) || (yPartialTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "yPartialTangents");
            if ((mixedTangents.GetUpperBound(0) != xLength) || (mixedTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "mixedTangents");

            for (int i = xLength; i >= 0; i--)
                for (int j = yLength; j >= 0; j--)
                    this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j], xPartialTangents[i, j], yPartialTangents[i, j], mixedTangents[i, j]);
        }

        public FloatCurve2(IEnumerable<float> xKeys, IEnumerable<float> yKeys, float[,] values, float[,] xinTangents, float[,] xoutTangents, float[,] yinTangents, float[,] youtTangents, float[,] xinyinTangents, float[,] xinyoutTangents, float[,] xoutyinTangents, float[,] xoutyoutTangents) : this(xKeys, yKeys)
        {
            int xLength = this.xKeys.Length - 1;
            int yLength = this.yKeys.Length - 1;

            if ((values.GetUpperBound(0) != xLength) || (values.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "values");

            if ((xinTangents.GetUpperBound(0) != xLength) || (xinTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xinTangents");
            if ((xoutTangents.GetUpperBound(0) != xLength) || (xoutTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xoutTangents");
            if ((yinTangents.GetUpperBound(0) != xLength) || (yinTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "yinTangents");
            if ((youtTangents.GetUpperBound(0) != xLength) || (youtTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "youtTangents");

            if ((xinyinTangents.GetUpperBound(0) != xLength) || (xinyinTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xinyinTangents");
            if ((xinyoutTangents.GetUpperBound(0) != xLength) || (xinyoutTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xinyoutTangents");
            if ((xoutyinTangents.GetUpperBound(0) != xLength) || (xoutyinTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xoutyinTangents");
            if ((xoutyoutTangents.GetUpperBound(0) != xLength) || (xoutyoutTangents.GetUpperBound(1) != yLength))
                throw new ArgumentException("Array dimensions do not match provided keys.", "xoutyoutTangents");


            for (int i = xLength; i >= 0; i--)
                for (int j = yLength; j >= 0; j--)
                    this.values[i, j] = new Keyframe2(this.xKeys[i], this.yKeys[j], values[i, j], xinTangents[i, j], xoutTangents[i, j], yinTangents[i, j], youtTangents[i, j], xinyinTangents[i, j], xinyoutTangents[i, j], xoutyinTangents[i, j], xoutyoutTangents[i, j]);
        }

        public static FloatCurve2 ComputeCurve(IEnumerable<float> xKeys, IEnumerable<float> yKeys, Func<float, float, float> func, DiffSettings settings = new DiffSettings())
            => ComputeCurve(xKeys.Select(k => (k, false)), yKeys.Select(k => (k, false)), func, settings);
        public static FloatCurve2 ComputeCurve(IEnumerable<(float value, bool continuousDerivative)> xKeys, IEnumerable<(float value, bool continuousDerivative)> yKeys, Func<float, float, float> func, DiffSettings settings = new DiffSettings())
        {
            IList<(float value, bool continuousDerivative)> xKeys_ = xKeys as IList<(float, bool)> ?? xKeys.ToArray();
            IList<(float value, bool continuousDerivative)> yKeys_ = yKeys as IList<(float, bool)> ?? yKeys.ToArray();

            FloatCurve2 curve = new FloatCurve2(xKeys_.Select(k => k.value), yKeys_.Select(k => k.value));
            int lx = xKeys_.Count - 1, ly = yKeys_.Count - 1;


            for (int i = lx; i >= 0; i--)
            {
                float deltaX = xKeys_[i].continuousDerivative ? settings.dx_continuous : settings.dx;
                float invDeltaX = 1 / deltaX;
                for (int j = ly; j >= 0; j--)
                {
                    float deltaY = yKeys_[j].continuousDerivative ? settings.dy_continuous : settings.dy;
                    float invDeltaY = 1 / deltaY;
                    float invDelta2 = invDeltaX * invDeltaY;

                    // Mapping is as follows:
                    //  6   2   5
                    //  3   0   1
                    //  8   4   7

                    Span<float> values = stackalloc float[9];

                    // Calculate base value
                    values[0] = func(xKeys_[i].value, yKeys_[j].value);
                    // Calculate X differential values (1 and 3)
                    if (i < lx)
                    {
                        values[1] = func(xKeys_[i].value + deltaX, yKeys_[j].value);
                        if (i > 0)
                        {
                            if (xKeys_[i].continuousDerivative)
                                values[3] = 2 * values[0] - values[1];
                            else
                                values[3] = func(xKeys_[i].value - deltaX, yKeys_[j].value);
                        }
                        else
                            values[3] = values[0];
                    }
                    else if (i > 0)
                    {
                        values[3] = func(xKeys_[i].value - deltaX, yKeys_[j].value);
                        values[1] = values[0];
                    }
                    else
                    {
                        values[1] = values[0];
                        values[3] = values[0];
                    }
                    // Calculate Y differential values (2 and 4)
                    if (j < ly)
                    {
                        values[2] = func(xKeys_[i].value, yKeys_[j].value + deltaY);
                        if (j > 0)
                        {
                            if (yKeys_[j].continuousDerivative)
                                values[4] = 2 * values[0] - values[2];
                            else
                                values[4] = func(xKeys_[i].value, yKeys_[j].value - deltaY);
                        }
                        else
                            values[4] = values[0];
                    }
                    else if (j > 0)
                    {
                        values[4] = func(xKeys_[i].value, yKeys_[j].value - deltaY);
                        values[2] = values[0];
                    }
                    else
                    {
                        values[2] = values[0];
                        values[4] = values[0];
                    }
                    // Calculate combined differential values (5, 6, 7, and 8)
                    if (i < lx && j < ly)
                    {
                        values[5] = func(xKeys_[i].value + deltaX, yKeys_[j].value + deltaY);
                        if (i > 0)
                        {
                            if (xKeys_[i].continuousDerivative)
                                values[6] = 2 * values[2] - values[5];
                            else
                                values[6] = func(xKeys_[i].value - deltaX, yKeys_[j].value + deltaY);
                        }
                        else
                            values[6] = values[0];
                        if (j > 0)
                        {
                            if (yKeys_[j].continuousDerivative)
                                values[7] = 2 * values[1] - values[5];
                            else
                                values[7] = func(xKeys_[i].value + deltaX, yKeys_[j].value - deltaY);
                        }
                        else
                            values[7] = values[0];
                        if (i > 0 && j > 0)
                        {
                            if (xKeys_[i].continuousDerivative && yKeys_[j].continuousDerivative)
                                values[8] = 2 * values[0] - values[5];
                            else if (xKeys_[i].continuousDerivative)
                                values[8] = 2 * values[4] - values[7];
                            else if (yKeys_[j].continuousDerivative)
                                values[8] = 2 * values[2] - values[6];
                            else
                                values[8] = func(xKeys_[i].value - deltaX, yKeys_[j].value - deltaY);
                        }
                        else
                            values[8] = values[0];
                    }
                    else if (i > 0 && j < ly)   // i >= lx  5 and 7 are not used
                    {
                        values[5] = values[0];
                        values[7] = values[0];
                        values[6] = values[6] = func(xKeys_[i].value - deltaX, yKeys_[j].value + deltaY);
                        if (j > 0)
                        {
                            if (yKeys_[j].continuousDerivative)
                                values[8] = 2 * values[2] - values[6];
                            else
                                values[8] = func(xKeys_[i].value - deltaX, yKeys_[j].value - deltaY);
                        }
                        else
                            values[8] = values[0];
                    }
                    else if (i < lx && j > 0)   // j >= ly  5 and 6 are not used
                    {
                        values[5] = values[0];
                        values[6] = values[0];
                        values[7] = func(xKeys_[i].value + deltaX, yKeys_[j].value - deltaY);
                        if (i > 0)
                        {
                            if (xKeys_[i].continuousDerivative)
                                values[8] = 2 * values[4] - values[7];
                            else
                                values[8] = func(xKeys_[i].value - deltaX, yKeys_[j].value - deltaY);
                        }
                        else
                            values[8] = values[0];
                    }
                    else if (i > 0 && j > 0)    // i >= lx & j >= ly    5, 6, and 7 are not used
                    {
                        values[5] = values[0];
                        values[6] = values[0];
                        values[7] = values[0];
                        values[8] = func(xKeys_[i].value - deltaX, yKeys_[j].value - deltaY);
                    }
                    else
                    {
                        values[5] = values[0];
                        values[6] = values[0];
                        values[7] = values[0];
                        values[8] = values[0];
                    }

                    // Mapping is as follows:
                    //  6   2   5
                    //  3   0   1
                    //  8   4   7

                    // 'Values' becomes differentials here to save allocations
                    float values2 = values[2];
                    float values4 = values[4];
                    values[1] = (values[1] - values[0]) * invDeltaX;
                    values[3] = (values[0] - values[3]) * invDeltaX;
                    values[2] = (values[2] - values[0]) * invDeltaY;
                    values[4] = (values[0] - values[4]) * invDeltaY;
                    values[5] = ((values[5] - values2) * invDeltaX - values[1]) * invDeltaY;
                    values[6] = ((values2 - values[6]) * invDeltaX - values[3]) * invDeltaY;
                    values[7] = (values[1] - (values[7] - values4) * invDeltaX) * invDeltaY;
                    values[8] = (values[3] - (values4 - values[8]) * invDeltaX) * invDeltaY;

                    curve.values[i, j] = new Keyframe2(
                        xKeys_[i].value, yKeys_[j].value,
                        values[0],
                        values[3], values[1],
                        values[4], values[2],
                        values[8], values[6], values[7], values[5]);
                }
            }
            return curve;
        }

        protected static int FindIndex(float[] keys, float value, out bool exact)
        {
            int result = Array.BinarySearch(keys, value);
            if (result >= 0)
            {
                exact = true;
                return result;
            }
            exact = false;
            return (~result) - 1;
        }

        private float[] GetCoeffs(float timeX, float timeY, out float normalizedX, out float normalizedY, bool prefer1 = false)
        {
            int xSquare;// = Array.FindIndex(xTimes, x => timeX < x) - 1;
            int ySquare;// = Array.FindIndex(yTimes, y => timeY < y) - 1;
            bool exactX, exactY;
            if (timeX <= xKeys[0])
            {
                xSquare = 0;
                timeX = xKeys[0];
                exactX = true;
            }
            else if (timeX > xKeys[xKeys.Length - 1])
            {
                xSquare = xKeys.Length - 2;
                timeX = xKeys[xKeys.Length - 1];
                exactX = true;
            }
            else
            {
                //xSquare = Array.FindIndex(xKeys, x => timeX < x) - 1;
                xSquare = FindIndex(xKeys, timeX, out exactX);
                if (prefer1 && exactX)
                    xSquare -= 1;
            }

            if (timeY <= yKeys[0])
            {
                ySquare = 0;
                timeY = yKeys[0];
                exactY = true;
            }
            else if (timeY > yKeys[yKeys.Length - 1])
            {
                ySquare = yKeys.Length - 2;
                timeY = yKeys[yKeys.Length - 1];
                exactY = true;
            }
            else
            {
                ySquare = FindIndex(yKeys, timeY, out exactY);
                if (prefer1 && exactY)
                    ySquare -= 1;
            }

            (int, int) squareIndex = (xSquare, ySquare);

            float dx = (xKeys[xSquare + 1] - xKeys[xSquare]);
            float dy = (yKeys[ySquare + 1] - yKeys[ySquare]);
            normalizedX = Mathf.Clamp01((timeX - xKeys[xSquare]) / dx);
            normalizedY = Mathf.Clamp01((timeY - yKeys[ySquare]) / dy);

            Span<float> knowns = stackalloc float[16] {
                    values[xSquare,ySquare].value,
                    values[xSquare + 1,ySquare].value,
                    values[xSquare,ySquare + 1].value,
                    values[xSquare + 1,ySquare + 1].value,
                    values[xSquare,ySquare].dDx_out * dx,
                    values[xSquare + 1,ySquare].dDx_in * dx,
                    values[xSquare,ySquare + 1].dDx_out * dx,
                    values[xSquare + 1,ySquare + 1].dDx_in * dx,
                    values[xSquare,ySquare].dDy_out * dy,
                    values[xSquare + 1,ySquare].dDy_out * dy,
                    values[xSquare,ySquare + 1].dDy_in * dy,
                    values[xSquare + 1,ySquare + 1].dDy_in * dy,
                    values[xSquare,ySquare].ddDx_out_Dy_out * dx * dy,
                    values[xSquare + 1,ySquare].ddDx_in_Dy_out * dx * dy,
                    values[xSquare,ySquare + 1].ddDx_out_Dy_in * dx * dy,
                    values[xSquare + 1,ySquare + 1].ddDx_in_Dy_in * dx * dy
                };

            HashCode knownsHashCode = new HashCode();
            for (int i = 0; i <= 15; i++)
                knownsHashCode.Add(knowns[i]);
            int knownsHash = knownsHashCode.ToHashCode();

            if (!coeffCache.ContainsKey(squareIndex) || coeffCacheHash[squareIndex] != knownsHash)
            {
                float[] coeffs = new float[16] {
                        1 * knowns[0],
                        1 * knowns[4],
                        -3 * knowns[0] + 3 * knowns[1] - 2 * knowns[4] - 1 * knowns[5],
                        2 * knowns[0] - 2 * knowns[1] + 1 * knowns[4] + 1 * knowns[5],
                        1 * knowns[8],
                        1 * knowns[12],
                        -3 * knowns[8] + 3 * knowns[9] - 2 * knowns[12] - 1 * knowns[13],
                        2 * knowns[8] - 2 * knowns[9] + 1 * knowns[12] + 1 * knowns[13],
                        -3 * knowns[0] + 3 * knowns[2] - 2 * knowns[8] - 1 * knowns[10],
                        -3 * knowns[4] + 3 * knowns[6] - 2 * knowns[12] - 1 * knowns[14],
                        9 * knowns[0] - 9 * knowns[1] - 9 * knowns[2] + 9 * knowns[3] + 6 * knowns[4] + 3 * knowns[5] - 6 * knowns[6] - 3 * knowns[7] + 6 * knowns[8] - 6 * knowns[9] + 3 * knowns[10] - 3 * knowns[11] + 4 * knowns[12] + 2 * knowns[13] + 2 * knowns[14] + 1 * knowns[15],
                        -6 * knowns[0] + 6 * knowns[1] + 6 * knowns[2] - 6 * knowns[3] - 3 * knowns[4] - 3 * knowns[5] + 3 * knowns[6] + 3 * knowns[7] - 4 * knowns[8] + 4 * knowns[9] - 2 * knowns[10] + 2 * knowns[11] - 2 * knowns[12] - 2 * knowns[13] - 1 * knowns[14] - 1 * knowns[15],
                        2 * knowns[0] - 2 * knowns[2] + 1 * knowns[8] + 1 * knowns[10],
                        2 * knowns[4] - 2 * knowns[6] + 1 * knowns[12] + 1 * knowns[14],
                        -6 * knowns[0] + 6 * knowns[1] + 6 * knowns[2] - 6 * knowns[3] - 4 * knowns[4] - 2 * knowns[5] + 4 * knowns[6] + 2 * knowns[7] - 3 * knowns[8] + 3 * knowns[9] - 3 * knowns[10] + 3 * knowns[11] - 2 * knowns[12] - 1 * knowns[13] - 2 * knowns[14] - 1 * knowns[15],
                        4 * knowns[0] - 4 * knowns[1] - 4 * knowns[2] + 4 * knowns[3] + 2 * knowns[4] + 2 * knowns[5] - 2 * knowns[6] - 2 * knowns[7] + 2 * knowns[8] - 2 * knowns[9] + 2 * knowns[10] - 2 * knowns[11] + 1 * knowns[12] + 1 * knowns[13] + 1 * knowns[14] + 1 * knowns[15]
                        };

                lock (coeffCache)
                {
                    coeffCacheHash[squareIndex] = knownsHash;

                    coeffCache[squareIndex] = coeffs;

                    return coeffs;
                }
            }
            return coeffCache[squareIndex];
        }

        public float Evaluate(float timeX, float timeY)
        {
            float[] coeffs = GetCoeffs(timeX, timeY, out float x, out float y);

            float x2 = x * x;
            float x3 = x2 * x;
            float y2 = y * y;
            float y3 = y2 * y;

            return (coeffs[0] + coeffs[1] * x + coeffs[2] * x2 + coeffs[3] * x3) +
                (coeffs[4] + coeffs[5] * x + coeffs[6] * x2 + coeffs[7] * x3) * y +
                (coeffs[8] + coeffs[9] * x + coeffs[10] * x2 + coeffs[11] * x3) * y2 +
                (coeffs[12] + coeffs[13] * x + coeffs[14] * x2 + coeffs[15] * x3) * y3;
        }

        public float EvaluateDerivative(float timeX, float timeY, (int, int) dimension, bool prefer1 = false)
        {
            float[] coeffs = GetCoeffs(timeX, timeY, out float x, out float y, prefer1);

            float x2 = x * x;
            float x3 = x2 * x;
            float y2 = y * y;
            float y3 = y2 * y;

            if (dimension.Equals((1, 0)))
            {
                if (timeX < xKeys[0] || timeX > xKeys[xKeys.Length - 1])
                    return 0;
                else
                    return (coeffs[1] + 2 * coeffs[2] * x + 3 * coeffs[3] * x2) +
                        (coeffs[5] + 2 * coeffs[6] * x + 3 * coeffs[7] * x2) * y +
                        (coeffs[9] + 2 * coeffs[10] * x + 3 * coeffs[11] * x2) * y2 +
                        (coeffs[13] + 2 * coeffs[14] * x + 3 * coeffs[15] * x2) * y3;
            }
            else if (dimension.Equals((0, 1)))
            {
                if (timeY < yKeys[0] || timeY > yKeys[yKeys.Length - 1])
                    return 0;
                else
                    return (coeffs[4] + coeffs[5] * x + coeffs[6] * x2 + coeffs[7] * x3) +
                        2 * (coeffs[8] + coeffs[9] * x + coeffs[10] * x2 + coeffs[11] * x3) * y +
                        3 * (coeffs[12] + coeffs[13] * x + coeffs[14] * x2 + coeffs[15] * x3) * y2;
            }
            else if (dimension.Equals((1, 1)))
            {
                if ((timeX < xKeys[0] || timeX > xKeys[xKeys.Length - 1]) && (timeY < yKeys[0] || timeY > yKeys[yKeys.Length - 1]))
                    return 0;
                else
                    return (coeffs[5] + 2 * coeffs[6] * x + 3 * coeffs[7] * x2) +
                        2 * (coeffs[9] + 2 * coeffs[10] * x + 3 * coeffs[11] * x2) * y +
                        3 * (coeffs[13] + 2 * coeffs[14] * x + 3 * coeffs[15] * x2) * y2;
            }
            else
                throw new ArgumentOutOfRangeException("dimension");
        }

        public static FloatCurve2 Superposition(IEnumerable<FloatCurve2> curves)
        {
            SortedSet<float> xKeys = new SortedSet<float>();
            SortedSet<float> yKeys = new SortedSet<float>();
            foreach (FloatCurve2 curve in curves)
            {
                if (curve == null) continue;
                xKeys.UnionWith(curve.xKeys);
                yKeys.UnionWith(curve.yKeys);
            }
            return Superposition(curves, xKeys.ToArray(), yKeys.ToArray());
        }
        public static FloatCurve2 Superposition(IEnumerable<FloatCurve2> curves, IEnumerable<float> xKeys, IEnumerable<float> yKeys)
        {
            float[] xKeys_ = xKeys.Distinct().ToArray();
            float[] yKeys_ = yKeys.Distinct().ToArray();
            Array.Sort(xKeys_);
            Array.Sort(yKeys_);
            return Superposition(curves, xKeys_, yKeys_);
        }
        public static FloatCurve2 Superposition(IEnumerable<FloatCurve2> curves, IList<float> xKeys, IList<float> yKeys)
        {
            int xLength = xKeys.Count;
            int yLength = yKeys.Count;
            float[,] values = new float[xLength, yLength];
            float[,] dDx_in = new float[xLength, yLength];
            float[,] dDx_out = new float[xLength, yLength];
            float[,] dDy_in = new float[xLength, yLength];
            float[,] dDy_out = new float[xLength, yLength];
            float[,] ddDx_in_Dy_in = new float[xLength, yLength];
            float[,] ddDx_in_Dy_out = new float[xLength, yLength];
            float[,] ddDx_out_Dy_in = new float[xLength, yLength];
            float[,] ddDx_out_Dy_out = new float[xLength, yLength];

            foreach (FloatCurve2 curve in curves)
            {
                if (curve == null) continue;
                for (int i = xLength - 1; i >= 0; i--)
                {
                    float xTime = xKeys[i];
                    for (int j = yLength - 1; j >= 0; j--)
                    {
                        float yTime = yKeys[j];
                        if (curve.xKeys.Contains(xTime) && curve.yKeys.Contains(yTime))
                        {
                            Keyframe2 value = curve.values[Array.IndexOf(curve.xKeys, xTime), Array.IndexOf(curve.yKeys, yTime)];
                            values[i, j] += value.value;
                            dDx_in[i, j] += value.dDx_in;
                            dDx_out[i, j] += value.dDx_out;
                            dDy_in[i, j] += value.dDy_in;
                            dDy_out[i, j] += value.dDy_out;
                            ddDx_in_Dy_in[i, j] += value.ddDx_in_Dy_in;
                            ddDx_in_Dy_out[i, j] += value.ddDx_in_Dy_out;
                            ddDx_out_Dy_in[i, j] += value.ddDx_out_Dy_in;
                            ddDx_out_Dy_out[i, j] += value.ddDx_out_Dy_out;
                            continue;
                        }
                        values[i, j] += curve.Evaluate(xTime, yTime);
                        float ddx = curve.EvaluateDerivative(xTime, yTime, (0, 1));
                        float ddy = curve.EvaluateDerivative(xTime, yTime, (1, 0));
                        float dddxdy = curve.EvaluateDerivative(xTime, yTime, (1, 1));
                        dDx_out[i, j] += ddx;
                        dDy_out[i, j] += ddy;
                        if (curve.xKeys.Contains(xTime))
                        {
                            dDx_in[i, j] += curve.EvaluateDerivative(xTime, yTime, (0, 1), true);
                            float cross = curve.EvaluateDerivative(xTime, yTime, (1, 1), true);
                            ddDx_in_Dy_in[i, j] += cross;
                            ddDx_in_Dy_out[i, j] += cross;
                            ddDx_out_Dy_in[i, j] += dddxdy;
                            ddDx_out_Dy_out[i, j] += dddxdy;
                        }
                        else if (curve.yKeys.Contains(yTime))
                        {
                            dDy_in[i, j] += curve.EvaluateDerivative(xTime, yTime, (0, 1), true);
                            float cross = curve.EvaluateDerivative(xTime, yTime, (1, 1), true);
                            ddDx_in_Dy_in[i, j] += cross;
                            ddDx_out_Dy_in[i, j] += cross;
                            ddDx_in_Dy_out[i, j] += dddxdy;
                            ddDx_out_Dy_out[i, j] += dddxdy;
                        }
                        else
                        {
                            dDx_in[i, j] += ddx;
                            dDy_in[i, j] += ddy;
                            ddDx_in_Dy_in[i, j] += dddxdy;
                            ddDx_in_Dy_out[i, j] += dddxdy;
                            ddDx_out_Dy_in[i, j] += dddxdy;
                            ddDx_out_Dy_out[i, j] += dddxdy;
                        }
                    }
                }
            }
            return new FloatCurve2(xKeys, yKeys, values, dDx_in, dDx_out, dDy_in, dDy_out, ddDx_in_Dy_in, ddDx_in_Dy_out, ddDx_out_Dy_in, ddDx_out_Dy_out);
        }

        public struct Keyframe2
        {
            public float timeX;
            public float timeY;
            public float value;
            public float dDx_in;
            public float dDx_out;
            public float dDy_in;
            public float dDy_out;
            public float ddDx_in_Dy_in;
            public float ddDx_in_Dy_out;
            public float ddDx_out_Dy_in;
            public float ddDx_out_Dy_out;

            public Keyframe2(float timeX, float timeY, float value, float dDx_in, float dDx_out, float dDy_in, float dDy_out, float ddDx_in_Dy_in, float ddDx_in_Dy_out, float ddDx_out_Dy_in, float ddDx_out_Dy_out)
            {
                this.timeX = timeX;
                this.timeY = timeY;
                this.value = value;
                this.dDx_in = dDx_in;
                this.dDx_out = dDx_out;
                this.dDy_in = dDy_in;
                this.dDy_out = dDy_out;
                this.ddDx_in_Dy_in = ddDx_in_Dy_in;
                this.ddDx_in_Dy_out = ddDx_in_Dy_out;
                this.ddDx_out_Dy_in = ddDx_out_Dy_in;
                this.ddDx_out_Dy_out = ddDx_out_Dy_out;
            }

            public Keyframe2(float timeX, float timeY, float value, float dDx, float dDy, float ddDyDx) : this(timeX, timeY, value, dDx, dDx, dDy, dDy, ddDyDx, ddDyDx, ddDyDx, ddDyDx) { }

            public static Keyframe2 operator +(Keyframe2 key, float value)
            {
                key.value += value;
                return key;
            }
            /*public static Keyframe2 operator + (Keyframe2 key1, Keyframe2 key2)
            {
                if (key1.timeX != key2.timeX || key1.timeY != key2.timeY)
                    throw new ArgumentException("The given keys did not match coordinates.");
                return new Keyframe2(key1.timeX, key2.timeX, key1.value + key2.value, key1.dDx + key2.dDx, key1.dDy + key2.dDy, key1.ddDxDy + key2.ddDxDy);
            }*/
            public static implicit operator float(Keyframe2 key)
            {
                return key.value;
            }
        }

        public readonly struct DiffSettings
        {
            public readonly float dx;
            public readonly float dy;
            public readonly float dx_continuous;
            public readonly float dy_continuous;
            public DiffSettings(float dx = 1E-6f, float dy = 1E-6f)
            {
                this.dx = dx;
                this.dx_continuous = dx;
                this.dy = dy;
                this.dy_continuous = dy;
            }
            public DiffSettings(float dx = 1E-6f, float dy = 1E-6f, float dx_continuous = 1E-6f, float dy_continuous = 1E-6f)
            {
                this.dx = dx;
                this.dx_continuous = dx_continuous;
                this.dy = dy;
                this.dy_continuous = dy_continuous;
            }
        }
    }
}
