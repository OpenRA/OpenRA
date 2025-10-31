#version {VERSION}

uniform vec2 Pos, Scroll;
uniform vec2 p1, p2;

in vec2 aVertexPosition;
in vec2 aVertexTexCoord;
out vec2 vTexCoord;

void main()
{
	gl_Position = vec4((aVertexPosition + Pos - Scroll) * p1 + p2, 0, 1);
	vTexCoord = aVertexTexCoord;
}
