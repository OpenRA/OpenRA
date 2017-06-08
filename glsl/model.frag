uniform sampler2D Palette, DiffuseTexture;
uniform vec2 PaletteRows;

uniform vec4 LightDirection;
uniform vec3 AmbientLight, DiffuseLight;

varying vec4 vTexCoord;
varying vec4 vChannelMask;
varying vec4 vNormalsMask;

void main()
{
	vec4 x = texture2D(DiffuseTexture, vTexCoord.st);
	vec4 color = texture2D(Palette, vec2(dot(x, vChannelMask), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	vec4 y = texture2D(DiffuseTexture, vTexCoord.pq);
	vec4 normal = (2.0 * texture2D(Palette, vec2(dot(y, vNormalsMask), PaletteRows.y)) - 1.0);
	vec3 intensity = AmbientLight + DiffuseLight * max(dot(normal, LightDirection), 0.0);
	gl_FragColor = vec4(intensity * color.rgb, color.a);
}
