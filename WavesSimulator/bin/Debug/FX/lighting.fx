/**
* Code Improved from http://richardssoftware.net/ SlimDX Lighting Tutorial.
*
* Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
* Source: https://github.com/ericrrichards/dx11/blob/master/DX11/LightingDemo/FX/lighting.fx
*
* Code related to Texturing and Lighting are Copied from Above mentioned Source of RichardsSoftware.net
* Code related to reflection and refraction are based on http://www.rastertek.com/ Direct 3D 11 Tutorial - Water. Some minor portions are directly copied from the source http://www.rastertek.com/dx11tut29.html
*
**/

#include "LightHelper.fx"

struct Solution
{
	float4 Pos;
	float4 Normal;
	float4 Tangent;
};

cbuffer cbPerFrame
{
	DirectionalLight gDirLight;
	PointLight gPointLight;
	SpotLight gSpotLight;
	float3 gEyePosW;
};

cbuffer cbPerObject
{
	float4x4 gWorld;
	float4x4 gWorldInvTranspose;
	float4x4 gWorldViewProj;
	float4x4 gReflectViewProj;
	float4 gClipPlane;
	Material gMaterial;
	bool gUseStructBuf; //should the StructuredBuffer gSolution be used to retrieve vertices positions. Set true for rendering water.
	bool3 dummy;
	
};


struct VertexIn {
	float3 PosL    : POSITION;
	float3 NormalL : NORMAL;
	float2 Tex : TEXCOORD;
	uint VertexID : SV_VertexID;
};

StructuredBuffer<Solution> gSolution; //used to pass output from compute shader consisting of newly calculated vertex positions
Texture2D gDiffuseMap;
Texture2D gRefractiveMap;
Texture2D gReflectiveMap;
Texture2D gRefractionPositionMap;

SamplerState samAnisotropic
{
	Filter = ANISOTROPIC;
	MaxAnisotropy = 4;

	AddressU = WRAP;
	AddressV = WRAP;
};


struct VertexOut
{
	float4 PosH    : SV_POSITION;
	float3 PosW    : POSITION;
	float3 NormalW : NORMAL;
	float2 Tex :	TEXCOORD0;
	float4 RefractText : TEXCOORD1;
	float4 ReflectText : TEXCOORD2;
	float clip : SV_ClipDistance0;
};

struct PSOut
{
	float4 color : SV_TARGET0;
	float4 pos : SV_TARGET1;
};

VertexOut VS(VertexIn vin)
{
	VertexOut vout;

	if (gUseStructBuf)
	{
		vin.PosL = gSolution[vin.VertexID].Pos;
		vin.NormalL = gSolution[vin.VertexID].Normal;
	}

	// Transform to world space space.
	vout.PosW = mul(float4(vin.PosL, 1.0f), gWorld).xyz;


	vout.NormalW = mul(vin.NormalL, (float3x3)gWorldInvTranspose);

	// Transform to homogeneous clip space.
	vout.PosH = mul(float4(vin.PosL, 1.0f), gWorldViewProj);
	vout.Tex = vin.Tex;

	vout.RefractText = mul(float4(vin.PosL, 1.0f), gWorldViewProj);
	vout.ReflectText = mul(float4(vin.PosL, 1.0f), gReflectViewProj);
	vout.clip = vout.PosW.x * gClipPlane.x + vout.PosW.y * gClipPlane.y + vout.PosW.z * gClipPlane.z + gClipPlane.w;
	
	return vout;
}

PSOut PS(VertexOut pin)
{
	// Interpolating normal can unnormalize it, so normalize it.
	pin.NormalW = normalize(pin.NormalW);

	float3 toEyeW = normalize(gEyePosW - pin.PosW);

		// Start with a sum of zero. 
		float4 ambient = float4(0.0f, 0.0f, 0.0f, 0.0f);
		float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
		float4 spec = float4(0.0f, 0.0f, 0.0f, 0.0f);

		// Sum the light contribution from each light source.
		float4 A, D, S;

	ComputeDirectionalLight(gMaterial, gDirLight, pin.NormalW, toEyeW, A, D, S);
	ambient += A;
	diffuse += D;
	spec += S;

	ComputePointLight(gMaterial, gPointLight, pin.PosW, pin.NormalW, toEyeW, A, D, S);
	ambient += A;
	diffuse += D;
	spec += S;

	ComputeSpotLight(gMaterial, gSpotLight, pin.PosW, pin.NormalW, toEyeW, A, D, S);
	ambient += A;
	diffuse += D;
	spec += S;

	//Sample Difuse Texture
	float4 textColor = float4(1, 1, 1, 1);
	textColor = gDiffuseMap.Sample(samAnisotropic, pin.Tex);
	if (textColor.a == 0)
	{
		textColor = float4(1, 1, 1, 1);
	}
	
	//Sample Refraction
	float4 refColor = float4(1, 1, 1, 1);

	float2 refCord = pin.RefractText.xy;

	// Calculate the projected refraction texture coordinates.
	refCord.x = pin.RefractText.x / pin.RefractText.w / 2.0f + 0.5f;
	refCord.y = -pin.RefractText.y / pin.RefractText.w / 2.0f + 0.5f;

	refCord = refCord + (pin.NormalW.xz * 0.02f);

	refColor = gRefractiveMap.Sample(samAnisotropic, refCord);
	if (refColor.a == 0)
		refColor = float4(1, 1, 1, 1);

	//Sample Reflection

	float4 reflColor = float4(1, 1, 1, 1);

	float2 reflCord = pin.ReflectText.xy;

	// Calculate the projected refraction texture coordinates.
	reflCord.x = pin.ReflectText.x / pin.ReflectText.w / 2.0f + 0.5f;
	reflCord.y = -pin.ReflectText.y / pin.ReflectText.w / 2.0f + 0.5f;

	reflCord = reflCord + (pin.NormalW.xz * 0.02f);

	reflColor = gReflectiveMap.Sample(samAnisotropic, reflCord);
	if (reflColor.a == 0)
		reflColor = float4(1, 1, 1, 1);

	//Sample Reflection, Refraction blend factor
	float4 refPosition = gRefractionPositionMap.Sample(samAnisotropic, refCord);
	float blendFactor = refPosition.y * refPosition.y / 600.0f;
	blendFactor = min(blendFactor, 1);

	//Calculate net result from Diffuse Map, Reflection and Refraction
	textColor = textColor * (lerp(reflColor,float4(1,1,1,1),float4(0.0f,0.0f,0.0f,0.0f))  )*lerp(refColor,float4(1,1,1,1),float4(blendFactor,blendFactor,blendFactor,blendFactor)) * gMaterial.Reflect.r;
	
	float4 litColor = (ambient + diffuse) + spec;
	if (textColor.a > 0)
		litColor = (ambient + diffuse) *textColor  + spec;
	

	
		
	
	litColor.a = gMaterial.Diffuse.a ;
	

	PSOut output;
	output.color = litColor;
	output.pos = float4(pin.PosW.xyz,1);
	return output;
}


technique11 LightTech
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}