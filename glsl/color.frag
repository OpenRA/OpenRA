varying vec4 vColor;
uniform bool EnableDepthPreview;

float jet_r(float x)
{
	return x < 0.7 ? 4.0 * x - 1.5 : -4.0 * x + 4.5;
}

float jet_g(float x)
{
	return x < 0.5 ? 4.0 * x - 0.5 : -4.0 * x + 3.5;
}

float jet_b(float x)
{
	return x < 0.3 ? 4.0 * x + 0.5 : -4.0 * x + 2.5;
}

void main()
{
	float depth = gl_FragCoord.z;

	// Convert to window coords
	gl_FragDepth = 0.5 * depth + 0.5;

	if (EnableDepthPreview)
	{
		float x = 1.0 - gl_FragDepth;
		float r = clamp(jet_r(x), 0.0, 1.0);
		float g = clamp(jet_g(x), 0.0, 1.0);
		float b = clamp(jet_b(x), 0.0, 1.0);
		gl_FragColor = vec4(r, g, b, 1.0);
	}
	else
		gl_FragColor = vColor;
}