using UnityEngine;

namespace Graphing
{
    public struct AxisStyle
    {
        public static readonly AxisStyle Default = new AxisStyle()
        {
            autoMin = true,
            autoMax = true,
            barThickness = 1,
            tickThickness = 1,
            tickWidth = 5,
            tickSpacing = 2,
            axisColor = Color.black,
            tickColor = Color.black,
            textColor = Color.black,
            fontSize = 8,
            fontSizeMin = 1,
            fontSizeMax = 72,
            autoFontSize = false
        };

        public bool? autoMin;
        public bool? autoMax;
        public int? barThickness;
        public int? tickThickness;
        public int? tickWidth;
        public int? tickSpacing;
        public Color? axisColor;
        public Color? tickColor;
        public Color? textColor;
        public float? fontSize;
        public float? fontSizeMin;
        public float? fontSizeMax;
        public bool? autoFontSize;
    }
}