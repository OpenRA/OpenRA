#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform float Blend;
uniform sampler2D WorldTexture;
out vec4 fragColor;

void main()
{
	vec4 c = texture(WorldTexture, gl_FragCoord.xy / textureSize(WorldTexture, 0));
	float lum = 0.5 * (min(c.r, min(c.g, c.b)) + max(c.r, max(c.g, c.b)));
	fragColor = vec4(lum, lum, lum, c.a) * Blend + c * (1.0 - Blend);
}
