#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D WorldTexture;
uniform float Scale;

in vec2 vTexCoord;
out vec4 fragColor;

void main()
{
    if (dot(vTexCoord, vTexCoord) >= 1.0)
	    discard;

	fragColor = texelFetch(WorldTexture, ivec2(gl_FragCoord.xy + Scale * vTexCoord), 0);
}
