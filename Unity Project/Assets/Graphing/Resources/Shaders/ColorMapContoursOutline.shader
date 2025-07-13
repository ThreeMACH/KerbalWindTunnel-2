Shader "ColorMap/ColorMapContoursOutline"
{
    Properties
    {
        _Min ("Lower Bound", Float) = 0
        _Max ("Upper Bound", Float) = 1

        [Header(Color Map)][Toggle(_DRAWGRADIENT)] _DRAWGRADIENT ("Draw gradient", Int) = 1
        [KeywordEnum(Jet,Jet_Dark,Grayscale,Custom)] _MODE ("Mode", Int) = 0
        [MainTexture][Header(Custom Color Map)][HideIfDisabled(_MODE_CUSTOM)][NoScaleOffset] _ColorTex ("Custom map texture", 2D) = "gray" {}
        [HideInInspector][MainColor] _Color ("Color", Color) = (1, 1, 1, 1)
        [KeywordEnum(Even,Texture,Alpha)] _MAPSOURCE ("Map value source", Int) = 0
        [HideIfDisabled(_MODE_CUSTOM)][NoScaleOffset] _ValueMapTex ("Map value texture", 2D) = "white" {}
        [Toggle] _Step ("Stepwise", Int) = 0
        [Toggle] _Clip ("Clip beyond range", Int) = 1

        [Header(Contours)][Toggle(_DRAWCONTOURS)] _DRAWCONTOURS ("Draw contours", Int) = 0
        [HideIfDisabled(_DRAWCONTOURS)][NoScaleOffset] _ContourTex ("Contour colors", 2D) = "white" {}
        [HideIfDisabled(_DRAWCONTOURS)]_ContourColor ("Contour color", Color) = (1, 1, 1, 1)
        [KeywordEnum(Even,Texture,Alpha)] _CONTOURMAPSOURCE ("Contour value source", Int) = 0
        [HideIfDisabled(_DRAWCONTOURS)][NoScaleOffset] _ContourMapTex ("Contour value texture", 2D) = "black" {}
        [HideIfDisabled(_DRAWCONTOURS)]_ContourThickness ("Contour thickness", Range(0, 20)) = 3
        [KeywordEnum(Texture,Number)] _ContourCountSource ("Contour count source", Int) = 0
        [IntRange] _NumContours ("Number of contours", Range(2, 10)) = 5
        [Toggle] _ContourValuesNormalized ("Contour heights are normalized", Int) = 1

        [Header(Outline)][Toggle(_DRAWOUTLINE)] _DRAWOUTLINE ("Draw outline", Int) = 0
        [HideIfDisabled(_DRAWOUTLINE)]_OutlineColor ("Outline color", Color) = (0.5, 0.5, 0.5, 1)
        [HideIfDisabled(_DRAWOUTLINE)]_OutlineThickness ("Outline thickness", Range(0, 20)) = 4
        [HideIfDisabled(_DRAWOUTLINE)]_Bias ("Outline Bias", Float) = 0
        [Toggle(_OUTLINE_SOURCE)] _OUTLINE_SOURCE ("Use 2nd channel for outline", Int) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local __ _DRAWGRADIENT
#ifdef _DRAWGRADIENT
            #pragma multi_compile_local _MODE_JET _MODE_JET_DARK _MODE_GRAYSCALE _MODE_CUSTOM
# ifdef _MODE_CUSTOM
            #pragma multi_compile_local _MAPSOURCE_EVEN __ _MAPSOURCE_ALPHA
# endif
#endif
            #pragma multi_compile_local  __ _DRAWCONTOURS
#ifdef _DRAWCONTOURS
            #pragma multi_compile_local _CONTOURMAPSOURCE_EVEN  _CONTOURMAPSOURCE_ALPHA
#endif
            #pragma shader_feature_local __ _DRAWOUTLINE
#ifdef _DRAWOUTLINE
            #pragma multi_compile_local  __ _OUTLINE_SOURCE
#endif
            #pragma vertex Vertex_Shader alpha
            #pragma fragment Fragment_Shader alpha
            
            #include "_colorMapBase.shader"
            #include "_colorMapInc.shader"
            #include "_contours.shader"
            #include "_outline.shader"
            
            fixed4 Fragment_Shader (v2f i) : SV_Target
            {
                float z = GetHeight(i);
                
#ifdef _OUTLINE_SOURCE
                float xl = (i.localPos.x - i.coords.x) / i.coords.z;
                float yl = (i.localPos.y - i.coords.y) / i.coords.w;
                float z_outline = lerp(lerp(i.outlineValue.x, i.outlineValue.y, xl), lerp(i.outlineValue.z, i.outlineValue.w, xl), yl);
#else
                float z_outline = z;
#endif
                z_outline -= _Bias;
                
                z = (z - _Min) / (_Max - _Min);
                bool clipFlag = (z < 0 || z > 1) && _Clip; // =0 if z is [0, 1], else 1
                float z_sat = saturate(z);

                fixed4 col;
                
#ifdef _DRAWGRADIENT
# ifdef _MODE_JET_DARK
                col = Jet_Dark(z_sat) * _Color;
# elif defined (_MODE_GRAYSCALE)
                col = Grayscale(z_sat) * _Color;
# elif defined (_MODE_CUSTOM)
                col = CustomMap(z_sat) * _Color;
# else
                col = Jet(z_sat) * _Color;
# endif
#else
                col = fixed4(0, 0, 0, 0);
#endif
                
                col.a *= !clipFlag;
                
#if _DRAWCONTOURS
                col = ApplyContourMap(z, col);
#endif
#if _DRAWOUTLINE
                col = ApplyOutlineMap(z_outline, col);
#endif
                clip(col.a - 0.01);

                return col;
            }
            ENDHLSL
        }
    }
}
