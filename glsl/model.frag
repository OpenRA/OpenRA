#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D Palette, DiffuseTexture;
uniform vec2 PaletteRows;

uniform vec4 LightDirection;
uniform vec3 AmbientLight, DiffuseLight;

#if __VERSION__ == 120
varying vec4 vTexCoord;
varying vec4 vChannelMask;
varying vec4 vNormalsMask;
#else
in vec4 vTexCoord;
in vec4 vChannelMask;
in vec4 vNormalsMask;
out vec4 fragColor;
#endif

void main()
{
	#if __VERSION__ == 120
	vec4 x = texture2D(DiffuseTexture, vTexCoord.st);
	vec4 color = texture2D(Palette, vec2(dot(x, vChannelMask), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	vec4 y = texture2D(DiffuseTexture, vTexCoord.pq);
	vec4 normal = (2.0 * texture2D(Palette, vec2(dot(y, vNormalsMask), PaletteRows.y)) - 1.0);
	vec3 intensity = AmbientLight + DiffuseLight * max(dot(normal, LightDirection), 0.0);
	gl_FragColor = vec4(intensity * color.rgb, color.a);
	#else
	vec4 x = texture(DiffuseTexture, vTexCoord.st);
	vec4 color = texture(Palette, vec2(dot(x, vChannelMask), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	vec4 y = texture(DiffuseTexture, vTexCoord.pq);
	vec4 normal = (2.0 * texture(Palette, vec2(dot(y, vNormalsMask), PaletteRows.y)) - 1.0);
	vec3 intensity = AmbientLight + DiffuseLight * max(dot(normal, LightDirection), 0.0);
	fragColor = vec4(intensity * color.rgb, color.a);
	#endif
}
