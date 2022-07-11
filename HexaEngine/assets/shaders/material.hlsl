struct Material {
    float3 Color;
    float reserved1;
    
    float Roughness;
    float3 reserved2;
    
    float Metalness;
    float3 reserved3;
    
    float3 Emissive;
    float reserved4;
    
    float Ao;
    float3 reserved5;
    
    bool HasDisplacementMap;
    bool HasAlbedoMap;
    bool HasNormalMap;
    bool HasRoughnessMap;
    bool HasMetalnessMap;
    bool HasEmissiveMap;
    bool HasAoMap;
    
};