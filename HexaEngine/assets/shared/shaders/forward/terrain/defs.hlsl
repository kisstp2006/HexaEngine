struct VertexInput
{
    float3 pos : POSITION;
    float3 tex : TEXCOORD;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float3 bitangent : BINORMAL;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float3 pos : POSITION;
    float3 tex : TEXCOORD0;
    float2 ctex : TEXCOORD1;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float3 bitangent : BINORMAL;

#if HasBakedLightMap
    float3 H0 : H0;
    float3 H1 : H1;
    float3 H2 : H2;
    float3 H3 : H3;
#endif

#if BAKE_FORWARD
    bool IsFrontFace : SV_IsFrontFace;
#endif
};