uniform sampler2D DiffuseTexture;
varying vec4 vTexCoord;

void main()
{
	gl_FragColor = texture2D(DiffuseTexture, vTexCoord.st);
}