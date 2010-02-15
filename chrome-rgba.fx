// OpenRA ui shader for non-SHP sprites (mostly just chrome)
// Author: C. Forbes
//--------------------------------------------------------

float2 r1, r2;		// matrix elements

sampler2D DiffuseTexture = sampler_state {
	MinFilter = Nearest;
	MagFilter = Nearest;
};

struct VertexIn {
	float4 Position: POSITION;
	float4 Tex0: TEXCOORD0;
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
	o.Tex0 = v.Tex0.xy;
	return o;
}

float4 Simple_fp(FragmentIn f) : COLOR0 {
	float4 r = tex2D(DiffuseTexture, f.Tex0);
	return r;
}

technique high_quality {
	pass p0	{
		BlendEnable = true;
		DepthTestEnable = false;
//		CullMode = None;
//		FillMode = Solid;
		VertexProgram = compile arbvp1 Simple_vp();
		FragmentProgram = compile arbfp1 Simple_fp();
		
		//SrcBlend = SrcAlpha;
		//DestBlend = InvSrcAlpha;
	}
}
