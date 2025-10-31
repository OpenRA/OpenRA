#version {VERSION}

in vec2 aVertexPosition;

void main()
{
	gl_Position = vec4(aVertexPosition, 0, 1);
}
