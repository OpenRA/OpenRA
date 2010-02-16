// OpenRA test shader
// Author: C. Forbes
//--------------------------------------------------------

float2 Scroll;
float2 r1, r2;		// matrix elements

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
	float3 Tex0: TEXCOORD0;
	float4 ChannelMask: TEXCOORD1;
};

float4 DecodeChannelMask( float x )
{
	if (x > 0)
		return (x > 0.5f) ? float4(1,0,0,0) : float4(0,1,0,0);
	else
		return (x <-0.5f) ? float4(0,0,0,1) : float4(0,0,1,0);
}

VertexOut Simple_vp(VertexIn v) {
	VertexOut o;
	float2 p = v.Position.xy * r1 + r2;
	o.Position = float4(p.x,p.y,0,1);
	o.Tex0 = float3(v.Tex0.x, v.Tex0.y, v.Tex0.z);
	o.ChannelMask = DecodeChannelMask( v.Tex0.w );
	return o;
}

const float2 texelOffset = float2( 0, 1.0f/32.0f );

float4 Palette_fp(VertexOut f) : COLOR0 {
	float4 x = tex2D(DiffuseTexture, f.Tex0.xy);
	float2 p = float2( dot(x, f.ChannelMask), f.Tex0.z );
	return tex2D(Palette, p + texelOffset);
}

technique low_quality {
	pass p0 {
		BlendEnable = true;
		DepthTestEnable = false;
		CullFaceEnable = false;
		VertexProgram = compile latest Simple_vp();
		FragmentProgram = compile latest Palette_fp();

		BlendEquation = FuncAdd;
		BlendFunc = int2( SrcAlpha, OneMinusSrcAlpha );
	}
}

