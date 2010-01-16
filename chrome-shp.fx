// OpenRA test shader
// Author: C. Forbes
//--------------------------------------------------------

shared texture DiffuseTexture, Palette;
shared float2 Scroll;

shared float2 r1, r2;		// matrix elements

sampler s_DiffuseTexture = sampler_state {
	Texture = <DiffuseTexture>;
	MinFilter = None;
	MagFilter = None;
	MipFilter = None;
  
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

sampler s_PaletteTexture = sampler_state {
	Texture = <Palette>;
	MinFilter = None;
	MagFilter = None;
	MipFilter = None;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexIn {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
	float2 Tex1: TEXCOORD1;
};

struct VertexOut {
	float4 Position: POSITION;
	float3 Tex0: TEXCOORD0;
	float4 ChannelMask: TEXCOORD1;
};

struct FragmentIn {
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
	o.Tex0 = float3(v.Tex0.x, v.Tex0.y, v.Tex1.x);
	o.ChannelMask = DecodeChannelMask( v.Tex1.y );
	return o;
}

const float2 texelOffset = float2( 0, 1.0f/32.0f );

float4 Palette_fp(FragmentIn f) : COLOR0 {
	float4 x = tex2D(s_DiffuseTexture, f.Tex0.xy);
	float2 p = float2( dot(x, f.ChannelMask), f.Tex0.z );
	return tex2D(s_PaletteTexture, p + texelOffset);
}

technique low_quality {
	pass p0 {
		AlphaBlendEnable = false;
		ZWriteEnable = false;
		ZEnable = false;
		CullMode = None;
		FillMode = Solid;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Palette_fp();
	}
}

technique high_quality {
	pass p0	{
		AlphaBlendEnable = true;
		ZWriteEnable = false;
		ZEnable = false;
		CullMode = None;
		FillMode = Solid;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Palette_fp();
		
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	}
}
