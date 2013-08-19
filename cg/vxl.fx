mat4x4 View;
mat4x4 TransformMatrix;
float2 PaletteRows;

float4 LightDirection;
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
	VertexOut o;
	o.Position = mul(mul(v.Position, TransformMatrix), View);
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

	float4 normal = (2.0*tex2D(Palette, vec2(dot(x, f.NormalsChannel), PaletteRows.y)) - 1.0);
	float3 intensity = AmbientLight + DiffuseLight*max(dot(normal, LightDirection), 0.0);
	return float4(intensity*color.rgb, color.a);
}

technique high_quality {
	pass p0 {
		DepthTestEnable = true;
		CullFaceEnable = false;
		VertexProgram = compile latest Simple_vp();
		FragmentProgram = compile latest Simple_fp();
	}
}

technique high_quality_cg21 {
	pass p0 {
		DepthTestEnable = true;
		CullFaceEnable = false;
		VertexProgram = compile arbvp1 Simple_vp();
		FragmentProgram = compile arbfp1 Simple_fp();
	}
}