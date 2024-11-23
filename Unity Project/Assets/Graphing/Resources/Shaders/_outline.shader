fixed4 _OutlineColor;
float _OutlineThickness;
float _Bias;

fixed4 ApplyOutlineMap (float f, fixed4 baseColor)
{
    bool posThickness = _OutlineThickness > 0;
                
    fixed4 color = baseColor;
                
    float grad = length(float2(ddx(f), ddy(f)));
    bool flat = grad == 0;
                
    float4 contourCol = _OutlineColor;
    float pixelsFromLine = abs(f) / grad;
    float fContour = saturate(1 - saturate((pixelsFromLine - _OutlineThickness * 0.5) * 2));
    fContour = posThickness & !flat ? fContour : 0;
    color = lerp(color, (fixed4)contourCol, fContour);
                
    return color;
}