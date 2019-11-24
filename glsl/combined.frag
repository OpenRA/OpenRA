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
uniform float AntialiasPixelsPerTexel;

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

ivec2 Size(float samplerIndex)
{
	if (samplerIndex < 0.5)
		return textureSize(Texture0, 0);
	else if (samplerIndex < 1.5)
		return textureSize(Texture1, 0);
	else if (samplerIndex < 2.5)
		return textureSize(Texture2, 0);
	else if (samplerIndex < 3.5)
		return textureSize(Texture3, 0);
	else if (samplerIndex < 4.5)
		return textureSize(Texture4, 0);
	else if (samplerIndex < 5.5)
		return textureSize(Texture5, 0);

	return textureSize(Texture6, 0);
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
	vec2 coords = vTexCoord.st;

	if (AntialiasPixelsPerTexel > 0)
	{
		vec2 textureSize = Size(vTexSampler.s);
		vec2 offset = fract(coords.st * textureSize);

		// Offset the sampling point to simulate bilinear intepolation in window coordinates instead of texture coordinates
		// https://csantosbh.wordpress.com/2014/01/25/manual-texture-filtering-for-pixelated-games-in-webgl/
		// https://csantosbh.wordpress.com/2014/02/05/automatically-detecting-the-texture-filter-threshold-for-pixelated-magnifications/
		// ik is defined as 1/k from the articles, set to 1/0.7 because it looks good 
		float ik = 1.43;
		vec2 interp = clamp(offset * ik * AntialiasPixelsPerTexel, 0.0, .5) + clamp((offset - 1.0) * ik * AntialiasPixelsPerTexel + .5, 0.0, .5);
		coords = (floor(coords.st * textureSize) + interp) / textureSize;
	}

	vec4 x = Sample(vTexSampler.s, coords);
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
