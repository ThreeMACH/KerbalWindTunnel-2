texture2D _ColorTex;
fixed4 _Color;
texture2D _ValueMapTex;
bool _Step;
float4 _ColorTex_TexelSize;
            
fixed4 Jet(float f)
{
    fixed4 color = clamp(fixed4(
                    1.5 * f,
                    mad(f, -1.5, 2),
                    mad(f, -1.5, 1),
                    1), 0.5, 1);
    return color;
}
            
fixed4 Jet_Dark(float f)
{
    fixed4 color = clamp(fixed4(
                    mad(f, 2, -0.5),
                    mad(abs(f - 0.5), -2, 1.5),
                    mad(f, -2, 1.5),
                    1), 0.5, 1);
    return color;
}
            
fixed4 Grayscale(float f)
{
    return fixed4(f, f, f, 1);
}
            
fixed4 CustomMap(float f)
{
    int w = _ColorTex_TexelSize.z;
    float wInv = _ColorTex_TexelSize.x;
                
    bool i2_mod = w > 1; // Normally 1, but 0 if the texture is 1 pixel
    int3 i1 = int3(0, 0, 0); // Pixel coordinate of current segment
    float4 c1 = _ColorTex.Load(i1); // Lerp color value at start of current segment
    float n1; // = saturate(_useAlpha ? c1.a : tex2D(_ValueMapTex, i1));    // Lerp start of current segment
#ifdef _MAPSOURCE_ALPHA
                n1 = saturate(c1.a);
                c1.a = 1;
#elif defined (_MAPSOURCE_EVEN)
                float wInv_ = w > 1 ? 1.0 / (w - 1) : 0;
                n1 = 0;
#else
    n1 = saturate(_ValueMapTex.Load(i1));
#endif
                
    // If f<=0, the below loop won't add anything since sn will always exclude it.
    // We don't need to worry about a similar case for f >= 1, though.
    fixed4 color = f > 0 ? fixed4(0, 0, 0, 0) : c1;
                
    for (int i = 0; i < w - 1; i++)
    {
        int3 i2 = int3((i + i2_mod), 0, 0); // Pixel coordinate of next segment
        float4 c2 = _ColorTex.Load(i2); // Lerp value at end of current segment
        float n2; // = saturate(_useAlpha ? c2.a : tex2D(_ValueMapTex, i2));    // Lerp end for current segment
#ifdef _MAPSOURCE_ALPHA
        n2 = saturate(c2.a);
        c2.a = 1;
#elif defined (_MAPSOURCE_EVEN)
        n2 = (i + 1.0) * wInv_;
#else
        n2 = saturate(_ValueMapTex.Load(i2));
#endif
        
        float fn = (f - n1) / (n2 - n1); // Inner Lerp value
        bool sn = fn > 0 && fn <= 1; // =1 if fn is [0, 1], else 0
        fn = _Step ? 0 : fn; // Set the fractional part to zero if we want color steps
        color += lerp((fixed4) c1, (fixed4) c2, fn) * sn; // Adds the Lerp'd color if within the current segment, otherwise adds 0.
        i1 = i2;
        c1 = c2;
        n1 = n2;
    }
    return color;
}