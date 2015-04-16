uniform sampler2D DiffuseTexture, Palette;

varying vec4 TexCoord;
varying vec4 ChannelMask;
varying vec4 DepthMask;

void main()
{
	vec4 x = texture2D(DiffuseTexture, TexCoord.st);
	vec2 p = vec2(dot(x, ChannelMask), TexCoord.p);
	gl_FragColor = texture2D(Palette, p);
}
