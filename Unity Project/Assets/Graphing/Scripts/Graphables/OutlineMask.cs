using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graphing
{
    /// <summary>
    /// A class representing a contour of a surface graph.
    /// </summary>
    public class OutlineMask : Graphable
    {
        /// <summary>
        /// The unit for the Z axis.
        /// </summary>
        public string ZUnit { get => _zUnit ?? ""; set { _zUnit = value; OnDisplayChanged(new AxisUnitChangedEventArgs(AxisUI.AxisDirection.Depth, ZUnit)); } }
        private string _zUnit;
        /// <summary>
        /// The name of the Z axis.
        /// </summary>
        public string ZName { get { return string.IsNullOrEmpty(zName) ? DisplayName : zName; } set { zName = value; OnDisplayChanged(new AxisNameChangedEventArgs(AxisUI.AxisDirection.Depth, ZName)); } }
        protected string zName = null;

        private Func<Vector3, float> _maskCriteria = (v) => v.z;
        public Func<Vector3, float> MaskCriteria {
            get => _maskCriteria;
            set
            {
                _maskCriteria = value;
                OnDisplayChanged(new MaskCriteriaChangedEventArgs(MaskCriteria));
            }
        }

        public bool LineOnly { get => _lineOnly; set { _lineOnly = value; OnDisplayChanged(new MaskLineOnlyChangedEventArgs(LineOnly)); } }
        private bool _lineOnly = true;
        public float LineWidth { get => _lineWidth; set { _lineWidth = value; OnDisplayChanged(new LineWidthChangedEventArgs(LineWidth)); } }
        private float _lineWidth = 1;
        public bool ForceClear { get; set; } = false;

        protected float[,] _values;
        public float[,] Values
        {
            get { return _values; }
            set
            {
                lock (this)
                {
                    _values = value;
                    OnValuesChanged(new ValuesChangedEventArgs(Values, false, new (float, float)[] { (XMin, XMax), (YMin, YMax) }));
                }
            }
        }

        protected override float DefaultColorFunc(Vector3 value) => 0;

        public OutlineMask() { UseSingleColor = true; color = Color.gray; }
        public OutlineMask(float[,] values, float xLeft, float xRight, float yBottom, float yTop, Func<Vector3, float> maskCriteria = null) : this()
        {
            this._values = values;
            this.xMin = xLeft;
            this.xMax = xRight;
            this.yMin = yBottom;
            this.yMax = yTop;
            if (maskCriteria != null)
                this._maskCriteria = maskCriteria;
        }
        public OutlineMask(SurfGraph surfGraph, Func<Vector3, float> maskCriteria = null) :
            this((float[,])surfGraph.Values.Clone(), surfGraph.XMin, surfGraph.XMax, surfGraph.YMin, surfGraph.YMax, maskCriteria)
        { }
        public OutlineMask(OutlineMask outlineMask, Func<Vector3, float> maskCriteria) :
            this((float[,])outlineMask.Values.Clone(), outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax, maskCriteria)
        { }

        /// <summary>
        /// Draws the object on the specified <see cref="UnityEngine.Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        public override void Draw(ref Texture2D texture, float xLeft, float xRight, float yBottom, float yTop)
        {
            if (!Visible) return;
            int width = texture.width - 1;
            int height = texture.height - 1;

            float graphStepX = (xRight - xLeft) / width;
            float graphStepY = (yTop - yBottom) / height;

            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    float xF = x * graphStepX + xLeft;
                    float yF = y * graphStepY + yBottom;

                    bool maskPredicate(float v) => MaskCriteria(new Vector3(xF, yF, v)) >= 0;

                    float pixelValue = ValueAt(xF, yF);

                    if (LineOnly)
                    {
                        bool mask = false;

                        if (!maskPredicate(pixelValue))
                        {
                            for (int w = 1; w <= LineWidth; w++)
                            {
                                if ((x >= w && maskPredicate(ValueAt((x - w) * graphStepX + xLeft, yF))) ||
                                    (x < width - w && maskPredicate(ValueAt((x + w) * graphStepX + xLeft, yF))) ||
                                    (y >= w && maskPredicate(ValueAt(xF, (y - w) * graphStepY + yBottom))) ||
                                    (y < height - w && maskPredicate(ValueAt(xF, (y + w) * graphStepY + yBottom))))
                                {
                                    mask = true;
                                    break;
                                }
                            }
                        }
                        if (mask)
                            texture.SetPixel(x, y, EvaluateColor(new Vector3(x, y, pixelValue)));
                        else if (ForceClear)
                            texture.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        if (!maskPredicate(pixelValue) || xF < XMin || xF > XMax || yF < YMin || yF > YMax)
                            texture.SetPixel(x, y, EvaluateColor(new Vector3(xF, yF, pixelValue)));
                        else if (ForceClear)
                            texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
        }

        /// <summary>
        /// Gets a formatted value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        public override string GetFormattedValueAt(float x, float y, bool withName = false)
        {
            return "";
        }

        /// <summary>
        /// Gets a value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <returns></returns>
        public override float ValueAt(float x, float y)
        {
            if (Transpose)
            {
                (y, x) = (x, y);
            }

            int xI1, xI2;
            float fX;
            if (x <= XMin)
            {
                xI1 = xI2 = 0;
                fX = 0;
            }
            else
            {
                int lengthX = _values.GetUpperBound(0);
                if (lengthX < 0)
                    return 0;
                if (x >= XMax)
                {
                    xI1 = xI2 = lengthX;
                    fX = 1;
                }
                else
                {
                    float stepX = (XMax - XMin) / lengthX;
                    xI1 = (int)Math.Floor((x - XMin) / stepX);
                    fX = (x - XMin) / stepX % 1;
                    if (fX == 0)
                        xI2 = xI1;
                    else
                        xI2 = xI1 + 1;
                }
            }

            if (y <= YMin)
            {
                if (xI1 == xI2) return _values[xI1, 0];
                return _values[xI1, 0] * (1 - fX) + _values[xI2, 0] * fX;
            }
            else
            {
                int lengthY = _values.GetUpperBound(1);
                if (lengthY < 0)
                    return 0;
                if (y >= YMax)
                {
                    if (xI1 == xI2) return _values[xI1, 0];
                    return _values[xI1, lengthY] * (1 - fX) + _values[xI2, lengthY] * fX;
                }
                else
                {
                    float stepY = (YMax - YMin) / lengthY;
                    int yI1 = (int)Math.Floor((y - YMin) / stepY);
                    float fY = (y - YMin) / stepY % 1;
                    int yI2;
                    if (fY == 0)
                        yI2 = yI1;
                    else
                        yI2 = yI1 + 1;

                    if (xI1 == xI2 && yI1 == yI2)
                        return _values[xI1, yI1];
                    else if (xI1 == xI2)
                        return _values[xI1, yI1] * (1 - fY) + _values[xI1, yI2] * fY;
                    else if (yI1 == yI2)
                        return _values[xI1, yI1] * (1 - fX) + _values[xI2, yI1] * fX;

                    return _values[xI1, yI1] * (1 - fX) * (1 - fY) +
                        _values[xI2, yI1] * fX * (1 - fY) +
                        _values[xI1, yI2] * (1 - fX) * fY +
                        _values[xI2, yI2] * fX * fY;
                }
            }
        }

        public void SetValues(float[,] values, float xLeft, float xRight, float yBottom, float yTop)
        {
            lock (this)
            {
                this._values = values;
                this.xMin = xLeft;
                this.xMax = xRight;
                this.yMin = yBottom;
                this.yMax = yTop;
                OnValuesChanged(new ValuesChangedEventArgs(Values, new (float, float)[] { (XMin, XMax), (YMin, YMax) }));
            }
        }

        /// <summary>
        /// Outputs the object's values to file.
        /// </summary>
        /// <param name="directory">The directory in which to place the file.</param>
        /// <param name="filename">The filename for the file.</param>
        /// <param name="sheetName">An optional sheet name for within the file.</param>
        public override void WriteToFileCSV(string path)
        {
            // COULDDO print each contour.
            // List<Vector3[]> lines = OutlineGraphDrawer.GenerateOutlines(this);
            return;
        }
    }
}
