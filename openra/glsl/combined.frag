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
uniform sampler2D Texture7;
uniform sampler2D Palette;
uniform sampler2D ColorShifts;

uniform bool EnableDepthPreview;
uniform vec2 DepthPreviewParams;
uniform float DepthTextureScale;
uniform bool EnablePixelArtScaling;

in vec4 vTexCoord;
flat in float vTexPalette;
flat in vec4 vChannelMask;
flat in uint vChannelSampler;
flat in uint vChannelType;
flat in vec4 vDepthMask;
flat in uint vDepthSampler;
in vec4 vTint;

out vec4 fragColor;

vec3 rgb2hsv(vec3 c)
{
	// From http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
	vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
	// From http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float srgb2linear(float c)
{
	// Standard gamma conversion equation: see e.g. https://entropymine.com/imageworsener/srgbformula/
	return c <= 0.04045f ? c / 12.92f : pow((c + 0.055f) / 1.055f, 2.4f);
}

vec4 srgb2linear(vec4 c)
{
	// The SRGB color has pre-multiplied alpha which we must undo before removing the the gamma correction
	return c.a * vec4(srgb2linear(c.r / c.a), srgb2linear(c.g / c.a), srgb2linear(c.b / c.a), 1.0f);
}

float linear2srgb(float c)
{
	// Standard gamma conversion equation: see e.g. https://entropymine.com/imageworsener/srgbformula/
	return c <= 0.0031308 ? c * 12.92f : 1.055f * pow(c, 1.0f / 2.4f) - 0.055f;
}

vec4 linear2srgb(vec4 c)
{
	// The linear color has pre-multiplied alpha which we must undo before applying the the gamma correction
	return c.a * vec4(linear2srgb(c.r / c.a), linear2srgb(c.g / c.a), linear2srgb(c.b / c.a), 1.0f);
}

vec2 Size(uint samplerIndex)
{
	switch (samplerIndex)
	{
		case 7u:
			return vec2(textureSize(Texture7, 0));
		case 6u:
			return vec2(textureSize(Texture6, 0));
		case 5u:
			return vec2(textureSize(Texture5, 0));
		case 4u:
			return vec2(textureSize(Texture4, 0));
		case 3u:
			return vec2(textureSize(Texture3, 0));
		case 2u:
			return vec2(textureSize(Texture2, 0));
		case 1u:
			return vec2(textureSize(Texture1, 0));
		default:
			return vec2(textureSize(Texture0, 0));
	}
}

vec4 Sample(uint samplerIndex, vec2 pos)
{
	switch (samplerIndex)
	{
		case 7u:
			return texture(Texture7, pos);
		case 6u:
			return texture(Texture6, pos);
		case 5u:
			return texture(Texture5, pos);
		case 4u:
			return texture(Texture4, pos);
		case 3u:
			return texture(Texture3, pos);
		case 2u:
			return texture(Texture2, pos);
		case 1u:
			return texture(Texture1, pos);
		default:
			return texture(Texture0, pos);
	}
}

vec4 SamplePalettedBilinear(uint samplerIndex, vec2 coords, vec2 textureSize)
{
	vec2 texPos = (coords * textureSize) - vec2(0.5);
	vec2 interp = fract(texPos);
	vec2 tl = (floor(texPos) + vec2(0.5)) / textureSize;
	vec2 px = 1.0 / textureSize;

	vec4 x1 = Sample(samplerIndex, tl);
	vec4 x2 = Sample(samplerIndex, tl + vec2(px.x, 0.));
	vec4 x3 = Sample(samplerIndex, tl + vec2(0., px.y));
	vec4 x4 = Sample(samplerIndex, tl + px);

	vec4 c1 = texture(Palette, vec2(dot(x1, vChannelMask), vTexPalette));
	vec4 c2 = texture(Palette, vec2(dot(x2, vChannelMask), vTexPalette));
	vec4 c3 = texture(Palette, vec2(dot(x3, vChannelMask), vTexPalette));
	vec4 c4 = texture(Palette, vec2(dot(x4, vChannelMask), vTexPalette));

	return mix(mix(c1, c2, interp.x), mix(c3, c4, interp.x), interp.y);
}

vec4 ColorShift(vec4 c, float p)
{
	vec4 range = texture(ColorShifts, vec2(0.25, p));
 	vec4 shift = texture(ColorShifts, vec2(0.75, p));

	vec3 hsv = rgb2hsv(srgb2linear(c).rgb);
	if (hsv.r > range.r && range.g >= hsv.r)
		c = linear2srgb(vec4(hsv2rgb(vec3(hsv.r + shift.r, clamp(hsv.g + shift.g, 0.0, 1.0), hsv.b * clamp(shift.b, 0.0, 1.0))), c.a));

	return c;
}

void main()
{
	vec2 coords = vTexCoord.st;
	bool isPaletted = (vChannelType & 0x01u) != 0u;
	bool isColor = vChannelType == 0u;

	vec4 c;
	if (EnablePixelArtScaling)
	{
		vec2 textureSize = Size(vChannelSampler);
		vec2 vUv = coords.st * textureSize;
		vec2 offset = fract(vUv);
		vec2 pixelsPerTexel = vec2(1.0 / dFdx(vUv.x), 1.0 / dFdy(vUv.y));

		// Offset the sampling point to simulate bilinear intepolation in window coordinates instead of texture coordinates
		// https://csantosbh.wordpress.com/2014/01/25/manual-texture-filtering-for-pixelated-games-in-webgl/
		// https://csantosbh.wordpress.com/2014/02/05/automatically-detecting-the-texture-filter-threshold-for-pixelated-magnifications/
		// ik is defined as 1/k from the articles, set to 1/0.7 because it looks good
		float ik = 1.43;
		vec2 interp = clamp(offset * ik * pixelsPerTexel, 0.0, .5) + clamp((offset - 1.0) * ik * pixelsPerTexel + .5, 0.0, .5);
		coords = (floor(coords.st * textureSize) + interp) / textureSize;

		if (isPaletted)
			c = SamplePalettedBilinear(vChannelSampler, coords, textureSize);
	}

	if (!(EnablePixelArtScaling && isPaletted))
	{
		vec4 x = Sample(vChannelSampler, coords);
		vec2 p = vec2(dot(x, vChannelMask), vTexPalette);
		if (isPaletted)
			c = texture(Palette, p);
		else if (isColor)
			c = vTexCoord;
		else
			c = x;
	}

	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	if (!isPaletted && vTexPalette > 0.0)
		c = ColorShift(c, vTexPalette);

	float depth = gl_FragCoord.z;
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vDepthSampler, vTexCoord.pq);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}

	gl_FragDepth = depth;

	if (EnableDepthPreview)
	{
		float intensity = 1.0 - clamp(DepthPreviewParams.x * depth - 0.5 * DepthPreviewParams.x - DepthPreviewParams.y + 0.5, 0.0, 1.0);
		fragColor = vec4(vec3(intensity), 1.0);
	}
	else
	{
		// A negative tint alpha indicates that the tint should replace the colour instead of multiplying it
		if (vTint.a < 0.0)
			c = vec4(vTint.rgb, -vTint.a);
		else
			c *= vTint;

		fragColor = c;
	}
}
