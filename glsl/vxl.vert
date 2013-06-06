uniform mat4 View;
uniform mat4 TransformMatrix;

vec4 DecodeChannelMask(float x)
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	gl_Position = View*TransformMatrix*gl_Vertex;
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_TexCoord[1] = DecodeChannelMask(gl_MultiTexCoord0.z);
	gl_TexCoord[2] = DecodeChannelMask(gl_MultiTexCoord0.w);
}
