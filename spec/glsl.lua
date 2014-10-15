-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local funccall = "([A-Za-z_][A-Za-z0-9_]*)%s*"

if not CMarkSymbols then dofile "spec/cbase.lua" end
return {
  exts = {"glsl","vert","frag","geom","cont","eval", "glslv", "glslf"},
  lexer = wxstc.wxSTC_LEX_CPP,
  apitype = "glsl",
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
    [[int uint half float bool double atomic_uint binding offset
    vec2 vec3 vec4 dvec2 dvec3 dvec4
    ivec2 ivec3 ivec4 uvec2 uvec3 uvec4 bvec2 bvec3 bvec4
    mat2 mat3 mat4 mat2x2 mat3x3 mat4x4 mat2x3 mat3x2 mat4x2 mat2x4 mat4x3 mat3x4
    dmat2 dmat3 dmat4 dmat2x2 dmat3x3 dmat4x4 dmat2x3 dmat3x2 dmat4x2 dmat2x4 dmat4x3 dmat3x4
    float16_t f16vec2 f16vec3 f16vec4
    float32_t f32vec2 f32vec3 f32vec4
    float64_t f64vec2 f64vec3 f64vec4
    int8_t i8vec2 i8vec3 i8vec4
    int8_t i8vec2 i8vec3 i8vec4
    int16_t i16vec2 i16vec3 i16vec4
    int32_t i32vec2 i32vec3 i32vec4
    int64_t i64vec2 i64vec3 i64vec4
    uint8_t u8vec2 u8vec3 u8vec4
    uint16_t u16vec2 u16vec3 u16vec4
    uint32_t u32vec2 u32vec3 u32vec4
    uint64_t u64vec2 u64vec3 u64vec4
    struct typedef void
    usampler1D usampler2D usampler3D usampler2DRect usamplerCube isampler1DArray usampler2DARRAY usamplerCubeArray usampler2DMS usampler2DMSArray
    isampler1D isampler2D isampler3D isampler2DRect isamplerCube isampler1DArray isampler2DARRAY isamplerCubeArray isampler2DMS isampler2DMSArray
    sampler1D sampler2D sampler3D sampler2DRect samplerCube sampler1DArray sampler2DArray samplerCubeArray sampler2DMS sampler2DMSArray
    sampler1DShadow sampler2DShadow sampler2DRectShadow sampler1DArrayShadow sampler2DArrayShadow samplerCubeArrayShadow
    usamplerBuffer isamplerBuffer samplerBuffer samplerRenderbuffer isamplerRenderbuffer usamplerRenderbuffer
    in out inout uniform const centroid sample attribute varying patch index true false
    return switch case for do while if else break continue main inline
    layout location vertices line_strip triangle_strip max_vertices stream 
    triangles quads equal_spacing isolines fractional_even_spacing lines points
    fractional_odd_spacing cw ccw point_mode lines_adjacency triangles_adjacency
    invocations offset align xfb_offset xfb_buffer
    origin_upper_left pixel_center_integer depth_greater depth_greater depth_greater depth_unchanged
    smooth flat noperspective highp mediump lowp shared packed std140 std430 row_major column_major buffer
    gl_FrontColor gl_BackColor gl_FrontSecondaryColor gl_BackSecondaryColor gl_Color gl_SecondaryColor
    subroutine gl_Position gl_FragCoord
    gl_VertexID gl_InstanceID gl_Normal gl_Vertex gl_MultiTexCoord0 gl_MultiTexCoord1
    gl_MultiTexCoord2 gl_MultiTexCoord3 gl_MultiTexCoord4 gl_MultiTexCoord5 gl_MultiTexCoord6
    gl_MultiTexCoord7 gl_FogCoord gl_PointSize gl_ClipDistance
    gl_TexCoord gl_FogFragCoord gl_ClipVertex gl_in
    gl_PatchVerticesIn
    gl_PrimitiveID gl_InvocationID gl_TessLevelOuter gl_TessLevelInner gl_TessCoord
    gl_InvocationID gl_PrimitiveIDIn gl_Layer gl_ViewportIndex gl_FrontFacing
    gl_PointCoord gl_SampleID gl_SamplePosition gl_FragColor
    gl_FragData gl_FragDepth gl_SampleMask
    gl_NumWorkGroups gl_WorkGroupSize gl_WorkGroupID gl_LocalInvocationID gl_GlobalInvocationID gl_LocalInvocationIndex
    local_size_x local_size_y local_size_z
    gl_BaseVertexARB gl_BaseInstanceARB gl_DrawIDARB
    bindless_sampler bound_sampler bindless_image bound_image early_fragment_tests
    gl_HelperInvocation gl_CullDistance

    coherent volatile restrict readonly writeonly
    image1D image2D image3D image2DRect imageCube imageBuffer image1DArray image2DArray imageCubeArray image2DMS image2DMSArray
    uimage1D uimage2D uimage3D uimage2DRect uimageCube uimageBuffer uimage1DArray uimage2DArray uimageCubeArray uimage2DMS uimage2DMSArray
    iimage1D iimage2D iimage3D iimage2DRect iimageCube iimageBuffer iimage1DArray iimage2DArray iimageCubeArray iimage2DMS iimage2DMSArray
    size1x8 size1x16 size1x32 size2x32 size4x32 rgba32f rgba16f rg32f rg16f r32f r16f rgba8 rgba16 r11f_g11f_b10f rgb10_a2ui
    rgb10_a2i rg16 rg8 r16 r8 rgba32i rgba16i rgba8i rg32i rg16i rg8i r32i r16i r8i rgba32ui rgba16ui rgba8ui rg32ui rg16ui rg8ui
    r32ui r16ui r8ui rgba16_snorm rgba8_snorm rg16_snorm rg8_snorm r16_snorm r8_snorm
    ]],

    [[discard
    radians degrees sin cos tan asin acos atan sinh cosh tanh asinh acosh atanh
    pow exp log exp2 log2 sqrt inversesqrt abs sign floor trunc round
    roundEven ceil fract mod modf min max mix step isnan isinf clamp smoothstep
    floatBitsToInt floatBitsToUint intBitsToFloat uintBitsToFloat fma frexp ldexp
    packUnorm2x16 packUnorm4x8 packSnorm4x8
    unpackUnorm2x16 unpackUnorm4x8 unpackSnorm4x8
    packDouble2x32 unpackDouble2x32 packHalf2x16 unpackHalf2x16
    packInt2x32 packUint2x32 unpackInt2x32 unpackUint2x32
    packFloat2x16 unpackFloat2x16 doubleBitsToInt64
    doubleBitsToUint64 int64BitsToDouble uint64BitsToDouble
    
    length distance dot cross normalize ftransform faceforward
    reflect refract
    matrixCompMult outerProduct transpose determinant inverse
    lessThan lessThanEqual greaterThan greaterThanEqual equal
    notEqual any all not
    uaddCarry usubBorrow umulExtended imulExtended
    bitfeldExtract bitfieldInsert bitfeldReverse bitCount
    findLSB findMSB
    dFdx dFdy fwidth dFdxFine dFdyFine fwidthFine dFdxCoarse dFdyCoarse fwidthCoarse
    interpolateAtCentroid interpolateAtSample interpolateAtOffset
    noise1 noise2 noise3 noise4
    EmitStreamVertex EndStreamPrimitive EmitVertex EndPrimitive
    barrier
    textureSize textureSamples textureQueryLod texture textureOffset textureProj
    textureLod textureProjOffset textureLodOffset
    texelFetchOffset texelFetch textureProjLod textureProjLodOffset
    textureGrad textureGradOffset textureProjGrad textureProjGradOffset
    textureGather textureGatherOffset
    
    texture2D texture1D texture3D textureCube texture2DRect
    texture1DProj texture1DLod texture1DProjLod
    texture2DProj texture2DLod texture2DProjLod
    texture3DProj texture3DLod texture3DProjLod
    textureCubeLod
    shadow1D shadow2D
    shadow1DProj shadow1DLod shadow1DProjLod
    shadow2DProj shadow2DLod shadow2DProjLod
    texelFetch1D texelFetch2D texelFetch3D texelFetch2DRect texelFetch1DArray texelFetch2DArray texelFetchBuffer
    textureSizeBuffer textureSize1D textureSize2D textureSize3D textureSizeCube textureSize2DRect
    textureSize1DArray textureSize2DArray
    texture1DArray texture1DArrayLod
    texture2DArray texture2DArrayLod
    shadow1DArray shadow1DArrayLod shadow2DArray shadowCube
    texture1DGrad texture1DProjGrad texture1DProjGrad texture1DArrayGrad
    texture2DGrad texture2DProjGrad texture2DProjGrad texture2DArrayGrad
    texture3DGrad texture3DProjGrad textureCubeGrad
    shadow1DGrad shadow1DProjGrad shadow1DArrayGrad shadow2DGrad shadow2DProjGrad shadow2DArrayGrad
    texture2DRectGrad texture2DRectProjGrad texture2DRectProjGrad shadow2DRectGrad shadow2DRectProjGrad
    shadowCubeGrad
    texture1DOffset texture1DProjOffset texture1DLodOffset texture1DProjLodOffset
    texture2DOffset texture2DProjOffset texture2DLodOffset texture2DProjLodOffset
    texture3DOffset texture3DProjOffset texture3DLodOffset texture3DProjLodOffset

    imageLoad imageStore
    imageAtomicAdd imageAtomicMin imageAtomicMax
    imageAtomicIncWrap imageAtomicDecWrap imageAtomicAnd
    imageAtomicOr imageAtomixXor imageAtomicExchange
    imageAtomicCompSwap imageSize imageSamples
    
    memoryBarrier groupMemoryBarrier memoryBarrierAtomicCounter memoryBarrierShared memoryBarrierBuffer memoryBarrierImage
    
    atomicCounterIncrement atomicCounterDecrement atomicCounter
    atomicMin atomicMax atomicAdd atomicAnd atomicOr atomicXor atomicExchange atomicCompSwap
    
    anyInvocationARB allInvocationsARB allInvocationsEqualARB

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
