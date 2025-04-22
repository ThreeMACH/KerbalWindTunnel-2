bool _DrawContours;
texture2D _ContourTex;
fixed4 _ContourColor;
texture2D _ContourMapTex;
float _ContourThickness;
bool _ContourValuesNormalized;
int _NumContours;
bool _ContourCountSource;
float4 _ContourTex_TexelSize;
float4 _ContourMapTex_TexelSize;

fixed4 ApplyContourMap(float f, fixed4 baseColor)
{
    int w;
    float wInv;
    w = _ContourTex_TexelSize.z;
    wInv = _ContourTex_TexelSize.x;
#ifndef _CONTOURMAPSOURCE_EVEN
    w = _ContourMapTex_TexelSize.z;
    wInv = _ContourMapTex_TexelSize.x;
#endif
#ifdef _CONTOURMAPSOURCE_EVEN
    w = _ContourCountSource ? _NumContours : w;
    float wInv_ = w > 1 ? 1.0 / (w - 1) : 0;
#endif
    bool posThickness = _ContourThickness > 0;
                
    fixed4 color = baseColor;
                
    float grad = length(float2(ddx(f), ddy(f)));
    bool flat = grad == 0;
    float invRange = 1 / (_Max - _Min);
                
    for (int i = 0; i < w; i++)
    {
        float n;    // Value of current contour
        float4 contourCol;
#ifdef _CONTOURMAPSOURCE_EVEN
        n = i * wInv_;
        contourCol = _ContourColor;
#endif
        int3 index = int3((i + 0.5), 0, 0); // Pixel coordinate of current point
        contourCol = _ContourTex.Load(index); // Color of current contour
#ifdef _CONTOURMAPSOURCE_ALPHA
        n = contourCol.a;
        contourCol.a = 1;
#elif !defined (_CONTOURMAPSOURCE_EVEN)
        n = _ContourMapTex.Load(index);
#endif
#ifndef _CONTOURMAPSOURCE_EVEN
        n = _ContourValuesNormalized ? saturate(n) : (n - _Min) * invRange;
#endif
        contourCol *= _ContourColor;
        float pixelsFromLine = abs(f - n) / grad;
        float fContour = saturate(1 - saturate((pixelsFromLine - _ContourThickness * 0.5) * 2));
        fContour = posThickness & !flat ? fContour : 0;
        color = lerp(color, (fixed4) contourCol, fContour);
    }
                
    return color;
}