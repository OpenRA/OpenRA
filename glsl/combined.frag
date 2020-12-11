#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

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

#if __VERSION__ == 120
varying vec4 vTexCoord;
varying vec2 vTexMetadata;
varying vec4 vChannelMask;
varying vec4 vDepthMask;
varying vec2 vTexSampler;

varying vec4 vColorFraction;
varying vec4 vRGBAFraction;
varying vec4 vPalettedFraction;
varying vec4 vTint;

uniform vec2 Texture0Size;
uniform vec2 Texture1Size;
uniform vec2 Texture2Size;
uniform vec2 Texture3Size;
uniform vec2 Texture4Size;
uniform vec2 Texture5Size;
uniform vec2 Texture6Size;
#else
in vec4 vColor;

in vec4 vTexCoord;
in vec2 vTexMetadata;
in vec4 vChannelMask;
in vec4 vDepthMask;
in vec2 vTexSampler;

in vec4 vColorFraction;
in vec4 vRGBAFraction;
in vec4 vPalettedFraction;
in vec4 vTint;

out vec4 fragColor;
#endif

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

#if __VERSION__ == 120
vec2 Size(float samplerIndex)
{
	if (samplerIndex < 0.5)
		return Texture0Size;
	else if (samplerIndex < 1.5)
		return Texture1Size;
	else if (samplerIndex < 2.5)
		return Texture2Size;
	else if (samplerIndex < 3.5)
		return Texture3Size;
	else if (samplerIndex < 4.5)
		return Texture4Size;
	else if (samplerIndex < 5.5)
		return Texture5Size;

	return Texture6Size;
}

vec4 Sample(float samplerIndex, vec2 pos)
{
	if (samplerIndex < 0.5)
		return texture2D(Texture0, pos);
	else if (samplerIndex < 1.5)
		return texture2D(Texture1, pos);
	else if (samplerIndex < 2.5)
		return texture2D(Texture2, pos);
	else if (samplerIndex < 3.5)
		return texture2D(Texture3, pos);
	else if (samplerIndex < 4.5)
		return texture2D(Texture4, pos);
	else if (samplerIndex < 5.5)
		return texture2D(Texture5, pos);

	return texture2D(Texture6, pos);
}
#else
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
#endif

vec4 SamplePalettedBilinear(float samplerIndex, vec2 coords, vec2 textureSize)
{
	vec2 texPos = (coords * textureSize) - vec2(0.5);
	vec2 interp = fract(texPos);
	vec2 tl = (floor(texPos) + vec2(0.5)) / textureSize;
	vec2 px = 1.0 / textureSize;

	vec4 x1 = Sample(samplerIndex, tl);
	vec4 x2 = Sample(samplerIndex, tl + vec2(px.x, 0.));
	vec4 x3 = Sample(samplerIndex, tl + vec2(0., px.y));
	vec4 x4 = Sample(samplerIndex, tl + px);

	#if __VERSION__ == 120
	vec4 c1 = texture2D(Palette, vec2(dot(x1, vChannelMask), vTexMetadata.s));
	vec4 c2 = texture2D(Palette, vec2(dot(x2, vChannelMask), vTexMetadata.s));
	vec4 c3 = texture2D(Palette, vec2(dot(x3, vChannelMask), vTexMetadata.s));
	vec4 c4 = texture2D(Palette, vec2(dot(x4, vChannelMask), vTexMetadata.s));
	#else
	vec4 c1 = texture(Palette, vec2(dot(x1, vChannelMask), vTexMetadata.s));
	vec4 c2 = texture(Palette, vec2(dot(x2, vChannelMask), vTexMetadata.s));
	vec4 c3 = texture(Palette, vec2(dot(x3, vChannelMask), vTexMetadata.s));
	vec4 c4 = texture(Palette, vec2(dot(x4, vChannelMask), vTexMetadata.s));
	#endif

	return mix(mix(c1, c2, interp.x), mix(c3, c4, interp.x), interp.y);
}

void main()
{
	vec2 coords = vTexCoord.st;

	vec4 c;
	if (AntialiasPixelsPerTexel > 0.0)
	{
		vec2 textureSize = vec2(Size(vTexSampler.s));
		vec2 offset = fract(coords.st * textureSize);

		// Offset the sampling point to simulate bilinear intepolation in window coordinates instead of texture coordinates
		// https://csantosbh.wordpress.com/2014/01/25/manual-texture-filtering-for-pixelated-games-in-webgl/
		// https://csantosbh.wordpress.com/2014/02/05/automatically-detecting-the-texture-filter-threshold-for-pixelated-magnifications/
		// ik is defined as 1/k from the articles, set to 1/0.7 because it looks good
		float ik = 1.43;
		vec2 interp = clamp(offset * ik * AntialiasPixelsPerTexel, 0.0, .5) + clamp((offset - 1.0) * ik * AntialiasPixelsPerTexel + .5, 0.0, .5);
		coords = (floor(coords.st * textureSize) + interp) / textureSize;

		if (vPalettedFraction.x > 0.0)
			c = SamplePalettedBilinear(vTexSampler.s, coords, textureSize);
	}

	if (!(AntialiasPixelsPerTexel > 0.0 && vPalettedFraction.x > 0.0))
	{
		vec4 x = Sample(vTexSampler.s, coords);
		vec2 p = vec2(dot(x, vChannelMask), vTexMetadata.s);
		#if __VERSION__ == 120
		c = vPalettedFraction * texture2D(Palette, p) + vRGBAFraction * x + vColorFraction * vTexCoord;
		#else
		c = vPalettedFraction * texture(Palette, p) + vRGBAFraction * x + vColorFraction * vTexCoord;
		#endif
	}

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
		#if __VERSION__ == 120
		gl_FragColor = vec4(r, g, b, 1.0);
		#else
		fragColor = vec4(r, g, b, 1.0);
		#endif
	}
	else
	{
		// A negative tint alpha indicates that the tint should replace the colour instead of multiplying it
		if (vTint.a < 0.0)
			c = vec4(vTint.rgb, -vTint.a);
		else
			c *= vTint;

		#if __VERSION__ == 120
		gl_FragColor = c;
		#else
		fragColor = c;
		#endif
	}
}
