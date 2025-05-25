using System;
using Graphing.Extensions;

namespace Graphing
{
    public class SurfGraph : Graphable3, IColorGraph
    {
        public override float ZMin { get => base.ZMin; set => UnityEngine.Debug.LogError("Cannot set bounds for this type of object."); }
        public override float ZMax { get => base.ZMax; set => UnityEngine.Debug.LogError("Cannot set bounds for this type of object."); }

        protected float cMin = float.NaN;
        /// <summary>
        /// The color axis lower bound.
        /// </summary>
        public float CMin { get => float.IsNaN(cMin) ? ZMin : cMin; set { cMin = value; cRange = cMax - cMin; useCRange = float.IsNaN(cRange); OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Color, CMin, CMax)); } }

        protected float cMax = float.NaN;
        /// <summary>
        /// The color axis upper bound.
        /// </summary>
        public float CMax { get => float.IsNaN(cMax) ? ZMax : cMax; set { cMax = value; cRange = cMax - cMin; useCRange = float.IsNaN(cRange); OnDisplayChanged(new BoundsChangedEventArgs(AxisUI.AxisDirection.Color, CMin, CMax)); } }
        public string CName { get => ZName; set => ZName = value; }
        public string CUnit { get => ZUnit; set => ZUnit = value; }

        protected float cRange = float.NaN;
        protected bool useCRange = false;

        protected float[,] _values;
        /// <summary>
        /// Gets or sets the points in the surface.
        /// </summary>
        public float[,] Values
        {
            get { return _values; }
            set
            {
                lock (this)
                {
                    _values = value;
                    zMin = _values.Min(true);
                    zMax = _values.Max(true);
                    OnValuesChanged(new ValuesChangedEventArgs(Values, true, new (float, float)[] { (XMin, XMax), (YMin, YMax), (ZMin, ZMax) }));
                }
            }
        }

        protected override float DefaultColorFunc(UnityEngine.Vector3 value) => value.z;
        protected override float NormalizeColorFuncOutput(float value) => (value - CMin) / (useCRange ? cRange : CMax - CMin);

        /// <summary>
        /// Constructs a blank <see cref="SurfGraph"/>.
        /// </summary>
        public SurfGraph() { colorScheme = GradientExtensions.Jet_Dark; }
        /// <summary>
        /// Constructs a <see cref="SurfGraph"/> with the specified values spaced evenly on a grid.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="xLeft">The left x bound.</param>
        /// <param name="xRight">The right x bound.</param>
        /// <param name="yBottom">The bottom y bound.</param>
        /// <param name="yTop">The top y bound.</param>
        public SurfGraph(float[,] values, float xLeft, float xRight, float yBottom, float yTop) : this()
        {
            _values = values;
            xMin = xLeft;
            xMax = xRight;
            yMin = yBottom;
            yMax = yTop;
            if (_values.GetUpperBound(0) < 0 || _values.GetUpperBound(1) < 0)
            {
                zMin = zMax = 0;
                return;
            }
            zMin = values.Min(true);
            zMax = values.Max(true);
        }
        public SurfGraph(OutlineMask outlineMask) : this((float[,])outlineMask.Values.Clone(), outlineMask.XMin, outlineMask.XMax, outlineMask.YMin, outlineMask.YMax) { }

        /// <summary>
        /// Draws the object on the specified <see cref="UnityEngine.Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        public override void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop)
            => Draw(ref texture, xLeft, xRight, yBottom, yTop, ZMin, ZMax);

        /// <summary>
        /// Draws the object on the specified <see cref="UnityEngine.Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        /// <param name="cMin">The color axis lower bound.</param>
        /// <param name="cMax">The color axis upper bound.</param>
        public void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop, float cMin, float cMax)
        {
            if (!Visible) return;
            int width = texture.width - 1;
            int height = texture.height - 1;
            float cRange = cMax - cMin;

            float graphStepX = (xRight - xLeft) / width;
            float graphStepY = (yTop - yBottom) / height;

            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    float xF = x * graphStepX + xLeft;
                    float yF = y * graphStepY + yBottom;
                    if (xF < XMin || xF > XMax || yF < YMin || yF > YMax)
                        continue;
                    texture.SetPixel(x, y, EvaluateColor(new UnityEngine.Vector3(xF, yF, (ValueAt(xF, yF) - cMin) / cRange)));
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
        {
            float xMin = XMin, xMax = XMax, yMin = YMin, yMax = YMax;
            if (Transpose)
            {
                (y, x) = (x, y);
            }

            int xI1, xI2;
            float fX;
            if (x <= xMin)
            {
                xI1 = xI2 = 0;
                fX = 0;
            }
            else
            {
                int lengthX = _values.GetUpperBound(0);
                if (lengthX < 0)
                    return 0;
                if (x >= xMax)
                {
                    xI1 = xI2 = lengthX;
                    fX = 1;
                }
                else
                {
                    float stepX = (xMax - xMin) / lengthX;
                    xI1 = (int)Math.Floor((x - xMin) / stepX);
                    fX = (x - xMin) / stepX % 1;
                    if (fX == 0)
                        xI2 = xI1;
                    else
                        xI2 = xI1 + 1;
                }
            }

            if (y <= yMin)
            {
                if (xI1 == xI2) return _values[xI1, 0];
                return _values[xI1, 0] * (1 - fX) + _values[xI2, 0] * fX;
            }
            else
            {
                int lengthY = _values.GetUpperBound(1);
                if (lengthY < 0)
                    return 0;
                if (y >= yMax)
                {
                    if (xI1 == xI2) return _values[xI1, 0];
                    return _values[xI1, lengthY] * (1 - fX) + _values[xI2, lengthY] * fX;
                }
                else
                {
                    float stepY = (yMax - yMin) / lengthY;
                    int yI1 = (int)Math.Floor((y - yMin) / stepY);
                    float fY = (y - yMin) / stepY % 1;
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

        /// <summary>
        /// Draws an equivalent to <see cref="OutlineMask"/>.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="xLeft"></param>
        /// <param name="xRight"></param>
        /// <param name="yBottom"></param>
        /// <param name="yTop"></param>
        /// <param name="maskCriteria"></param>
        /// <param name="maskColor"></param>
        /// <param name="lineOnly"></param>
        /// <param name="lineWidth"></param>
        public void DrawMask(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop, Func<float, bool> maskCriteria, UnityEngine.Color maskColor, bool lineOnly = true, int lineWidth = 1)
        {
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

                    if (lineOnly)
                    {
                        float pixelValue = ValueAt(xF, yF);
                        bool mask = false;

                        if (!maskCriteria(pixelValue))
                        {
                            for (int w = 1; w <= lineWidth; w++)
                            {
                                if ((x >= w && maskCriteria(ValueAt((x - w) * graphStepX + xLeft, yF))) ||
                                    (x < width - w && maskCriteria(ValueAt((x + w) * graphStepX + xLeft, yF))) ||
                                    (y >= w && maskCriteria(ValueAt(xF, (y - w) * graphStepY + yBottom))) ||
                                    (y < height - w && maskCriteria(ValueAt(xF, (y + w) * graphStepY + yBottom))))
                                {
                                    mask = true;
                                    break;
                                }
                            }
                        }
                        if (mask)
                            texture.SetPixel(x, y, maskColor);
                        else
                            texture.SetPixel(x, y, UnityEngine.Color.clear);
                    }
                    else
                    {
                        if (!maskCriteria(ValueAt(xF, yF)) || xF < XMin || xF > XMax || yF < YMin || yF > YMax)
                            texture.SetPixel(x, y, maskColor);
                        else
                            texture.SetPixel(x, y, UnityEngine.Color.clear);
                    }
                }
            }

            texture.Apply();
        }

        public void SetValues(float[,] values, float xLeft, float xRight, float yBottom, float yTop)
        {
            lock (this)
            {
                _values = values;
                xMin = xLeft;
                xMax = xRight;
                yMin = yBottom;
                yMax = yTop;
                if (_values.GetUpperBound(0) < 0 || _values.GetUpperBound(1) < 0)
                {
                    zMin = zMax = 0;
                    return;
                }
                zMin = values.Min(true);
                zMax = values.Max(true);

                OnValuesChanged(new ValuesChangedEventArgs(Values, new (float, float)[] { (XMin, XMax), (YMin, YMax), (ZMin, ZMax) }));
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
        {
            if (_values.GetUpperBound(0) < 0 || _values.GetUpperBound(1) < 0) return "";
            return base.GetFormattedValueAt(x, y, withName);
        }

        public override void WriteToFileCSV(string path)
        {
            int height = _values.GetUpperBound(1);
            int width = _values.GetUpperBound(0);
            if (height < 0 || width < 0)
                return;
            float xStep = (XMax - XMin) / width;
            float yStep = (YMax - YMin) / height;

            string strCsv;

            for (int y = height; y >= 0; y--)
            {
                strCsv = string.Format("{0}", y * yStep + YMin);
                for (int x = 0; x <= width; x++)
                    strCsv += string.Format(",{0:" + StringFormat.Replace("N", "F") + "}", _values[x, y]);

                try
                {
                    System.IO.File.AppendAllText(path, strCsv + "\r\n");
                }
                catch (Exception) { }
            }

            strCsv = FormatNameAndUnit(ZName, ZUnit);

            for (int x = 0; x <= width; x++)
                strCsv += string.Format(",{0}", xStep * x + XMin);

            try
            {
                System.IO.File.AppendAllText(path, strCsv + "\r\n");
            }
            catch (Exception) { }
        }

        protected override IO.SpreadsheetOptions SpreadsheetOptions => _spreadsheetOptions;
        private static readonly IO.SpreadsheetOptions _spreadsheetOptions = new IO.SpreadsheetOptions(false, false, 2, 2);

        public override void WriteToDataTable(System.Data.DataTable dataTable)
        {
            const int columnOffest = 2;
            if (_values.Length <= 0)
                return;
            int height = _values.GetUpperBound(1);
            int width = _values.GetUpperBound(0);
            if (height < 0 || width < 0)
                return;
            float xStep = (XMax - XMin) / width;
            float yStep = (YMax - YMin) / height;

            dataTable.Columns.Add("YName", typeof(string));
            dataTable.Columns.Add("YValues", typeof(float));
            dataTable.Columns.Add("Col0", typeof(object));  // Needs to be object since there's a single string value for the X name/unit.
            for (int x = 0; x <= width; x++)
                dataTable.Columns.Add().DataType = typeof(float);
            System.Data.DataRow row;

            dataTable.Rows.Add();
            row = dataTable.Rows.Add();
            for (int x = 0; x <= width; x++)
                row[columnOffest + x] = XMin + xStep * x;

            for (int y = 0; y <= height; y++)
            {
                row = dataTable.Rows.Add();
                row[1] = YMin + yStep * y;
                for (int x = 0; x <= width; x++)
                    row[columnOffest + x] = _values[x, y];
            }

            dataTable.Rows[2][0] = FormatNameAndUnit(XName, XUnit, "X");
            dataTable.Rows[0][2] = FormatNameAndUnit(YName, YUnit, "Y");
            dataTable.Rows[0][0] = FormatNameAndUnit(ZName, ZUnit, "Z");
        }
    }
}
