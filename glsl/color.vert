uniform vec3 Scroll;
uniform vec3 r1, r2;

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
varying vec4 vColor;

void main()
{
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);  
	vColor = aVertexTexCoord;
} 
