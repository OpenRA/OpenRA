#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D VortexTexture;
uniform sampler2D WorldTexture;

in vec2 vTexCoord;
out vec4 fragColor;

void main()
{
	vec4 vtx = texture(VortexTexture, vTexCoord.xy);

	vec2 delta = (vtx.bg - 0.5) * 256.0;
	float frac = 16.0 * vtx.r + 0.0625;
	if (vtx.r > 0.055)
		discard;

	fragColor = texelFetch(WorldTexture, ivec2(gl_FragCoord.xy + delta), 0) * vec4(frac, frac, frac, 1);
}
