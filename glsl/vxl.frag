uniform sampler2D Palette, DiffuseTexture;
uniform vec2 PaletteRows;

uniform vec4 LightDirection;
uniform vec3 AmbientLight, DiffuseLight;

varying vec4 TexCoord;
varying vec4 ChannelMask;
varying vec4 NormalsMask;

void main()
{
	vec4 x = texture2D(DiffuseTexture, TexCoord.st);
	vec4 color = texture2D(Palette, vec2(dot(x, ChannelMask), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	vec4 normal = (2.0 * texture2D(Palette, vec2(dot(x, NormalsMask), PaletteRows.y)) - 1.0);
	vec3 intensity = AmbientLight + DiffuseLight * max(dot(normal, LightDirection), 0.0);
	gl_FragColor = vec4(intensity * color.rgb, color.a);
}
