#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D Palette, DiffuseTexture;
uniform vec2 Palettes;
uniform float PaletteRows;

uniform vec4 LightDirection;
uniform vec3 AmbientLight, DiffuseLight;

in vec4 vTexCoord;
in vec4 vChannelMask;
in vec4 vNormalsMask;
out vec4 fragColor;

void main()
{
	vec4 x = texture(DiffuseTexture, vTexCoord.st);
	vec4 color = texture(Palette, vec2(dot(x, vChannelMask), (Palettes.x + 0.5) / PaletteRows));
	if (color.a < 0.01)
		discard;

	vec4 y = texture(DiffuseTexture, vTexCoord.pq);
	vec4 normal = (2.0 * texture(Palette, vec2(dot(y, vNormalsMask), (Palettes.y + 0.5) / PaletteRows)) - 1.0);
	vec3 intensity = AmbientLight + DiffuseLight * max(dot(normal, LightDirection), 0.0);
	fragColor = vec4(intensity * color.rgb, color.a);
}
