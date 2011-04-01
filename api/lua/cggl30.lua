--[[// cggl cgGL | Cg OpenGL runtime
typedef unsigned int GLuint;
typedef unsigned int GLenum;
typedef void GLvoid;
typedef int GLsizei;
typedef int GLint;

typedef enum
{
  CG_GL_MATRIX_IDENTITY             = 0,
  CG_GL_MATRIX_TRANSPOSE            = 1,
  CG_GL_MATRIX_INVERSE              = 2,
  CG_GL_MATRIX_INVERSE_TRANSPOSE    = 3,
  CG_GL_MODELVIEW_MATRIX            = 4,
  CG_GL_PROJECTION_MATRIX           = 5,
  CG_GL_TEXTURE_MATRIX              = 6,
  CG_GL_MODELVIEW_PROJECTION_MATRIX = 7,
  CG_GL_VERTEX                      = 8,
  CG_GL_FRAGMENT                    = 9,
  CG_GL_GEOMETRY                    = 10,
  CG_GL_TESSELLATION_CONTROL        = 11,
  CG_GL_TESSELLATION_EVALUATION     = 12
} CGGLenum;

 CGbool  cgGLIsProfileSupported(CGprofile profile);
 void  cgGLEnableProfile(CGprofile profile);
 void  cgGLDisableProfile(CGprofile profile);
 CGprofile  cgGLGetLatestProfile(CGGLenum profile_type);
 void  cgGLSetOptimalOptions(CGprofile profile);
 char const **  cgGLGetOptimalOptions(CGprofile profile);
 void  cgGLLoadProgram(CGprogram program);
 void  cgGLUnloadProgram(CGprogram program);
 CGbool  cgGLIsProgramLoaded(CGprogram program);
 void  cgGLBindProgram(CGprogram program);
 void  cgGLUnbindProgram(CGprofile profile);
 GLuint  cgGLGetProgramID(CGprogram program);
 void  cgGLSetParameter1f(CGparameter param, float x);
 void  cgGLSetParameter2f(CGparameter param, float x, float y);
 void  cgGLSetParameter3f(CGparameter param, float x, float y, float z);
 void  cgGLSetParameter4f(CGparameter param, float x, float y, float z, float w);
 void  cgGLSetParameter1fv(CGparameter param, const float *v);
 void  cgGLSetParameter2fv(CGparameter param, const float *v);
 void  cgGLSetParameter3fv(CGparameter param, const float *v);
 void  cgGLSetParameter4fv(CGparameter param, const float *v);
 void  cgGLSetParameter1d(CGparameter param, double x);
 void  cgGLSetParameter2d(CGparameter param, double x, double y);
 void  cgGLSetParameter3d(CGparameter param, double x, double y, double z);
 void  cgGLSetParameter4d(CGparameter param, double x, double y, double z, double w);
 void  cgGLSetParameter1dv(CGparameter param, const double *v);
 void  cgGLSetParameter2dv(CGparameter param, const double *v);
 void  cgGLSetParameter3dv(CGparameter param, const double *v);
 void  cgGLSetParameter4dv(CGparameter param, const double *v);
 void  cgGLGetParameter1f(CGparameter param, float *v);
 void  cgGLGetParameter2f(CGparameter param, float *v);
 void  cgGLGetParameter3f(CGparameter param, float *v);
 void  cgGLGetParameter4f(CGparameter param, float *v);
 void  cgGLGetParameter1d(CGparameter param, double *v);
 void  cgGLGetParameter2d(CGparameter param, double *v);
 void  cgGLGetParameter3d(CGparameter param, double *v);
 void  cgGLGetParameter4d(CGparameter param, double *v);
 void  cgGLSetParameterArray1f(CGparameter param, long offset, long nelements, const float *v);
 void  cgGLSetParameterArray2f(CGparameter param, long offset, long nelements, const float *v);
 void  cgGLSetParameterArray3f(CGparameter param, long offset, long nelements, const float *v);
 void  cgGLSetParameterArray4f(CGparameter param, long offset, long nelements, const float *v);
 void  cgGLSetParameterArray1d(CGparameter param, long offset, long nelements, const double *v);
 void  cgGLSetParameterArray2d(CGparameter param, long offset, long nelements, const double *v);
 void  cgGLSetParameterArray3d(CGparameter param, long offset, long nelements, const double *v);
 void  cgGLSetParameterArray4d(CGparameter param, long offset, long nelements, const double *v);
 void  cgGLGetParameterArray1f(CGparameter param, long offset, long nelements, float *v);
 void  cgGLGetParameterArray2f(CGparameter param, long offset, long nelements, float *v);
 void  cgGLGetParameterArray3f(CGparameter param, long offset, long nelements, float *v);
 void  cgGLGetParameterArray4f(CGparameter param, long offset, long nelements, float *v);
 void  cgGLGetParameterArray1d(CGparameter param, long offset, long nelements, double *v);
 void  cgGLGetParameterArray2d(CGparameter param, long offset, long nelements, double *v);
 void  cgGLGetParameterArray3d(CGparameter param, long offset, long nelements, double *v);
 void  cgGLGetParameterArray4d(CGparameter param, long offset, long nelements, double *v);
 void  cgGLSetParameterPointer(CGparameter param, GLint fsize, GLenum type, GLsizei stride, const GLvoid *pointer);
 void  cgGLEnableClientState(CGparameter param);
 void  cgGLDisableClientState(CGparameter param);
 void  cgGLSetMatrixParameterdr(CGparameter param, const double *matrix);
 void  cgGLSetMatrixParameterfr(CGparameter param, const float *matrix);
 void  cgGLSetMatrixParameterdc(CGparameter param, const double *matrix);
 void  cgGLSetMatrixParameterfc(CGparameter param, const float *matrix);
 void  cgGLGetMatrixParameterdr(CGparameter param, double *matrix);
 void  cgGLGetMatrixParameterfr(CGparameter param, float *matrix);
 void  cgGLGetMatrixParameterdc(CGparameter param, double *matrix);
 void  cgGLGetMatrixParameterfc(CGparameter param, float *matrix);
 void  cgGLSetStateMatrixParameter(CGparameter param, CGGLenum matrix, CGGLenum transform);
 void  cgGLSetMatrixParameterArrayfc(CGparameter param, long offset, long nelements, const float *matrices);
 void  cgGLSetMatrixParameterArrayfr(CGparameter param, long offset, long nelements, const float *matrices);
 void  cgGLSetMatrixParameterArraydc(CGparameter param, long offset, long nelements, const double *matrices);
 void  cgGLSetMatrixParameterArraydr(CGparameter param, long offset, long nelements, const double *matrices);
 void  cgGLGetMatrixParameterArrayfc(CGparameter param, long offset, long nelements, float *matrices);
 void  cgGLGetMatrixParameterArrayfr(CGparameter param, long offset, long nelements, float *matrices);
 void  cgGLGetMatrixParameterArraydc(CGparameter param, long offset, long nelements, double *matrices);
 void  cgGLGetMatrixParameterArraydr(CGparameter param, long offset, long nelements, double *matrices);
 void  cgGLSetTextureParameter(CGparameter param, GLuint texobj);
 GLuint  cgGLGetTextureParameter(CGparameter param);
 void  cgGLEnableTextureParameter(CGparameter param);
 void  cgGLDisableTextureParameter(CGparameter param);
 GLenum  cgGLGetTextureEnum(CGparameter param);
 void  cgGLSetManageTextureParameters(CGcontext ctx, CGbool flag);
 CGbool  cgGLGetManageTextureParameters(CGcontext ctx);
 void  cgGLSetupSampler(CGparameter param, GLuint texobj);
 void  cgGLRegisterStates(CGcontext ctx);
 void  cgGLEnableProgramProfiles(CGprogram program);
 void  cgGLDisableProgramProfiles(CGprogram program);
 void  cgGLSetDebugMode(CGbool debug);
 CGbuffer  cgGLCreateBuffer(CGcontext context, int size, const void *data, GLenum bufferUsage);
 GLuint  cgGLGetBufferObject(CGbuffer buffer);

]]--auto-generated api from ffi headers

