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
uniform vec2 Texture7Size;
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
	// Standard gamma conversion equation: see e.g. http://entropymine.com/imageworsener/srgbformula/
	return c <= 0.04045f ? c / 12.92f : pow((c + 0.055f) / 1.055f, 2.4f);
}

vec4 srgb2linear(vec4 c)
{
	// The SRGB color has pre-multiplied alpha which we must undo before removing the the gamma correction
	return c.a * vec4(srgb2linear(c.r / c.a), srgb2linear(c.g / c.a), srgb2linear(c.b / c.a), 1.0f);
}

float linear2srgb(float c)
{
	// Standard gamma conversion equation: see e.g. http://entropymine.com/imageworsener/srgbformula/
	return c <= 0.0031308 ? c * 12.92f : 1.055f * pow(c, 1.0f / 2.4f) - 0.055f;
}

vec4 linear2srgb(vec4 c)
{
	// The linear color has pre-multiplied alpha which we must undo before applying the the gamma correction
	return c.a * vec4(linear2srgb(c.r / c.a), linear2srgb(c.g / c.a), linear2srgb(c.b / c.a), 1.0f);
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
	else if (samplerIndex < 6.5)
		return Texture6Size;

	return Texture7Size;
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
	else if (samplerIndex < 6.5)
		return texture2D(Texture6, pos);

	return texture2D(Texture7, pos);
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
	else if (samplerIndex < 6.5)
		return textureSize(Texture6, 0);

	return textureSize(Texture7, 0);
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
	else if (samplerIndex < 6.5)
		return texture(Texture6, pos);

	return texture(Texture7, pos);
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

vec4 ColorShift(vec4 c, float p)
{
	#if __VERSION__ == 120
	vec4 range = texture2D(ColorShifts, vec2(0.25, p));
 	vec4 shift = texture2D(ColorShifts, vec2(0.75, p));
	#else
	vec4 range = texture(ColorShifts, vec2(0.25, p));
 	vec4 shift = texture(ColorShifts, vec2(0.75, p));
	#endif

	vec3 hsv = rgb2hsv(srgb2linear(c).rgb);
	if (hsv.r > range.r && range.g >= hsv.r)
		c = linear2srgb(vec4(hsv2rgb(vec3(hsv.r + shift.r, clamp(hsv.g + shift.g, 0.0, 1.0), hsv.b * clamp(shift.b, 0.0, 1.0))), c.a));

	return c;
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

	if (vRGBAFraction.r > 0.0 && vTexMetadata.s > 0.0)
		c = ColorShift(c, vTexMetadata.s);

	float depth = gl_FragCoord.z;
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vTexSampler.t, vTexCoord.pq);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}

	gl_FragDepth = depth;

	if (EnableDepthPreview)
	{
		float intensity = 1.0 - clamp(DepthPreviewParams.x * depth - 0.5 * DepthPreviewParams.x - DepthPreviewParams.y + 0.5, 0.0, 1.0);

		#if __VERSION__ == 120
		gl_FragColor = vec4(vec3(intensity), 1.0);
		#else
		fragColor = vec4(vec3(intensity), 1.0);
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
