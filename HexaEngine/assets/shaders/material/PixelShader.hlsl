#include "defs.hlsl"
#include "../gbuffer.hlsl"
#include "../material.hlsl"

Texture2D albedoTexture : register(t0);
Texture2D normalTexture : register(t1);
Texture2D roughnessTexture : register(t2);
Texture2D metalnessTexture : register(t3);
Texture2D emissiveTexture : register(t4);
Texture2D aoTexture : register(t5);

SamplerState materialSamplerState : register(s0);

cbuffer MaterialBuffer : register(b2)
{
	Material material;
};

float3 NormalSampleToWorldSpace(float3 normalMapSample, float3 unitNormalW, float3 tangentW)
{
	// Uncompress each component from [0,1] to [-1,1].
	float3 normalT = 2.0f * normalMapSample - 1.0f;

	// Build orthonormal basis.
	float3 N = unitNormalW;
	float3 T = normalize(tangentW - dot(tangentW, N) * N);
	float3 B = cross(N, T);

	float3x3 TBN = float3x3(T, B, N);

	// Transform from tangent space to world space.
	float3 bumpedNormalW = mul(normalT, TBN);

	return bumpedNormalW;
}

GeometryData main(PixelInput input)
{
    float3 albedo;
    float3 pos = (float3)input.pos;
    float3 normal;
    float3 emissive;
    float opacity = 1;
    float metalness;
    float roughness;
    float ao;


    if (material.HasAlbedoMap)
    {
        albedo = albedoTexture.Sample(materialSamplerState, (float2) input.tex).rgb;
    }
    else
    {
        albedo = material.Color;
    }
    if (material.HasNormalMap)
    {      
        normal = NormalSampleToWorldSpace(normalTexture.Sample(materialSamplerState, (float2) input.tex).rgb, input.normal, input.tangent);
    }
    else
    {
        normal = input.normal;
    }
    if (material.HasRoughnessMap)
    {
        roughness = roughnessTexture.Sample(materialSamplerState, (float2) input.tex).r;
    }
    else
    {
        roughness = material.Roughness;
    }
    if (material.HasMetalnessMap)
    {
        metalness = metalnessTexture.Sample(materialSamplerState, (float2) input.tex).r;
    }
    else
    {
        metalness = material.Metalness;
    }
    if (material.HasEmissiveMap)
    {
        emissive = emissiveTexture.Sample(materialSamplerState, (float2) input.tex).rgb;
    }
    else
    {
        emissive = material.Emissive;
    }
    if (material.HasAoMap)
    {
        ao = aoTexture.Sample(materialSamplerState, (float2) input.tex).r;
    }
    else
    {
        ao = material.Ao;
    }
	
    return PackGeometryData(albedo, opacity, pos, input.depth, normal, roughness, metalness, float3(0, 0, 0), float3(0,0,0),0, 1, 0.5f,ao,1,0,0,0,0,0,0,0,0);
}