uniform mat4 View;
uniform mat4 TransformMatrix;

varying vec4 TexCoord;
varying vec4 ChannelMask;
varying vec4 NormalsMask;

vec4 DecodeMask(float x)
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	gl_Position = View*TransformMatrix*gl_Vertex;
	TexCoord = gl_MultiTexCoord0;
	ChannelMask = DecodeMask(gl_MultiTexCoord0.z);
	NormalsMask = DecodeMask(gl_MultiTexCoord0.w);
}
