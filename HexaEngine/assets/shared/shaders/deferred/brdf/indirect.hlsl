#include "../../gbuffer.hlsl"
#include "../../camera.hlsl"
#include "../../light.hlsl"
#include "../../weather.hlsl"

Texture2D GBufferA : register(t0);
Texture2D GBufferB : register(t1);
Texture2D GBufferC : register(t2);
Texture2D GBufferD : register(t3);
Texture2D<float> Depth : register(t4);
Texture2D ssao : register(t5);
Texture2D brdfLUT : register(t9);
StructuredBuffer<GlobalProbe> globalProbes : register(t10);
TextureCube globalDiffuse : register(t11);
TextureCube globalSpecular : register(t12);

SamplerState linearClampSampler : register(s0);
SamplerState linearWrapSampler : register(s1);
SamplerState pointClampSampler : register(s2);
SamplerComparisonState shadowSampler : register(s3);

struct Params
{
    uint globalProbeCount;
    uint localProbeCount;
    uint padd0;
    uint padd1;
};

cbuffer paramsBuffer : register(b0)
{
    Params params;
};

struct VSOut
{
    float4 Pos : SV_Position;
    float2 Tex : TEXCOORD;
};

float4 ComputeLightingPBR(VSOut input, float3 position, GeometryAttributes attrs)
{
    float3 baseColor = attrs.baseColor;

    float roughness = attrs.roughness;
    float metallic = attrs.metallic;

    float3 N = normalize(attrs.normal);
    float3 V = normalize(GetCameraPos() - position);
    float ao = ssao.Sample(linearWrapSampler, input.Tex).r * attrs.ao;
    float3 ambient = 0;

    float3 F0 = lerp(float3(0.04f, 0.04f, 0.04f), baseColor, metallic);

    ambient += BRDF_IBL(linearWrapSampler, globalDiffuse, globalSpecular, brdfLUT, F0, N, V, baseColor, roughness);

    float3 color = ambient * ao + attrs.emission * attrs.emissionStrength;

    return float4(color, 1);
}

float4 main(VSOut pixel) : SV_TARGET
{
    float depth = Depth.Sample(linearWrapSampler, pixel.Tex);
    float3 position = GetPositionWS(pixel.Tex, depth);
    GeometryAttributes attrs;
    ExtractGeometryData(pixel.Tex, GBufferA, GBufferB, GBufferC, GBufferD, linearWrapSampler, attrs);

    return ComputeLightingPBR(pixel, position, attrs);
}