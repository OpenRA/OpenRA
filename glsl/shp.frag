uniform sampler2D DiffuseTexture, Palette;

uniform bool EnableDepthPreview;

varying vec4 TexCoord;
varying vec4 ChannelMask;
varying vec4 DepthMask;

void main()
{
	vec4 x = texture2D(DiffuseTexture, TexCoord.st);
	vec2 p = vec2(dot(x, ChannelMask), TexCoord.p);
	vec4 c = texture2D(Palette, p);

	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	if (EnableDepthPreview && length(DepthMask) > 0.0)
	{
		float depth = dot(x, DepthMask);
		gl_FragColor = vec4(depth, depth, depth, 1);
	}
	else
		gl_FragColor = c;
}
