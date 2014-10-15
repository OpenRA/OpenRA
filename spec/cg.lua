-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local funccall = "([A-Za-z_][A-Za-z0-9_]*)%s*"

if not CMarkSymbols then dofile "spec/cbase.lua" end
return {
  exts = {"cg","cgh","cgfx","cgfxh",},
  lexer = wxstc.wxSTC_LEX_CPP,
  apitype = "cg",
  sep = ".",
  linecomment = "//",
  
  isfncall = function(str)
    return string.find(str, funccall .. "%(")
  end,

  marksymbols = CMarkSymbols,

  lexerstyleconvert = {
    text = {wxstc.wxSTC_C_IDENTIFIER,},

    lexerdef = {wxstc.wxSTC_C_DEFAULT,},
    comment = {wxstc.wxSTC_C_COMMENT,
      wxstc.wxSTC_C_COMMENTLINE,
      wxstc.wxSTC_C_COMMENTDOC,},
    stringtxt = {wxstc.wxSTC_C_STRING,
      wxstc.wxSTC_C_CHARACTER,
      wxstc.wxSTC_C_VERBATIM,},
    stringeol = {wxstc.wxSTC_C_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
    operator = {wxstc.wxSTC_C_OPERATOR,},
    number = {wxstc.wxSTC_C_NUMBER,},

    keywords0 = {wxstc.wxSTC_C_WORD,},
    keywords1 = {wxstc.wxSTC_C_WORD2,},
  },

  keywords = {
    [[int half float float3 float4 float2 float3x3 float3x4 float4x3 float4x4
    float1x2 float2x1 float2x2 float2x3 float3x2 float1x3 float3x1 float4x1 float1x4
    float2x4 float4x2 double1x4 double4x4 double4x2 double4x3 double3x4 double2x4 double1x4
    double half half2 half3 half4 int2 int3 uint uint2 uint3 uint4
    int4 bool bool2 bool3 bool4 string struct typedef
    usampler usampler1D usampler2D usampler3D usamplerRECT usamplerCUBE isampler1DARRAY usampler2DARRAY usamplerCUBEARRAY isampler
    isampler1D isampler2D isampler3D isamplerRECT isamplerCUBE isampler1DARRAY isampler2DARRAY isamplerCUBEARRAY sampler sampler1D
    sampler2D sampler3D samplerRECT samplerCUBE sampler1DARRAY sampler2DARRAY samplerCUBEARRAY texture texture1D texture2D
    texture3D textureRECT textureCUBE texture1DARRAY texture2DARRAY textureCUBEARRAY decl do double else
    usamplerBUF isamplerBUF samplerBUF samplerRBUF sampler2DMS sampler2DMSARRAY usamplerRBUF usampler2DMS usampler2DMSARRAY
    isamplerRBUF isampler2DMS isampler2DMSARRAY
    extern false for if in inline inout out pass pixelshader
    return shared static string technique true uniform vector vertexshader void
    volatile while asm compile const auto break case catch
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
    texBUF texBUFsize texRBUF texRBUFsize tex2DMS tex2DMSARRAY tex2DMSsize tex2DMSARRAYsize
    unpack_4ubyte pack_4ubyte unpack_4byte pack_4byte unpack_2ushort pack_2ushort
    unpack_2half pack_2half

    transpose trunc POSITION PSIZE DIFFUSE SPECULAR TEXCOORD FOG FOGP COLOR WPOS
    COLOR0 COLOR1 COLOR2 COLOR3 TEXCOORD0 TEXCOORD1 TEXCOORD2 TEXCOORD3 TEXCOORD4 TEXCOORD5
    TEXCOORD6 TEXCOORD7 TEXCOORD8 TEXCOORD9 TEXCOORD10 TEXCOORD11 TEXCOORD12 TEXCOORD13 TEXCOORD14 TEXCOORD15
    NORMAL FACE PRIMITIVEID DEPTH ATTR0 ATTR1 ATTR2 ATTR3 ATTR4 ATTR5
    ATTR6 ATTR7 ATTR8 ATTR9 ATTR10 ATTR11 ATTR12 ATTR13 ATTR14 ATTR15
    TEXUNIT0 TEXUNIT1 TEXUNIT2 TEXUNIT3 TEXUNIT4 TEXUNIT5 TEXUNIT6 TEXUNIT7 TEXUNIT8 TEXUNIT9
    TEXUNIT10 TEXUNIT11 TEXUNIT12 TEXUNIT13 TEXUNIT14 TEXUNIT15 LAYER INSTANCEID

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
    ]],

  },
}
