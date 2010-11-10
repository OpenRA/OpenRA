uniform vec2 r1;
uniform vec2 r2;		// matrix elements

void main()
{
	vec2 p = gl_Vertex.xy*vec2(0.001538462,-0.0025) + vec2(-1,1);
	gl_Position = vec4(p.x,p.y,0,1);
} 
