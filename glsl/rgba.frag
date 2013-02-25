uniform sampler2D DiffuseTexture;
void main()
{
	gl_FragColor = texture2D(DiffuseTexture,gl_TexCoord[0].st);
}