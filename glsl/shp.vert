uniform vec2 Scroll;
uniform vec2 r1,r2;		// matrix elements

attribute vec4 aVertexPosition;
attribute vec4 aVertexTexCoord;
varying vec4 vTexCoord;
varying vec4 vChannelMask;
varying vec4 vDepthMask;

vec4 DecodeChannelMask(float x)
{
	float y = abs(x);
	if (y > 0.7)
		return vec4(0,0,0,1);
	if (y > 0.5)
		return vec4(0,0,1,0);
	if (y > 0.3)
		return vec4(0,1,0,0);
	else
		return vec4(1,0,0,0);
}

vec4 DecodeDepthChannelMask(float x)
{
	if (x > 0.0)
		return vec4(0,0,0,0);
	if (x < -0.7)
		return vec4(1,0,0,0);
	if (x < -0.5)
		return vec4(0,0,0,1);
	if (x < -0.3)
		return vec4(0,0,1,0);
	else
		return vec4(0,1,0,0);
}

void main()
{
	vec2 p = (aVertexPosition.xy - Scroll.xy) * r1 + r2;
	gl_Position = vec4(p.x,p.y,0,1);
	vTexCoord = aVertexTexCoord;
	vChannelMask = DecodeChannelMask(aVertexTexCoord.w);
	vDepthMask = DecodeDepthChannelMask(aVertexTexCoord.w);
} 
