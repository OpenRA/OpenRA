#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform float Blend;
uniform vec3 Color;
uniform sampler2D WorldTexture;
out vec4 fragColor;

void main()
{
	vec4 c = texelFetch(WorldTexture, ivec2(gl_FragCoord.xy), 0);
	fragColor = mix(c, vec4(Color, c.a), Blend);
}
