uniform vec2 Scroll;
uniform vec2 r1, r2;

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
varying vec4 vColor;

void main()
{
	vec2 p = (aVertexPosition.xy - Scroll.xy)*r1 + r2;
	gl_Position = vec4(p.x,p.y,0,1);
	vColor = aVertexTexCoord;
} 
