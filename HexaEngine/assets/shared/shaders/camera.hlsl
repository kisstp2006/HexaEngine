cbuffer CameraBuffer : register(b1)
{
    float4x4 view;
    float4x4 proj;
    float4x4 viewInv;
    float4x4 projInv;
    float4x4 viewProj;
    float4x4 viewProjInv;
    float4x4 prevViewProj;
    float camFar;
    float camNear;
    float2 screenDim;
};

float3 GetCameraPos()
{
    return float3(viewInv._41, viewInv._42, viewInv._43);
}

float GetLinearDepth(float depth)
{
    float z_b = depth;
    float z_n = 2.0 * z_b - 1.0;
    float z_e = 2.0 * camNear * camFar / (camFar + camNear - z_n * (camFar - camNear));
    return z_e;
}

float SampleLinearDepth(Texture2D tex, SamplerState smp, float2 texCoord)
{
    float depth = tex.Sample(smp, texCoord).r;
    return GetLinearDepth(depth);
}

float SampleLinearDepth(Texture2D<float> tex, SamplerState smp, float2 texCoord)
{
    float depth = tex.Sample(smp, texCoord);
    return GetLinearDepth(depth);
}

float3 GetPositionVS(float2 uv, float depth)
{
    float4 ndc = float4(uv * 2.0f - 1.0f, depth, 1.0f);
    ndc.y *= -1;
    float4 wp = mul(ndc, projInv);
    return wp.xyz / wp.w;
}

float4 Screen2View(float3 screen)
{
    float2 uv = screen.xy / screenDim.xy;
    float4 ndc = float4(uv * 2.0f - 1.0f, screen.z, 1.0f);
    ndc.y *= -1;
    float4 wp = mul(ndc, projInv);
    return wp.xyzw / wp.w;
}

float3 GetPositionWS(float2 uv, float depth)
{
    float4 ndc = float4(uv * 2.0f - 1.0f, depth, 1.0f);
    ndc.y *= -1;
    float4 wp = mul(ndc, viewProjInv);
    return wp.xyz / wp.w;
}

float2 ProjectUV(float3 uv)
{
    float4 uv_projected = mul(float4(uv, 1.0), proj);
    uv_projected.xy /= uv_projected.w;
    return uv_projected.xy * float2(0.5f, -0.5f) + 0.5f;
}