-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local convtypes = [[bool char uchar short ushort int uint long ulong float double]]
local convout = {}
for i in convtypes:gmatch("([%w_]+)") do
  local suffix = {"","_rte","_rtz","_rtp","_rtn"}
  for k,t in ipairs(suffix) do
    table.insert(convout,"convert_"..i..t)
    table.insert(convout,"convert_"..i.."_sat"..t)
    local vectors = {2,4,8,16}
    for n,v in ipairs(vectors) do
      table.insert(convout,"convert_"..i..v..t)
      table.insert(convout,"convert_"..i..v.."_sat"..t)
    end
  end
end
convout = table.concat(convout, " ")

local astypes = [[int uint uchar ushort float double size_t ptrdiff_t intptr_t uintptr_t
long ulong char short unsigned
float2 float4 float8 float16
double2 double4 double8 double16
char2 char4 char8 char16
uchar2 uchar4 uchar8 uchar16
short2 short4 short8 short16
ushort2 ushort4 ushort8 ushort16
int2 int4 int8 int16
uint2 uint4 uint8 uint16
long2 long4 long8 long16
ulong2 ulong4 ulong8 ulong16]]

local astypeout = {}
for i in astypes:gmatch("([%w_]+)") do
  table.insert(astypeout, "as_"..i)
end
astypeout = table.concat(astypeout, " ")

return {
  exts = {"cl","ocl","clh",},
  lexer = wxstc.wxSTC_LEX_CPP,
  apitype = "opencl",
  sep = "%.",
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
    text = {wxstc.wxSTC_C_IDENTIFIER,
      wxstc.wxSTC_C_VERBATIM,
      wxstc.wxSTC_C_REGEX,
      wxstc.wxSTC_C_REGEX,
      wxstc.wxSTC_C_GLOBALCLASS,},

    lexerdef = {wxstc.wxSTC_C_DEFAULT,},
    comment = {wxstc.wxSTC_C_COMMENT,
      wxstc.wxSTC_C_COMMENTLINE,
      wxstc.wxSTC_C_COMMENTDOC,
      wxstc.wxSTC_C_COMMENTLINEDOC,
      wxstc.wxSTC_C_COMMENTDOCKEYWORD,
      wxstc.wxSTC_C_COMMENTDOCKEYWORDERROR,},
    stringtxt = {wxstc.wxSTC_C_STRING,
      wxstc.wxSTC_C_CHARACTER,
      wxstc.wxSTC_C_UUID,},
    stringeol = {wxstc.wxSTC_C_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
    operator = {wxstc.wxSTC_C_OPERATOR,},
    number = {wxstc.wxSTC_C_NUMBER,
      wxstc.wxSTC_C_WORD},

    keywords0 = {wxstc.wxSTC_C_WORD,},
    keywords1 = {wxstc.wxSTC_C_WORD2,},
  },

  keywords = {
    [[int uint uchar ushort float double size_t ptrdiff_t intptr_t uintptr_t
    long ulong char short unsigned

    float2 float4 float8 float16
    double2 double4 double8 double16
    char2 char4 char8 char16
    uchar2 uchar4 uchar8 uchar16
    short2 short4 short8 short16
    ushort2 ushort4 ushort8 ushort16
    int2 int4 int8 int16
    uint2 uint4 uint8 uint16
    long2 long4 long8 long16
    ulong2 ulong4 ulong8 ulong16

    half2 half4 half8 half16
    void half bool
    image2d_t image3d_t sampler_t event_t cl_image_format

    struct typedef void const inline
    return switch case for do while if else break continue volatile
    CLK_A CLK_R CLK_RG CLK_RGB CLK_RGBA CLK_ARGB CLK_BGRA CLK_INTENSITY CLK_LUMINANCE

    MAXFLOAT HUGE_VALF INFINITY NAN
    CLK_LOCAL_MEM_FENCE CLK_GLOBAL_MEM_FENCE
    CLK_SNORM_INT8
    CLK_SNORM_INT16
    CLK_UNORM_INT8
    CLK_UNORM_INT16
    CLK_UNORM_SHORT_565
    CLK_UNORM_SHORT_555
    CLK_UNORM_SHORT_101010
    CLK_SIGNED_INT8
    CLK_SIGNED_INT16
    CLK_SIGNED_INT32
    CLK_UNSIGNED_INT8
    CLK_UNSIGNED_INT16
    CLK_UNSIGNED_INT32
    CLK_HALF_FLOAT
    CLK_FLOAT
    __FILE__ __LINE__ __OPENCL_VERSION__ __ENDIAN_LITTLE__
    __ROUNDING_MODE__ __IMAGE_SUPPORT__ __FAST_RELAXED_MATH__
    ]],

    [[__kernel kernel __attribute__ __read_only __write_only read_only write_only
    __constant constant __local local __global global __private private
    vec_type_hint work_group_size_hint reqd_work_group_size
    aligned packed endian host device

    async_work_group_copy wait_group_events prefetch
    clamp min max degrees radians sign smoothstep step mix
    mem_fence read_mem_fence write_mem_fence
    cross prod distance dot length normalize fast_distance fast_length fast_normalize
    get_image_width get_image_height get_image_depth
    get_image_channel_data_type get_image_channel_order
    get_image_dim
    abs abs_diff add_sat clz hadd mad24 mad_hi mad_sat
    mul24 mul_hi rhadd rotate sub_sat upsample
    read_imagei write_imagei read_imageui write_imageui
    read_imagef write_imagef

    isequal isnotequal isgreater isgreaterequal isless islessequal islessgreater
    isfinite isinf isnan isnormal isordered isunordered signbit any all bitselect select

    acos acosh acospi asin asinh asinpi atan atan2 atanh atanpi atan2pi
    cbrt ceil copysign cos half_cos native_cos cosh cospi half_divide native_divide
    erf erfc exp half_exp native_exp exp2 half_exp2 native_exp2 exp10 half_exp10 native_exp10
    expm1 fabs fdim floor fma fmax fmin fmod fract frexp hypot ilogb
    ldexp lgamma lgamma_r log half_log native_log log2 half_log2 native_log2
    log10 half_log10 native_log10 log1p logb mad modf nan nextafter
    pow pown powr half_powr native_powr half_recip native_recip
    remainder remquo rint round rootn rsqrt half_rsqrt native_rsqrt
    sin half_sin native_sin sincos sinh sinpi sqrt half_sqrt native_sqrt
    tan half_tan native_tan tanh tanpi tgamma trunc

    barrier
    vload2 vload4 vload8 vload16
    vload_half vload_half2 vload_half4 vload_half8 vload_half16 vloada_half4 vloada_half8 vloada_half16
    vstore2 vstore4 vstore8 vstore16
    vstore_half vstore_half2 vstore_half4 vstore_half8 vstore_half16 vstorea_half4 vstorea_half8 vstorea_half16
    get_global_id get_global_size get_group_id get_local_id get_local_size get_num_groups get_work_dim

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
    xw yw xyw zw xzw yzw xyzw ]]..convout.." "..astypeout,

  },
}
