uniform vec2 r1,r2;		// matrix elements

vec4 DecodeChannelMask( float x )
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	vec2 p = gl_Vertex.xy*r1 + r2;
	gl_Position = vec4(p.x,p.y,0,1);
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_TexCoord[1] = DecodeChannelMask(gl_MultiTexCoord0.w);
} 
