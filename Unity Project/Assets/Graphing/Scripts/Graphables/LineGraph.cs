using System;
using System.Linq;
using Graphing.Extensions;
using UnityEngine;

namespace Graphing
{
    /// <summary>
    /// A class representing a line graph.
    /// </summary>
    public class LineGraph : Graphable, ILineGraph
    {
        public override float XMin { get => base.XMin; set => Debug.LogError("Cannot set bounds for this type of object."); }
        public override float XMax { get => base.XMax; set => Debug.LogError("Cannot set bounds for this type of object."); }
        public override float YMin { get => base.YMin; set => Debug.LogError("Cannot set bounds for this type of object."); }
        public override float YMax { get => base.YMax; set => Debug.LogError("Cannot set bounds for this type of object."); }
        /// <summary>
        /// The width of the line in pixels.
        /// </summary>
        public float LineWidth { get => lineWidth; set { lineWidth = value; OnDisplayChanged(new LineWidthChangedEventArgs(LineWidth)); } }
        protected float lineWidth = 3;
        protected Vector2[] _values;
        /// <summary>
        /// Gets or sets the points in the line.
        /// </summary>
        public Vector2[] Values
        {
            get => _values;
            private set => SetValues(value);
        }

        protected override float DefaultColorFunc(Vector3 value) => 0;

        protected bool sorted = false;
        protected bool equalSteps = false;

        private LineGraph() { UseSingleColor = true; }

        /// <summary>
        /// Constructs a new <see cref="LineGraph"/> with the provided y-values at evenly spaced points.
        /// </summary>
        /// <param name="xLeft">The x-value at the leftmost point.</param>
        /// <param name="xRight">The x-value at the rightmost point.</param>
        /// <param name="values"></param>
        public LineGraph(float[] values, float xLeft, float xRight) : this()
        {
            SetValuesInternal(values, xLeft, xRight);
        }
        /// <summary>
        /// Constructs a new <see cref="LineGraph"/> with the provided points.
        /// </summary>
        /// <param name="values"></param>
        public LineGraph(Vector2[] values) : this()
        {
            SetValuesInternal(values);
        }

        /// <summary>
        /// Draws the object on the specified <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        public override void Draw(ref Texture2D texture, float xLeft, float xRight, float yBottom, float yTop)
        {
            if (!Visible) return;
            float xRange = xRight - xLeft;
            float yRange = yTop - yBottom;
            int width = texture.width;
            int height = texture.height;
            int[] xPix, yPix;
            // TODO: Add robustness for NaNs and Infinities.
            if (!Transpose)
            {
                xPix = _values.Select(vect => Mathf.RoundToInt((vect.x - xLeft) / xRange * width)).ToArray();
                yPix = _values.Select(vect => Mathf.RoundToInt((vect.y - yBottom) / yRange * height)).ToArray();
            }
            else
            {
                xPix = _values.Select(vect => Mathf.RoundToInt((vect.y - yBottom) / yRange * width)).ToArray();
                yPix = _values.Select(vect => Mathf.RoundToInt((vect.x - xLeft) / xRange * height)).ToArray();
            }

            for (int i = _values.Length - 2; i >= 0; i--)
            {
                DrawingHelper.DrawLine(ref texture, xPix[i], yPix[i], xPix[i + 1], yPix[i + 1], EvaluateColor(new Vector3(xPix[i], yPix[i], 0)), EvaluateColor(new Vector3(xPix[i + 1], yPix[i + 1], 0)));
                for (int w = 2; w <= LineWidth; w++)
                {
                    int l = w % 2 == 0 ? (-w) >> 1 : (w - 1) >> 1;
                    DrawingHelper.DrawLine(ref texture, xPix[i] + l, yPix[i], xPix[i + 1] + l, yPix[i + 1], EvaluateColor(new Vector3(xPix[i], yPix[i], 0)), EvaluateColor(new Vector3(xPix[i + 1], yPix[i + 1], 0)));
                    DrawingHelper.DrawLine(ref texture, xPix[i], yPix[i] + l, xPix[i + 1], yPix[i + 1] + l, EvaluateColor(new Vector3(xPix[i], yPix[i], 0)), EvaluateColor(new Vector3(xPix[i + 1], yPix[i + 1], 0)));
                }
            }

            texture.Apply();
        }

