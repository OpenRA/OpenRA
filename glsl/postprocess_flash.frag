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
	vec4 c = texture(WorldTexture, gl_FragCoord.xy / textureSize(WorldTexture, 0));
	fragColor = vec4(Color, c.a) * Blend + c * (1.0 - Blend);
}
