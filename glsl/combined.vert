#version {VERSION}

uniform vec3 Scroll;
uniform vec3 r1, r2;

#if __VERSION__ == 120
attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
attribute vec2 aVertexTexMetadata;
attribute vec4 aVertexTint;

varying vec4 vTexCoord;
varying vec2 vTexMetadata;
varying vec4 vChannelMask;
varying vec4 vDepthMask;
varying vec2 vTexSampler;

varying vec4 vColorFraction;
varying vec4 vRGBAFraction;
varying vec4 vPalettedFraction;
varying vec4 vTint;
#else
in vec4 aVertexPosition;
in vec4 aVertexTexCoord;
in vec2 aVertexTexMetadata;
in vec4 aVertexTint;

out vec4 vTexCoord;
out vec2 vTexMetadata;
out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
out vec4 vTint;
#endif

vec4 UnpackChannelAttributes(float x)
{
	// The channel attributes float encodes a set of attributes
	// stored as flags in the mantissa of the unnormalized float value.
	// Bits 9-11 define the sampler index (0-7) that the secondary texture is bound to
	// Bits 6-8 define the sampler index (0-7) that the primary texture is bound to
	// Bits 3-5 define the behaviour of the secondary texture channel:
	//    000: Channel is not used
	//    001, 011, 101, 111: Sample depth sprite from channel R,G,B,A
	// Bits 0-2 define the behaviour of the primary texture channel:
	//    000: Channel is not used (aVertexTexCoord instead defines a color value)
	//    010: Sample RGBA sprite from all four channels
	//    001, 011, 101, 111: Sample paletted sprite from channel R,G,B,A

	float secondarySampler = 0.0;
	if (x >= 2048.0) { x -= 2048.0;  secondarySampler += 4.0; }
	if (x >= 1024.0) { x -= 1024.0;  secondarySampler += 2.0; }
	if (x >= 512.0) { x -= 512.0;  secondarySampler += 1.0; }

	float primarySampler = 0.0;
	if (x >= 256.0) { x -= 256.0;  primarySampler += 4.0; }
	if (x >= 128.0) { x -= 128.0;  primarySampler += 2.0; }
	if (x >= 64.0) { x -= 64.0;  primarySampler += 1.0; }

	float secondaryChannel = 0.0;
	if (x >= 32.0) { x -= 32.0;  secondaryChannel += 4.0; }
	if (x >= 16.0) { x -= 16.0;  secondaryChannel += 2.0; }
	if (x >= 8.0) { x -= 8.0;  secondaryChannel += 1.0; }

	float primaryChannel = 0.0;
	if (x >= 4.0) { x -= 4.0;  primaryChannel += 4.0; }
	if (x >= 2.0) { x -= 2.0;  primaryChannel += 2.0; }
	if (x >= 1.0) { x -= 1.0;  primaryChannel += 1.0; }

	return vec4(primaryChannel, secondaryChannel, primarySampler, secondarySampler);
}

vec4 SelectChannelMask(float x)
{
	if (x >= 7.0)
		return vec4(0,0,0,1);
	if (x >= 5.0)
		return vec4(0,0,1,0);
	if (x >= 3.0)
		return vec4(0,1,0,0);
	if (x >= 2.0)
		return vec4(1,1,1,1);
	if (x >= 1.0)
		return vec4(1,0,0,0);

	return vec4(0, 0, 0, 0);
}

vec4 SelectColorFraction(float x)
{
	if (x > 0.0)
		return vec4(0, 0, 0, 0);

	return vec4(1, 1, 1, 1);
}

vec4 SelectRGBAFraction(float x)
{
	if (x == 2.0)
		return vec4(1, 1, 1, 1);

	return vec4(0, 0, 0, 0);
}

vec4 SelectPalettedFraction(float x)
{
	if (x == 0.0 || x == 2.0)
		return vec4(0, 0, 0, 0);

	return vec4(1, 1, 1, 1);
}

void main()
{
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aVertexTexCoord;
	vTexMetadata = aVertexTexMetadata;

	vec4 attrib = UnpackChannelAttributes(aVertexTexMetadata.t);
	vChannelMask = SelectChannelMask(attrib.s);
	vColorFraction = SelectColorFraction(attrib.s);
	vRGBAFraction = SelectRGBAFraction(attrib.s);
	vPalettedFraction = SelectPalettedFraction(attrib.s);
	vDepthMask = SelectChannelMask(attrib.t);
	vTexSampler = attrib.pq;
	vTint = aVertexTint;
}
