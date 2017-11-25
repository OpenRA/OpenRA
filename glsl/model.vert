uniform mat4 View;
uniform mat4 TransformMatrix;

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
attribute vec2 aVertexTexMetadata;
varying vec4 vTexCoord;
varying vec4 vChannelMask;
varying vec4 vNormalsMask;

vec4 DecodeMask(float x)
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	gl_Position = View*TransformMatrix*aVertexPosition;
	vTexCoord = aVertexTexCoord;
	vChannelMask = DecodeMask(aVertexTexMetadata.s);
	vNormalsMask = DecodeMask(aVertexTexMetadata.t);
}
