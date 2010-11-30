return {
	exts = {"cg","cgh","fx","fxh","cgfx","cgfxh",},
	lexer = wxstc.wxSTC_LEX_CPP,
	apitype = "cg",
	linecomment = "//",
	
	isfndef = function(str)
					local l
					local s,e,cap = string.find(str,"^%s*([A-Za-z0-9_]+%s+[A-Za-z0-9_]+%s*%(.+%))")
					if (not s) then
						s,e,cap = string.find(str,"^%s*([A-Za-z0-9_]+%s+[A-Za-z0-9_]+)%s*%(")
					end
					if (cap and (string.find(cap,"^return") or string.find(cap,"else"))) then return end
					return s,e,cap,l
				end,
	
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
		double vector vec matrix half half2 half3 half4 int2 int3 
		int4 bool bool2 bool3 bool4 mat string struct typedef matrix 
		usampler usampler1D usampler2D usampler3D usamplerRECT usamplerCUBE isampler1DARRAY usampler2DARRAY usamplerCUBEARRAY isampler 
		isampler1D isampler2D isampler3D isamplerRECT isamplerCUBE isampler1DARRAY isampler2DARRAY isamplerCUBEARRAY sampler sampler1D 
		sampler2D sampler3D samplerRECT samplerCUBE sampler1DARRAY sampler2DARRAY samplerCUBEARRAY texture texture1D texture2D 
		texture3D textureRECT textureCUBE texture1DARRAY texture2DARRAY textureCUBEARRAY decl do double else 
		usamplerBUF isamplerBUF samplerBUF
		extern false for if in inline inout out pass pixelshader 
		return shared static string technique true uniform vector vertexshader void 
		volatile while asm bool compile const auto break case catch 
		char class const_cast continue default delete dynamic_cast enum explicit friend 
		goto long mutable namespace new operator private protected public register 
		reinterpret_case short signed sizeof static_cast switch template this throw try 
		typename union unsigned using virtual ]],

		[[abs acos all any asin atan atan2 ceil clamp clip 
		cos cosh cross ddx ddy degrees determinant distance dot exp 
		exp2 faceforward floatToIntBits floatToRawIntBits floor fmod frac frexp fwidth intBitsToFloat 
		isfinite isinf isnan ldexp length lerp lit log log10 log2 
		max min mul normalize pow radians reflect refract round rsqrt 
		saturate sign sin sincos sinh sqrt step tan tanh tex1D 
		tex1DARRAY tex1DARRAYbias tex1DARRAYcmpbias tex1DARRAYcmplod tex1DARRAYfetch tex1DARRAYlod tex1DARRAYproj tex1DARRAYsize tex1Dbias tex1Dcmpbias 
		tex1Dcmplod tex1Dfetch tex1Dlod tex1Dproj tex1Dsize tex2D tex2DARRAY tex2DARRAYbias tex2DARRAYfetch tex2DARRAYlod 
		tex2DARRAYproj tex2DARRAYsize tex2Dbias tex2Dcmpbias tex2Dcmplod tex2Dfetch tex2Dlod tex2Dproj tex2Dsize tex3D 
		tex3Dbias tex3Dfetch tex3Dlod tex3Dproj tex3Dsize texBUF texBUFsize texCUBE texCUBEARRAY texCUBEARRAYsize 
		texCUBEbias texCUBElod texCUBEproj texCUBEsize texRECT texRECTbias texRECTfetch texRECTlod texRECTproj texRECTsize 
		texBUF texBUFsize
		unpack_4ubyte pack_4ubyte unpack_4byte pack_4byte unpack_2ushort pack_2ushort
		unpack_2half pack_2half
		
		transpose trunc POSITION PSIZE DIFFUSE SPECULAR TEXCOORD FOG FOGP COLOR WPOS
		COLOR0 COLOR1 COLOR2 COLOR3 TEXCOORD0 TEXCOORD1 TEXCOORD2 TEXCOORD3 TEXCOORD4 TEXCOORD5 
		TEXCOORD6 TEXCOORD7 TEXCOORD8 TEXCOORD9 TEXCOORD10 TEXCOORD11 TEXCOORD12 TEXCOORD13 TEXCOORD14 TEXCOORD15 
		NORMAL FACE PRIMITIVEID DEPTH ATTR0 ATTR1 ATTR2 ATTR3 ATTR4 ATTR5 
		ATTR6 ATTR7 ATTR8 ATTR9 ATTR10 ATTR11 ATTR12 ATTR13 ATTR14 ATTR15 
		TEXUNIT0 TEXUNIT1 TEXUNIT2 TEXUNIT3 TEXUNIT4 TEXUNIT5 TEXUNIT6 TEXUNIT7 TEXUNIT8 TEXUNIT9 
		TEXUNIT10 TEXUNIT11 TEXUNIT12 TEXUNIT13 TEXUNIT14 TEXUNIT15 
		
		PROJ PROJECTION PROJECTIONMATRIX PROJMATRIX
		PROJMATRIXINV PROJINV PROJECTIONINV PROJINVERSE PROJECTIONINVERSE PROJINVMATRIX PROJECTIONINVMATRIX PROJINVERSEMATRIX PROJECTIONINVERSEMATRIX
		VIEW VIEWMATRIX VIEWMATRIXINV VIEWINV VIEWINVERSE VIEWINVERSEMATRIX VIEWINVMATRIX
		VIEWPROJECTION VIEWPROJ VIEWPROJMATRIX VIEWPROJECTIONMATRIX
		WORLD WORLDMATRIX WORLDVIEW WORLDVIEWMATRIX
		WORLDVIEWPROJ WORLDVIEWPROJECTION WORLDVIEWPROJMATRIX WORLDVIEWPROJECTIONMATRIX
		VIEWPORTSIZE VIEWPORTDIMENSION
		VIEWPORTSIZEINV VIEWPORTSIZEINVERSE VIEWPORTDIMENSIONINV VIEWPORTDIMENSIONINVERSE INVERSEVIEWPORTDIMENSIONS
		FOGCOLOR FOGDISTANCE CAMERAWORLDPOS CAMERAWORLDDIR
		
		CENTROID FLAT NOPERSPECTIVE FACE PRIMITIVEID VERTEXID
		
		x y z w 
		xxxx xxxy xxxz xxxw xxyx xxyy xxyz xxyw xxzx xxzy 
		xxzz xxzw xxwx xxwy xxwz xxww xyxx xyxy xyxz xyxw 
		xyyx xyyy xyyz xyyw xyzx xyzy xyzz xyzw xywx xywy 
		xywz xyww xzxx xzxy xzxz xzxw xzyx xzyy xzyz xzyw 
		xzzx xzzy xzzz xzzw xzwx xzwy xzwz xzww xwxx xwxy 
		xwxz xwxw xwyx xwyy xwyz xwyw xwzx xwzy xwzz xwzw 
		xwwx xwwy xwwz xwww yxxx yxxy yxxz yxxw yxyx yxyy 
		yxyz yxyw yxzx yxzy yxzz yxzw yxwx yxwy yxwz yxww 
		yyxx yyxy yyxz yyxw yyyx yyyy yyyz yyyw yyzx yyzy 
		yyzz yyzw yywx yywy yywz yyww yzxx yzxy yzxz yzxw 
		yzyx yzyy yzyz yzyw yzzx yzzy yzzz yzzw yzwx yzwy 
		yzwz yzww ywxx ywxy ywxz ywxw ywyx ywyy ywyz ywyw 
		ywzx ywzy ywzz ywzw ywwx ywwy ywwz ywww zxxx zxxy 
		zxxz zxxw zxyx zxyy zxyz zxyw zxzx zxzy zxzz zxzw 
		zxwx zxwy zxwz zxww zyxx zyxy zyxz zyxw zyyx zyyy 
		zyyz zyyw zyzx zyzy zyzz zyzw zywx zywy zywz zyww 
		zzxx zzxy zzxz zzxw zzyx zzyy zzyz zzyw zzzx zzzy 
		zzzz zzzw zzwx zzwy zzwz zzww zwxx zwxy zwxz zwxw 
		zwyx zwyy zwyz zwyw zwzx zwzy zwzz zwzw zwwx zwwy 
		zwwz zwww wxxx wxxy wxxz wxxw wxyx wxyy wxyz wxyw 
		wxzx wxzy wxzz wxzw wxwx wxwy wxwz wxww wyxx wyxy 
		wyxz wyxw wyyx wyyy wyyz wyyw wyzx wyzy wyzz wyzw 
		wywx wywy wywz wyww wzxx wzxy wzxz wzxw wzyx wzyy 
		wzyz wzyw wzzx wzzy wzzz wzzw wzwx wzwy wzwz wzww 
		wwxx wwxy wwxz wwxw wwyx wwyy wwyz wwyw wwzx wwzy 
		wwzz wwzw wwwx wwwy wwwz wwww xy xz yz xyz 
		xw yw xyw zw xzw yzw xyzw ]],

		},
	}
