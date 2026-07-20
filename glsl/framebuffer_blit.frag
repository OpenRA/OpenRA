#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D SourceTexture;

in vec2 vTexCoord;
out vec4 fragColor;

void main()
{
	fragColor = texture(SourceTexture, vTexCoord);
}
