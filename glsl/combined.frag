#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

in vec4 vColor;

uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform sampler2D Texture2;
uniform sampler2D Texture3;
uniform sampler2D Texture4;
uniform sampler2D Texture5;
uniform sampler2D Texture6;
uniform sampler2D Palette;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;

in vec4 vTexCoord;
in vec2 vTexMetadata;
in vec4 vChannelMask;
in vec4 vDepthMask;
in vec2 vTexSampler;

in vec4 vColorFraction;
in vec4 vRGBAFraction;
in vec4 vPalettedFraction;

out vec4 fragColor;

float jet_r(float x)
{
	return x < 0.7 ? 4.0 * x - 1.5 : -4.0 * x + 4.5;
}

float jet_g(float x)
{
	return x < 0.5 ? 4.0 * x - 0.5 : -4.0 * x + 3.5;
}

float jet_b(float x)
{
	return x < 0.3 ? 4.0 * x + 0.5 : -4.0 * x + 2.5;
}

vec4 Sample(float samplerIndex, vec2 pos)
{
	if (samplerIndex < 0.5)
		return texture(Texture0, pos);
	else if (samplerIndex < 1.5)
		return texture(Texture1, pos);
	else if (samplerIndex < 2.5)
		return texture(Texture2, pos);
	else if (samplerIndex < 3.5)
		return texture(Texture3, pos);
	else if (samplerIndex < 4.5)
		return texture(Texture4, pos);
	else if (samplerIndex < 5.5)
		return texture(Texture5, pos);

	return texture(Texture6, pos);
}

void main()
{
	vec4 x = Sample(vTexSampler.s, vTexCoord.st);
	vec2 p = vec2(dot(x, vChannelMask), vTexMetadata.s);
	vec4 c = vPalettedFraction * texture(Palette, p) + vRGBAFraction * x + vColorFraction * vTexCoord;

	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	float depth = gl_FragCoord.z;
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vTexSampler.t, vTexCoord.pq);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}

	// Convert to window coords
	gl_FragDepth = 0.5 * depth + 0.5;

	if (EnableDepthPreview)
	{
		float x = 1.0 - gl_FragDepth;
		float r = clamp(jet_r(x), 0.0, 1.0);
		float g = clamp(jet_g(x), 0.0, 1.0);
		float b = clamp(jet_b(x), 0.0, 1.0);
		fragColor = vec4(r, g, b, 1.0);
	}
	else
		fragColor = c;
}
