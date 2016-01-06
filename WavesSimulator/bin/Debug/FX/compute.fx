/**
* This is a Direct Compute based implementation of Wave Simulation Algorithm provided in http://richardssoftware.net/ DirectX 11 Tutorials - Waves Tutorial
* The original code calculates Vertex Displacements in Software using CPU.
* I have modified the original code and written it to be compatible with Direct Compute. 
* Original Code Source  - https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Waves.cs
*/
#define BLOCK_SIZE 32

cbuffer perFrame : register(b0)
{
	uint RowCount;
	uint ColumnCount;
	float _spatialStep;
	float dummy;	
	float4 k; 

}

struct Solution
{
	float4 Pos;
	float4 Normal;
	float4 Tangent;
};

StructuredBuffer<Solution> gPrevSolution : register(t0);
RWStructuredBuffer<Solution> gCurrentSolution : register(u0);

[numthreads(BLOCK_SIZE,BLOCK_SIZE,1)]
void ProcessVertex(uint3 id: SV_DispatchThreadID)
{
	if (id.x < ColumnCount-1 && id.y < RowCount-1 && id.x > 0 && id.y > 0)
	{

		//Calculate height
		float n =
			k.x * gCurrentSolution[id.y * ColumnCount + id.x].Pos.y +
			k.y * gPrevSolution[id.y * ColumnCount + id.x].Pos.y +
			k.z * (gPrevSolution[(id.y + 1) * ColumnCount + id.x].Pos.y +
			gPrevSolution[(id.y - 1) * ColumnCount + id.x].Pos.y +
			gPrevSolution[id.y * ColumnCount + id.x + 1].Pos.y +
			gPrevSolution[id.y * ColumnCount + id.x - 1].Pos.y);

			gCurrentSolution[id.y * ColumnCount + id.x].Pos.y = n;

		//Calculate Normal and Tangent
			float l = gCurrentSolution[id.y * ColumnCount + id.x - 1].Pos.y;
			float r = gCurrentSolution[id.y * ColumnCount + id.x + 1].Pos.y;
			float t = gCurrentSolution[(id.y - 1) * ColumnCount + id.x].Pos.y;
			float b = gCurrentSolution[(id.y + 1) * ColumnCount + id.x].Pos.y;
			gCurrentSolution[id.y * ColumnCount + id.x].Normal = float4(normalize(float3(-r + l, 2.0f * _spatialStep, b - t)),1.0f);

			gCurrentSolution[id.y * ColumnCount + id.x].Tangent = float4(normalize(float3(2.0f * _spatialStep, r - l, 0.0f)),1.0f);
	}
}
