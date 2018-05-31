uniform vec3 Scroll;
uniform vec3 r1, r2;

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
attribute vec2 aVertexTexMetadata;
varying vec4 vTexCoord;
varying vec2 vTexMetadata;
varying vec4 vChannelMask;
varying vec4 vDepthMask;
varying vec4 vColorFraction;

vec2 UnpackChannelAttributes(float x)
{
	// The channel attributes float encodes a set of attributes
	// stored as flags in the mantissa of the unnormalized float value.
	// Bits 3-5 define the behaviour of the secondary texture channel:
	//    000: Channel is not used
	//    001, 011, 101, 111: Sample depth sprite from channel R,G,B,A
	// Bits 0-2 define the behaviour of the primary texture channel:
	//    000: Channel is not used (aVertexTexCoord instead defines a color value)
	//    001, 011, 101, 111: Sample paletted sprite from channel R,G,B,A

	float secondaryChannel = 0.0;
	if (x >= 32.0) { x -= 32.0;  secondaryChannel += 4.0; }
	if (x >= 16.0) { x -= 16.0;  secondaryChannel += 2.0; }
	if (x >= 8.0) { x -= 8.0;  secondaryChannel += 1.0; }
	
	float primaryChannel = 0.0;
	if (x >= 4.0) { x -= 4.0;  primaryChannel += 4.0; }
	if (x >= 2.0) { x -= 2.0;  primaryChannel += 2.0; }
	if (x >= 1.0) { x -= 1.0;  primaryChannel += 1.0; }

	return vec2(primaryChannel, secondaryChannel);
}

vec4 SelectChannelMask(float x)
{
	if (x >= 7.0)
		return vec4(0,0,0,1);
	if (x >= 5.0)
		return vec4(0,0,1,0);
	if (x >= 3.0)
		return vec4(0,1,0,0);
	if (x >= 1.0)
		return vec4(1,0,0,0);

	return vec4(0, 0, 0, 0);
}

vec4 SelectColorFraction(float x)
{
	if (x > 0.0)
		return vec4(0, 0, 0, 0);
	else
		return vec4(1, 1, 1, 1);
}

void main()
{
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aVertexTexCoord;
	vTexMetadata = aVertexTexMetadata;
	
	vec2 attrib = UnpackChannelAttributes(aVertexTexMetadata.t);
	vChannelMask = SelectChannelMask(attrib.s);
	vColorFraction = SelectColorFraction(attrib.s);
	vDepthMask = SelectChannelMask(attrib.t);
} 
