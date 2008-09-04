return {
	exts = {"cg"},
	lexer = wxstc.wxSTC_LEX_CPP,
	apitype = "cg",
	linecomment = "//",
	lexerstyleconvert = {
		text		= {wxstc.wxSTC_C_IDENTIFIER,
						wxstc.wxSTC_C_VERBATIM,
						wxstc.wxSTC_C_REGEX,
						wxstc.wxSTC_C_REGEX,
						wxstc.wxSTC_C_GLOBALCLASS,},
	
		lexerdef	= {wxstc.wxSTC_C_DEFAULT,},
		comment 	= {wxstc.wxSTC_C_COMMENT, 
						wxstc.wxSTC_C_COMMENTLINE, 
						wxstc.wxSTC_C_COMMENTDOC,
						wxstc.wxSTC_C_COMMENTLINEDOC,
						wxstc.wxSTC_C_COMMENTDOCKEYWORD,
						wxstc.wxSTC_C_COMMENTDOCKEYWORDERROR,},
		stringtxt 	= {wxstc.wxSTC_C_STRING,
						wxstc.wxSTC_C_CHARACTER,
						wxstc.wxSTC_C_UUID,},
		stringeol 	= {wxstc.wxSTC_C_STRINGEOL,},
		preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
		operator 	= {wxstc.wxSTC_C_OPERATOR,},
		number 		= {wxstc.wxSTC_C_NUMBER,
						wxstc.wxSTC_C_WORD},
		
		
		keywords0	= {wxstc.wxSTC_C_WORD,},
		keywords1	= {wxstc.wxSTC_C_WORD2,},
		},
	
	keywords = {
		[[int half float float3 float4 float2 float3x3 float3x4 float4x3 float4x4 
		double vector vec matrix half half2 half3 half4 mat string 
		struct typedef matrix sampler sampler1D sampler2D sampler3D samplerRECT samplerCUBE texture 
		texture1D texture2D texture3D textureRECT textureCUBE decl do double else extern 
		false for if in inline inout out pass pixelshader return 
		shared static string technique true uniform vector vertexshader void volatile 
		while asm bool compile const auto break case catch char 
		class const_cast continue default delete dynamic_cast enum explicit friend goto 
		long mutable namespace new operator private protected public register reinterpret_case 
		short signed sizeof static_cast switch template this throw try typename 
		union unsigned using virtual ]],

		[[abs acos all any asin atan atan2 ceil clamp clip 
		cos cosh cross ddx ddy degrees determinant distance dot exp 
		exp2 faceforward floor fmod frac frc frexp fwidth isfinite isinf 
		isnan ldexp len length lerp lit log log10 log2 max 
		min modf mul noise normalize pow radians reflect refract found 
		rsqrt saturate sign sin sincos sinh smoothstep sqrt step tan 
		tanh tex1D tex1Dproj tex1Dbias tex2D tex2Dproj text2Dbias tex3D tex3Dproj tex3Dbias 
		texCUBE texCUBEproj texCUBEbias texRECT texRECTproj texRECTbias tex2Dlod tex3Dlod texRECTlod tex1Dlod 
		texCUBElod transpose POSITION PSIZE DIFFUSE SPECULAR TEXCOORD FOG COLOR COLOR0 
		COLOR1 COLOR2 COLOR3 TEXCOORD0 TEXCOORD1 TEXCOORD2 TEXCOORD3 TEXCOORD4 TEXCOORD5 TEXCOORD6 
		TEXCOORD7 TEXCOORD8 TEXCOORD9 TEXCOORD10 TEXCOORD11 TEXCOORD12 TEXCOORD13 TEXCOORD14 TEXCOORD15 NORMAL 
		ATTR0 ATTR1 ATTR2 ATTR3 ATTR4 ATTR5 ATTR6 ATTR7 ATTR8 ATTR9 
		ATTR10 ATTR11 ATTR12 ATTR13 ATTR14 ATTR15 TEXUNIT0 TEXUNIT1 TEXUNIT2 TEXUNIT3 
		TEXUNIT4 TEXUNIT5 TEXUNIT6 TEXUNIT7 TEXUNIT8 TEXUNIT9 TEXUNIT10 TEXUNIT11 TEXUNIT12 TEXUNIT13 
		TEXUNIT14 TEXUNIT15 x y z w xxxx xxxy xxxz xxxw 
		xxyx xxyy xxyz xxyw xxzx xxzy xxzz xxzw xxwx xxwy 
		xxwz xxww xyxx xyxy xyxz xyxw xyyx xyyy xyyz xyyw 
		xyzx xyzy xyzz xyzw xywx xywy xywz xyww xzxx xzxy 
		xzxz xzxw xzyx xzyy xzyz xzyw xzzx xzzy xzzz xzzw 
		xzwx xzwy xzwz xzww xwxx xwxy xwxz xwxw xwyx xwyy 
		xwyz xwyw xwzx xwzy xwzz xwzw xwwx xwwy xwwz xwww 
		yxxx yxxy yxxz yxxw yxyx yxyy yxyz yxyw yxzx yxzy 
		yxzz yxzw yxwx yxwy yxwz yxww yyxx yyxy yyxz yyxw 
		yyyx yyyy yyyz yyyw yyzx yyzy yyzz yyzw yywx yywy 
		yywz yyww yzxx yzxy yzxz yzxw yzyx yzyy yzyz yzyw 
		yzzx yzzy yzzz yzzw yzwx yzwy yzwz yzww ywxx ywxy 
		ywxz ywxw ywyx ywyy ywyz ywyw ywzx ywzy ywzz ywzw 
		ywwx ywwy ywwz ywww zxxx zxxy zxxz zxxw zxyx zxyy 
		zxyz zxyw zxzx zxzy zxzz zxzw zxwx zxwy zxwz zxww 
		zyxx zyxy zyxz zyxw zyyx zyyy zyyz zyyw zyzx zyzy 
		zyzz zyzw zywx zywy zywz zyww zzxx zzxy zzxz zzxw 
		zzyx zzyy zzyz zzyw zzzx zzzy zzzz zzzw zzwx zzwy 
		zzwz zzww zwxx zwxy zwxz zwxw zwyx zwyy zwyz zwyw 
		zwzx zwzy zwzz zwzw zwwx zwwy zwwz zwww wxxx wxxy 
		wxxz wxxw wxyx wxyy wxyz wxyw wxzx wxzy wxzz wxzw 
		wxwx wxwy wxwz wxww wyxx wyxy wyxz wyxw wyyx wyyy 
		wyyz wyyw wyzx wyzy wyzz wyzw wywx wywy wywz wyww 
		wzxx wzxy wzxz wzxw wzyx wzyy wzyz wzyw wzzx wzzy 
		wzzz wzzw wzwx wzwy wzwz wzww wwxx wwxy wwxz wwxw 
		wwyx wwyy wwyz wwyw wwzx wwzy wwzz wwzw wwwx wwwy 
		wwwz wwww xy xz yz xyz xw yw xyw zw 
		xzw yzw xyzw ]],

		},
	}
