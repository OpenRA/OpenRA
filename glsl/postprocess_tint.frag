#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform vec3 Tint;
uniform sampler2D WorldTexture;

#if __VERSION__ == 120
uniform vec2 WorldTextureSize;
#else
out vec4 fragColor;
#endif

void main()
{
#if __VERSION__ == 120
	vec4 c = texture2D(WorldTexture, gl_FragCoord.xy / WorldTextureSize);
#else
	vec4 c = texture(WorldTexture, gl_FragCoord.xy / textureSize(WorldTexture, 0));
#endif

	c = vec4(min(c.r * Tint.r, 1.0), min(c.g * Tint.g, 1.0), min(c.b * Tint.b, 1.0), c.a);

	#if __VERSION__ == 120
	gl_FragColor = c;
	#else
	fragColor = c;
	#endif
}
