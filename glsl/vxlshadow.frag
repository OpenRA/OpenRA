uniform sampler2D Palette, DiffuseTexture;
uniform vec2 PaletteRows;

void main()
{
	vec4 x = texture2D(DiffuseTexture, gl_TexCoord[0].st);
	vec4 color = texture2D(Palette, vec2(dot(x, gl_TexCoord[1]), PaletteRows.x));
	if (color.a < 0.01)
		discard;

	gl_FragColor = color;
}
