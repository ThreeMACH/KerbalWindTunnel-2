Shader "Hidden/JumpFloodOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always

        CGINCLUDE
        // just inside the precision of a R16G16_SNorm to keep encoded range 1.0 >= and > -1.0
        #define SNORM16_MAX_FLOAT_MINUS_EPSILON ((float)(32768-2) / (float)(32768-1))
        #define FLOOD_ENCODE_OFFSET float2(1.0, SNORM16_MAX_FLOAT_MINUS_EPSILON)
        #define FLOOD_ENCODE_SCALE float2(2.0, 1.0 + SNORM16_MAX_FLOAT_MINUS_EPSILON)

        #define FLOOD_NULL_POS -1.0
        #define FLOOD_NULL_POS_FLOAT2 float2(FLOOD_NULL_POS, FLOOD_NULL_POS)
        #define FLOOD_NULL_POS_FLOAT3 float3(FLOOD_NULL_POS, FLOOD_NULL_POS, 0)
        #define FLOOD_NULL_POS_FLOAT4 float4(FLOOD_NULL_POS, FLOOD_NULL_POS, 0, 1)
        ENDCG
        
        Pass // 0
        {
            Name "INNERSTENCIL"

            Stencil {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp NotEqual
                Pass Replace
            }

            ColorMask 0
            Blend Zero One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            float4 vert (float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            // null frag
            void frag () {}
            ENDCG
        }

        Pass // 1
        {
            Name "BUFFERFILL"

            ZWrite On
            ZTest Less

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            bool _AllowFlip;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            float4 vert (appdata v) : SV_POSITION
            {
                float4 pos = UnityObjectToClipPos(v.vertex);

                // flip the rendering "upside down" in non OpenGL to make things easier later
                // you'll notice none of the later passes need to pass UVs
                // We'll do everything in flipped space instead and un-flip at the end.
                //#ifdef UNITY_UV_STARTS_AT_TOP
                    //pos.y = -pos.y;
                //#endif

                return pos;
            }

            fixed4 frag (float4 pos : SV_POSITION) : SV_Target
            {
                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }

        Pass // 2
        {
            Name "JUMPFLOODINIT"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            //Texture2D _DepthTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = i.pos.xy;
                // calculate output position for this pixel
                float2 outPos = i.pos.xy * abs(_MainTex_TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;
                if (_MainTex.Load(int3(uvInt, 0)).a < 0.5)
                    return FLOOD_NULL_POS_FLOAT4;
                return float4(outPos, 0, 1); //_DepthTex.Load(int3(uvInt, 0)).r

                /* Removed to enable depth texture
                // sample silhouette texture for sobel
                half3x3 values;
                UNITY_UNROLL
                for(int u=0; u<3; u++)
                {
                    UNITY_UNROLL
                    for(int v=0; v<3; v++)
                    {
                        uint2 sampleUV = clamp(uvInt + int2(u-1, v-1), int2(0,0), (int2)_MainTex_TexelSize.zw - 1);
                        values[u][v] = _MainTex.Load(int3(sampleUV, 0)).a;
                    }
                }

                // interior, return position
                if (values._m11 > 0.99)
                    return outPos;

                // exterior, return no position
                if (values._m11 < 0.01)
                    return FLOOD_NULL_POS_FLOAT2;

                // sobel to estimate edge direction
                float2 dir = -float2(
                    values[0][0] + values[0][1] * 2.0 + values[0][2] - values[2][0] - values[2][1] * 2.0 - values[2][2],
                    values[0][0] + values[1][0] * 2.0 + values[2][0] - values[0][2] - values[1][2] * 2.0 - values[2][2]
                    );

                // if dir length is small, this is either a sub pixel dot or line
                // no way to estimate sub pixel edge, so output position
                //if (abs(dir.x) <= 0.005 && abs(dir.y) <= 0.005)
                    return outPos;

                // normalize direction
                dir = normalize(dir);

                // sub pixel offset
                float2 offset = dir * (1.0 - values._m11);

                // output encoded offset position
                return (i.pos.xy + offset) * abs(_MainTex_TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;
                */
            }
            ENDCG
        }

        Pass // 3
        {
            Name "JUMPFLOOD"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            int _StepWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);

                // initialize best distance at infinity
                float bestDist = 1.#INF;
                float2 bestCoord;
                //float bestDepth;

                // jump samples
                UNITY_UNROLL
                for(int u=-1; u<=1; u++)
                {
                    UNITY_UNROLL
                    for(int v=-1; v<=1; v++)
                    {
                        // calculate offset sample position
                        int2 offsetUV = uvInt + int2(u, v) * _StepWidth;

                        // .Load() acts funny when sampling outside of bounds, so don't
                        offsetUV = clamp(offsetUV, int2(0,0), (int2)_MainTex_TexelSize.zw - 1);

                        // decode position from buffer
                        float3 texLoad = _MainTex.Load(int3(offsetUV, 0)).rgb;
                        float2 offsetPos = (texLoad.rg + FLOOD_ENCODE_OFFSET) * _MainTex_TexelSize.zw / FLOOD_ENCODE_SCALE;

                        // the offset from current position
                        float2 disp = i.pos.xy - offsetPos;

                        // square distance
                        float dist = dot(disp, disp);

                        // if offset position isn't a null position or is closer than the best
                        // set as the new best and store the position
                        if (offsetPos.y != FLOOD_NULL_POS && dist < bestDist)
                        {
                            bestDist = dist;
                            bestCoord = offsetPos;
                            //bestDepth = texLoad.z;
                        }
                    }
                }

                // if not valid best distance output null position, otherwise output encoded position
                return isinf(bestDist) ? FLOOD_NULL_POS_FLOAT4 : float4(bestCoord * _MainTex_TexelSize.xy * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET, 0, 1);//, bestDepth, 1);
            }
            ENDCG
        }

        Pass // 4
        {
            Name "JUMPFLOOD_SINGLEAXIS"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            int2 _AxisWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);

                // initialize best distance at infinity
                float bestDist = 1.#INF;
                float2 bestCoord;
                float bestDepth;

                // jump samples
                // only one loop
                UNITY_UNROLL
                for(int u=-1; u<=1; u++)
                {
                    // calculate offset sample position
                    int2 offsetUV = uvInt + _AxisWidth * u;

                    // .Load() acts funny when sampling outside of bounds, so don't
                    offsetUV = clamp(offsetUV, int2(0,0), (int2)_MainTex_TexelSize.zw - 1);

                    // decode position from buffer
                    float3 texLoad = _MainTex.Load(int3(offsetUV, 0)).rgb;
                    float2 offsetPos = (texLoad.rg + FLOOD_ENCODE_OFFSET) * _MainTex_TexelSize.zw / FLOOD_ENCODE_SCALE;

                    // the offset from current position
                    float2 disp = i.pos.xy - offsetPos;

                    // square distance
                    float dist = dot(disp, disp);

                    // if offset position isn't a null position or is closer than the best
                    // set as the new best and store the position
                    if (offsetPos.x != -1.0 && dist < bestDist)
                    {
                        bestDist = dist;
                        bestCoord = offsetPos;
                        bestDepth = texLoad.z;
                    }
                }

                // if not valid best distance output null position, otherwise output encoded position
                return isinf(bestDist) ? FLOOD_NULL_POS_FLOAT4 : float4(bestCoord * _MainTex_TexelSize.xy * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET, bestDepth, 1);
            }
            ENDCG
        }

        Pass // 5
        {
            Name "JUMPFLOODOUTLINE"

            Stencil {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp NotEqual
                Pass Zero
                Fail Zero
            }

            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            Texture2D _ColorTex;
            float4 _MainTex_TexelSize;

            half4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);
                #ifdef UNITY_UV_STARTS_AT_TOP
                    uvInt.y = _MainTex_TexelSize.w - uvInt.y;
                #endif

                // load encoded position
                float3 encodedPos = _MainTex.Load(int3(uvInt, 0)).rgb;

                // early out if null position
                if (encodedPos.y == -1)
                    return half4(0,0,0,0);

                // decode closest position
                float2 nearestPos = (encodedPos.xy + FLOOD_ENCODE_OFFSET) * abs(_ScreenParams.xy) / FLOOD_ENCODE_SCALE;

                // current pixel position
                float2 currentPos = uvInt;//i.pos.xy;

                // distance in pixels to closest position
                half dist = length(nearestPos - currentPos);

                // calculate outline
                // + 1.0 is because encoded nearest position is half a pixel inset
                // not + 0.5 because we want the anti-aliased edge to be aligned between pixels
                // distance is already in pixels, so this is already perfectly anti-aliased!
                half outline = saturate(_OutlineWidth - dist + 0.5);

                half4 col = _ColorTex.Load(int3(nearestPos, 0));//_OutlineColor;
                col.a *= outline;

                // profit!
                return col;
            }
            ENDCG
        }

        Pass // 6
        {
            Name "JUMPFLOODOUTLINE_DEPTH"

            Stencil {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp NotEqual
                Pass Zero
                Fail Zero
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            Texture2D _ColorTex;
            float4 _MainTex_TexelSize;
            Texture2D _DepthTex;

            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag (v2f i, out float depth : SV_DEPTH) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);
                #ifdef UNITY_UV_STARTS_AT_TOP
                    uvInt.y = _MainTex_TexelSize.w - uvInt.y;
                #endif
                //uvInt.x = uvInt.x + 1;

                // load encoded position
                float2 encodedPos = _MainTex.Load(int3(uvInt, 0)).xy;//z;

                // early out if null position
                if (encodedPos.y == -1)
                    clip(-1);
                
                // decode closest position
                float2 nearestPos = (encodedPos.xy + FLOOD_ENCODE_OFFSET) * abs(_ScreenParams.xy) / FLOOD_ENCODE_SCALE;

                // current pixel position
                float2 currentPos = uvInt;//i.pos.xy;

                // distance in pixels to closest position
                half dist = length(nearestPos - currentPos);

                // calculate outline
                // + 1.0 is because encoded nearest position is half a pixel inset
                // not + 0.5 because we want the anti-aliased edge to be aligned between pixels
                // distance is already in pixels, so this is already perfectly anti-aliased!
                half outline = saturate(_OutlineWidth - dist + 0.5);
                clip(outline - 0.01);

                half4 col = _ColorTex.Load(int3(nearestPos, 0));
                col.a *= outline;

                //if (outline >= 0.9)
                //depth = encodedPos.z;
                depth = _DepthTex.Load(int3(nearestPos, 0)).r;

                // profit!
                return col;
            }
            ENDCG
        }

        Pass // 7
        {
            Name "DEPTH_INIT"

            Blend One Zero
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float depth : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.depth = o.pos.z;
                return o;
            }

            float frag (v2f i) : SV_Target {
                return i.depth;
            }
            ENDCG
        }
    }
}