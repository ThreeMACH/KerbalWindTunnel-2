Shader "ColorMap/LineVertexShader"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _AlphaMult ("Alpha multiplier (used for <1 pixel thick lines)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass // 0
        {
            Name "FILL"

            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            bool _AllowFlip;
            fixed4 _Color;
            fixed _AlphaMult;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);

                o.pos = pos;
                o.color = v.color * _Color;
                o.color.a *= _AlphaMult;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
