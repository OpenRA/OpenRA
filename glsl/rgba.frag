uniform sampler2D DiffuseTexture;
uniform bool EnableDepthPreview;

varying vec4 vTexCoord;

void main()
{
	vec4 c = texture2D(DiffuseTexture, vTexCoord.st);
	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	float depth = gl_FragCoord.z;

	// Convert to window coords
	gl_FragDepth = 0.5 * depth + 0.5;

	if (EnableDepthPreview)
	{
		// Front of the depth buffer is at 0, but we want to render it as bright
		gl_FragColor = vec4(vec3(1.0 - gl_FragDepth), 1.0);
	}
	else
		gl_FragColor = c;
}