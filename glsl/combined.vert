#version {VERSION}

uniform vec3 Scroll;
uniform vec3 p1, p2;
uniform float PaletteRows;

in vec3 aVertexPosition;
in vec4 aVertexTexCoord;
in uint aVertexAttributes;
in vec4 aVertexTint;

out vec4 vTexCoord;
flat out float vTexPalette;
flat out vec4 vChannelMask;
flat out uint vChannelSampler;
flat out uint vChannelType;
flat out vec4 vDepthMask;
flat out uint vDepthSampler;
out vec4 vTint;
	
vec4 SelectChannelMask(uint x)
{
	switch (x)
	{
		case 7u:
			return vec4(0.0, 0.0, 0.0, 1.0);
		case 5u:
			return vec4(0.0, 0.0, 1.0, 0.0);
		case 3u:
			return vec4(0, 1.0, 0.0, 0.0);
		case 2u:
			return vec4(1.0, 1.0, 1.0, 1.0);
		case 1u:
			return vec4(1.0, 0.0, 0.0, 0.0);
		default:
			return vec4(0.0, 0.0, 0.0, 0.0);
	}
}

void main()
{
	gl_Position = vec4((aVertexPosition - Scroll) * p1 + p2, 1);
	vTexCoord = aVertexTexCoord;

	// aVertexAttributes is a packed bitfield, where:
	// Bits 0-2 define the behaviour of the primary texture channel:
	//    000: Channel is not used (aVertexTexCoord instead defines a color value)
	//    010: Sample RGBA sprite from all four channels
	//    001, 011, 101, 111: Sample paletted sprite from channel R,G,B,A
	// Bits 3-5 define the behaviour of the secondary texture channel:
	//    000: Channel is not used
	//    001, 011, 101, 111: Sample depth sprite from channel R,G,B,A
	// Bits 6-8 define the sampler index (0-7) that the primary texture is bound to
	// Bits 9-11 define the sampler index (0-7) that the secondary texture is bound to
	// Bits 16-31 define the palette row for paletted sprites
	vChannelType = aVertexAttributes & 0x07u;
	vChannelMask = SelectChannelMask(vChannelType);
	vDepthMask = SelectChannelMask((aVertexAttributes >> 3) & 0x07u);
	vChannelSampler = (aVertexAttributes >> 6) & 0x07u;
	vDepthSampler = (aVertexAttributes >> 9) & 0x07u;
	vTexPalette = float(aVertexAttributes >> 16) / PaletteRows;

	vTint = aVertexTint;
}
