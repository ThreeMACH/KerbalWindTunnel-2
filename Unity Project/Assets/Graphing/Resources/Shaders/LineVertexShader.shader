Shader "ColorMap/LineVertexShader"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        //_AlphaMult ("Alpha multiplier (used for <1 pixel thick lines)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass // 0
        {
            Name "FILL"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            //#pragma geometry geom
            #pragma fragment frag
            //#pragma require geometry

            #include "UnityCG.cginc"

            //bool _AllowFlip;
            fixed4 _Color;
            //fixed _AlphaMult;

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
                //o.color.a *= _AlphaMult;

                return o;
            }

            [maxvertexcount(4)]
            void geom(line v2f v[2], inout LineStream<v2f> triStream)
            {
                triStream.Append(v[0]);
                triStream.Append(v[1]);
                triStream.RestartStrip();
                return;

                const float width = 500;

                float2 p0 = v[0].pos.xy * _ScreenParams.xy / 4;
                float2 p1 = v[1].pos.xy * _ScreenParams.xy / 4;
                float2 dir = p1 - p0;
                float2 offset = float2(-dir.y, dir.x) * width;
                v2f output[4];
                output[0].pos.xy = p0 + offset;
                output[0].pos.z = v[0].pos.z;
                output[0].pos.xy /= _ScreenParams.xy;
                output[0].color = v[0].color;

                output[1].pos.xy = p1 + offset;
                output[1].pos.z = v[1].pos.z;
                output[1].pos.xy /= _ScreenParams.xy;
                output[1].color = v[1].color;

                output[2].pos.xy = p0 - offset;
                output[2].pos.z = v[0].pos.z;
                output[2].pos.xy /= _ScreenParams.xy;
                output[2].color = v[0].color;

                output[3].pos.xy = p1 - offset;
                output[3].pos.z = v[1].pos.z;
                output[3].pos.xy /= _ScreenParams.xy;
                output[3].color = v[1].color;

                triStream.Append(output[0]);
                triStream.Append(output[1]);
                triStream.Append(output[2]);
                triStream.Append(output[3]);

                triStream.RestartStrip();
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}
