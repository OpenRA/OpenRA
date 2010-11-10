// OpenRA gui lines shader
// Author: C. Forbes
//--------------------------------------------------------

float2 Scroll;

float2 r1, r2;		// matrix elements

struct VertexIn {
	float4 Position: POSITION;
	float4 Color: TEXCOORD0;
};

struct VertexOut {
	float4 Position: POSITION;
	float4 Color: COLOR0;
};

VertexOut Simple_vp(VertexIn v) {
	VertexOut o;
	float2 p = (v.Position.xy - Scroll.xy) * r1 + r2;
	o.Position = float4(p.x,p.y,0,1);
	o.Color = v.Color;
	return o;
}

float4 Simple_fp(VertexOut f) : COLOR0 {
	return f.Color;
}

technique high_quality {
	pass p0	{
		BlendEnable = true;
		DepthTestEnable = false;
		//CullMode = None;
		//FillMode = Wireframe;
		VertexProgram = compile latest Simple_vp();
		FragmentProgram = compile latest Simple_fp();
		
		BlendEquation = FuncAdd;
		BlendFunc = int2( SrcAlpha, OneMinusSrcAlpha );
	}
}

technique high_quality_cg21 {
	pass p0	{
		BlendEnable = true;
		DepthTestEnable = false;
		//CullMode = None;
		//FillMode = Wireframe;
		VertexProgram = compile arbvp1 Simple_vp();
		FragmentProgram = compile arbfp1 Simple_fp();
		
		BlendEquation = FuncAdd;
		BlendFunc = int2( SrcAlpha, OneMinusSrcAlpha );
	}
}