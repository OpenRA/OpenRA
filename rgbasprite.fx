// OpenRA ui shader for non-SHP sprites (mostly just chrome)
// Author: C. Forbes
//--------------------------------------------------------

shared texture DiffuseTexture;
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

struct VertexIn {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
	float2 Tex1: TEXCOORD1;
};

struct VertexOut {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
};

struct FragmentIn {
	float2 Tex0: TEXCOORD0;
};

VertexOut Simple_vp(VertexIn v) {
	VertexOut o;	
	float2 p = v.Position.xy * r1 + r2;
	o.Position = float4(p.x,p.y,0,1);
	o.Tex0 = v.Tex0;
	return o;
}

float4 Simple_fp(FragmentIn f) : COLOR0 {
	float4 r = tex2D(s_DiffuseTexture, f.Tex0.xy);
	return r;
}

/*
technique low_quality {
	pass p0 {
		AlphaBlendEnable = false;
		ZWriteEnable = false;
		ZEnable = false;
		CullMode = None;
		FillMode = Solid;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Simple_fp();
	}
}
*/

technique high_quality {
	pass p0	{
		AlphaBlendEnable = true;
		ZWriteEnable = false;
		ZEnable = false;
		CullMode = None;
		FillMode = Solid;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Simple_fp();
		
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	}
}
