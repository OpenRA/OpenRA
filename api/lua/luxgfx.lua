--[[// lxg | Lux Graphics
typedef struct lxgContext_s * lxgContextPTR ;
typedef struct lxgBuffer_s * lxgBufferPTR ;
typedef struct lxgStreamHost_s * lxgStreamHostPTR ;
typedef struct lxgVertexDecl_s * lxgVertexDeclPTR ;
typedef struct lxgFeedbackState_s * lxgFeedbackStatePTR ;
typedef struct lxgTextureImage_s * lxgTextureImagePTR ;
typedef struct lxgSampler_s * lxgSamplerPTR ;
typedef struct lxgTexture_s * lxgTexturePTR ;
typedef struct lxgRenderBuffer_s * lxgRenderBufferPTR ;
typedef struct lxgTextureUpdate_s * lxgTextureUpdatePTR ;
typedef struct lxgProgramParameter_s * lxgProgramParameterPTR ;
typedef struct lxgStageProgram_s * lxgStageProgramPTR ;
typedef struct lxgProgram_s * lxgProgramPTR ;
typedef struct lxgRenderTarget_s * lxgRenderTargetPTR ;
typedef struct lxgViewPort_s * lxgViewPortPTR ;
typedef struct lxgViewPortMrt_s * lxgViewPortMrtPTR ;
typedef struct lxgFrameBounds_s * lxgFrameBoundsPTR ;
typedef struct lxgRenderAssign_s * lxgRenderAssignPTR ;
typedef struct lxgBlend_s * lxgBlendPTR ;
typedef struct lxgStencil_s * lxgStencilPTR ;
typedef struct lxgLogic_s * lxgLogicPTR ;
typedef struct lxgDepth_s * lxgDepthPTR ;
typedef struct lxgColor_s * lxgColorPTR ;
typedef struct lxgRasterizer_s * lxgRasterizerPTR ;
typedef const struct lxgContext_s * lxgContextCPTR ;
typedef const struct lxgBuffer_s * lxgBufferCPTR ;
typedef const struct lxgStreamHost_s * lxgStreamHostCPTR ;
typedef const struct lxgVertexDecl_s * lxgVertexDeclCPTR ;
typedef const struct lxgFeedbackState_s * lxgFeedbackStateCPTR ;
typedef const struct lxgTextureImage_s * lxgTextureImageCPTR ;
typedef const struct lxgSampler_s * lxgSamplerCPTR ;
typedef const struct lxgTexture_s * lxgTextureCPTR ;
typedef const struct lxgRenderBuffer_s * lxgRenderBufferCPTR ;
typedef const struct lxgTextureUpdate_s * lxgTextureUpdateCPTR ;
typedef const struct lxgProgramParameter_s * lxgProgramParameterCPTR ;
typedef const struct lxgStageProgram_s * lxgStageProgramCPTR ;
typedef const struct lxgProgram_s * lxgProgramCPTR ;
typedef const struct lxgRenderTarget_s * lxgRenderTargetCPTR ;
typedef const struct lxgViewPort_s * lxgViewPortCPTR ;
typedef const struct lxgViewPortMrt_s * lxgViewPortMrtCPTR ;
typedef const struct lxgFrameBounds_s * lxgFrameBoundsCPTR ;
typedef const struct lxgRenderAssign_s * lxgRenderAssignCPTR ;
typedef const struct lxgBlend_s * lxgBlendCPTR ;
typedef const struct lxgColor_s * lxgColorCPTR ;
typedef const struct lxgStencil_s * lxgStencilCPTR ;
typedef const struct lxgLogic_s * lxgLogicCPTR ;
typedef const struct lxgDepth_s * lxgDepthCPTR ;
typedef const struct lxgRasterizer_s * lxgRasterizerCPTR ;
typedef flags32 lxgRenderFlag_t ;
typedef enum lxgAccessMode_e
{
    LUXGFX_ACCESS_READ , LUXGFX_ACCESS_WRITE , LUXGFX_ACCESS_READWRITE , LUXGFX_ACCESS_WRITEDISCARD , LUXGFX_ACCESS_WRITEDISCARDALL , LUXGFX_ACCESSES , }
lxgAccessMode_t ;
enum
{
    LUXGFX_MAX_TEXTURE_IMAGES = 32 , LUXGFX_MAX_RENDERTARGETS = 16 , LUXGFX_MAX_RWTEXTURE_IMAGES = 8 , LUXGFX_MAX_STAGE_BUFFERS = 12 , LUXGFX_MAX_TEXTURE_MIPMAPS = 16 , LUXGFX_MAX_VERTEX_STREAMS = 8 , LUXGFX_MAX_STAGE_SUBROUTINES = 1024 , }
;
typedef enum lxGLCompareMode_e
{
    LUXGL_COMPARE_NEVER = GL_NEVER , LUXGL_COMPARE_LESS = GL_LESS , LUXGL_COMPARE_EQUAL = GL_EQUAL , LUXGL_COMPARE_LEQUAL = GL_LEQUAL , LUXGL_COMPARE_GREATER = GL_GREATER , LUXGL_COMPARE_NOTEQUAL = GL_NOTEQUAL , LUXGL_COMPARE_GEQUAL = GL_GEQUAL , LUXGL_COMPARE_ALWAYS = GL_ALWAYS , LUXGL_COMPARE_DONTEXECUTE = 0xFFFFFFFFu , }
lxGLCompareMode_t ;
typedef enum lxGLBufferHint_e
{
    LUXGL_STATIC_DRAW = GL_STATIC_DRAW , LUXGL_STATIC_READ = GL_STATIC_READ , LUXGL_STATIC_COPY = GL_STATIC_COPY , LUXGL_DYNAMIC_DRAW = GL_DYNAMIC_DRAW , LUXGL_DYNAMIC_READ = GL_DYNAMIC_READ , LUXGL_DYNAMIC_COPY = GL_DYNAMIC_COPY , LUXGL_STREAM_DRAW = GL_STREAM_DRAW , LUXGL_STREAM_READ = GL_STREAM_READ , LUXGL_STREAM_COPY = GL_STREAM_COPY , }
lxGLBufferHint_t ;
typedef enum lxGLStencilMode_e
{
    LUXGL_STENCIL_KEEP = GL_KEEP , LUXGL_STENCIL_ZERO = GL_ZERO , LUXGL_STENCIL_REPLACE = GL_REPLACE , LUXGL_STENCIL_INCR_SAT = GL_INCR , LUXGL_STENCIL_DECR_SAT = GL_DECR , LUXGL_STENCIL_INVERT = GL_INVERT , LUXGL_STENCIL_INCR = GL_INCR_WRAP , LUXGL_STENCIL_DECR = GL_DECR_WRAP , }
lxGLStencilMode_t ;
typedef enum lxGLBlendWeight_e
{
    LUXGL_BLENDW_ZERO = GL_ZERO , LUXGL_BLENDW_ONE = GL_ONE , LUXGL_BLENDW_RGB_SRC = GL_SRC_COLOR , LUXGL_BLENDW_RGB_DST = GL_DST_COLOR , LUXGL_BLENDW_A_SRC = GL_SRC_ALPHA , LUXGL_BLENDW_A_DST = GL_DST_ALPHA , LUXGL_BLENDW_INVRGB_SRC = GL_ONE_MINUS_SRC_COLOR , LUXGL_BLENDW_INVRGB_DST = GL_ONE_MINUS_DST_COLOR , LUXGL_BLENDW_INVA_SRC = GL_ONE_MINUS_SRC_ALPHA , LUXGL_BLENDW_INVA_DST = GL_ONE_MINUS_DST_ALPHA , LUXGL_BLENDW_UNKOWN = 0xFFFFABCD , }
lxGLBlendWeight_t ;
typedef enum lxGLBlendEquation_e
{
    LUXGL_BLENDE_ADD = GL_FUNC_ADD , LUXGL_BLENDE_SUB = GL_FUNC_SUBTRACT , LUXGL_BLENDE_SUB_REV = GL_FUNC_REVERSE_SUBTRACT , LUXGL_BLENDE_MIN = GL_MIN , LUXGL_BLENDE_MAX = GL_MAX , LUXGL_BLENDE_UNKOWN = 0xFFFFABCD , }
lxGLBlendEquation_t ;
typedef enum lxGLLogicOp_e
{
    LUXGL_LOGICOP_CLEAR = GL_CLEAR , LUXGL_LOGICOP_SET = GL_SET , LUXGL_LOGICOP_COPY = GL_COPY , LUXGL_LOGICOP_INVERTED = GL_COPY_INVERTED , LUXGL_LOGICOP_NOOP = GL_NOOP , LUXGL_LOGICOP_INVERT = GL_INVERT , LUXGL_LOGICOP_AND = GL_AND , LUXGL_LOGICOP_NAND = GL_NAND , LUXGL_LOGICOP_OR = GL_OR , LUXGL_LOGICOP_NOR = GL_NOR , LUXGL_LOGICOP_XOR = GL_XOR , LUXGL_LOGICOP_EQUIV = GL_EQUIV , LUXGL_LOGICOP_AND_REVERSE = GL_AND_REVERSE , LUXGL_LOGICOP_AND_INVERTED = GL_AND_INVERTED , LUXGL_LOGICOP_OR_REVERSE = GL_OR_REVERSE , LUXGL_LOGICOP_OR_INVERTED = GL_OR_INVERTED , LUXGL_LOGICOP_ILLEGAL = 0 , }
lxGLLogicOp_t ;
typedef enum lxGLPrimitiveType_e
{
    LUXGL_POINTS = GL_POINTS , LUXGL_TRIANGLES = GL_TRIANGLES , LUXGL_TRIANGLE_STRIP = GL_TRIANGLE_STRIP , LUXGL_TRIANGLE_FAN = GL_TRIANGLE_FAN , LUXGL_LINES = GL_LINES , LUXGL_LINE_LOOP = GL_LINE_LOOP , LUXGL_LINE_STRIP = GL_LINE_STRIP , LUXGL_QUADS = GL_QUADS , LUXGL_QUAD_STRIP = GL_QUAD_STRIP , LUXGL_LINE_ADJ = GL_LINES_ADJACENCY , LUXGL_LINE_STRIP_ADJ = GL_LINE_STRIP_ADJACENCY , LUXGL_TRIANGLE_STRIP_ADJ = GL_TRIANGLE_STRIP_ADJACENCY , LUXGL_TRIANGLE_ADJ = GL_TRIANGLES_ADJACENCY , LUXGL_PATCHES = GL_PATCHES , LUXGL_POLYGON = GL_POLYGON , }
lxGLPrimitiveType_t ;
typedef enum lxGLTextureTarget_e
{
    LUXGL_TEXTURE_1D = GL_TEXTURE_1D , LUXGL_TEXTURE_2D = GL_TEXTURE_2D , LUXGL_TEXTURE_3D = GL_TEXTURE_3D , LUXGL_TEXTURE_RECT = GL_TEXTURE_RECTANGLE , LUXGL_TEXTURE_1DARRAY = GL_TEXTURE_1D_ARRAY , LUXGL_TEXTURE_2DARRAY = GL_TEXTURE_2D_ARRAY , LUXGL_TEXTURE_CUBE = GL_TEXTURE_CUBE_MAP , LUXGL_TEXTURE_CUBEARRAY = GL_TEXTURE_CUBE_MAP_ARRAY , LUXGL_TEXTURE_2DMS = GL_TEXTURE_2D_MULTISAMPLE , LUXGL_TEXTURE_2DMSARRAY = GL_TEXTURE_2D_MULTISAMPLE_ARRAY , LUXGL_TEXTURE_BUFFER = GL_TEXTURE_BUFFER , LUXGL_TEXTURE_RENDERBUFFER = GL_TEXTURE_RENDERBUFFER_NV , LUXGL_TEXTURE_INVALID = 0 , }
lxGLTextureTarget_t ;
typedef enum lxGLBufferTarget_e
{
    LUXGL_BUFFER_VERTEX = GL_ARRAY_BUFFER , LUXGL_BUFFER_INDEX = GL_ELEMENT_ARRAY_BUFFER , LUXGL_BUFFER_PIXELWRITE = GL_PIXEL_PACK_BUFFER , LUXGL_BUFFER_PIXELREAD = GL_PIXEL_UNPACK_BUFFER , LUXGL_BUFFER_UNIFORM = GL_UNIFORM_BUFFER , LUXGL_BUFFER_TEXTURE = GL_TEXTURE_BUFFER , LUXGL_BUFFER_FEEDBACK = GL_TRANSFORM_FEEDBACK_BUFFER , LUXGL_BUFFER_CPYWRITE = GL_COPY_WRITE_BUFFER , LUXGL_BUFFER_CPYREAD = GL_COPY_READ_BUFFER , LUXGL_BUFFER_DRAWINDIRECT = GL_DRAW_INDIRECT_BUFFER , LUXGL_BUFFER_NVVIDEO = 0x9020 , LUXGL_BUFFER_NVPARAM_VERTEX = GL_VERTEX_PROGRAM_PARAMETER_BUFFER_NV , LUXGL_BUFFER_NVPARAM_GEOMETRY = GL_GEOMETRY_PROGRAM_PARAMETER_BUFFER_NV , LUXGL_BUFFER_NVPARAM_FRAGMENT = GL_FRAGMENT_PROGRAM_PARAMETER_BUFFER_NV , LUXGL_BUFFER_NVPARAM_TESSCTRL = GL_TESS_CONTROL_PROGRAM_PARAMETER_BUFFER_NV , LUXGL_BUFFER_NVPARAM_TESSEVAL = GL_TESS_EVALUATION_PROGRAM_PARAMETER_BUFFER_NV , LUXGL_BUFFER_INVALID = 0 , }
lxGLBufferTarget_t ;
typedef enum lxGLShaderType_e
{
    LUXGL_SHADER_VERTEX = GL_VERTEX_SHADER , LUXGL_SHADER_FRAGMENT = GL_FRAGMENT_SHADER , LUXGL_SHADER_GEOMETRY = GL_GEOMETRY_SHADER , LUXGL_SHADER_TESSCTRL = GL_TESS_CONTROL_SHADER , LUXGL_SHADER_TESSEVAL = GL_TESS_EVALUATION_SHADER , }
lxGLShaderType_t ;
typedef enum lxGLProgramType_e
{
    LUXGL_PROGRAM_VERTEX = GL_VERTEX_PROGRAM_ARB , LUXGL_PROGRAM_FRAGMENT = GL_FRAGMENT_PROGRAM_ARB , LUXGL_PROGRAM_GEOMETRY = GL_GEOMETRY_PROGRAM_NV , LUXGL_PROGRAM_TESSCTRL = GL_TESS_CONTROL_PROGRAM_NV , LUXGL_PROGRAM_TESSEVAL = GL_TESS_EVALUATION_PROGRAM_NV , }
lxGLProgramType_t ;
typedef enum lxGLAccessFormat_e
{
    LUXGL_ACCESSFORMAT_R8UI = GL_R8UI , LUXGL_ACCESSFORMAT_R8I = GL_R8I , LUXGL_ACCESSFORMAT_R16UI = GL_R16UI , LUXGL_ACCESSFORMAT_R16I = GL_R16I , LUXGL_ACCESSFORMAT_R32UI = GL_R32UI , LUXGL_ACCESSFORMAT_R32I = GL_R32I , LUXGL_ACCESSFORMAT_R32F = GL_R32F , LUXGL_ACCESSFORMAT_RG32UI = GL_RG32UI , LUXGL_ACCESSFORMAT_RG32I = GL_RG32I , LUXGL_ACCESSFORMAT_RG32F = GL_RG32F , LUXGL_ACCESSFORMAT_RGBA32UI = GL_RGBA32UI , LUXGL_ACCESSFORMAT_RGBA32I = GL_RGBA32I , LUXGL_ACCESSFORMAT_RGBA32F = GL_RGBA32F , }
lxGLAccessFormat_t ;
typedef enum lxGLAccessMode_e
{
    LUXGL_ACCESS_READ_ONLY = GL_READ_ONLY , LUXGL_ACCESS_WRITE_ONLY = GL_WRITE_ONLY , LUXGL_ACCESS_READ_WRITE = GL_READ_WRITE , }
lxGLAccessMode_t ;
typedef enum lxGLParameterType_e
{
    LUXGL_PARAM_FLOAT = GL_FLOAT , LUXGL_PARAM_FLOAT2 = GL_FLOAT_VEC2 , LUXGL_PARAM_FLOAT3 = GL_FLOAT_VEC3 , LUXGL_PARAM_FLOAT4 = GL_FLOAT_VEC4 , LUXGL_PARAM_INT = GL_INT , LUXGL_PARAM_INT2 = GL_INT_VEC2 , LUXGL_PARAM_INT3 = GL_INT_VEC3 , LUXGL_PARAM_INT4 = GL_INT_VEC4 , LUXGL_PARAM_UINT = GL_UNSIGNED_INT , LUXGL_PARAM_UINT2 = GL_UNSIGNED_INT_VEC2 , LUXGL_PARAM_UINT3 = GL_UNSIGNED_INT_VEC3 , LUXGL_PARAM_UINT4 = GL_UNSIGNED_INT_VEC4 , LUXGL_PARAM_BOOL = GL_BOOL , LUXGL_PARAM_BOOL2 = GL_BOOL_VEC2 , LUXGL_PARAM_BOOL3 = GL_BOOL_VEC3 , LUXGL_PARAM_BOOL4 = GL_BOOL_VEC4 , LUXGL_PARAM_MAT2 = GL_FLOAT_MAT2 , LUXGL_PARAM_MAT3 = GL_FLOAT_MAT3 , LUXGL_PARAM_MAT4 = GL_FLOAT_MAT4 , LUXGL_PARAM_MAT2x3 = GL_FLOAT_MAT2x3 , LUXGL_PARAM_MAT2x4 = GL_FLOAT_MAT2x4 , LUXGL_PARAM_MAT3x2 = GL_FLOAT_MAT3x2 , LUXGL_PARAM_MAT3x4 = GL_FLOAT_MAT3x4 , LUXGL_PARAM_MAT4x2 = GL_FLOAT_MAT4x2 , LUXGL_PARAM_MAT4x3 = GL_FLOAT_MAT4x3 , LUXGL_PARAM_SAMPLER_1D = GL_SAMPLER_1D , LUXGL_PARAM_SAMPLER_2D = GL_SAMPLER_2D , LUXGL_PARAM_SAMPLER_3D = GL_SAMPLER_3D , LUXGL_PARAM_SAMPLER_CUBE = GL_SAMPLER_CUBE , LUXGL_PARAM_SAMPLER_2DRECT = GL_SAMPLER_2D_RECT , LUXGL_PARAM_SAMPLER_2DMS = GL_SAMPLER_2D_MULTISAMPLE , LUXGL_PARAM_SAMPLER_1DARRAY = GL_SAMPLER_1D_ARRAY , LUXGL_PARAM_SAMPLER_2DARRAY = GL_SAMPLER_2D_ARRAY , LUXGL_PARAM_SAMPLER_CUBEARRAY = GL_SAMPLER_CUBE_MAP_ARRAY , LUXGL_PARAM_SAMPLER_2DMSARRAY = GL_SAMPLER_2D_MULTISAMPLE_ARRAY , LUXGL_PARAM_SAMPLER_BUFFER = GL_SAMPLER_BUFFER , LUXGL_PARAM_ISAMPLER_1D = GL_INT_SAMPLER_1D , LUXGL_PARAM_ISAMPLER_2D = GL_INT_SAMPLER_2D , LUXGL_PARAM_ISAMPLER_3D = GL_INT_SAMPLER_3D , LUXGL_PARAM_ISAMPLER_CUBE = GL_INT_SAMPLER_CUBE , LUXGL_PARAM_ISAMPLER_2DRECT = GL_INT_SAMPLER_2D_RECT , LUXGL_PARAM_ISAMPLER_2DMS = GL_INT_SAMPLER_2D_MULTISAMPLE , LUXGL_PARAM_ISAMPLER_1DARRAY = GL_INT_SAMPLER_1D_ARRAY , LUXGL_PARAM_ISAMPLER_2DARRAY = GL_INT_SAMPLER_2D_ARRAY , LUXGL_PARAM_ISAMPLER_CUBEARRAY = GL_INT_SAMPLER_CUBE_MAP_ARRAY , LUXGL_PARAM_ISAMPLER_2DMSARRAY = GL_INT_SAMPLER_2D_MULTISAMPLE_ARRAY , LUXGL_PARAM_ISAMPLER_BUFFER = GL_INT_SAMPLER_BUFFER , LUXGL_PARAM_USAMPLER_1D = GL_UNSIGNED_INT_SAMPLER_1D , LUXGL_PARAM_USAMPLER_2D = GL_UNSIGNED_INT_SAMPLER_2D , LUXGL_PARAM_USAMPLER_3D = GL_UNSIGNED_INT_SAMPLER_3D , LUXGL_PARAM_USAMPLER_CUBE = GL_UNSIGNED_INT_SAMPLER_CUBE , LUXGL_PARAM_USAMPLER_2DRECT = GL_UNSIGNED_INT_SAMPLER_2D_RECT , LUXGL_PARAM_USAMPLER_2DMS = GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE , LUXGL_PARAM_USAMPLER_1DARRAY = GL_UNSIGNED_INT_SAMPLER_1D_ARRAY , LUXGL_PARAM_USAMPLER_2DARRAY = GL_UNSIGNED_INT_SAMPLER_2D_ARRAY , LUXGL_PARAM_USAMPLER_CUBEARRAY = GL_UNSIGNED_INT_SAMPLER_CUBE_MAP_ARRAY , LUXGL_PARAM_USAMPLER_2DMSARRAY = GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY , LUXGL_PARAM_USAMPLER_BUFFER = GL_UNSIGNED_INT_SAMPLER_BUFFER , LUXGL_PARAM_SAMPLER_1D_SHADOW = GL_SAMPLER_1D_SHADOW , LUXGL_PARAM_SAMPLER_2D_SHADOW = GL_SAMPLER_2D_SHADOW , LUXGL_PARAM_SAMPLER_CUBE_SHADOW = GL_SAMPLER_CUBE_SHADOW , LUXGL_PARAM_SAMPLER_2DRECT_SHADOW = GL_SAMPLER_2D_RECT_SHADOW , LUXGL_PARAM_SAMPLER_1DARRAY_SHADOW = GL_SAMPLER_1D_ARRAY_SHADOW , LUXGL_PARAM_SAMPLER_2DARRAY_SHADOW = GL_SAMPLER_2D_ARRAY_SHADOW , LUXGL_PARAM_SAMPLER_CUBEARRAY_SHADOW = GL_SAMPLER_CUBE_MAP_ARRAY_SHADOW , LUXGL_PARAM_IMAGE_1D = GL_IMAGE_1D_EXT , LUXGL_PARAM_IMAGE_2D = GL_IMAGE_2D_EXT , LUXGL_PARAM_IMAGE_3D = GL_IMAGE_3D_EXT , LUXGL_PARAM_IMAGE_CUBE = GL_IMAGE_CUBE_EXT , LUXGL_PARAM_IMAGE_2DRECT = GL_IMAGE_2D_RECT_EXT , LUXGL_PARAM_IMAGE_2DMS = GL_IMAGE_2D_MULTISAMPLE_EXT , LUXGL_PARAM_IMAGE_1DARRAY = GL_IMAGE_1D_ARRAY_EXT , LUXGL_PARAM_IMAGE_2DARRAY = GL_IMAGE_2D_ARRAY_EXT , LUXGL_PARAM_IMAGE_CUBEARRAY = GL_IMAGE_CUBE_MAP_ARRAY_EXT , LUXGL_PARAM_IMAGE_2DMSARRAY = GL_IMAGE_2D_MULTISAMPLE_ARRAY_EXT , LUXGL_PARAM_IMAGE_BUFFER = GL_IMAGE_BUFFER_EXT , LUXGL_PARAM_IIMAGE_1D = GL_INT_IMAGE_1D_EXT , LUXGL_PARAM_IIMAGE_2D = GL_INT_IMAGE_2D_EXT , LUXGL_PARAM_IIMAGE_3D = GL_INT_IMAGE_3D_EXT , LUXGL_PARAM_IIMAGE_CUBE = GL_INT_IMAGE_CUBE_EXT , LUXGL_PARAM_IIMAGE_2DRECT = GL_INT_IMAGE_2D_RECT_EXT , LUXGL_PARAM_IIMAGE_2DMS = GL_INT_IMAGE_2D_MULTISAMPLE_EXT , LUXGL_PARAM_IIMAGE_1DARRAY = GL_INT_IMAGE_1D_ARRAY_EXT , LUXGL_PARAM_IIMAGE_2DARRAY = GL_INT_IMAGE_2D_ARRAY_EXT , LUXGL_PARAM_IIMAGE_CUBEARRAY = GL_INT_IMAGE_CUBE_MAP_ARRAY_EXT , LUXGL_PARAM_IIMAGE_2DMSARRAY = GL_INT_IMAGE_2D_MULTISAMPLE_ARRAY_EXT , LUXGL_PARAM_IIMAGE_BUFFER = GL_INT_IMAGE_BUFFER_EXT , LUXGL_PARAM_UIMAGE_1D = GL_UNSIGNED_INT_IMAGE_1D_EXT , LUXGL_PARAM_UIMAGE_2D = GL_UNSIGNED_INT_IMAGE_2D_EXT , LUXGL_PARAM_UIMAGE_3D = GL_UNSIGNED_INT_IMAGE_3D_EXT , LUXGL_PARAM_UIMAGE_CUBE = GL_UNSIGNED_INT_IMAGE_CUBE_EXT , LUXGL_PARAM_UIMAGE_2DRECT = GL_UNSIGNED_INT_IMAGE_2D_RECT_EXT , LUXGL_PARAM_UIMAGE_2DMS = GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_EXT , LUXGL_PARAM_UIMAGE_1DARRAY = GL_UNSIGNED_INT_IMAGE_1D_ARRAY_EXT , LUXGL_PARAM_UIMAGE_2DARRAY = GL_UNSIGNED_INT_IMAGE_2D_ARRAY_EXT , LUXGL_PARAM_UIMAGE_CUBEARRAY = GL_UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY_EXT , LUXGL_PARAM_UIMAGE_2DMSARRAY = GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY_EXT , LUXGL_PARAM_UIMAGE_BUFFER = GL_UNSIGNED_INT_IMAGE_BUFFER_EXT , LUXGL_PARAM_GPU_ADDRESS = GL_GPU_ADDRESS_NV , LUXGL_PARAM_BUFFER = 0x7FFFFFF0 , LUXGL_PARAM_SUBROUTINE = 0x7FFFFFF1 , LUXGL_PARAM_USER = 0x7FFFFFFF , }
lxGLParameterType_t ;
typedef enum lxGLError_e
{
    LUXGL_ERROR_NONE = GL_NO_ERROR , LUXGL_ERROR_OP = GL_INVALID_OPERATION , LUXGL_ERROR_ENUM = GL_INVALID_ENUM , LUXGL_ERROR_VALUE = GL_INVALID_VALUE , LUXGL_ERROR_INDEX = GL_INVALID_INDEX , LUXGL_ERROR_FBOP = GL_INVALID_FRAMEBUFFER_OPERATION , }
lxGLError_t ;
typedef struct lxgBuffer_s
{
    lxGLBufferTarget_t gltarget ;
    GLuint glid ;
    GLuint64 address ;
    flags32 ctxcapbits ;
    void * user ;
    void * mapped ;
    lxgAccessMode_t maptype ;
    uint mapstart ;
    uint maplength ;
    uint size ;
    uint used ;
    lxGLBufferHint_t hint ;
    lxgContextPTR ctx ;
}
lxgBuffer_t ;
uint lxgBuffer_alloc ( lxgBufferPTR buffer , uint needed , uint padsize ) ;
void lxgBuffer_bind ( lxgBufferCPTR buffer , lxGLBufferTarget_t type ) ;
void lxgBuffer_bindIndexed ( lxgBufferCPTR buffer , lxGLBufferTarget_t type , uint idx ) ;
void lxgBuffer_bindRanged ( lxgBufferCPTR buffer , lxGLBufferTarget_t type , uint idx , size_t offset , size_t size ) ;
void * lxgBuffer_map ( lxgBufferPTR buffer , lxgAccessMode_t type , booln * succ ) ;
void * lxgBuffer_mapRange ( lxgBufferPTR buffer , uint from , uint length , lxgAccessMode_t type , booln manualflush , booln unsynch , booln * succ ) ;
booln lxgBuffer_flushRange ( lxgBufferPTR buffer , uint from , uint length ) ;
booln lxgBuffer_unmap ( lxgBufferPTR buffer ) ;
booln lxgBuffer_copy ( lxgBufferPTR buffer , uint bufferoffset , lxgBufferPTR src , uint srcoffset , uint size ) ;
GLuint64 lxgBuffer_addressNV ( lxgBufferPTR buffer ) ;
void lxgBuffer_residentNV ( lxgBufferPTR buffer , lxgAccessMode_t mode ) ;
void lxgBuffer_unresidentNV ( lxgBufferPTR buffer ) ;
void lxgBuffer_deinit ( lxgBufferPTR buffer , lxgContextPTR ctx ) ;
void lxgBuffer_reset ( lxgBufferPTR buffer , void * data ) ;
void lxgBuffer_init ( lxgBufferPTR buffer , lxgContextPTR ctx , lxGLBufferHint_t hint , uint size , void * data ) ;
typedef enum lxgVertexAttrib_e
{
    LUXGFX_VERTEX_ATTRIB_POS , LUXGFX_VERTEX_ATTRIB_ATTR1 , LUXGFX_VERTEX_ATTRIB_NORMAL , LUXGFX_VERTEX_ATTRIB_COLOR , LUXGFX_VERTEX_ATTRIB_ATTR4 , LUXGFX_VERTEX_ATTRIB_ATTR5 , LUXGFX_VERTEX_ATTRIB_ATTR6 , LUXGFX_VERTEX_ATTRIB_ATTR7 , LUXGFX_VERTEX_ATTRIB_TEXCOORD0 , LUXGFX_VERTEX_ATTRIB_TEXCOORD1 , LUXGFX_VERTEX_ATTRIB_TEXCOORD2 , LUXGFX_VERTEX_ATTRIB_TEXCOORD3 , LUXGFX_VERTEX_ATTRIB_ATTR12 , LUXGFX_VERTEX_ATTRIB_ATTR13 , LUXGFX_VERTEX_ATTRIB_ATTR14 , LUXGFX_VERTEX_ATTRIB_ATTR15 , LUXGFX_VERTEX_ATTRIBS , }
lxgVertexAttrib_t ;
typedef struct lxgVertexElement_s
{
    unsigned normalize : 1 ;
    unsigned integer : 1 ;
    unsigned cnt : 2 ;
    unsigned stream : 4 ;
    unsigned scalartype : 8 ;
    unsigned stridehalf : 8 ;
    unsigned offset : 8 ;
}
lxgVertexElement_t ;
typedef struct lxgVertexDecl_s
{
    flags32 available ;
    uint streams ;
    lxgVertexElement_t table [ LUXGFX_VERTEX_ATTRIBS ] ;
}
lxgVertexDecl_t ;
typedef struct lxgStreamHost_s
{
    lxgBufferPTR buffer ;
    union
    {
        void * ptr ;
        size_t offset ;
    }
    ;
    size_t len ;
}
lxgStreamHost_t ;
typedef struct lxgVertexPointer_s
{
    lxgVertexElement_t element [ LUXGFX_VERTEX_ATTRIBS ] ;
    lxgStreamHost_t streams [ LUXGFX_MAX_VERTEX_STREAMS ] ;
}
lxgVertexPointer_t ;
typedef struct lxgVertexState_s
{
    lxgVertexDeclCPTR decl ;
    flags32 active ;
    flags32 declvalid ;
    flags32 declstreams ;
    flags32 streamvalid ;
    flags32 declchange ;
    flags32 streamchange ;
    lxgVertexPointer_t setup ;
}
lxgVertexState_t ;
typedef struct lxgFeedbackState_s
{
    lxGLPrimitiveType_t capture ;
    int active ;
    flags32 usedvalid ;
    flags32 streamvalid ;
    flags32 streamchange ;
    lxgStreamHost_t streams [ LUXGFX_MAX_VERTEX_STREAMS ] ;
}
lxgFeedbackState_t ;
flags32 lxgVertexAttrib_bit ( lxgVertexAttrib_t attrib ) ;
lxgVertexElement_t lxgVertexElement_set ( uint cnt , enum lxScalarType_e type , booln normalize , booln integer , uint stride , uint offset , uint stream ) ;
void lxgVertexAttrib_applyFloat ( lxgVertexAttrib_t attrib , const float * vec4 ) ;
void lxgVertexAttrib_applyInteger ( lxgVertexAttrib_t attrib , const int * vec4 ) ;
void lxgVertexAttrib_applyFloatFIXED ( lxgVertexAttrib_t attrib , const float * vec4 ) ;
void lxgContext_applyVertexAttribs ( lxgContextPTR ctx , flags32 attribs , flags32 changed ) ;
void lxgContext_applyVertexAttribsFIXED ( lxgContextPTR ctx , flags32 attribs , flags32 changed ) ;
void lxgContext_clearVertexState ( lxgContextPTR ctx ) ;
void lxgContext_setVertexDecl ( lxgContextPTR ctx , lxgVertexDeclCPTR decl ) ;
void lxgContext_setVertexDeclStreams ( lxgContextPTR ctx , lxgVertexDeclCPTR decl , lxgStreamHostCPTR hosts ) ;
void lxgContext_setVertexStream ( lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host ) ;
void lxgContext_invalidateVertexStreams ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexState ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateFIXED ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateNV ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateFIXEDNV ( lxgContextPTR ctx ) ;
void lxgContext_clearFeedbackState ( lxgContextPTR ctx ) ;
void lxgContext_applyFeedbackStreams ( lxgContextPTR ctx ) ;
void lxgContext_setFeedbackStreams ( lxgContextPTR ctx , lxgStreamHostCPTR hosts , int numStreams ) ;
void lxgContext_setFeedbackStream ( lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host ) ;
void lxgContext_enableFeedback ( lxgContextPTR ctx , lxGLPrimitiveType_t type , int numStreams ) ;
void lxgContext_disableFeedback ( lxgContextPTR ctx ) ;
typedef enum lxgSamplerFilter_e
{
    LUXGFX_SAMPLERFILTER_NEAREST , LUXGFX_SAMPLERFILTER_LINEAR , LUXGFX_SAMPLERFILTER_MIPMAP_NEAREST , LUXGFX_SAMPLERFILTER_MIPMAP_LINEAR , LUXGFX_SAMPLERFILTERS , }
lxgSamplerFilter_t ;
typedef enum lxgSamplerAddress_e
{
    LUXGFX_SAMPLERADDRESS_REPEAT , LUXGFX_SAMPLERADDRESS_MIRROR , LUXGFX_SAMPLERADDRESS_CLAMP , LUXGFX_SAMPLERADDRESS_BORDER , LUXGFX_SAMPLERADDRESSES , }
lxgSamplerAddress_t ;
enum lxgSamplerAttrib_e
{
    LUXGFX_SAMPLERATTRIB_FILTER = 1 << 0 , LUXGFX_SAMPLERATTRIB_CMP = 1 << 1 , LUXGFX_SAMPLERATTRIB_ADDRESS = 1 << 2 , LUXGFX_SAMPLERATTRIB_ANISO = 1 << 3 , LUXGFX_SAMPLERATTRIB_LOD = 1 << 4 , LUXGFX_SAMPLERATTRIB_BORDER = 1 << 5 , LUXGFX_SAMPLERATTRIB_ALL = ( 1 << 6 ) - 1 , }
;
typedef struct lxgSamplerLod_s
{
    float bias ;
    float min ;
    float max ;
}
lxgSamplerLod_t ;
typedef struct lxgSampler_s
{
    GLuint glid ;
    uint32 incarnation ;
    lxGLCompareMode_t cmpfunc ;
    lxgSamplerFilter_t filter ;
    lxgSamplerAddress_t addru ;
    lxgSamplerAddress_t addrv ;
    lxgSamplerAddress_t addrw ;
    uint aniso ;
    lxgSamplerLod_t lod ;
    float border [ 4 ] ;
}
lxgSampler_t ;
typedef enum lxgTextureFlags_e
{
    LUXGFX_TEXTUREFLAG_AUTOMIPMAP = 1 << 0 , LUXGFX_TEXTUREFLAG_MANMIPMAP = 1 << 1 , LUXGFX_TEXTUREFLAG_COMPRESS = 1 << 2 , LUXGFX_TEXTUREFLAG_COMPRESSED = 1 << 3 , LUXGFX_TEXTUREFLAG_SAMPLESFIXED = 1 << 4 , LUXGFX_TEXTUREFLAG_HASLOD = 1 << 30 , LUXGFX_TEXTUREFLAG_HASCOMPARE = 1 << 31 , }
lxgTextureFlags_t ;
typedef enum lxgTextureChannel_e
{
    LUXGFX_TEXTURECHANNEL_RGB , LUXGFX_TEXTURECHANNEL_RGBA , LUXGFX_TEXTURECHANNEL_R , LUXGFX_TEXTURECHANNEL_RG , LUXGFX_TEXTURECHANNEL_SRGB , LUXGFX_TEXTURECHANNEL_SRGBA , LUXGFX_TEXTURECHANNEL_DEPTH , LUXGFX_TEXTURECHANNEL_DEPTHSTENCIL , LUXGFX_TEXTURECHANNEL_CUSTOM , LUXGFX_TEXTURECHANNEL_NATIVE , }
lxgTextureChannel_t ;
typedef enum lxgTextureDataType_e
{
    LUXGFX_TEXTUREDATA_BASE , LUXGFX_TEXTUREDATA_UNORM8 , LUXGFX_TEXTUREDATA_UNORM16 , LUXGFX_TEXTUREDATA_SNORM8 , LUXGFX_TEXTUREDATA_SNORM16 , LUXGFX_TEXTUREDATA_FLOAT16 , LUXGFX_TEXTUREDATA_FLOAT32 , LUXGFX_TEXTUREDATA_SINT8 , LUXGFX_TEXTUREDATA_UINT8 , LUXGFX_TEXTUREDATA_SINT16 , LUXGFX_TEXTUREDATA_UINT16 , LUXGFX_TEXTUREDATA_SINT32 , LUXGFX_TEXTUREDATA_UINT32 , LUXGFX_TEXTUREDATAS , LUXGFX_TEXTUREDATA_DEPTH16 , LUXGFX_TEXTUREDATA_DEPTH24 , LUXGFX_TEXTUREDATA_DEPTH32 , LUXGFX_TEXTUREDATA_DEPTH32F , LUXGFX_TEXTUREDATA_UNORM1010102 , LUXGFX_TEXTUREDATA_UINT1010102 , LUXGFX_TEXTUREDATA_FLOAT111110 , LUXGFX_TEXTUREDATA_EXP999 , LUXGFX_TEXTUREDATA_COMPRESSED , LUXGFX_TEXTUREDATA_COMPRESSED_DXT1 , LUXGFX_TEXTUREDATA_COMPRESSED_DXT3 , LUXGFX_TEXTUREDATA_COMPRESSED_DXT5 , LUXGFX_TEXTUREDATA_COMPRESSED_TC , LUXGFX_TEXTUREDATA_COMPRESSED_SIGNED_TC , LUXGFX_TEXTUREDATA_COMPRESSED_UNORM_BPTC , LUXGFX_TEXTUREDATA_COMPRESSED_FLOAT_BPTC , LUXGFX_TEXTUREDATA_COMPRESSED_SIGNED_FLOAT_BPTC , LUXGFX_TEXTUREDATA_CUSTOM , }
lxgTextureDataType_t ;
typedef struct lxgTexture_s
{
    lxGLTextureTarget_t gltarget ;
    GLuint glid ;
    lxgSamplerCPTR lastSampler ;
    uint32 lastSamplerIncarnation ;
    lxgContextPTR ctx ;
    lxgTextureChannel_t formattype ;
    lxgTextureDataType_t datatype ;
    flags32 flags ;
    int width ;
    int height ;
    int depth ;
    int arraysize ;
    int samples ;
    flags32 mipsdefined ;
    uint miplevels ;
    lxVec3i_t mipsizes [ LUXGFX_MAX_TEXTURE_MIPMAPS ] ;
    uint pixelsizes [ LUXGFX_MAX_TEXTURE_MIPMAPS ] ;
    size_t nativesizes [ LUXGFX_MAX_TEXTURE_MIPMAPS ] ;
    uint components ;
    uint componentsize ;
    lxgSampler_t sampler ;
    GLenum glinternalformat ;
    GLenum gldatatype ;
    GLenum gldataformat ;
}
lxgTexture_t ;
typedef struct lxgRenderBuffer_s
{
    GLuint glid ;
    lxgContextPTR ctx ;
    lxgTextureChannel_t formattype ;
    int width ;
    int height ;
    uint samples ;
}
lxgRenderBuffer_t ;
typedef struct lxgTextureUpdate_s
{
    lxVec3i_t from ;
    lxVec3i_t to ;
    lxVec3i_t size ;
}
lxgTextureUpdate_t ;
typedef struct lxgTextureImage_s
{
    lxgTexturePTR tex ;
    int level ;
    booln layered ;
    int layer ;
    lxGLAccessFormat_t glformat ;
    lxGLAccessMode_t glaccess ;
}
lxgTextureImage_t ;
void lxgContext_clearTextureState ( lxgContextPTR ctx ) ;
void lxgContext_setTextureSampler ( lxgContextPTR ctx , uint imageunit , flags32 what ) ;
void lxgContext_changedTextureSampler ( lxgContextPTR ctx , uint imageunit , flags32 what ) ;
void lxgContext_applyTexture ( lxgContextPTR ctx , lxgTexturePTR obj , uint imageunit ) ;
void lxgContext_applyTextures ( lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num ) ;
void lxgContext_applySampler ( lxgContextPTR ctx , lxgSamplerCPTR obj , uint imageunit ) ;
void lxgContext_applySamplers ( lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num ) ;
void lxgContext_applyTextureImages ( lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num ) ;
void lxgContext_applyTextureImage ( lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit ) ;
booln lxgTextureChannel_valid ( lxgContextPTR ctx , lxgTextureChannel_t channel ) ;
booln lxgTextureTarget_valid ( lxgContextPTR ctx , lxGLTextureTarget_t type ) ;
GLenum lxgTextureDataType_getData ( lxgTextureDataType_t data , booln rev , booln depthstencil ) ;
GLenum lxgTextureChannel_getFormat ( lxgTextureChannel_t type , booln rev , booln integer ) ;
GLenum lxgTextureChannel_getInternal ( lxgTextureChannel_t type , lxgTextureDataType_t data ) ;
void lxgTexture_init ( lxgTexturePTR tex , lxgContextPTR ctx ) ;
void lxgTexture_deinit ( lxgTexturePTR tex , lxgContextPTR ctx ) ;
booln lxgTexture_setup ( lxgTexturePTR tex , lxGLTextureTarget_t type , lxgTextureChannel_t format , lxgTextureDataType_t data , int width , int height , int depth , int arraysize , flags32 flags ) ;
booln lxgTexture_resize ( lxgTexturePTR tex , int width , int height , int depth , int arraysize ) ;
booln lxgTexture_readFrame ( lxgTexturePTR tex , lxgContextPTR ctx , const lxgTextureUpdate_t * update , uint miplevel ) ;
booln lxgTexture_readData ( lxgTexturePTR tex , const lxgTextureUpdate_t * update , uint miplevel , GLenum datatype , GLenum dataformat , const void * buffer , uint buffersize ) ;
booln lxgTexture_readBuffer ( lxgTexturePTR tex , const lxgTextureUpdate_t * update , uint miplevel , GLenum datatype , GLenum dataformat , const struct lxgBuffer_s * buffer , uint bufferoffset ) ;
booln lxgTexture_writeData ( lxgTexturePTR tex , uint side , booln ascompressed , uint mip , GLenum datatype , GLenum dataformat , void * buffer , uint buffersize ) ;
booln lxgTexture_writeBuffer ( lxgTexturePTR tex , uint side , booln ascompressed , uint mip , GLenum datatype , GLenum dataformat , lxgBufferPTR buffer , uint bufferoffset ) ;
void lxgTexture_getSampler ( lxgTextureCPTR tex , lxgSamplerPTR sampler ) ;
void lxgTexture_boundSetSampler ( lxgTexturePTR tex , lxgSamplerCPTR sampler , flags32 what ) ;
lxVec3iCPTR lxgTexture_getMipSize ( lxgTextureCPTR tex , uint mipLevel ) ;
void lxgSampler_init ( lxgSamplerPTR sampler , lxgContextPTR ctx ) ;
void lxgSampler_deinit ( lxgSamplerPTR sampler , lxgContextPTR ctx ) ;
void lxgSampler_setAddress ( lxgSamplerPTR sampler , uint n , lxgSamplerAddress_t address ) ;
void lxgSampler_setCompare ( lxgSamplerPTR sampler , enum lxGLCompareMode_t cmp ) ;
void lxgSampler_update ( lxgSamplerPTR sampler ) ;
booln lxgRenderBuffer_init ( lxgRenderBufferPTR rb , lxgContextPTR ctx , lxgTextureChannel_t format , int width , int height , int samples ) ;
booln lxgRenderBuffer_change ( lxgRenderBufferPTR rb , lxgTextureChannel_t format , int width , int height , int samples ) ;
void lxgRenderBuffer_deinit ( lxgRenderBufferPTR rb , lxgContextPTR ctx ) ;
booln lxgTextureImage_init ( lxgTextureImagePTR img , lxgContextPTR ctx , lxgTexturePTR tex , lxgAccessMode_t acces , uint level , booln layered , int layer ) ;
typedef struct lxgDepth_s
{
    bool16 enabled ;
    bool16 write ;
    lxGLCompareMode_t func ;
}
lxgDepth_t ;
typedef struct lxgLogic_s
{
    bool32 enabled ;
    lxGLLogicOp_t op ;
}
lxgLogic_t ;
typedef enum lxgColorChannel_e
{
    LUXGFX_COLOR_RED , LUXGFX_COLOR_GREEN , LUXGFX_COLOR_BLUE , LUXGFX_COLOR_ALPHA , LUXGFX_COLORS , }
lxgColorChannel_t ;
typedef struct lxgColor_s
{
    bool32 individual ;
    bool8 write [ LUXGFX_MAX_RENDERTARGETS ] [ LUXGFX_COLORS ] ;
}
lxgColor_t ;
typedef enum lxgFaceSide_e
{
    LUXGFX_FACE_FRONT , LUXGFX_FACE_BACK , LUXGFX_FACES , }
lxgFaceSide_t ;
typedef struct lxgStencilOp_s
{
    lxGLStencilMode_t fail ;
    lxGLStencilMode_t zfail ;
    lxGLStencilMode_t zpass ;
    lxGLCompareMode_t func ;
}
lxgStencilOp_t ;
typedef struct lxgStencil_s
{
    bool8 enabled ;
    flags32 write ;
    flags32 mask ;
    uint32 refvalue ;
    lxgStencilOp_t ops [ LUXGFX_FACES ] ;
}
lxgStencil_t ;
typedef struct lxgBlendMode_s
{
    lxGLBlendWeight_t srcw ;
    lxGLBlendWeight_t dstw ;
    lxGLBlendEquation_t equ ;
}
lxgBlendMode_t ;
typedef struct lxgBlendStage_s
{
    bool32 enabled ;
    lxgBlendMode_t colormode ;
    lxgBlendMode_t alphamode ;
}
lxgBlendStage_t ;
typedef struct lxgBlend_s
{
    bool16 individual ;
    bool16 separateStages ;
    lxgBlendStage_t blends [ LUXGFX_MAX_RENDERTARGETS ] ;
}
lxgBlend_t ;
typedef struct lxgRasterizer_s
{
    bool8 cull ;
    bool8 cullfront ;
    bool8 ccw ;
    enum32 fill ;
}
lxgRasterizer_t ;
typedef struct lxgRasterState_s
{
    lxgRasterizerCPTR rasterizerObj ;
    lxgColorCPTR colorObj ;
    lxgBlendCPTR blendObj ;
    lxgDepthCPTR depthObj ;
    lxgStencilCPTR stencilObj ;
    lxgLogicCPTR logicObj ;
    lxgRasterizer_t rasterizer ;
    lxgColor_t color ;
    lxgBlend_t blend ;
    lxgDepth_t depth ;
    lxgStencil_t stencil ;
    lxgLogic_t logic ;
}
lxgRasterState_t ;
void lxgRasterizer_init ( lxgRasterizerPTR obj ) ;
void lxgRasterizer_sync ( lxgRasterizerPTR obj , lxgContextPTR ctx ) ;
void lxgColor_init ( lxgColorPTR obj ) ;
void lxgColor_sync ( lxgColorPTR obj , lxgContextPTR ctx ) ;
void lxgDepth_init ( lxgDepthPTR obj ) ;
void lxgDepth_sync ( lxgDepthPTR obj , lxgContextPTR ctx ) ;
void lxgLogic_init ( lxgLogicPTR obj ) ;
void lxgLogic_sync ( lxgLogicPTR obj , lxgContextPTR ctx ) ;
void lxgStencil_init ( lxgStencilPTR obj ) ;
void lxgStencil_sync ( lxgStencilPTR obj , lxgContextPTR ctx ) ;
void lxgBlend_init ( lxgBlendPTR obj ) ;
void lxgBlend_sync ( lxgBlendPTR obj , lxgContextPTR ctx ) ;
void lxgContext_applyColor ( lxgContextPTR ctx , lxgColorCPTR obj ) ;
void lxgContext_applyDepth ( lxgContextPTR ctx , lxgDepthCPTR obj ) ;
void lxgContext_applyLogic ( lxgContextPTR ctx , lxgLogicCPTR obj ) ;
void lxgContext_applyStencil ( lxgContextPTR ctx , lxgStencilCPTR obj ) ;
void lxgContext_applyBlend ( lxgContextPTR ctx , lxgBlendCPTR obj ) ;
void lxgContext_applyRasterizer ( lxgContextPTR ctx , lxgRasterizerCPTR obj ) ;
void lxgProgramParameter_stateColor ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
void lxgProgramParameter_stateDepth ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
void lxgProgramParameter_stateLogic ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
void lxgProgramParameter_stateStencil ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
void lxgProgramParameter_stateBlend ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
void lxgProgramParameter_stateRasterizer ( lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj ) ;
typedef struct lxgFrameBounds_s
{
    int width ;
    int height ;
}
lxgFrameBounds_t ;
typedef struct lxgViewDepth_s
{
    double near ;
    double far ;
}
lxgViewDepth_t ;
typedef struct lxgViewPort_s
{
    booln scissor ;
    lxRectanglei_t scissorRect ;
    lxRectanglei_t viewRect ;
    lxgViewDepth_t depth ;
}
lxgViewPort_t ;
typedef struct lxgViewPortMrt_s
{
    uint numused ;
    flags32 scissored ;
    lxRectanglef_t bounds [ LUXGFX_MAX_RENDERTARGETS ] ;
    lxRectanglei_t scissors [ LUXGFX_MAX_RENDERTARGETS ] ;
    lxgViewDepth_t depths [ LUXGFX_MAX_RENDERTARGETS ] ;
}
lxgViewPortMrt_t ;
typedef enum lxgRenderTargetType_e
{
    LUXGFX_RENDERTARGET_DRAW , LUXGFX_RENDERTARGET_READ , LUXGFX_RENDERTARGETS }
lxgRenderTargetType_t ;
typedef struct lxgRenderAssign_s
{
    lxgTexturePTR tex ;
    lxgRenderBufferPTR rbuf ;
    uint mip ;
    uint layer ;
}
lxgRenderAssign_t ;
typedef enum lxgRenderAssignType_e
{
    LUXGFX_RENDERASSIGN_DEPTH , LUXGFX_RENDERASSIGN_STENCIL , LUXGFX_RENDERASSIGN_COLOR0 , LUXGFX_RENDERASSIGNS = LUXGFX_RENDERASSIGN_COLOR0 + LUXGFX_MAX_RENDERTARGETS , }
lxgRenderAssignType_t ;
typedef struct lxgRenderTarget_s
{
    GLuint glid ;
    lxgContextPTR ctx ;
    uint maxidx ;
    flags32 dirty ;
    lxgRenderAssign_t assigns [ LUXGFX_RENDERASSIGNS ] ;
    booln equalsized ;
    lxgFrameBounds_t bounds ;
}
lxgRenderTarget_t ;
typedef struct lxgRenderTargetBlit_s
{
    lxVec2i_t fromStart ;
    lxVec2i_t fromEnd ;
    lxVec2i_t toStart ;
    lxVec2i_t toEnd ;
}
lxgRenderTargetBlit_t ;
typedef struct lxgRenderTargetBlit_s * lxgRenderTargetBlitPTR ;
void lxgRenderTarget_init ( lxgRenderTargetPTR rt , lxgContextPTR ctx ) ;
void lxgRenderTarget_deinit ( lxgRenderTargetPTR rt , lxgContextPTR ctx ) ;
void lxgRenderTarget_applyAssigns ( lxgRenderTargetPTR rt , lxgRenderTargetType_t mode ) ;
void lxgRenderTarget_setAssign ( lxgRenderTargetPTR rt , uint assigntype , lxgRenderAssignPTR assign ) ;
booln lxgRenderTarget_checkSize ( lxgRenderTargetPTR rt ) ;
lxgFrameBoundsCPTR lxgRenderTarget_getBounds ( lxgRenderTargetPTR rt ) ;
void lxgViewPort_sync ( lxgViewPortPTR obj , lxgContextPTR ctx ) ;
void lxgViewPortMrt_sync ( lxgViewPortMrtPTR obj , lxgContextPTR ctx ) ;
void lxgContext_applyRenderTarget ( lxgContextPTR ctx , lxgRenderTargetPTR obj , lxgRenderTargetType_t type ) ;
void lxgContext_applyRenderTargetDraw ( lxgContextPTR ctx , lxgRenderTargetPTR obj , booln setViewport ) ;
void lxgContext_blitRenderTargets ( lxgContextPTR ctx , lxgRenderTargetPTR to , lxgRenderTargetPTR from , lxgRenderTargetBlitPTR update , flags32 mask , booln linearFilter ) ;
booln lxgContext_applyViewPortRect ( lxgContextPTR ctx , lxRectangleiCPTR rect ) ;
booln lxgContext_applyViewPortScissorState ( lxgContextPTR ctx , booln state ) ;
booln lxgContext_applyViewPort ( lxgContextPTR ctx , lxgViewPortPTR obj ) ;
void lxgContext_applyViewPortMrt ( lxgContextPTR ctx , lxgViewPortMrtPTR obj ) ;
enum lxgCapability_e
{
    LUXGFX_CAP_POINTSPRITE = 1 << 0 , LUXGFX_CAP_STENCILWRAP = 1 << 1 , LUXGFX_CAP_BLENDSEP = 1 << 2 , LUXGFX_CAP_OCCQUERY = 1 << 3 , LUXGFX_CAP_FBO = 1 << 4 , LUXGFX_CAP_FBOMS = 1 << 5 , LUXGFX_CAP_DEPTHFLOAT = 1 << 6 , LUXGFX_CAP_VBO = 1 << 7 , LUXGFX_CAP_PBO = 1 << 8 , LUXGFX_CAP_UBO = 1 << 9 , LUXGFX_CAP_TEX3D = 1 << 10 , LUXGFX_CAP_TEXRECT = 1 << 11 , LUXGFX_CAP_TEXNP2 = 1 << 12 , LUXGFX_CAP_TEXCUBEARRAY = 1 << 13 , LUXGFX_CAP_TEXS3TC = 1 << 14 , LUXGFX_CAP_TEXRGTC = 1 << 15 , LUXGFX_CAP_TEXRW = 1 << 16 , LUXGFX_CAP_BUFMAPRANGE = 1 << 17 , LUXGFX_CAP_BUFCOPY = 1 << 18 , LUXGFX_CAP_DEPTHCLAMP = 1 << 19 , LUXGFX_CAP_SM0 = 1 << 20 , LUXGFX_CAP_SM1 = 1 << 21 , LUXGFX_CAP_SM2 = 1 << 22 , LUXGFX_CAP_SM2EXT = 1 << 23 , LUXGFX_CAP_SM3 = 1 << 24 , LUXGFX_CAP_SM4 = 1 << 25 , LUXGFX_CAP_SM5 = 1 << 26 , }
;
typedef enum lxgGPUVendor_e
{
    LUXGFX_GPUVENDOR_UNKNOWN , LUXGFX_GPUVENDOR_NVIDIA , LUXGFX_GPUVENDOR_ATI , LUXGFX_GPUVENDOR_INTEL , }
lxgGPUVendor_t ;
typedef enum lxgGPUMode_e
{
    LUXGFX_GPUMODE_FIXED , LUXGFX_GPUMODE_ASM , LUXGFX_GPUMODE_HL , }
lxgGPUMode_t ;
typedef struct lxgCapabilites_s
{
    int texsize ;
    int texsize3d ;
    int texlayers ;
    int texunits ;
    int teximages ;
    int texcoords ;
    int texvtxunits ;
    float texaniso ;
    float pointsize ;
    int drawbuffers ;
    int viewports ;
    int fbosamples ;
}
lxgCapabilites_t ;
typedef struct lxgContext_s
{
    flags32 capbits ;
    lxgTexturePTR textures [ LUXGFX_MAX_TEXTURE_IMAGES ] ;
    lxgSamplerCPTR samplers [ LUXGFX_MAX_TEXTURE_IMAGES ] ;
    lxgTextureImageCPTR images [ LUXGFX_MAX_RWTEXTURE_IMAGES ] ;
    lxgRenderTargetPTR rendertargets [ LUXGFX_RENDERTARGETS ] ;
    lxgVertexState_t vertex ;
    lxgFeedbackState_t feedback ;
    lxgProgramState_t program ;
    lxgRasterState_t raster ;
    lxgViewPort_t viewport ;
    lxgFrameBounds_t framebounds ;
    lxgFrameBounds_t window ;
    lxgViewPortMrt_t viewportMRT ;
    lxgCapabilites_t capabilites ;
}
lxgContext_t ;
const char * lxgContext_init ( lxgContextPTR ctx ) ;
void lxgContext_syncRasterStates ( lxgContextPTR ctx ) ;
booln lxgContext_checkStates ( lxgContextPTR ctx ) ;
void lxgContext_clearVertexState ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexAttribs ( lxgContextPTR ctx , flags32 attribs , flags32 changed ) ;
void lxgContext_applyVertexAttribsFIXED ( lxgContextPTR ctx , flags32 attribs , flags32 changed ) ;
void lxgContext_applyVertexState ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateFIXED ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateNV ( lxgContextPTR ctx ) ;
void lxgContext_applyVertexStateFIXEDNV ( lxgContextPTR ctx ) ;
void lxgContext_setVertexDecl ( lxgContextPTR ctx , lxgVertexDeclCPTR decl ) ;
void lxgContext_setVertexDeclStreams ( lxgContextPTR ctx , lxgVertexDeclCPTR decl , lxgStreamHostCPTR hosts ) ;
void lxgContext_setVertexStream ( lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host ) ;
void lxgContext_invalidateVertexStreams ( lxgContextPTR ctx ) ;
void lxgContext_clearFeedbackState ( lxgContextPTR ctx ) ;
void lxgContext_applyFeedbackStreams ( lxgContextPTR ctx ) ;
void lxgContext_setFeedbackStreams ( lxgContextPTR ctx , lxgStreamHostCPTR hosts , int numStreams ) ;
void lxgContext_setFeedbackStream ( lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host ) ;
void lxgContext_enableFeedback ( lxgContextPTR ctx , lxGLPrimitiveType_t type , int numStreams ) ;
void lxgContext_disableFeedback ( lxgContextPTR ctx ) ;
void lxgContext_clearProgramState ( lxgContextPTR ctx ) ;
void lxgContext_applyProgram ( lxgContextPTR ctx , lxgProgramCPTR prog ) ;
void lxgContext_applyProgramParameters ( lxgContextPTR ctx , lxgProgramCPTR prog , uint num , lxgProgramParameterPTR * params , const void * * data ) ;
void lxgContext_updateProgramSubroutines ( lxgContextPTR ctx , lxgProgramCPTR prog ) ;
void lxgContext_clearTextureState ( lxgContextPTR ctx ) ;
void lxgContext_setTextureSampler ( lxgContextPTR ctx , uint imageunit , flags32 what ) ;
void lxgContext_changedTextureSampler ( lxgContextPTR ctx , uint imageunit , flags32 what ) ;
void lxgContext_applyTexture ( lxgContextPTR ctx , lxgTexturePTR obj , uint imageunit ) ;
void lxgContext_applyTextures ( lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num ) ;
void lxgContext_applySampler ( lxgContextPTR ctx , lxgSamplerCPTR obj , uint imageunit ) ;
void lxgContext_applySamplers ( lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num ) ;
void lxgContext_applyTextureImages ( lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num ) ;
void lxgContext_applyTextureImage ( lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit ) ;
void lxgContext_clearRasterState ( lxgContextPTR ctx ) ;
void lxgContext_applyDepth ( lxgContextPTR ctx , lxgDepthCPTR obj ) ;
void lxgContext_applyLogic ( lxgContextPTR ctx , lxgLogicCPTR obj ) ;
void lxgContext_applyStencil ( lxgContextPTR ctx , lxgStencilCPTR obj ) ;
void lxgContext_applyBlend ( lxgContextPTR ctx , lxgBlendCPTR obj ) ;
void lxgContext_applyColor ( lxgContextPTR ctx , lxgColorCPTR obj ) ;
void lxgContext_applyRasterizer ( lxgContextPTR ctx , lxgRasterizerCPTR obj ) ;
void lxgContext_blitRenderTargets ( lxgContextPTR ctx , lxgRenderTargetPTR to , lxgRenderTargetPTR from , lxgRenderTargetBlitPTR update , flags32 mask , booln linearFilter ) ;
booln lxgContext_applyViewPortRect ( lxgContextPTR ctx , lxRectangleiCPTR rect ) ;
booln lxgContext_applyViewPortScissorState ( lxgContextPTR ctx , booln state ) ;
booln lxgContext_applyViewPort ( lxgContextPTR ctx , lxgViewPortPTR obj ) ;
void lxgContext_applyViewPortMrt ( lxgContextPTR ctx , lxgViewPortMrtPTR obj ) ;
void lxgContext_applyRenderTarget ( lxgContextPTR ctx , lxgRenderTargetPTR obj , lxgRenderTargetType_t type ) ;
void lxgContext_applyRenderTargetDraw ( lxgContextPTR ctx , lxgRenderTargetPTR obj , booln setViewport ) ;
void lxgContext_checkedBlend ( lxgContextPTR ctx , lxgBlendCPTR obj ) ;
void lxgContext_checkedColor ( lxgContextPTR ctx , lxgColorCPTR obj ) ;
void lxgContext_checkedDepth ( lxgContextPTR ctx , lxgDepthCPTR obj ) ;
void lxgContext_checkedLogic ( lxgContextPTR ctx , lxgLogicCPTR obj ) ;
void lxgContext_checkedStencil ( lxgContextPTR ctx , lxgStencilCPTR obj ) ;
void lxgContext_checkedRasterizer ( lxgContextPTR ctx , lxgRasterizerCPTR obj ) ;
void lxgContext_checkedTexture ( lxgContextPTR ctx , lxgTexturePTR tex , uint imageunit ) ;
void lxgContext_checkedSampler ( lxgContextPTR ctx , lxgSamplerCPTR samp , uint imageunit ) ;
void lxgContext_checkedTextureImage ( lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit ) ;
void lxgContext_checkedTextures ( lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num ) ;
void lxgContext_checkedSamplers ( lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num ) ;
void lxgContext_checkedTextureImages ( lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num ) ;
void lxgContext_checkedRenderFlag ( lxgContextPTR ctx , flags32 needed ) ;
void lxgContext_checkedVertexDecl ( lxgContextPTR ctx , lxgVertexDeclCPTR decl ) ;
void lxgContext_checkedVertexAttrib ( lxgContextPTR ctx , flags32 needed ) ;
void lxgContext_checkedVertexAttribFIXED ( lxgContextPTR ctx , flags32 needed ) ;
void lxgContext_checkedRenderTarget ( lxgContextPTR ctx , lxgRenderTargetPTR rt , lxgRenderTargetType_t type ) ;
void lxgContext_checkedProgram ( lxgContextPTR ctx , lxgProgramPTR prog ) ;
void lxgContext_checkedVertex ( lxgContextPTR ctx ) ;
void lxgContext_checkedVertexNV ( lxgContextPTR ctx ) ;
void lxgContext_checkedVertexFIXED ( lxgContextPTR ctx ) ;
void lxgContext_checkedVertexFIXEDNV ( lxgContextPTR ctx ) ;
void lxgContext_checkedViewPortScissor ( lxgContextPTR ctx , lxRectangleiCPTR rect ) ;
void lxgContext_checkedTextureSampler ( lxgContextPTR ctx , uint imageunit ) ;
booln lxgContext_setProgramBuffer ( lxgContextPTR ctx , uint idx , lxgBufferCPTR buffer ) ;
]]  
--auto-generated api from ffi headers
local api =
  {
  ["LUXGFX_ACCESS_READ"] = { type ='value', },
  ["LUXGFX_ACCESS_WRITE"] = { type ='value', },
  ["LUXGFX_ACCESS_READWRITE"] = { type ='value', },
  ["LUXGFX_ACCESS_WRITEDISCARD"] = { type ='value', },
  ["LUXGFX_ACCESS_WRITEDISCARDALL"] = { type ='value', },
  ["LUXGFX_ACCESSES"] = { type ='value', },
  ["LUXGFX_MAX_TEXTURE_IMAGES"] = { type ='value', },
  ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', },
  ["LUXGFX_MAX_RWTEXTURE_IMAGES"] = { type ='value', },
  ["LUXGFX_MAX_STAGE_BUFFERS"] = { type ='value', },
  ["LUXGFX_MAX_TEXTURE_MIPMAPS"] = { type ='value', },
  ["LUXGFX_MAX_VERTEX_STREAMS"] = { type ='value', },
  ["LUXGFX_MAX_STAGE_SUBROUTINES"] = { type ='value', },
  ["LUXGL_COMPARE_NEVER"] = { type ='value', },
  ["LUXGL_COMPARE_LESS"] = { type ='value', },
  ["LUXGL_COMPARE_EQUAL"] = { type ='value', },
  ["LUXGL_COMPARE_LEQUAL"] = { type ='value', },
  ["LUXGL_COMPARE_GREATER"] = { type ='value', },
  ["LUXGL_COMPARE_NOTEQUAL"] = { type ='value', },
  ["LUXGL_COMPARE_GEQUAL"] = { type ='value', },
  ["LUXGL_COMPARE_ALWAYS"] = { type ='value', },
  ["LUXGL_COMPARE_DONTEXECUTE"] = { type ='value', },
  ["LUXGL_STATIC_DRAW"] = { type ='value', },
  ["LUXGL_STATIC_READ"] = { type ='value', },
  ["LUXGL_STATIC_COPY"] = { type ='value', },
  ["LUXGL_DYNAMIC_DRAW"] = { type ='value', },
  ["LUXGL_DYNAMIC_READ"] = { type ='value', },
  ["LUXGL_DYNAMIC_COPY"] = { type ='value', },
  ["LUXGL_STREAM_DRAW"] = { type ='value', },
  ["LUXGL_STREAM_READ"] = { type ='value', },
  ["LUXGL_STREAM_COPY"] = { type ='value', },
  ["LUXGL_STENCIL_KEEP"] = { type ='value', },
  ["LUXGL_STENCIL_ZERO"] = { type ='value', },
  ["LUXGL_STENCIL_REPLACE"] = { type ='value', },
  ["LUXGL_STENCIL_INCR_SAT"] = { type ='value', },
  ["LUXGL_STENCIL_DECR_SAT"] = { type ='value', },
  ["LUXGL_STENCIL_INVERT"] = { type ='value', },
  ["LUXGL_STENCIL_INCR"] = { type ='value', },
  ["LUXGL_STENCIL_DECR"] = { type ='value', },
  ["LUXGL_BLENDW_ZERO"] = { type ='value', },
  ["LUXGL_BLENDW_ONE"] = { type ='value', },
  ["LUXGL_BLENDW_RGB_SRC"] = { type ='value', },
  ["LUXGL_BLENDW_RGB_DST"] = { type ='value', },
  ["LUXGL_BLENDW_A_SRC"] = { type ='value', },
  ["LUXGL_BLENDW_A_DST"] = { type ='value', },
  ["LUXGL_BLENDW_INVRGB_SRC"] = { type ='value', },
  ["LUXGL_BLENDW_INVRGB_DST"] = { type ='value', },
  ["LUXGL_BLENDW_INVA_SRC"] = { type ='value', },
  ["LUXGL_BLENDW_INVA_DST"] = { type ='value', },
  ["LUXGL_BLENDW_UNKOWN"] = { type ='value', },
  ["LUXGL_BLENDE_ADD"] = { type ='value', },
  ["LUXGL_BLENDE_SUB"] = { type ='value', },
  ["LUXGL_BLENDE_SUB_REV"] = { type ='value', },
  ["LUXGL_BLENDE_MIN"] = { type ='value', },
  ["LUXGL_BLENDE_MAX"] = { type ='value', },
  ["LUXGL_BLENDE_UNKOWN"] = { type ='value', },
  ["LUXGL_LOGICOP_CLEAR"] = { type ='value', },
  ["LUXGL_LOGICOP_SET"] = { type ='value', },
  ["LUXGL_LOGICOP_COPY"] = { type ='value', },
  ["LUXGL_LOGICOP_INVERTED"] = { type ='value', },
  ["LUXGL_LOGICOP_NOOP"] = { type ='value', },
  ["LUXGL_LOGICOP_INVERT"] = { type ='value', },
  ["LUXGL_LOGICOP_AND"] = { type ='value', },
  ["LUXGL_LOGICOP_NAND"] = { type ='value', },
  ["LUXGL_LOGICOP_OR"] = { type ='value', },
  ["LUXGL_LOGICOP_NOR"] = { type ='value', },
  ["LUXGL_LOGICOP_XOR"] = { type ='value', },
  ["LUXGL_LOGICOP_EQUIV"] = { type ='value', },
  ["LUXGL_LOGICOP_AND_REVERSE"] = { type ='value', },
  ["LUXGL_LOGICOP_AND_INVERTED"] = { type ='value', },
  ["LUXGL_LOGICOP_OR_REVERSE"] = { type ='value', },
  ["LUXGL_LOGICOP_OR_INVERTED"] = { type ='value', },
  ["LUXGL_LOGICOP_ILLEGAL"] = { type ='value', },
  ["LUXGL_POINTS"] = { type ='value', },
  ["LUXGL_TRIANGLES"] = { type ='value', },
  ["LUXGL_TRIANGLE_STRIP"] = { type ='value', },
  ["LUXGL_TRIANGLE_FAN"] = { type ='value', },
  ["LUXGL_LINES"] = { type ='value', },
  ["LUXGL_LINE_LOOP"] = { type ='value', },
  ["LUXGL_LINE_STRIP"] = { type ='value', },
  ["LUXGL_QUADS"] = { type ='value', },
  ["LUXGL_QUAD_STRIP"] = { type ='value', },
  ["LUXGL_LINE_ADJ"] = { type ='value', },
  ["LUXGL_LINE_STRIP_ADJ"] = { type ='value', },
  ["LUXGL_TRIANGLE_STRIP_ADJ"] = { type ='value', },
  ["LUXGL_TRIANGLE_ADJ"] = { type ='value', },
  ["LUXGL_PATCHES"] = { type ='value', },
  ["LUXGL_POLYGON"] = { type ='value', },
  ["LUXGL_TEXTURE_1D"] = { type ='value', },
  ["LUXGL_TEXTURE_2D"] = { type ='value', },
  ["LUXGL_TEXTURE_3D"] = { type ='value', },
  ["LUXGL_TEXTURE_RECT"] = { type ='value', },
  ["LUXGL_TEXTURE_1DARRAY"] = { type ='value', },
  ["LUXGL_TEXTURE_2DARRAY"] = { type ='value', },
  ["LUXGL_TEXTURE_CUBE"] = { type ='value', },
  ["LUXGL_TEXTURE_CUBEARRAY"] = { type ='value', },
  ["LUXGL_TEXTURE_2DMS"] = { type ='value', },
  ["LUXGL_TEXTURE_2DMSARRAY"] = { type ='value', },
  ["LUXGL_TEXTURE_BUFFER"] = { type ='value', },
  ["LUXGL_TEXTURE_RENDERBUFFER"] = { type ='value', },
  ["LUXGL_TEXTURE_INVALID"] = { type ='value', },
  ["LUXGL_BUFFER_VERTEX"] = { type ='value', },
  ["LUXGL_BUFFER_INDEX"] = { type ='value', },
  ["LUXGL_BUFFER_PIXELWRITE"] = { type ='value', },
  ["LUXGL_BUFFER_PIXELREAD"] = { type ='value', },
  ["LUXGL_BUFFER_UNIFORM"] = { type ='value', },
  ["LUXGL_BUFFER_TEXTURE"] = { type ='value', },
  ["LUXGL_BUFFER_FEEDBACK"] = { type ='value', },
  ["LUXGL_BUFFER_CPYWRITE"] = { type ='value', },
  ["LUXGL_BUFFER_CPYREAD"] = { type ='value', },
  ["LUXGL_BUFFER_DRAWINDIRECT"] = { type ='value', },
  ["LUXGL_BUFFER_NVVIDEO"] = { type ='value', },
  ["LUXGL_BUFFER_NVPARAM_VERTEX"] = { type ='value', },
  ["LUXGL_BUFFER_NVPARAM_GEOMETRY"] = { type ='value', },
  ["LUXGL_BUFFER_NVPARAM_FRAGMENT"] = { type ='value', },
  ["LUXGL_BUFFER_NVPARAM_TESSCTRL"] = { type ='value', },
  ["LUXGL_BUFFER_NVPARAM_TESSEVAL"] = { type ='value', },
  ["LUXGL_BUFFER_INVALID"] = { type ='value', },
  ["LUXGL_SHADER_VERTEX"] = { type ='value', },
  ["LUXGL_SHADER_FRAGMENT"] = { type ='value', },
  ["LUXGL_SHADER_GEOMETRY"] = { type ='value', },
  ["LUXGL_SHADER_TESSCTRL"] = { type ='value', },
  ["LUXGL_SHADER_TESSEVAL"] = { type ='value', },
  ["LUXGL_PROGRAM_VERTEX"] = { type ='value', },
  ["LUXGL_PROGRAM_FRAGMENT"] = { type ='value', },
  ["LUXGL_PROGRAM_GEOMETRY"] = { type ='value', },
  ["LUXGL_PROGRAM_TESSCTRL"] = { type ='value', },
  ["LUXGL_PROGRAM_TESSEVAL"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R8UI"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R8I"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R16UI"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R16I"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R32UI"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R32I"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_R32F"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RG32UI"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RG32I"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RG32F"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RGBA32UI"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RGBA32I"] = { type ='value', },
  ["LUXGL_ACCESSFORMAT_RGBA32F"] = { type ='value', },
  ["LUXGL_ACCESS_READ_ONLY"] = { type ='value', },
  ["LUXGL_ACCESS_WRITE_ONLY"] = { type ='value', },
  ["LUXGL_ACCESS_READ_WRITE"] = { type ='value', },
  ["LUXGL_PARAM_FLOAT"] = { type ='value', },
  ["LUXGL_PARAM_FLOAT2"] = { type ='value', },
  ["LUXGL_PARAM_FLOAT3"] = { type ='value', },
  ["LUXGL_PARAM_FLOAT4"] = { type ='value', },
  ["LUXGL_PARAM_INT"] = { type ='value', },
  ["LUXGL_PARAM_INT2"] = { type ='value', },
  ["LUXGL_PARAM_INT3"] = { type ='value', },
  ["LUXGL_PARAM_INT4"] = { type ='value', },
  ["LUXGL_PARAM_UINT"] = { type ='value', },
  ["LUXGL_PARAM_UINT2"] = { type ='value', },
  ["LUXGL_PARAM_UINT3"] = { type ='value', },
  ["LUXGL_PARAM_UINT4"] = { type ='value', },
  ["LUXGL_PARAM_BOOL"] = { type ='value', },
  ["LUXGL_PARAM_BOOL2"] = { type ='value', },
  ["LUXGL_PARAM_BOOL3"] = { type ='value', },
  ["LUXGL_PARAM_BOOL4"] = { type ='value', },
  ["LUXGL_PARAM_MAT2"] = { type ='value', },
  ["LUXGL_PARAM_MAT3"] = { type ='value', },
  ["LUXGL_PARAM_MAT4"] = { type ='value', },
  ["LUXGL_PARAM_MAT2x3"] = { type ='value', },
  ["LUXGL_PARAM_MAT2x4"] = { type ='value', },
  ["LUXGL_PARAM_MAT3x2"] = { type ='value', },
  ["LUXGL_PARAM_MAT3x4"] = { type ='value', },
  ["LUXGL_PARAM_MAT4x2"] = { type ='value', },
  ["LUXGL_PARAM_MAT4x3"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_1D"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2D"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_3D"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_1D"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_2D"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_3D"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_ISAMPLER_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_1D"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_2D"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_3D"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_USAMPLER_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_1D_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2D_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_CUBE_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DRECT_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_1DARRAY_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_2DARRAY_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_SAMPLER_CUBEARRAY_SHADOW"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_1D"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_2D"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_3D"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IMAGE_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_1D"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_2D"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_3D"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_IIMAGE_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_1D"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_2D"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_3D"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_CUBE"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_2DRECT"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_2DMS"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_1DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_2DARRAY"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_CUBEARRAY"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_2DMSARRAY"] = { type ='value', },
  ["LUXGL_PARAM_UIMAGE_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_GPU_ADDRESS"] = { type ='value', },
  ["LUXGL_PARAM_BUFFER"] = { type ='value', },
  ["LUXGL_PARAM_SUBROUTINE"] = { type ='value', },
  ["LUXGL_PARAM_USER"] = { type ='value', },
  ["LUXGL_ERROR_NONE"] = { type ='value', },
  ["LUXGL_ERROR_OP"] = { type ='value', },
  ["LUXGL_ERROR_ENUM"] = { type ='value', },
  ["LUXGL_ERROR_VALUE"] = { type ='value', },
  ["LUXGL_ERROR_INDEX"] = { type ='value', },
  ["LUXGL_ERROR_FBOP"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_POS"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR1"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_NORMAL"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_COLOR"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR4"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR5"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR6"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR7"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_TEXCOORD0"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_TEXCOORD1"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_TEXCOORD2"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_TEXCOORD3"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR12"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR13"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR14"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIB_ATTR15"] = { type ='value', },
  ["LUXGFX_VERTEX_ATTRIBS"] = { type ='value', },
  ["LUXGFX_SAMPLERFILTER_NEAREST"] = { type ='value', },
  ["LUXGFX_SAMPLERFILTER_LINEAR"] = { type ='value', },
  ["LUXGFX_SAMPLERFILTER_MIPMAP_NEAREST"] = { type ='value', },
  ["LUXGFX_SAMPLERFILTER_MIPMAP_LINEAR"] = { type ='value', },
  ["LUXGFX_SAMPLERFILTERS"] = { type ='value', },
  ["LUXGFX_SAMPLERADDRESS_REPEAT"] = { type ='value', },
  ["LUXGFX_SAMPLERADDRESS_MIRROR"] = { type ='value', },
  ["LUXGFX_SAMPLERADDRESS_CLAMP"] = { type ='value', },
  ["LUXGFX_SAMPLERADDRESS_BORDER"] = { type ='value', },
  ["LUXGFX_SAMPLERADDRESSES"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_FILTER"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_CMP"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_ADDRESS"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_ANISO"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_LOD"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_BORDER"] = { type ='value', },
  ["LUXGFX_SAMPLERATTRIB_ALL"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_AUTOMIPMAP"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_MANMIPMAP"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_COMPRESS"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_COMPRESSED"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_SAMPLESFIXED"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_HASLOD"] = { type ='value', },
  ["LUXGFX_TEXTUREFLAG_HASCOMPARE"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_RGB"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_RGBA"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_R"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_RG"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_SRGB"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_SRGBA"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_DEPTH"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_DEPTHSTENCIL"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_CUSTOM"] = { type ='value', },
  ["LUXGFX_TEXTURECHANNEL_NATIVE"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_BASE"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UNORM8"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UNORM16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_SNORM8"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_SNORM16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_FLOAT16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_FLOAT32"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_SINT8"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UINT8"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_SINT16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UINT16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_SINT32"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UINT32"] = { type ='value', },
  ["LUXGFX_TEXTUREDATAS"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_DEPTH16"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_DEPTH24"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_DEPTH32"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_DEPTH32F"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UNORM1010102"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_UINT1010102"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_FLOAT111110"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_EXP999"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_DXT1"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_DXT3"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_DXT5"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_TC"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_SIGNED_TC"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_UNORM_BPTC"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_FLOAT_BPTC"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_COMPRESSED_SIGNED_FLOAT_BPTC"] = { type ='value', },
  ["LUXGFX_TEXTUREDATA_CUSTOM"] = { type ='value', },
  ["LUXGFX_COLOR_RED"] = { type ='value', },
  ["LUXGFX_COLOR_GREEN"] = { type ='value', },
  ["LUXGFX_COLOR_BLUE"] = { type ='value', },
  ["LUXGFX_COLOR_ALPHA"] = { type ='value', },
  ["LUXGFX_COLORS"] = { type ='value', },
  ["LUXGFX_FACE_FRONT"] = { type ='value', },
  ["LUXGFX_FACE_BACK"] = { type ='value', },
  ["LUXGFX_FACES"] = { type ='value', },
  ["LUXGFX_RENDERTARGET_DRAW"] = { type ='value', },
  ["LUXGFX_RENDERTARGET_READ"] = { type ='value', },
  ["LUXGFX_RENDERTARGETS"] = { type ='value', },
  ["LUXGFX_RENDERASSIGN_DEPTH"] = { type ='value', },
  ["LUXGFX_RENDERASSIGN_STENCIL"] = { type ='value', },
  ["LUXGFX_RENDERASSIGN_COLOR0"] = { type ='value', },
  ["LUXGFX_RENDERASSIGNS"] = { type ='value', },
  ["LUXGFX_CAP_POINTSPRITE"] = { type ='value', },
  ["LUXGFX_CAP_STENCILWRAP"] = { type ='value', },
  ["LUXGFX_CAP_BLENDSEP"] = { type ='value', },
  ["LUXGFX_CAP_OCCQUERY"] = { type ='value', },
  ["LUXGFX_CAP_FBO"] = { type ='value', },
  ["LUXGFX_CAP_FBOMS"] = { type ='value', },
  ["LUXGFX_CAP_DEPTHFLOAT"] = { type ='value', },
  ["LUXGFX_CAP_VBO"] = { type ='value', },
  ["LUXGFX_CAP_PBO"] = { type ='value', },
  ["LUXGFX_CAP_UBO"] = { type ='value', },
  ["LUXGFX_CAP_TEX3D"] = { type ='value', },
  ["LUXGFX_CAP_TEXRECT"] = { type ='value', },
  ["LUXGFX_CAP_TEXNP2"] = { type ='value', },
  ["LUXGFX_CAP_TEXCUBEARRAY"] = { type ='value', },
  ["LUXGFX_CAP_TEXS3TC"] = { type ='value', },
  ["LUXGFX_CAP_TEXRGTC"] = { type ='value', },
  ["LUXGFX_CAP_TEXRW"] = { type ='value', },
  ["LUXGFX_CAP_BUFMAPRANGE"] = { type ='value', },
  ["LUXGFX_CAP_BUFCOPY"] = { type ='value', },
  ["LUXGFX_CAP_DEPTHCLAMP"] = { type ='value', },
  ["LUXGFX_CAP_SM0"] = { type ='value', },
  ["LUXGFX_CAP_SM1"] = { type ='value', },
  ["LUXGFX_CAP_SM2"] = { type ='value', },
  ["LUXGFX_CAP_SM2EXT"] = { type ='value', },
  ["LUXGFX_CAP_SM3"] = { type ='value', },
  ["LUXGFX_CAP_SM4"] = { type ='value', },
  ["LUXGFX_CAP_SM5"] = { type ='value', },
  ["LUXGFX_GPUVENDOR_UNKNOWN"] = { type ='value', },
  ["LUXGFX_GPUVENDOR_NVIDIA"] = { type ='value', },
  ["LUXGFX_GPUVENDOR_ATI"] = { type ='value', },
  ["LUXGFX_GPUVENDOR_INTEL"] = { type ='value', },
  ["LUXGFX_GPUMODE_FIXED"] = { type ='value', },
  ["LUXGFX_GPUMODE_ASM"] = { type ='value', },
  ["LUXGFX_GPUMODE_HL"] = { type ='value', },
  ["lxgBuffer_alloc"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , uint needed , uint padsize)", },
  ["lxgBuffer_bind"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferCPTR buffer , lxGLBufferTarget_t type)", },
  ["lxgBuffer_bindIndexed"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferCPTR buffer , lxGLBufferTarget_t type , uint idx)", },
  ["lxgBuffer_bindRanged"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferCPTR buffer , lxGLBufferTarget_t type , uint idx , size_t offset , size_t size)", },
  ["lxgBuffer_map"] = { type ='function', 
    description = "", 
    returns = "(void *)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , lxgAccessMode_t type , booln * succ)", },
  ["lxgBuffer_mapRange"] = { type ='function', 
    description = "", 
    returns = "(void *)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , uint from , uint length , lxgAccessMode_t type , booln manualflush , booln unsynch , booln * succ)", },
  ["lxgBuffer_flushRange"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , uint from , uint length)", },
  ["lxgBuffer_unmap"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer)", },
  ["lxgBuffer_copy"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , uint bufferoffset , lxgBufferPTR src , uint srcoffset , uint size)", },
  ["lxgBuffer_addressNV"] = { type ='function', 
    description = "", 
    returns = "(GLuint64)",
    valuetype = nil,
    args = "(lxgBufferPTR buffer)", },
  ["lxgBuffer_residentNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , lxgAccessMode_t mode)", },
  ["lxgBuffer_unresidentNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferPTR buffer)", },
  ["lxgBuffer_deinit"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , lxgContextPTR ctx)", },
  ["lxgBuffer_reset"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , void * data)", },
  ["lxgBuffer_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBufferPTR buffer , lxgContextPTR ctx , lxGLBufferHint_t hint , uint size , void * data)", },
  ["lxgVertexAttrib_bit"] = { type ='function', 
    description = "", 
    returns = "(flags32)",
    valuetype = nil,
    args = "(lxgVertexAttrib_t attrib)", },
  ["lxgVertexElement_set"] = { type ='function', 
    description = "", 
    returns = "(lxgVertexElement_t)",
    valuetype = "lxg.lxgVertexElement_t",
    args = "(uint cnt , enum lxScalarType_e type , booln normalize , booln integer , uint stride , uint offset , uint stream)", },
  ["lxgVertexAttrib_applyFloat"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgVertexAttrib_t attrib , const float * vec4)", },
  ["lxgVertexAttrib_applyInteger"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgVertexAttrib_t attrib , const int * vec4)", },
  ["lxgVertexAttrib_applyFloatFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgVertexAttrib_t attrib , const float * vec4)", },
  ["lxgContext_applyVertexAttribs"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 attribs , flags32 changed)", },
  ["lxgContext_applyVertexAttribsFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 attribs , flags32 changed)", },
  ["lxgContext_clearVertexState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setVertexDecl"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgVertexDeclCPTR decl)", },
  ["lxgContext_setVertexDeclStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgVertexDeclCPTR decl , lxgStreamHostCPTR hosts)", },
  ["lxgContext_setVertexStream"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host)", },
  ["lxgContext_invalidateVertexStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateFIXEDNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_clearFeedbackState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyFeedbackStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setFeedbackStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgStreamHostCPTR hosts , int numStreams)", },
  ["lxgContext_setFeedbackStream"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host)", },
  ["lxgContext_enableFeedback"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxGLPrimitiveType_t type , int numStreams)", },
  ["lxgContext_disableFeedback"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_clearTextureState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setTextureSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint imageunit , flags32 what)", },
  ["lxgContext_changedTextureSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint imageunit , flags32 what)", },
  ["lxgContext_applyTexture"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR obj , uint imageunit)", },
  ["lxgContext_applyTextures"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num)", },
  ["lxgContext_applySampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR obj , uint imageunit)", },
  ["lxgContext_applySamplers"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num)", },
  ["lxgContext_applyTextureImages"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num)", },
  ["lxgContext_applyTextureImage"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit)", },
  ["lxgTextureChannel_valid"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureChannel_t channel)", },
  ["lxgTextureTarget_valid"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxGLTextureTarget_t type)", },
  ["lxgTextureDataType_getData"] = { type ='function', 
    description = "", 
    returns = "(GLenum)",
    valuetype = nil,
    args = "(lxgTextureDataType_t data , booln rev , booln depthstencil)", },
  ["lxgTextureChannel_getFormat"] = { type ='function', 
    description = "", 
    returns = "(GLenum)",
    valuetype = nil,
    args = "(lxgTextureChannel_t type , booln rev , booln integer)", },
  ["lxgTextureChannel_getInternal"] = { type ='function', 
    description = "", 
    returns = "(GLenum)",
    valuetype = nil,
    args = "(lxgTextureChannel_t type , lxgTextureDataType_t data)", },
  ["lxgTexture_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgTexturePTR tex , lxgContextPTR ctx)", },
  ["lxgTexture_deinit"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgTexturePTR tex , lxgContextPTR ctx)", },
  ["lxgTexture_setup"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , lxGLTextureTarget_t type , lxgTextureChannel_t format , lxgTextureDataType_t data , int width , int height , int depth , int arraysize , flags32 flags)", },
  ["lxgTexture_resize"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , int width , int height , int depth , int arraysize)", },
  ["lxgTexture_readFrame"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , lxgContextPTR ctx , const lxgTextureUpdate_t * update , uint miplevel)", },
  ["lxgTexture_readData"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , const lxgTextureUpdate_t * update , uint miplevel , GLenum datatype , GLenum dataformat , const void * buffer , uint buffersize)", },
  ["lxgTexture_readBuffer"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , const lxgTextureUpdate_t * update , uint miplevel , GLenum datatype , GLenum dataformat , const struct lxgBuffer_s * buffer , uint bufferoffset)", },
  ["lxgTexture_writeData"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , uint side , booln ascompressed , uint mip , GLenum datatype , GLenum dataformat , void * buffer , uint buffersize)", },
  ["lxgTexture_writeBuffer"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTexturePTR tex , uint side , booln ascompressed , uint mip , GLenum datatype , GLenum dataformat , lxgBufferPTR buffer , uint bufferoffset)", },
  ["lxgTexture_getSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgTextureCPTR tex , lxgSamplerPTR sampler)", },
  ["lxgTexture_boundSetSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgTexturePTR tex , lxgSamplerCPTR sampler , flags32 what)", },
  ["lxgTexture_getMipSize"] = { type ='function', 
    description = "", 
    returns = "(lxVec3iCPTR)",
    valuetype = nil,
    args = "(lxgTextureCPTR tex , uint mipLevel)", },
  ["lxgSampler_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgSamplerPTR sampler , lxgContextPTR ctx)", },
  ["lxgSampler_deinit"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgSamplerPTR sampler , lxgContextPTR ctx)", },
  ["lxgSampler_setAddress"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgSamplerPTR sampler , uint n , lxgSamplerAddress_t address)", },
  ["lxgSampler_setCompare"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgSamplerPTR sampler , enum lxGLCompareMode_t cmp)", },
  ["lxgSampler_update"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgSamplerPTR sampler)", },
  ["lxgRenderBuffer_init"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgRenderBufferPTR rb , lxgContextPTR ctx , lxgTextureChannel_t format , int width , int height , int samples)", },
  ["lxgRenderBuffer_change"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgRenderBufferPTR rb , lxgTextureChannel_t format , int width , int height , int samples)", },
  ["lxgRenderBuffer_deinit"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRenderBufferPTR rb , lxgContextPTR ctx)", },
  ["lxgTextureImage_init"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgTextureImagePTR img , lxgContextPTR ctx , lxgTexturePTR tex , lxgAccessMode_t acces , uint level , booln layered , int layer)", },
  ["lxgRasterizer_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRasterizerPTR obj)", },
  ["lxgRasterizer_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRasterizerPTR obj , lxgContextPTR ctx)", },
  ["lxgColor_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgColorPTR obj)", },
  ["lxgColor_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgColorPTR obj , lxgContextPTR ctx)", },
  ["lxgDepth_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgDepthPTR obj)", },
  ["lxgDepth_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgDepthPTR obj , lxgContextPTR ctx)", },
  ["lxgLogic_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgLogicPTR obj)", },
  ["lxgLogic_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgLogicPTR obj , lxgContextPTR ctx)", },
  ["lxgStencil_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgStencilPTR obj)", },
  ["lxgStencil_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgStencilPTR obj , lxgContextPTR ctx)", },
  ["lxgBlend_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBlendPTR obj)", },
  ["lxgBlend_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgBlendPTR obj , lxgContextPTR ctx)", },
  ["lxgContext_applyColor"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgColorCPTR obj)", },
  ["lxgContext_applyDepth"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgDepthCPTR obj)", },
  ["lxgContext_applyLogic"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgLogicCPTR obj)", },
  ["lxgContext_applyStencil"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgStencilCPTR obj)", },
  ["lxgContext_applyBlend"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgBlendCPTR obj)", },
  ["lxgContext_applyRasterizer"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRasterizerCPTR obj)", },
  ["lxgProgramParameter_stateColor"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgProgramParameter_stateDepth"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgProgramParameter_stateLogic"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgProgramParameter_stateStencil"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgProgramParameter_stateBlend"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgProgramParameter_stateRasterizer"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgProgramParameterPTR param , lxgContextPTR ctx , const void * obj)", },
  ["lxgRenderTarget_init"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRenderTargetPTR rt , lxgContextPTR ctx)", },
  ["lxgRenderTarget_deinit"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRenderTargetPTR rt , lxgContextPTR ctx)", },
  ["lxgRenderTarget_applyAssigns"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRenderTargetPTR rt , lxgRenderTargetType_t mode)", },
  ["lxgRenderTarget_setAssign"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgRenderTargetPTR rt , uint assigntype , lxgRenderAssignPTR assign)", },
  ["lxgRenderTarget_checkSize"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgRenderTargetPTR rt)", },
  ["lxgRenderTarget_getBounds"] = { type ='function', 
    description = "", 
    returns = "(lxgFrameBoundsCPTR)",
    valuetype = "lxg.lxgFrameBounds_t",
    args = "(lxgRenderTargetPTR rt)", },
  ["lxgViewPort_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgViewPortPTR obj , lxgContextPTR ctx)", },
  ["lxgViewPortMrt_sync"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgViewPortMrtPTR obj , lxgContextPTR ctx)", },
  ["lxgContext_applyRenderTarget"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR obj , lxgRenderTargetType_t type)", },
  ["lxgContext_applyRenderTargetDraw"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR obj , booln setViewport)", },
  ["lxgContext_blitRenderTargets"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR to , lxgRenderTargetPTR from , lxgRenderTargetBlitPTR update , flags32 mask , booln linearFilter)", },
  ["lxgContext_applyViewPortRect"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxRectangleiCPTR rect)", },
  ["lxgContext_applyViewPortScissorState"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , booln state)", },
  ["lxgContext_applyViewPort"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgViewPortPTR obj)", },
  ["lxgContext_applyViewPortMrt"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgViewPortMrtPTR obj)", },
  ["lxgContext_init"] = { type ='function', 
    description = "", 
    returns = "(const char *)",
    valuetype = "string",
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_syncRasterStates"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_checkStates"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_clearVertexState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexAttribs"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 attribs , flags32 changed)", },
  ["lxgContext_applyVertexAttribsFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 attribs , flags32 changed)", },
  ["lxgContext_applyVertexState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyVertexStateFIXEDNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setVertexDecl"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgVertexDeclCPTR decl)", },
  ["lxgContext_setVertexDeclStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgVertexDeclCPTR decl , lxgStreamHostCPTR hosts)", },
  ["lxgContext_setVertexStream"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host)", },
  ["lxgContext_invalidateVertexStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_clearFeedbackState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyFeedbackStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setFeedbackStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgStreamHostCPTR hosts , int numStreams)", },
  ["lxgContext_setFeedbackStream"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint idx , lxgStreamHostCPTR host)", },
  ["lxgContext_enableFeedback"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxGLPrimitiveType_t type , int numStreams)", },
  ["lxgContext_disableFeedback"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_clearProgramState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyProgram"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgProgramCPTR prog)", },
  ["lxgContext_applyProgramParameters"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgProgramCPTR prog , uint num , lxgProgramParameterPTR * params , const void * * data)", },
  ["lxgContext_updateProgramSubroutines"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgProgramCPTR prog)", },
  ["lxgContext_clearTextureState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_setTextureSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint imageunit , flags32 what)", },
  ["lxgContext_changedTextureSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint imageunit , flags32 what)", },
  ["lxgContext_applyTexture"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR obj , uint imageunit)", },
  ["lxgContext_applyTextures"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num)", },
  ["lxgContext_applySampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR obj , uint imageunit)", },
  ["lxgContext_applySamplers"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num)", },
  ["lxgContext_applyTextureImages"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num)", },
  ["lxgContext_applyTextureImage"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit)", },
  ["lxgContext_clearRasterState"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_applyDepth"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgDepthCPTR obj)", },
  ["lxgContext_applyLogic"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgLogicCPTR obj)", },
  ["lxgContext_applyStencil"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgStencilCPTR obj)", },
  ["lxgContext_applyBlend"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgBlendCPTR obj)", },
  ["lxgContext_applyColor"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgColorCPTR obj)", },
  ["lxgContext_applyRasterizer"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRasterizerCPTR obj)", },
  ["lxgContext_blitRenderTargets"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR to , lxgRenderTargetPTR from , lxgRenderTargetBlitPTR update , flags32 mask , booln linearFilter)", },
  ["lxgContext_applyViewPortRect"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxRectangleiCPTR rect)", },
  ["lxgContext_applyViewPortScissorState"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , booln state)", },
  ["lxgContext_applyViewPort"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgViewPortPTR obj)", },
  ["lxgContext_applyViewPortMrt"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgViewPortMrtPTR obj)", },
  ["lxgContext_applyRenderTarget"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR obj , lxgRenderTargetType_t type)", },
  ["lxgContext_applyRenderTargetDraw"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR obj , booln setViewport)", },
  ["lxgContext_checkedBlend"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgBlendCPTR obj)", },
  ["lxgContext_checkedColor"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgColorCPTR obj)", },
  ["lxgContext_checkedDepth"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgDepthCPTR obj)", },
  ["lxgContext_checkedLogic"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgLogicCPTR obj)", },
  ["lxgContext_checkedStencil"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgStencilCPTR obj)", },
  ["lxgContext_checkedRasterizer"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRasterizerCPTR obj)", },
  ["lxgContext_checkedTexture"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR tex , uint imageunit)", },
  ["lxgContext_checkedSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR samp , uint imageunit)", },
  ["lxgContext_checkedTextureImage"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR img , uint imageunit)", },
  ["lxgContext_checkedTextures"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTexturePTR * texs , uint start , uint num)", },
  ["lxgContext_checkedSamplers"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgSamplerCPTR * samps , uint start , uint num)", },
  ["lxgContext_checkedTextureImages"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgTextureImageCPTR * imgs , uint start , uint num)", },
  ["lxgContext_checkedRenderFlag"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 needed)", },
  ["lxgContext_checkedVertexDecl"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgVertexDeclCPTR decl)", },
  ["lxgContext_checkedVertexAttrib"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 needed)", },
  ["lxgContext_checkedVertexAttribFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , flags32 needed)", },
  ["lxgContext_checkedRenderTarget"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgRenderTargetPTR rt , lxgRenderTargetType_t type)", },
  ["lxgContext_checkedProgram"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxgProgramPTR prog)", },
  ["lxgContext_checkedVertex"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_checkedVertexNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_checkedVertexFIXED"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_checkedVertexFIXEDNV"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx)", },
  ["lxgContext_checkedViewPortScissor"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , lxRectangleiCPTR rect)", },
  ["lxgContext_checkedTextureSampler"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint imageunit)", },
  ["lxgContext_setProgramBuffer"] = { type ='function', 
    description = "", 
    returns = "(booln)",
    valuetype = nil,
    args = "(lxgContextPTR ctx , uint idx , lxgBufferCPTR buffer)", },
  ["lxgBuffer_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["gltarget"] = { type ='value', description = "lxGLBufferTarget_t", valuetype = nil, },
    ["glid"] = { type ='value', description = "GLuint", valuetype = nil, },
    ["address"] = { type ='value', description = "GLuint64", valuetype = nil, },
    ["ctxcapbits"] = { type ='value', description = "flags32", valuetype = nil, },
    ["user"] = { type ='value', description = "void *", valuetype = nil, },
    ["mapped"] = { type ='value', description = "void *", valuetype = nil, },
    ["maptype"] = { type ='value', description = "lxgAccessMode_t", valuetype = nil, },
    ["mapstart"] = { type ='value', description = "uint", valuetype = nil, },
    ["maplength"] = { type ='value', description = "uint", valuetype = nil, },
    ["size"] = { type ='value', description = "uint", valuetype = nil, },
    ["used"] = { type ='value', description = "uint", valuetype = nil, },
    ["hint"] = { type ='value', description = "lxGLBufferHint_t", valuetype = nil, },
    ["ctx"] = { type ='value', description = "lxgContextPTR", valuetype = "lxg.lxgContext_t", },
    }
  },
  ["lxgVertexElement_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["1"] = { type ='value', description = "unsigned normalize", valuetype = nil, },
    ["1"] = { type ='value', description = "unsigned integer", valuetype = nil, },
    ["2"] = { type ='value', description = "unsigned cnt", valuetype = nil, },
    ["4"] = { type ='value', description = "unsigned stream", valuetype = nil, },
    ["8"] = { type ='value', description = "unsigned scalartype", valuetype = nil, },
    ["8"] = { type ='value', description = "unsigned stridehalf", valuetype = nil, },
    ["8"] = { type ='value', description = "unsigned offset", valuetype = nil, },
    }
  },
  ["lxgVertexDecl_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["available"] = { type ='value', description = "flags32", valuetype = nil, },
    ["streams"] = { type ='value', description = "uint", valuetype = nil, },
    ["LUXGFX_VERTEX_ATTRIBS"] = { type ='value', description = "lxgVertexElement_t table]", valuetype = nil, },
    }
  },
  ["lxgStreamHost_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["buffer"] = { type ='value', description = "lxgBufferPTR", valuetype = "lxg.lxgBuffer_t", },
    ["len"] = { type ='value', description = "size_t", valuetype = nil, },
    }
  },
  ["lxgVertexPointer_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["LUXGFX_VERTEX_ATTRIBS"] = { type ='value', description = "lxgVertexElement_t element]", valuetype = nil, },
    ["LUXGFX_MAX_VERTEX_STREAMS"] = { type ='value', description = "lxgStreamHost_t streams]", valuetype = nil, },
    }
  },
  ["lxgVertexState_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["decl"] = { type ='value', description = "lxgVertexDeclCPTR", valuetype = "lxg.lxgVertexDecl_t", },
    ["active"] = { type ='value', description = "flags32", valuetype = nil, },
    ["declvalid"] = { type ='value', description = "flags32", valuetype = nil, },
    ["declstreams"] = { type ='value', description = "flags32", valuetype = nil, },
    ["streamvalid"] = { type ='value', description = "flags32", valuetype = nil, },
    ["declchange"] = { type ='value', description = "flags32", valuetype = nil, },
    ["streamchange"] = { type ='value', description = "flags32", valuetype = nil, },
    ["setup"] = { type ='value', description = "lxgVertexPointer_t", valuetype = "lxg.lxgVertexPointer_t", },
    }
  },
  ["lxgFeedbackState_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["capture"] = { type ='value', description = "lxGLPrimitiveType_t", valuetype = nil, },
    ["active"] = { type ='value', description = "int", valuetype = nil, },
    ["usedvalid"] = { type ='value', description = "flags32", valuetype = nil, },
    ["streamvalid"] = { type ='value', description = "flags32", valuetype = nil, },
    ["streamchange"] = { type ='value', description = "flags32", valuetype = nil, },
    ["LUXGFX_MAX_VERTEX_STREAMS"] = { type ='value', description = "lxgStreamHost_t streams]", valuetype = nil, },
    }
  },
  ["lxgSamplerLod_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["bias"] = { type ='value', description = "float", valuetype = nil, },
    ["min"] = { type ='value', description = "float", valuetype = nil, },
    ["max"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["lxgSampler_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["glid"] = { type ='value', description = "GLuint", valuetype = nil, },
    ["incarnation"] = { type ='value', description = "uint32", valuetype = nil, },
    ["cmpfunc"] = { type ='value', description = "lxGLCompareMode_t", valuetype = nil, },
    ["filter"] = { type ='value', description = "lxgSamplerFilter_t", valuetype = nil, },
    ["addru"] = { type ='value', description = "lxgSamplerAddress_t", valuetype = nil, },
    ["addrv"] = { type ='value', description = "lxgSamplerAddress_t", valuetype = nil, },
    ["addrw"] = { type ='value', description = "lxgSamplerAddress_t", valuetype = nil, },
    ["aniso"] = { type ='value', description = "uint", valuetype = nil, },
    ["lod"] = { type ='value', description = "lxgSamplerLod_t", valuetype = "lxg.lxgSamplerLod_t", },
    ["4"] = { type ='value', description = "float border]", valuetype = nil, },
    }
  },
  ["lxgTexture_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["gltarget"] = { type ='value', description = "lxGLTextureTarget_t", valuetype = nil, },
    ["glid"] = { type ='value', description = "GLuint", valuetype = nil, },
    ["lastSampler"] = { type ='value', description = "lxgSamplerCPTR", valuetype = "lxg.lxgSampler_t", },
    ["lastSamplerIncarnation"] = { type ='value', description = "uint32", valuetype = nil, },
    ["ctx"] = { type ='value', description = "lxgContextPTR", valuetype = "lxg.lxgContext_t", },
    ["formattype"] = { type ='value', description = "lxgTextureChannel_t", valuetype = nil, },
    ["datatype"] = { type ='value', description = "lxgTextureDataType_t", valuetype = nil, },
    ["flags"] = { type ='value', description = "flags32", valuetype = nil, },
    ["width"] = { type ='value', description = "int", valuetype = nil, },
    ["height"] = { type ='value', description = "int", valuetype = nil, },
    ["depth"] = { type ='value', description = "int", valuetype = nil, },
    ["arraysize"] = { type ='value', description = "int", valuetype = nil, },
    ["samples"] = { type ='value', description = "int", valuetype = nil, },
    ["mipsdefined"] = { type ='value', description = "flags32", valuetype = nil, },
    ["miplevels"] = { type ='value', description = "uint", valuetype = nil, },
    ["LUXGFX_MAX_TEXTURE_MIPMAPS"] = { type ='value', description = "lxVec3i_t mipsizes]", valuetype = nil, },
    ["LUXGFX_MAX_TEXTURE_MIPMAPS"] = { type ='value', description = "uint pixelsizes]", valuetype = nil, },
    ["LUXGFX_MAX_TEXTURE_MIPMAPS"] = { type ='value', description = "size_t nativesizes]", valuetype = nil, },
    ["components"] = { type ='value', description = "uint", valuetype = nil, },
    ["componentsize"] = { type ='value', description = "uint", valuetype = nil, },
    ["sampler"] = { type ='value', description = "lxgSampler_t", valuetype = "lxg.lxgSampler_t", },
    ["glinternalformat"] = { type ='value', description = "GLenum", valuetype = nil, },
    ["gldatatype"] = { type ='value', description = "GLenum", valuetype = nil, },
    ["gldataformat"] = { type ='value', description = "GLenum", valuetype = nil, },
    }
  },
  ["lxgRenderBuffer_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["glid"] = { type ='value', description = "GLuint", valuetype = nil, },
    ["ctx"] = { type ='value', description = "lxgContextPTR", valuetype = "lxg.lxgContext_t", },
    ["formattype"] = { type ='value', description = "lxgTextureChannel_t", valuetype = nil, },
    ["width"] = { type ='value', description = "int", valuetype = nil, },
    ["height"] = { type ='value', description = "int", valuetype = nil, },
    ["samples"] = { type ='value', description = "uint", valuetype = nil, },
    }
  },
  ["lxgTextureUpdate_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["from"] = { type ='value', description = "lxVec3i_t", valuetype = nil, },
    ["to"] = { type ='value', description = "lxVec3i_t", valuetype = nil, },
    ["size"] = { type ='value', description = "lxVec3i_t", valuetype = nil, },
    }
  },
  ["lxgTextureImage_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["tex"] = { type ='value', description = "lxgTexturePTR", valuetype = "lxg.lxgTexture_t", },
    ["level"] = { type ='value', description = "int", valuetype = nil, },
    ["layered"] = { type ='value', description = "booln", valuetype = nil, },
    ["layer"] = { type ='value', description = "int", valuetype = nil, },
    ["glformat"] = { type ='value', description = "lxGLAccessFormat_t", valuetype = nil, },
    ["glaccess"] = { type ='value', description = "lxGLAccessMode_t", valuetype = nil, },
    }
  },
  ["lxgDepth_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["enabled"] = { type ='value', description = "bool16", valuetype = nil, },
    ["write"] = { type ='value', description = "bool16", valuetype = nil, },
    ["func"] = { type ='value', description = "lxGLCompareMode_t", valuetype = nil, },
    }
  },
  ["lxgLogic_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["enabled"] = { type ='value', description = "bool32", valuetype = nil, },
    ["op"] = { type ='value', description = "lxGLLogicOp_t", valuetype = nil, },
    }
  },
  ["lxgColor_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["individual"] = { type ='value', description = "bool32", valuetype = nil, },
    ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', description = "bool8 write][LUXGFX_COLORS]", valuetype = nil, },
    }
  },
  ["lxgStencilOp_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["fail"] = { type ='value', description = "lxGLStencilMode_t", valuetype = nil, },
    ["zfail"] = { type ='value', description = "lxGLStencilMode_t", valuetype = nil, },
    ["zpass"] = { type ='value', description = "lxGLStencilMode_t", valuetype = nil, },
    ["func"] = { type ='value', description = "lxGLCompareMode_t", valuetype = nil, },
    }
  },
  ["lxgStencil_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["enabled"] = { type ='value', description = "bool8", valuetype = nil, },
    ["write"] = { type ='value', description = "flags32", valuetype = nil, },
    ["mask"] = { type ='value', description = "flags32", valuetype = nil, },
    ["refvalue"] = { type ='value', description = "uint32", valuetype = nil, },
    ["LUXGFX_FACES"] = { type ='value', description = "lxgStencilOp_t ops]", valuetype = nil, },
    }
  },
  ["lxgBlendMode_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["srcw"] = { type ='value', description = "lxGLBlendWeight_t", valuetype = nil, },
    ["dstw"] = { type ='value', description = "lxGLBlendWeight_t", valuetype = nil, },
    ["equ"] = { type ='value', description = "lxGLBlendEquation_t", valuetype = nil, },
    }
  },
  ["lxgBlendStage_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["enabled"] = { type ='value', description = "bool32", valuetype = nil, },
    ["colormode"] = { type ='value', description = "lxgBlendMode_t", valuetype = "lxg.lxgBlendMode_t", },
    ["alphamode"] = { type ='value', description = "lxgBlendMode_t", valuetype = "lxg.lxgBlendMode_t", },
    }
  },
  ["lxgBlend_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["individual"] = { type ='value', description = "bool16", valuetype = nil, },
    ["separateStages"] = { type ='value', description = "bool16", valuetype = nil, },
    ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', description = "lxgBlendStage_t blends]", valuetype = nil, },
    }
  },
  ["lxgRasterizer_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["cull"] = { type ='value', description = "bool8", valuetype = nil, },
    ["cullfront"] = { type ='value', description = "bool8", valuetype = nil, },
    ["ccw"] = { type ='value', description = "bool8", valuetype = nil, },
    ["fill"] = { type ='value', description = "enum32", valuetype = nil, },
    }
  },
  ["lxgRasterState_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["rasterizerObj"] = { type ='value', description = "lxgRasterizerCPTR", valuetype = "lxg.lxgRasterizer_t", },
    ["colorObj"] = { type ='value', description = "lxgColorCPTR", valuetype = "lxg.lxgColor_t", },
    ["blendObj"] = { type ='value', description = "lxgBlendCPTR", valuetype = "lxg.lxgBlend_t", },
    ["depthObj"] = { type ='value', description = "lxgDepthCPTR", valuetype = "lxg.lxgDepth_t", },
    ["stencilObj"] = { type ='value', description = "lxgStencilCPTR", valuetype = "lxg.lxgStencil_t", },
    ["logicObj"] = { type ='value', description = "lxgLogicCPTR", valuetype = "lxg.lxgLogic_t", },
    ["rasterizer"] = { type ='value', description = "lxgRasterizer_t", valuetype = "lxg.lxgRasterizer_t", },
    ["color"] = { type ='value', description = "lxgColor_t", valuetype = "lxg.lxgColor_t", },
    ["blend"] = { type ='value', description = "lxgBlend_t", valuetype = "lxg.lxgBlend_t", },
    ["depth"] = { type ='value', description = "lxgDepth_t", valuetype = "lxg.lxgDepth_t", },
    ["stencil"] = { type ='value', description = "lxgStencil_t", valuetype = "lxg.lxgStencil_t", },
    ["logic"] = { type ='value', description = "lxgLogic_t", valuetype = "lxg.lxgLogic_t", },
    }
  },
  ["lxgFrameBounds_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["width"] = { type ='value', description = "int", valuetype = nil, },
    ["height"] = { type ='value', description = "int", valuetype = nil, },
    }
  },
  ["lxgViewDepth_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["near"] = { type ='value', description = "double", valuetype = nil, },
    ["far"] = { type ='value', description = "double", valuetype = nil, },
    }
  },
  ["lxgViewPort_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["scissor"] = { type ='value', description = "booln", valuetype = nil, },
    ["scissorRect"] = { type ='value', description = "lxRectanglei_t", valuetype = nil, },
    ["viewRect"] = { type ='value', description = "lxRectanglei_t", valuetype = nil, },
    ["depth"] = { type ='value', description = "lxgViewDepth_t", valuetype = "lxg.lxgViewDepth_t", },
    }
  },
  ["lxgViewPortMrt_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["numused"] = { type ='value', description = "uint", valuetype = nil, },
    ["scissored"] = { type ='value', description = "flags32", valuetype = nil, },
    ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', description = "lxRectanglef_t bounds]", valuetype = nil, },
    ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', description = "lxRectanglei_t scissors]", valuetype = nil, },
    ["LUXGFX_MAX_RENDERTARGETS"] = { type ='value', description = "lxgViewDepth_t depths]", valuetype = nil, },
    }
  },
  ["lxgRenderAssign_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["tex"] = { type ='value', description = "lxgTexturePTR", valuetype = "lxg.lxgTexture_t", },
    ["rbuf"] = { type ='value', description = "lxgRenderBufferPTR", valuetype = "lxg.lxgRenderBuffer_t", },
    ["mip"] = { type ='value', description = "uint", valuetype = nil, },
    ["layer"] = { type ='value', description = "uint", valuetype = nil, },
    }
  },
  ["lxgRenderTarget_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["glid"] = { type ='value', description = "GLuint", valuetype = nil, },
    ["ctx"] = { type ='value', description = "lxgContextPTR", valuetype = "lxg.lxgContext_t", },
    ["maxidx"] = { type ='value', description = "uint", valuetype = nil, },
    ["dirty"] = { type ='value', description = "flags32", valuetype = nil, },
    ["LUXGFX_RENDERASSIGNS"] = { type ='value', description = "lxgRenderAssign_t assigns]", valuetype = nil, },
    ["equalsized"] = { type ='value', description = "booln", valuetype = nil, },
    ["bounds"] = { type ='value', description = "lxgFrameBounds_t", valuetype = "lxg.lxgFrameBounds_t", },
    }
  },
  ["lxgRenderTargetBlit_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["fromStart"] = { type ='value', description = "lxVec2i_t", valuetype = nil, },
    ["fromEnd"] = { type ='value', description = "lxVec2i_t", valuetype = nil, },
    ["toStart"] = { type ='value', description = "lxVec2i_t", valuetype = nil, },
    ["toEnd"] = { type ='value', description = "lxVec2i_t", valuetype = nil, },
    }
  },
  ["lxgCapabilites_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["texsize"] = { type ='value', description = "int", valuetype = nil, },
    ["texsize3d"] = { type ='value', description = "int", valuetype = nil, },
    ["texlayers"] = { type ='value', description = "int", valuetype = nil, },
    ["texunits"] = { type ='value', description = "int", valuetype = nil, },
    ["teximages"] = { type ='value', description = "int", valuetype = nil, },
    ["texcoords"] = { type ='value', description = "int", valuetype = nil, },
    ["texvtxunits"] = { type ='value', description = "int", valuetype = nil, },
    ["texaniso"] = { type ='value', description = "float", valuetype = nil, },
    ["pointsize"] = { type ='value', description = "float", valuetype = nil, },
    ["drawbuffers"] = { type ='value', description = "int", valuetype = nil, },
    ["viewports"] = { type ='value', description = "int", valuetype = nil, },
    ["fbosamples"] = { type ='value', description = "int", valuetype = nil, },
    }
  },
  ["lxgContext_t"] = { type ='class', 
    description = "", 
    childs =     {
    ["capbits"] = { type ='value', description = "flags32", valuetype = nil, },
    ["LUXGFX_MAX_TEXTURE_IMAGES"] = { type ='value', description = "lxgTexturePTR textures]", valuetype = nil, },
    ["LUXGFX_MAX_TEXTURE_IMAGES"] = { type ='value', description = "lxgSamplerCPTR samplers]", valuetype = nil, },
    ["LUXGFX_MAX_RWTEXTURE_IMAGES"] = { type ='value', description = "lxgTextureImageCPTR images]", valuetype = nil, },
    ["LUXGFX_RENDERTARGETS"] = { type ='value', description = "lxgRenderTargetPTR rendertargets]", valuetype = nil, },
    ["vertex"] = { type ='value', description = "lxgVertexState_t", valuetype = "lxg.lxgVertexState_t", },
    ["feedback"] = { type ='value', description = "lxgFeedbackState_t", valuetype = "lxg.lxgFeedbackState_t", },
    ["program"] = { type ='value', description = "lxgProgramState_t", valuetype = nil, },
    ["raster"] = { type ='value', description = "lxgRasterState_t", valuetype = "lxg.lxgRasterState_t", },
    ["viewport"] = { type ='value', description = "lxgViewPort_t", valuetype = "lxg.lxgViewPort_t", },
    ["framebounds"] = { type ='value', description = "lxgFrameBounds_t", valuetype = "lxg.lxgFrameBounds_t", },
    ["window"] = { type ='value', description = "lxgFrameBounds_t", valuetype = "lxg.lxgFrameBounds_t", },
    ["viewportMRT"] = { type ='value', description = "lxgViewPortMrt_t", valuetype = "lxg.lxgViewPortMrt_t", },
    ["capabilites"] = { type ='value', description = "lxgCapabilites_t", valuetype = "lxg.lxgCapabilites_t", },
    }
  },
  }
return {
  lxg = {
    type = 'lib',
    description = "Lux Graphics",
    childs = api,
  },
}
