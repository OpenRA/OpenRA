uniform sampler2D DiffuseTexture, Palette;

void main()
{
	vec4 x = texture2D(DiffuseTexture, gl_TexCoord[0].st);
	vec2 p = vec2( dot(x, gl_TexCoord[1]), gl_TexCoord[0].p );
	gl_FragColor = texture2D(Palette,p);
}