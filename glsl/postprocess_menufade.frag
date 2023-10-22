#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform float From;
uniform float To;
uniform float Blend;
uniform sampler2D WorldTexture;
out vec4 fragColor;

vec4 ColorForEffect(float effect, vec4 c)
{
	if (effect > 1.5)
	{
		float lum = 0.5 * (min(c.r, min(c.g, c.b)) + max(c.r, max(c.g, c.b)));
		return vec4(lum, lum, lum, c.a);
	}

	if (effect > 0.5)
	{
		return vec4(0, 0, 0, c.a);
	}

	return c;
}

void main()
{
	vec4 c = texture(WorldTexture, gl_FragCoord.xy / vec2(textureSize(WorldTexture, 0)));
	fragColor = ColorForEffect(From, c) * Blend + ColorForEffect(To, c) * (1.0 - Blend);
}
