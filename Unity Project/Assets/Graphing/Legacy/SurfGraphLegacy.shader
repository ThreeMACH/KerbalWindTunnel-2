Shader "SurfGraph"
{
    Properties
    {
        MapRange ("MapRange", Vector) = (0.000000,1.000000,0.000000,0.000000)
        [ToggleUI]  InvertZ ("Invert Z", Float) = 1.000000
        DepthCutoff ("Depth Cutoff", Float) = 0.000000
        [ToggleUI]  UseDepthCutoff ("Use Depth Cutoff", Float) = 0.000000
        [NoScaleOffset]  GradientTexture ("CustomGradientColors", 2D) = "white" { }
        [KeywordEnum(Jet, JetDark, Grayscale, Custom)]  SHADERMODE ("ColorMap", Float) = 0.000000
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent+0" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Tags { "QUEUE"="Transparent+0" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
