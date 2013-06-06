uniform mat4 View;
uniform mat4 TransformMatrix;
uniform vec4 LightDirection;
uniform float GroundZ;
uniform vec3 GroundNormal;

vec4 DecodeChannelMask(float x)
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	// Distance between vertex and ground
	float d = dot(gl_Vertex.xyz - vec3(0.0,0.0,GroundZ), GroundNormal) / dot(LightDirection.xyz, GroundNormal);

	// Project onto ground plane
	vec3 shadow = gl_Vertex.xyz - d*LightDirection.xyz;
	gl_Position = View*TransformMatrix*vec4(shadow, 1);
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_TexCoord[1] = DecodeChannelMask(gl_MultiTexCoord0.z);
	gl_TexCoord[2] = DecodeChannelMask(gl_MultiTexCoord0.w);
}
