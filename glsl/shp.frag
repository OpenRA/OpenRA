uniform sampler2D DiffuseTexture, Palette;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;

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
		if (abs(DepthTextureScale) > 0.0)
		{
			// Preview vertex aware depth
			float depth = gl_FragCoord.z + DepthTextureScale * dot(x, vDepthMask);

			// Convert to window coords
			depth = 0.5 * depth + 0.5;

			// Front of the depth buffer is at 0, but we want to render it as bright
			gl_FragColor = vec4(vec3(1.0 - depth), 1.0);
		}
		else
		{
			// Preview boring sprite-only depth
			float depth = dot(x, vDepthMask);
			gl_FragColor = vec4(depth, depth, depth, 1.0);
		}
	}
	else
		gl_FragColor = c;
}
