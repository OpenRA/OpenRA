#version {VERSION}

in vec2 aVertexPosition;
in vec2 aVertexTexCoord;

out vec2 vTexCoord;

void main()
{
	gl_Position = vec4(aVertexPosition, 0.0, 1.0);
	vTexCoord = aVertexTexCoord;
}
