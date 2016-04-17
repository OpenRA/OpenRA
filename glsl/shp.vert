uniform vec3 Scroll;
uniform vec3 r1, r2;

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
attribute vec2 aVertexTexMetadata;
varying vec4 vTexCoord;
varying vec2 vTexMetadata;
varying vec4 vChannelMask;
varying vec4 vDepthMask;

vec4 DecodeChannelMask(float x)
{
	if (x > 0.7)
		return vec4(0,0,0,1);
	if (x > 0.5)
		return vec4(0,0,1,0);
	if (x > 0.3)
		return vec4(0,1,0,0);
	else
		return vec4(1,0,0,0);
}

void main()
{
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aVertexTexCoord;
	vTexMetadata = aVertexTexMetadata;
	vChannelMask = DecodeChannelMask(abs(aVertexTexMetadata.t));
	if (aVertexTexMetadata.t < 0.0)
	{
		float x = -aVertexTexMetadata.t * 10.0;
		vDepthMask = DecodeChannelMask(x - floor(x));
	}
	else
		vDepthMask = vec4(0,0,0,0);
} 