local api = {
  ["CGGLenum"] = { type ='value', description = "", },
  ["CG_GL_MATRIX_IDENTITY"] = { type ='keyword', },
  ["CG_GL_MATRIX_TRANSPOSE"] = { type ='keyword', },
  ["CG_GL_MATRIX_INVERSE"] = { type ='keyword', },
  ["CG_GL_MATRIX_INVERSE_TRANSPOSE"] = { type ='keyword', },
  ["CG_GL_MODELVIEW_MATRIX"] = { type ='keyword', },
  ["CG_GL_PROJECTION_MATRIX"] = { type ='keyword', },
  ["CG_GL_TEXTURE_MATRIX"] = { type ='keyword', },
  ["CG_GL_MODELVIEW_PROJECTION_MATRIX"] = { type ='keyword', },
  ["CG_GL_VERTEX"] = { type ='keyword', },
  ["CG_GL_FRAGMENT"] = { type ='keyword', },
  ["CG_GL_GEOMETRY"] = { type ='keyword', },
  ["CG_GL_TESSELLATION_CONTROL"] = { type ='keyword', },
  ["CG_GL_TESSELLATION_EVALUATION"] = { type ='keyword', },
  ["cgGLIsProfileSupported"] = { type ='function', 
      description = "", 
      returns = "(CGbool)",
      args = "(CGprofile profile)", },
  ["cgGLEnableProfile"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprofile profile)", },
  ["cgGLDisableProfile"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprofile profile)", },
  ["cgGLGetLatestProfile"] = { type ='function', 
      description = "", 
      returns = "(CGprofile)",
      args = "(CGGLenum profile_type)", },
  ["cgGLSetOptimalOptions"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprofile profile)", },
  ["cgGLGetOptimalOptions"] = { type ='function', 
      description = "", 
      returns = "(const)",
      args = "(CGprofile profile)", },
  ["cgGLLoadProgram"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprogram program)", },
  ["cgGLUnloadProgram"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprogram program)", },
  ["cgGLIsProgramLoaded"] = { type ='function', 
      description = "", 
      returns = "(CGbool)",
      args = "(CGprogram program)", },
  ["cgGLBindProgram"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprogram program)", },
  ["cgGLUnbindProgram"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprofile profile)", },
  ["cgGLGetProgramID"] = { type ='function', 
      description = "", 
      returns = "(GLuint)",
      args = "(CGprogram program)", },
  ["cgGLSetParameter1f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float x)", },
  ["cgGLSetParameter2f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float x, float y)", },
  ["cgGLSetParameter3f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float x, float y, float z)", },
  ["cgGLSetParameter4f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float x, float y, float z, float w)", },
  ["cgGLSetParameter1fv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *v)", },
  ["cgGLSetParameter2fv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *v)", },
  ["cgGLSetParameter3fv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *v)", },
  ["cgGLSetParameter4fv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *v)", },
  ["cgGLSetParameter1d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double x)", },
  ["cgGLSetParameter2d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double x, double y)", },
  ["cgGLSetParameter3d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double x, double y, double z)", },
  ["cgGLSetParameter4d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double x, double y, double z, double w)", },
  ["cgGLSetParameter1dv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *v)", },
  ["cgGLSetParameter2dv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *v)", },
  ["cgGLSetParameter3dv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *v)", },
  ["cgGLSetParameter4dv"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *v)", },
  ["cgGLGetParameter1f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *v)", },
  ["cgGLGetParameter2f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *v)", },
  ["cgGLGetParameter3f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *v)", },
  ["cgGLGetParameter4f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *v)", },
  ["cgGLGetParameter1d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *v)", },
  ["cgGLGetParameter2d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *v)", },
  ["cgGLGetParameter3d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *v)", },
  ["cgGLGetParameter4d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *v)", },
  ["cgGLSetParameterArray1f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *v)", },
  ["cgGLSetParameterArray2f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *v)", },
  ["cgGLSetParameterArray3f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *v)", },
  ["cgGLSetParameterArray4f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *v)", },
  ["cgGLSetParameterArray1d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *v)", },
  ["cgGLSetParameterArray2d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *v)", },
  ["cgGLSetParameterArray3d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *v)", },
  ["cgGLSetParameterArray4d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *v)", },
  ["cgGLGetParameterArray1f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *v)", },
  ["cgGLGetParameterArray2f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *v)", },
  ["cgGLGetParameterArray3f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *v)", },
  ["cgGLGetParameterArray4f"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *v)", },
  ["cgGLGetParameterArray1d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *v)", },
  ["cgGLGetParameterArray2d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *v)", },
  ["cgGLGetParameterArray3d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *v)", },
  ["cgGLGetParameterArray4d"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *v)", },
  ["cgGLSetParameterPointer"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, GLint fsize, GLenum type, GLsizei stride, const GLvoid *pointer)", },
  ["cgGLEnableClientState"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param)", },
  ["cgGLDisableClientState"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param)", },
  ["cgGLSetMatrixParameterdr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *matrix)", },
  ["cgGLSetMatrixParameterfr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *matrix)", },
  ["cgGLSetMatrixParameterdc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const double *matrix)", },
  ["cgGLSetMatrixParameterfc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, const float *matrix)", },
  ["cgGLGetMatrixParameterdr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *matrix)", },
  ["cgGLGetMatrixParameterfr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *matrix)", },
  ["cgGLGetMatrixParameterdc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, double *matrix)", },
  ["cgGLGetMatrixParameterfc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, float *matrix)", },
  ["cgGLSetStateMatrixParameter"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, CGGLenum matrix, CGGLenum transform)", },
  ["cgGLSetMatrixParameterArrayfc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *matrices)", },
  ["cgGLSetMatrixParameterArrayfr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const float *matrices)", },
  ["cgGLSetMatrixParameterArraydc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *matrices)", },
  ["cgGLSetMatrixParameterArraydr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, const double *matrices)", },
  ["cgGLGetMatrixParameterArrayfc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *matrices)", },
  ["cgGLGetMatrixParameterArrayfr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, float *matrices)", },
  ["cgGLGetMatrixParameterArraydc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *matrices)", },
  ["cgGLGetMatrixParameterArraydr"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, long offset, long nelements, double *matrices)", },
  ["cgGLSetTextureParameter"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, GLuint texobj)", },
  ["cgGLGetTextureParameter"] = { type ='function', 
      description = "", 
      returns = "(GLuint)",
      args = "(CGparameter param)", },
  ["cgGLEnableTextureParameter"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param)", },
  ["cgGLDisableTextureParameter"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param)", },
  ["cgGLGetTextureEnum"] = { type ='function', 
      description = "", 
      returns = "(GLenum)",
      args = "(CGparameter param)", },
  ["cgGLSetManageTextureParameters"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGcontext ctx, CGbool flag)", },
  ["cgGLGetManageTextureParameters"] = { type ='function', 
      description = "", 
      returns = "(CGbool)",
      args = "(CGcontext ctx)", },
  ["cgGLSetupSampler"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGparameter param, GLuint texobj)", },
  ["cgGLRegisterStates"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGcontext ctx)", },
  ["cgGLEnableProgramProfiles"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprogram program)", },
  ["cgGLDisableProgramProfiles"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGprogram program)", },
  ["cgGLSetDebugMode"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(CGbool debug)", },
  ["cgGLCreateBuffer"] = { type ='function', 
      description = "", 
      returns = "(CGbuffer)",
      args = "(CGcontext context, int size, const void *data, GLenum bufferUsage)", },
  ["cgGLGetBufferObject"] = { type ='function', 
      description = "", 
      returns = "(GLuint)",
      args = "(CGbuffer buffer)", },
}
cggl = {
	type = 'class',
	description = "Cg OpenGL runtime",
	childs = api,
}
cgGL = {
	type = 'class',
	description = "Cg OpenGL runtime",
	childs = api,
}
