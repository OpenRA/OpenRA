mat4x4 View;
mat4x4 TransformMatrix;
float4 LightDirection;
float GroundZ;
float3 GroundNormal;

float2 PaletteRows;
float3 AmbientLight, DiffuseLight;

sampler2D DiffuseTexture = sampler_state {
	MinFilter = Nearest;
	MagFilter = Nearest;
	WrapS = Repeat;
	WrapT = Repeat;
};

sampler2D Palette = sampler_state {
	MinFilter = Nearest;
	MagFilter = Nearest;
	WrapS = Repeat;
	WrapT = Repeat;
};

struct VertexIn {
	float4 Position: POSITION;
	float4 Tex0: TEXCOORD0;
};

struct VertexOut {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
	float4 ColorChannel: TEXCOORD1;
	float4 NormalsChannel: TEXCOORD2;
};

float4 DecodeChannelMask(float x)
{
	if (x > 0)
		return (x > 0.5f) ? float4(1,0,0,0) : float4(0,1,0,0);
	else
		return (x <-0.5f) ? float4(0,0,0,1) : float4(0,0,1,0);
}

VertexOut Simple_vp(VertexIn v) {
	// Distance between vertex and ground
	float d = dot(v.Position.xyz - float3(0.0,0.0,GroundZ), GroundNormal) / dot(LightDirection.xyz, GroundNormal);
	float3 shadow = v.Position.xyz - d*LightDirection.xyz;

	VertexOut o;
	o.Position = mul(mul(vec4(shadow, 1), TransformMatrix), View);
	o.Tex0 = v.Tex0.xy;
	o.ColorChannel = DecodeChannelMask(v.Tex0.z);
	o.NormalsChannel = DecodeChannelMask(v.Tex0.w);
	return o;
}

float4 Simple_fp(VertexOut f) : COLOR0 {
	float4 x = tex2D(DiffuseTexture, f.Tex0.xy);
	vec4 color = tex2D(Palette, float2(dot(x, f.ColorChannel), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	return color;
}

technique high_quality {
	pass p0 {
		BlendEnable = true;
		DepthTestEnable = true;
		CullFaceEnable = false;
		VertexProgram = compile latest Simple_vp();
		FragmentProgram = compile latest Simple_fp();

		BlendEquation = FuncAdd;
		BlendFunc = int2(SrcAlpha, OneMinusSrcAlpha);
	}
}

technique high_quality_cg21 {
	pass p0 {
		BlendEnable = true;
		DepthTestEnable = true;
		CullFaceEnable = false;
		VertexProgram = compile arbvp1 Simple_vp();
		FragmentProgram = compile arbfp1 Simple_fp();

		BlendEquation = FuncAdd;
		BlendFunc = int2(SrcAlpha, OneMinusSrcAlpha);
	}
}