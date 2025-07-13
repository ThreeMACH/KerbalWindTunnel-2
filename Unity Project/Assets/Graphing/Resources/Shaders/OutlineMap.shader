Shader "ColorMap/OutlineMap"
{
    Properties
    {
        [Toggle(_OUTLINE_SOURCE)] _OUTLINE_SOURCE ("Use 2nd channel", Int) = 0
        [MainColor]_OutlineColor ("Outline color", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineThickness ("Outline thickness", Range(0, 20)) = 4
        _Bias ("Bias", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local __ _OUTLINE_SOURCE
            #pragma vertex Vertex_Shader alpha
            #pragma fragment Fragment_Shader alpha
            
            #include "_colorMapBase.shader"
            #include "_outline.shader"
            
            fixed4 Fragment_Shader (v2f i) : SV_Target
            {
#ifdef _OUTLINE_SOURCE
                float xl = (i.localPos.x - i.coords.x) / i.coords.z;
                float yl = (i.localPos.y - i.coords.y) / i.coords.w;
                float z_outline = lerp(lerp(i.outlineValue.x, i.outlineValue.y, xl), lerp(i.outlineValue.z, i.outlineValue.w, xl), yl);
#else
                float z_outline = GetHeight(i);
#endif
                
                fixed4 col = fixed4(0, 0, 0, 0);
                
                z_outline -= _Bias;
                
                col = ApplyOutlineMap(z_outline, col);
                
                clip(col.a - 0.01);

                return col;
            }
            ENDHLSL
        }
    }
}
