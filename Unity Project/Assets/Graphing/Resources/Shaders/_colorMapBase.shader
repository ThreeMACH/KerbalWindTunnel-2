#include "UnityCG.cginc"

float _Min;
float _Max;
bool _Clip;

struct appdata
{
    float4 position : POSITION;
    float4 coords : TEXCOORD0;
    float4 heights : TEXCOORD1;
#ifdef _OUTLINE_SOURCE
    float4 outlineValue : TEXCOORD2;
#endif
};
            
struct v2f
{
    float4 position : SV_POSITION;
    float2 localPos : TEXCOORD0;
    nointerpolation float4 coords : TEXCOORD1;
    nointerpolation float4 heights : TEXCOORD2;
#ifdef _OUTLINE_SOURCE
    nointerpolation float4 outlineValue : TEXCOORD3;
#endif
};
            
v2f Vertex_Shader (appdata v)
{
    v2f o;
    o.position = UnityObjectToClipPos(v.position);
    o.localPos = v.position;
    o.coords = v.coords;
    o.heights = v.heights;
#ifdef _OUTLINE_SOURCE
    o.outlineValue = v.outlineValue;
#endif
    return o;
}

float GetHeight(v2f i)
{
    float xl = (i.localPos.x - i.coords.x) / i.coords.z;
    float yl = (i.localPos.y - i.coords.y) / i.coords.w;
    return lerp(lerp(i.heights.x, i.heights.y, xl), lerp(i.heights.z, i.heights.w, xl), yl);
}