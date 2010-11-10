uniform sampler2D DiffuseTexture, Palette;

void main()
{
	//float4 x = tex2D(DiffuseTexture, f.Tex0.xy);
	//float2 p = float2( dot(x, f.ChannelMask), f.Tex0.z );
	//return tex2D(Palette, p);
	//st
	gl_FragColor = texture2D(Palette,gl_TexCoord[0].xy);
}