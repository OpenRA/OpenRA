#version {VERSION}

#if __VERSION__ == 120
attribute vec2 aVertexPosition;
#else
in vec2 aVertexPosition;
#endif

void main()
{
	gl_Position = vec4(aVertexPosition, 0, 1);
}
