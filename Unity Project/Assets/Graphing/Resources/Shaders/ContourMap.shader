Shader "ColorMap/ContourMap"
{
    Properties
    {
        _Min ("Lower Bound", Float) = 0
        _Max ("Upper Bound", Float) = 1

        [Toggle] _Clip ("Clip beyond range", Int) = 1
        [NoScaleOffset] _ContourTex ("Contour colors", 2D) = "white" {}
        [MainColor]_ContourColor ("Contour color", Color) = (1, 1, 1, 1)
        [KeywordEnum(Even,Texture,Alpha)] _CONTOURMAPSOURCE ("Contour value source", Int) = 0
        [MainTexture][NoScaleOffset] _ContourMapTex ("Contour value texture", 2D) = "black" {}
        _ContourThickness ("Contour thickness", Range(0, 20)) = 3
        [KeywordEnum(Texture,Number)] _ContourCountSource ("Contour count source", Int) = 0
        [IntRange] _NumContours ("Number of contours", Range(2, 10)) = 5
        [Toggle] _ContourValuesNormalized ("Contour heights are normalized", Int) = 1
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
            #pragma multi_compile_local_fragment _CONTOURMAPSOURCE_EVEN __ _CONTOURMAPSOURCE_ALPHA
            #pragma vertex Vertex_Shader alpha
            #pragma fragment Fragment_Shader alpha
            
            #include "_colorMapBase.shader"
            #include "_contours.shader"
            
            fixed4 Fragment_Shader (v2f i) : SV_Target
            {
                float z = (GetHeight(i) - _Min) / (_Max - _Min);
                if ((z < 0 || z > 1) && _Clip)
                    clip(-1);
                float z_sat = saturate(z);

                fixed4 col = fixed4(0, 0, 0, 0);
                
                col = ApplyContourMap(z, col);
                clip(col.a - 0.01);
                return col;
            }
            ENDHLSL
        }
    }
}