        /// <summary>
        /// Gets a value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <returns></returns>
        public override float ValueAt(float x, float y)
            => ValueAt(x, y, 1, 1);
        /// <summary>
        /// Gets a value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="width">The x domain of the graph.</param>
        /// <param name="height">The y range of the graph.</param>
        /// <returns></returns>
        public virtual float ValueAt(float x, float y, float width, float height)
        {
            if (_values.Length <= 0)
                return 0;

            if (Transpose) x = y;

            if (equalSteps && sorted)
            {
                if (x <= XMin) return _values[0].y;

                int length = _values.Length - 1;
                if (x >= XMax) return _values[length].y;

                float step = (XMax - XMin) / length;
                int index = (int)Math.Floor((x - XMin) / step);
                float f = (x - XMin) / step % 1;
                if (f == 0)
                    return _values[index].y;

                return _values[index].y * (1 - f) + _values[index + 1].y * f;
            }
            else
            {
                if (sorted)
                {
                    //if (x <= Values[0].x)
                    //    return Values[0].y;
                    if (x >= _values[_values.Length - 1].x)
                        return _values[_values.Length - 1].y;
                    for (int i = _values.Length - 2; i >= 0; i--)
                    {
                        if (x > _values[i].x)
                        {
                            float f = (x - _values[i].x) / (_values[i + 1].x - _values[i].x);

                            return _values[i].y * (1 - f) + _values[i + 1].y * f;
                        }
                    }
                    return _values[0].y;
                }
                else
                {
                    Vector2 point = new Vector2(x, y);
                    Vector2 closestPoint = _values[0];

                    float currentDistance = float.PositiveInfinity;
                    int length = _values.Length;
                    Vector2 LocalTransform(Vector2 vector) => new Vector2(vector.x / width, vector.y / height);
                    for (int i = 0; i < length - 1 - 1; i++)
                    {

                        Vector2 lineDir = (_values[i + 1] - _values[i]).normalized;
                        float distance = Vector2.Dot(point - _values[i], lineDir);
                        Vector2 closestPtOnLine = _values[i] + distance * lineDir;
                        if (distance <= 0)
                        {
                            closestPtOnLine = _values[i];

                        }
                        else if ((closestPtOnLine - _values[i]).sqrMagnitude > (_values[i + 1] - _values[i]).sqrMagnitude)//(distance * distance >= (_values[i + 1] - _values[i]).sqrMagnitude)
                        {
                            closestPtOnLine = _values[i + 1];

                        }

                        float ptDistance = LocalTransform(point - closestPtOnLine).sqrMagnitude;

                        if (ptDistance < currentDistance)
                        {
                            currentDistance = ptDistance;
                            closestPoint = closestPtOnLine;
                        }
                    }
                    return Transpose ? closestPoint.x : closestPoint.y;
                }
            }
        }

        /// <summary>
        /// Sets the values in the line with the provided y-values at evenly spaced points.
        /// </summary>
        /// <param name="xLeft">The x-value at the leftmost point.</param>
        /// <param name="xRight">The x-value at the rightmost point.</param>
        /// <param name="values"></param>
        public void SetValues(float[] values, float xLeft, float xRight)
        {
            lock (this)
            {
                SetValuesInternal(values, xLeft, xRight);
                OnValuesChanged(new ValuesChangedEventArgs(Values, new (float, float)[] { (XMin, XMax), (YMin, YMax) }));
            }
        }
        private void SetValuesInternal(float[] values, float xLeft, float xRight)
        {
            this._values = new Vector2[values.Length];
            if (_values.Length <= 0)
            {
                this.yMin = this.yMax = this.xMin = this.xMax = 0;
                return;
            }
            float xStep = (xRight - xLeft) / (values.Length - 1);
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                this._values[i] = new Vector2(xLeft + xStep * i, values[i]);
                if (!float.IsInfinity(_values[i].y) && !float.IsNaN(_values[i].y))
                {
                    yMin = Math.Min(yMin, _values[i].y);
                    yMax = Math.Max(yMax, _values[i].y);
                }
            }
            this.xMax = xRight;
            this.xMin = xLeft;
            this.yMax = yMax;
            this.yMin = yMin;
            this.sorted = true;
            this.equalSteps = true;
        }
        /// <summary>
        /// Sets the values in the line.
        /// </summary>
        /// <param name="values"></param>
        public void SetValues(Vector2[] values)
        {
            lock (this)
            {
                SetValuesInternal(values);
                OnValuesChanged(new ValuesChangedEventArgs(Values, new (float, float)[] { (XMin, XMax), (YMin, YMax) }));
            }
        }
        private void SetValuesInternal(Vector2[] values)
        {
            _values = values;
            if (_values.Length <= 0)
            {
                this.yMin = this.yMax = this.xMin = this.xMax = 0;
                return;
            }
            float xLeft = float.MaxValue;
            float xRight = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (!float.IsInfinity(values[i].x) && !float.IsNaN(values[i].x))
                {
                    xLeft = Mathf.Min(xLeft, values[i].x);
                    xRight = Mathf.Max(xRight, values[i].x);
                }
                if (!float.IsInfinity(values[i].y) && !float.IsNaN(values[i].y))
                {
                    yMin = Mathf.Min(yMin, values[i].y);
                    yMax = Mathf.Max(yMax, values[i].y);
                }
            }
            this.xMax = xRight;
            this.xMin = xLeft;
            this.yMax = yMax;
            this.yMin = yMin;

