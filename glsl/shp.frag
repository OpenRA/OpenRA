uniform sampler2D DiffuseTexture, Palette;

uniform bool EnableDepthPreview;

varying vec4 vTexCoord;
varying vec4 vChannelMask;
varying vec4 vDepthMask;

void main()
{
	vec4 x = texture2D(DiffuseTexture, vTexCoord.st);
	vec2 p = vec2(dot(x, vChannelMask), vTexCoord.p);
	vec4 c = texture2D(Palette, p);

	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	if (EnableDepthPreview && length(vDepthMask) > 0.0)
	{
		float depth = dot(x, vDepthMask);
		gl_FragColor = vec4(depth, depth, depth, 1);
	}
	else
		gl_FragColor = c;
}
