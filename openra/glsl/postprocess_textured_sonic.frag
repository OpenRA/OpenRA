#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D SourceTexture;
uniform float Scale;

in vec2 vTexCoord;
out vec4 fragColor;

void main()
{
    if (dot(vTexCoord, vTexCoord) >= 1.0)
	    discard;

	fragColor = texelFetch(SourceTexture, ivec2(gl_FragCoord.xy + Scale * vTexCoord), 0);
}
