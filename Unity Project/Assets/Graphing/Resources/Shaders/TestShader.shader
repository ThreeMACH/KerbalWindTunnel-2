Shader "ColorMap/TestShader" {
	Properties 
	{
		_Color ("Color", Color) = (0, 1, 0, 1)
	}
	SubShader
	{
		
		ZWrite On
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
	
			fixed4 _Color;
			sampler2D _GrabTexture;

			struct appdata
			{
				float4 vertex : POSITION;
			};
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
			};
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = o.pos;
				return o;
			}
			half4 frag(v2f i) : COLOR
			{
				float4 rgba = _Color;

				return rgba;
			}
			ENDCG
		}
	}
}