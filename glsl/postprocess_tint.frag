#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform vec3 Tint;
uniform sampler2D WorldTexture;
out vec4 fragColor;

void main()
{
	vec4 c = texture(WorldTexture, gl_FragCoord.xy / textureSize(WorldTexture, 0));
	fragColor = vec4(min(c.r * Tint.r, 1.0), min(c.g * Tint.g, 1.0), min(c.b * Tint.b, 1.0), c.a);
}
