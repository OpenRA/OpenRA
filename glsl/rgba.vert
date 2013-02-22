uniform vec2 r1;
uniform vec2 r2;		// matrix elements
void main()
{
	vec2 p = gl_Vertex.xy*r1 + r2;
	gl_Position = vec4(p.x,p.y,0,1);
	gl_TexCoord[0]  = gl_MultiTexCoord0;
} 
