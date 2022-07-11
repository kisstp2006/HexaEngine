////////////////////////////////////////////////////////////////////////////////
// Filename: deferred.vs
////////////////////////////////////////////////////////////////////////////////

//////////////
// TYPEDEFS //
//////////////
struct VertexInputType
{
	float4 position : POSITION;
	float3 tex : TEXCOORD0;
	float3 normal : NORMAL;
};

struct HullInputType
{
	float3 pos : POSITION;
	float3 tex : TEXCOORD0;
	float3 normal : NORMAL;
};

////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
HullInputType main(VertexInputType input)
{
	HullInputType output;

	// Calculate the position of the vertex against the world, view, and projection matrices.
	output.pos = (float3)input.position;
	output.tex = input.tex;
	output.normal = input.normal;

	return output;
}