            float step = (xRight - xLeft) / (values.Length - 1);
            this.sorted = true;
            this.equalSteps = true;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (equalSteps && _values[i].x != xLeft + step * i)
                    equalSteps = false;
                if (sorted && i > 0 && _values[i].x < _values[i - 1].x)
                    sorted = false;
                if (!equalSteps && !sorted)
                    break;
            }
        }

        /// <summary>
        /// Gets a formatted value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        public override string GetFormattedValueAt(float x, float y, bool withName = false)
            => GetFormattedValueAt(x, y, 1, 1, withName);
        /// <summary>
        /// Gets a formatted value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="width">The x domain of the graph.</param>
        /// <param name="height">The y range of the graph.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        public virtual string GetFormattedValueAt(float x, float y, float width, float height, bool withName = false)
        {
            if (_values.Length <= 0) return "";
            return String.Format("{2}{0:" + StringFormat + "}{1}", ValueAt(x, y, width, height), YUnit, withName && DisplayName != "" ? DisplayName + ": " : "");
        }

        /// <summary>
        /// Outputs the object's values to file.
        /// </summary>
        /// <param name="directory">The directory in which to place the file.</param>
        /// <param name="filename">The filename for the file.</param>
        /// <param name="sheetName">An optional sheet name for within the file.</param>
        public override void WriteToFile(string directory, string filename, string sheetName = "")
        {
            if (_values.Length <= 0)
                return;

            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            if (sheetName == "")
                sheetName = this.Name.Replace("/", "-").Replace("\\", "-");

            string fullFilePath = string.Format("{0}/{1}{2}.csv", directory, filename, sheetName != "" ? "_" + sheetName : "");

            try
            {
                if (System.IO.File.Exists(fullFilePath))
                    System.IO.File.Delete(fullFilePath);
            }
            catch (Exception ex) { Debug.LogErrorFormat("Unable to delete file:{0}", ex.Message); }

            string strCsv = "";
            if (XName != "")
                strCsv += string.Format("{0} [{1}]", XName, XUnit != "" ? XUnit : "-");
            else
                strCsv += string.Format("{0}", XUnit != "" ? XUnit : "-");

            if (YName != "")
                strCsv += String.Format(",{0} [{1}]", YName, YUnit != "" ? YUnit : "-");
            else
                strCsv += String.Format(",{0}", YUnit != "" ? YUnit : "-");

            try
            {
                System.IO.File.AppendAllText(fullFilePath, strCsv + "\r\n");
            }
            catch (Exception ex) { Debug.LogException(ex); }

            for (int i = 0; i < _values.Length; i++)
            {
                strCsv = String.Format("{0}, {1:" + StringFormat.Replace("N", "F") + "}", _values[i].x, _values[i].y);

                try
                {
                    System.IO.File.AppendAllText(fullFilePath, strCsv + "\r\n");
                }
                catch (Exception) { }
            }
        }
    }
}
