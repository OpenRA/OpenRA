// OpenRA gui lines shader
// Author: C. Forbes
//--------------------------------------------------------

shared float2 Scroll;

shared float2 r1, r2;		// matrix elements

struct VertexIn {
	float4 Position: POSITION;
	float2 RG: TEXCOORD0;
	float2 BA: TEXCOORD1;
};

struct VertexOut {
	float4 Position: POSITION;
	float4 Color: COLOR0;
};

struct FragmentIn {
	float4 Color: COLOR0;
};

VertexOut Simple_vp(VertexIn v) {
	VertexOut o;
	float2 p = (v.Position.xy - Scroll.xy) * r1 + r2;
	o.Position = float4(p.x,p.y,0,1);
	o.Color.rg = v.RG.xy;
	o.Color.ba = v.BA.xy;
	o.Color.a = 1.0f;
	return o;
}

const float2 texelOffset = float2( 0, 1.0f/32.0f );

float4 Simple_fp(FragmentIn f) : COLOR0 {
	return float4(1,1,1,1);
	//return f.Color;
}

technique high_quality {
	pass p0	{
		AlphaBlendEnable = false;
		ZWriteEnable = false;
		ZEnable = false;
		CullMode = None;
		FillMode = Wireframe;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Simple_fp();
		
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	}
}
