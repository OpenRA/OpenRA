--[[// ai assimp | AssetImporter Model Loader Library
typedef enum aiBool {
  aiBool_FALSE = 0,
  aiBool_TRUE = 1,
} aiBool;
typedef enum aiReturn {
  aiReturn_SUCCESS = 0x0,
  aiReturn_FAILURE = -0x1,
  aiReturn_OUTOFMEMORY = -0x3,
} aiReturn;
typedef enum aiOrigin {
  aiOrigin_SET = 0x0,
  aiOrigin_CUR = 0x1,
  aiOrigin_END = 0x2,
} aiOrigin;
typedef enum aiDefaultLogStream {
  aiDefaultLogStream_FILE = 0x1,
  aiDefaultLogStream_STDOUT = 0x2,
  aiDefaultLogStream_STDERR = 0x4,
  aiDefaultLogStream_DEBUGGER = 0x8,
} aiDefaultLogStream;
typedef enum aiComponent {
  aiComponent_NORMALS = 0x2,
  aiComponent_TANGENTS_AND_BITANGENTS = 0x4,
  aiComponent_COLORS = 0x8,
  aiComponent_TEXCOORDS = 0x10,
  aiComponent_BONEWEIGHTS = 0x20,
  aiComponent_ANIMATIONS = 0x40,
  aiComponent_TEXTURES = 0x80,
  aiComponent_LIGHTS = 0x100,
  aiComponent_CAMERAS = 0x200,
  aiComponent_MESHES = 0x400,
  aiComponent_MATERIALS = 0x800,
} aiComponent;
typedef enum aiLightSourceType {
  aiLightSourceType_UNDEFINED = 0x0,
  aiLightSourceType_DIRECTIONAL = 0x1,
  aiLightSourceType_POINT = 0x2,
  aiLightSourceType_SPOT = 0x3,
} aiLightSourceType;
typedef enum aiAnimBehaviour {
  aiAnimBehaviour_DEFAULT = 0x0,
  aiAnimBehaviour_CONSTANT = 0x1,
  aiAnimBehaviour_LINEAR = 0x2,
  aiAnimBehaviour_REPEAT = 0x3,
} aiAnimBehaviour;
typedef enum aiPrimitiveType {
  aiPrimitiveType_POINT = 0x1,
  aiPrimitiveType_LINE = 0x2,
  aiPrimitiveType_TRIANGLE = 0x4,
  aiPrimitiveType_POLYGON = 0x8,
} aiPrimitiveType;
typedef enum aiTextureOp {
  aiTextureOp_Multiply = 0x0,
  aiTextureOp_Add = 0x1,
  aiTextureOp_Subtract = 0x2,
  aiTextureOp_Divide = 0x3,
  aiTextureOp_SmoothAdd = 0x4,
  aiTextureOp_SignedAdd = 0x5,
} aiTextureOp;
typedef enum aiTextureMapMode {
  aiTextureMapMode_Wrap = 0x0,
  aiTextureMapMode_Clamp = 0x1,
  aiTextureMapMode_Decal = 0x3,
  aiTextureMapMode_Mirror = 0x2,
} aiTextureMapMode;
typedef enum aiTextureMapping {
  aiTextureMapping_UV = 0x0,
  aiTextureMapping_SPHERE = 0x1,
  aiTextureMapping_CYLINDER = 0x2,
  aiTextureMapping_BOX = 0x3,
  aiTextureMapping_PLANE = 0x4,
  aiTextureMapping_OTHER = 0x5,
} aiTextureMapping;
typedef enum aiTextureType {
  aiTextureType_NONE = 0x0,
  aiTextureType_DIFFUSE = 0x1,
  aiTextureType_SPECULAR = 0x2,
  aiTextureType_AMBIENT = 0x3,
  aiTextureType_EMISSIVE = 0x4,
  aiTextureType_HEIGHT = 0x5,
  aiTextureType_NORMALS = 0x6,
  aiTextureType_SHININESS = 0x7,
  aiTextureType_OPACITY = 0x8,
  aiTextureType_DISPLACEMENT = 0x9,
  aiTextureType_LIGHTMAP = 0xA,
  aiTextureType_REFLECTION = 0xB,
  aiTextureType_UNKNOWN = 0xC,
} aiTextureType;
typedef enum aiShadingMode {
  aiShadingMode_Flat = 0x1,
  aiShadingMode_Gouraud = 0x2,
  aiShadingMode_Phong = 0x3,
  aiShadingMode_Blinn = 0x4,
  aiShadingMode_Toon = 0x5,
  aiShadingMode_OrenNayar = 0x6,
  aiShadingMode_Minnaert = 0x7,
  aiShadingMode_CookTorrance = 0x8,
  aiShadingMode_NoShading = 0x9,
  aiShadingMode_Fresnel = 0xa,
} aiShadingMode;
typedef enum aiTextureFlags {
  aiTextureFlags_Invert = 0x1,
  aiTextureFlags_UseAlpha = 0x2,
  aiTextureFlags_IgnoreAlpha = 0x4,
} aiTextureFlags;
typedef enum aiBlendMode {
  aiBlendMode_Default = 0x0,
  aiBlendMode_Additive = 0x1,
} aiBlendMode;
typedef enum aiPropertyTypeInfo {
  aiPTI_Float = 0x1,
  aiPTI_String = 0x3,
  aiPTI_Integer = 0x4,
  aiPTI_Buffer = 0x5,
} aiPropertyTypeInfo;
typedef enum aiSceneFlags {
  aiSceneFlags_INCOMPLETE = 0x1,
  aiSceneFlags_VALIDATED = 0x2,
  aiSceneFlags_VALIDATION_WARNING = 0x4,
  aiSceneFlags_NON_VERBOSE_FORMAT = 0x8,
  aiSceneFlags_FLAGS_TERRAIN = 0x10,
} aiSceneFlags;
typedef enum aiPostProcessSteps {
  aiProcess_CalcTangentSpace = 0x1,
  aiProcess_JoinIdenticalVertices = 0x2,
  aiProcess_MakeLeftHanded = 0x4,
  aiProcess_Triangulate = 0x8,
  aiProcess_RemoveComponent = 0x10,
  aiProcess_GenNormals = 0x20,
  aiProcess_GenSmoothNormals = 0x40,
  aiProcess_SplitLargeMeshes = 0x80,
  aiProcess_PreTransformVertices = 0x100,
  aiProcess_LimitBoneWeights = 0x200,
  aiProcess_ValidateDataStructure = 0x400,
  aiProcess_ImproveCacheLocality = 0x800,
  aiProcess_RemoveRedundantMaterials = 0x1000,
  aiProcess_FixInfacingNormals = 0x2000,
  aiProcess_SortByPType = 0x8000,
  aiProcess_FindDegenerates = 0x10000,
  aiProcess_FindInvalidData = 0x20000,
  aiProcess_GenUVCoords = 0x40000,
  aiProcess_TransformUVCoords = 0x80000,
  aiProcess_FindInstances = 0x100000,
  aiProcess_OptimizeMeshes = 0x200000,
  aiProcess_OptimizeGraph = 0x400000,
  aiProcess_FlipUVs = 0x800000,
  aiProcess_FlipWindingOrder = 0x1000000,
} aiPostProcessSteps;
typedef unsigned int uint;
typedef char byte;
typedef unsigned char ubyte;
typedef void (*aiLogStreamCallback)( char* message, char* user ) ;
typedef size_t (*aiFileWriteProc)( struct aiFile*, char*, size_t, size_t ) ;
typedef size_t (*aiFileReadProc)(  struct aiFile*, char*, size_t, size_t ) ;
typedef size_t (*aiFileTellProc)( struct aiFile* ) ;
typedef void (*aiFileFlushProc)( struct aiFile* ) ;
typedef aiReturn (*aiFileSeek)( struct aiFile*, size_t, aiOrigin ) ;
typedef struct aiFile* (*aiFileOpenProc)( struct aiFileIO*, char*, char* ) ;
typedef void (*aiFileCloseProc)( struct aiFileIO*,  struct aiFile* ) ;
typedef char* aiUserData;
static const uint ASSIMP_CFLAGS_SHARED = 0x1;
static const uint ASSIMP_CFLAGS_STLPORT = 0x2;
static const uint ASSIMP_CFLAGS_DEBUG = 0x4;
static const uint ASSIMP_CFLAGS_NOBOOST = 0x8;
static const uint ASSIMP_CFLAGS_SINGLETHREADED = 0x10;
static const size_t AI_TYPES_MAXLEN = 1024;
static const uint AI_SLM_DEFAULT_MAX_TRIANGLES = 1000000;
static const uint AI_SLM_DEFAULT_MAX_VERTICES = 1000000;
static const uint AI_LMW_MAX_WEIGHTS = 0x4;
static const uint AI_UVTRAFO_SCALING = 0x1;
static const uint AI_UVTRAFO_ROTATION = 0x2;
static const uint AI_UVTRAFO_TRANSLATION = 0x4;
static const uint AI_MAX_FACE_INDICES = 0x7fff;
static const uint AI_MAX_BONE_WEIGHTS = 0x7fffffff;
static const uint AI_MAX_VERTICES = 0x7fffffff;
static const uint AI_MAX_FACES = 0x7fffffff;
static const uint AI_MAX_NUMBER_OF_COLOR_SETS = 0x4;
static const uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 0x4;
typedef struct aiLogStream {
  aiLogStreamCallback callback;
  const char* user;
} aiLogStream;
typedef struct aiString {
  size_t length;
  char data[1024];
} aiString;
typedef struct aiMemoryInfo {
  uint textures;
  uint materials;
  uint meshes;
  uint nodes;
  uint animations;
  uint cameras;
  uint lights;
  uint total;
} aiMemoryInfo;
typedef struct aiVector2D {
  float x, y;
} aiVector2D;
typedef struct aiVector3D {
  float x, y, z;
} aiVector3D;
typedef struct aiQuaternion {
  float w, x, y, z;
} aiQuaternion;
typedef struct aiMatrix3x3 {
  float a1, a2, a3;
  float b1, b2, b3;
  float c1, c2, c3;
} aiMatrix3x3;
typedef struct aiMatrix4x4 {
  float a1, a2, a3, a4;
  float b1, b2, b3, b4;
  float c1, c2, c3, c4;
  float d1, d2, d3, d4;
} aiMatrix4x4;
typedef struct aiPlane {
  float a;
  float b;
  float c;
  float d;
} aiPlane;
typedef struct aiRay {
  aiVector3D pos;
  aiVector3D dir;
} aiRay;
typedef struct aiColor3D {
  float r;
  float g;
  float b;
} aiColor3D;
typedef struct aiColor4D {
  float r;
  float g;
  float b;
  float a;
} aiColor4D;
typedef struct aiFileIO {
  aiFileOpenProc OpenProc;
  aiFileCloseProc CloseProc;
  aiUserData UserData;
} aiFileIO;
typedef struct aiFile {
  aiFileReadProc ReadProc;
  aiFileWriteProc WriteProc;
  aiFileTellProc TellProc;
  aiFileTellProc FileSizeProc;
  aiFileSeek SeekProc;
  aiFileFlushProc FlushProc;
  aiUserData UserData;
} aiFile;
typedef struct aiLight {
  aiString mName;
  aiLightSourceType mType;
  aiVector3D mPosition;
  aiVector3D mDirection;
  float mAttenuationConstant;
  float mAttenuationLinear;
  float mAttenuationQuadratic;
  aiColor3D mColorDiffuse;
  aiColor3D mColorSpecular;
  aiColor3D mColorAmbient;
  float mAngleInnerCone;
  float mAngleOuterCone;
} aiLight;
typedef struct aiCamera {
  aiString mName;
  aiVector3D mPosition;
  aiVector3D mUp;
  aiVector3D mLookAt;
  float mHorizontalFOV;
  float mClipPlaneNear;
  float mClipPlaneFar;
  float mAspect;
} aiCamera;
typedef struct aiVectorKey {
  double mTime;
  aiVector3D mValue;
} aiVectorKey;
typedef struct aiQuatKey {
  double mTime;
  aiQuaternion mValue;
} aiQuatKey;
typedef struct aiNodeAnim {
  aiString mNodeName;
  uint mNumPositionKeys;
  aiVectorKey* mPositionKeys;
  uint mNumRotationKeys;
  aiQuatKey* mRotationKeys;
  uint mNumScalingKeys;
  aiVectorKey* mScalingKeys;
  aiAnimBehaviour mPreState;
  aiAnimBehaviour mPostState;
} aiNodeAnim;
typedef struct aiAnimation {
  aiString mName;
  double mDuration;
  double mTicksPerSecond;
  uint mNumChannels;
  aiNodeAnim** mChannels;
} aiAnimation;
typedef struct aiFace {
  uint mNumIndices;
  uint* mIndices;
} aiFace;
typedef struct aiVertexWeight {
  uint mVertexId;
  float mWeight;
} aiVertexWeight;
typedef struct aiBone {
  aiString mName;
  uint mNumWeights;
  aiVertexWeight* mWeights;
  aiMatrix4x4 mOffsetMatrix;
} aiBone;
typedef struct aiAnimMesh {
  aiVector3D* mVertices;
  aiVector3D* mNormals;
  aiVector3D* mTangents;
  aiVector3D* mBitangents;
  aiColor4D* mColors[0x4];
  aiVector3D* mTextureCoords[0x4];
  uint mNumVertices;
} aiAnimMesh;
typedef struct aiMesh {
  uint mPrimitiveTypes;
  uint mNumVertices;
  uint mNumFaces;
  aiVector3D* mVertices;
  aiVector3D* mNormals;
  aiVector3D* mTangents;
  aiVector3D* mBitangents;
  aiColor4D* mColors[0x4];
  aiVector3D* mTextureCoords[0x4];
  uint mNumUVComponents[0x4];
  aiFace* mFaces;
  uint mNumBones;
  aiBone** mBones;
  uint mMaterialIndex;
  aiString mName;
  uint mNumAnimMeshes;
  aiAnimMesh** mAnimMeshes;
} aiMesh;
typedef struct aiUVTransform {
  aiVector2D mTranslation;
  aiVector2D mScaling;
  float mRotation;
} aiUVTransform;
typedef struct aiMaterialProperty {
  aiString mKey;
  uint mSemantic;
  uint mIndex;
  uint mDataLength;
  aiPropertyTypeInfo mType;
  char* mData;
} aiMaterialProperty;
typedef struct aiMaterial {
  aiMaterialProperty** mProperties;
  uint mNumProperties;
  uint mNumAllocated;
} aiMaterial;
typedef struct aiTexel {
  ubyte b, g, r, a;
} aiTexel;
typedef struct aiTexture {
  uint mWidth;
  uint mHeight;
  char achFormatHint[4];
  aiTexel* pcData;
} aiTexture;
typedef struct aiNode {
  aiString mName;
  aiMatrix4x4 mTransformation;
  struct aiNode* mParent;
  uint mNumChildren;
  struct aiNode** mChildren;
  int mNumMeshes;
  uint* mMeshes;
} aiNode;
typedef struct aiScene {
  uint mFlags;
  aiNode* mRootNode;
  uint mNumMeshes;
  aiMesh** mMeshes;
  uint mNumMaterials;
  aiMaterial** mMaterials;
  uint mNumAnimations;
  aiAnimation** mAnimations;
  uint mNumTextures;
  aiTexture** mTextures;
  uint mNumLights;
  aiLight** mLights;
  uint mNumCameras;
  aiCamera** mCameras;
} aiScene;
aiScene* aiImportFile( char* pFile, uint pFile );
aiScene* aiImportFileEx( char* pFile, uint pFlags, aiFileIO* pFS );
aiScene* aiImportFileFromMemory( char* pBuffer, uint pLength, uint pFlags, char* pHint );
aiScene* aiApplyPostProcessing( aiScene* pScene, uint pFlags );
aiLogStream aiGetPredefinedLogStream( aiDefaultLogStream pStreams, char* file );
void aiAttachLogStream( aiLogStream* stream );
void aiEnableVerboseLogging( aiBool d );
aiReturn aiDetachLogStream( aiLogStream* stream );
void aiDetachAllLogStreams();
void aiReleaseImport( aiScene* pScene );
const char* aiGetErrorString();
aiBool aiIsExtensionSupported( char* szExtension );
void aiGetExtensionList( aiString* szOut );
void aiGetMemoryRequirements( aiScene* pIn, aiMemoryInfo* info );
void aiSetImportPropertyInteger( char* szName, int value );
void aiSetImportPropertyFloat( char* szName, float value );
void aiSetImportPropertyString( char* szName, aiString* st );
void aiCreateQuaternionFromMatrix( aiQuaternion* quat, aiMatrix3x3* mat );
void aiDecomposeMatrix( aiMatrix4x4* mat, aiVector3D* scaling, aiQuaternion* rotation, aiVector3D* position );
void aiTransposeMatrix4( aiMatrix4x4* mat );
void aiTransposeMatrix3( aiMatrix3x3* mat );
void aiTransformVecByMatrix3( aiVector3D* vec, aiMatrix3x3* mat );
void aiTransformVecByMatrix4( aiVector3D* vec, aiMatrix4x4* mat );
void aiMultiplyMatrix4( aiMatrix4x4* dst, aiMatrix4x4* src );
void aiMultiplyMatrix3( aiMatrix3x3* dst, aiMatrix3x3* src );
void aiIdentityMatrix3( aiMatrix3x3* mat );
void aiIdentityMatrix4( aiMatrix4x4* mat );
aiReturn aiGetMaterialProperty( aiMaterial* pMat, char* pKey, uint type, uint index, aiMaterialProperty** pPropOut );
aiReturn aiGetMaterialFloatArray( aiMaterial* pMat, char* pKey, uint type, uint index, float* pOut, uint* pMax );
aiReturn aiGetMaterialIntegerArray( aiMaterial* pMat, char* pKey, uint type, uint index, int* pOut, uint* pMax );
aiReturn aiGetMaterialColor( aiMaterial* pMat, char* pKey, uint type, uint index, aiColor4D* pOut );
aiReturn aiGetMaterialString( aiMaterial* pMat, char* pKey, uint type, uint index, aiString* pOut );
uint aiGetMaterialTextureCount( aiMaterial* pMat, aiTextureType type );
aiReturn aiGetMaterialTexture( aiMaterial* mat, aiTextureType type, uint index, aiString* path, aiTextureMapping* mapping , uint* uvindex , float* blend , aiTextureOp* op , aiTextureMapMode* mapmode );
const char* aiGetLegalString();
uint aiGetVersionMinor();
uint aiGetVersionMajor();
uint aiGetVersionRevision();
uint aiGetCompileFlags();
]]  
--auto-generated api from ffi headers
local api =
  {
  ["ASSIMP_CFLAGS_SHARED"] = { type ='value', description = "static const uint = 0x1", valuetype = nil, },
  ["ASSIMP_CFLAGS_STLPORT"] = { type ='value', description = "static const uint = 0x2", valuetype = nil, },
  ["ASSIMP_CFLAGS_DEBUG"] = { type ='value', description = "static const uint = 0x4", valuetype = nil, },
  ["ASSIMP_CFLAGS_NOBOOST"] = { type ='value', description = "static const uint = 0x8", valuetype = nil, },
  ["ASSIMP_CFLAGS_SINGLETHREADED"] = { type ='value', description = "static const uint = 0x10", valuetype = nil, },
  ["AI_TYPES_MAXLEN"] = { type ='value', description = "static const size_t = 1024", valuetype = nil, },
  ["AI_SLM_DEFAULT_MAX_TRIANGLES"] = { type ='value', description = "static const uint = 1000000", valuetype = nil, },
  ["AI_SLM_DEFAULT_MAX_VERTICES"] = { type ='value', description = "static const uint = 1000000", valuetype = nil, },
  ["AI_LMW_MAX_WEIGHTS"] = { type ='value', description = "static const uint = 0x4", valuetype = nil, },
  ["AI_UVTRAFO_SCALING"] = { type ='value', description = "static const uint = 0x1", valuetype = nil, },
  ["AI_UVTRAFO_ROTATION"] = { type ='value', description = "static const uint = 0x2", valuetype = nil, },
  ["AI_UVTRAFO_TRANSLATION"] = { type ='value', description = "static const uint = 0x4", valuetype = nil, },
  ["AI_MAX_FACE_INDICES"] = { type ='value', description = "static const uint = 0x7fff", valuetype = nil, },
  ["AI_MAX_BONE_WEIGHTS"] = { type ='value', description = "static const uint = 0x7fffffff", valuetype = nil, },
  ["AI_MAX_VERTICES"] = { type ='value', description = "static const uint = 0x7fffffff", valuetype = nil, },
  ["AI_MAX_FACES"] = { type ='value', description = "static const uint = 0x7fffffff", valuetype = nil, },
  ["AI_MAX_NUMBER_OF_COLOR_SETS"] = { type ='value', description = "static const uint = 0x4", valuetype = nil, },
  ["AI_MAX_NUMBER_OF_TEXTURECOORDS"] = { type ='value', description = "static const uint = 0x4", valuetype = nil, },
  ["aiBool_FALSE"] = { type ='value', },
  ["aiBool_TRUE"] = { type ='value', },
  ["aiReturn_SUCCESS"] = { type ='value', },
  ["aiReturn_FAILURE"] = { type ='value', },
  ["aiReturn_OUTOFMEMORY"] = { type ='value', },
  ["aiOrigin_SET"] = { type ='value', },
  ["aiOrigin_CUR"] = { type ='value', },
  ["aiOrigin_END"] = { type ='value', },
  ["aiDefaultLogStream_FILE"] = { type ='value', },
  ["aiDefaultLogStream_STDOUT"] = { type ='value', },
  ["aiDefaultLogStream_STDERR"] = { type ='value', },
  ["aiDefaultLogStream_DEBUGGER"] = { type ='value', },
  ["aiComponent_NORMALS"] = { type ='value', },
  ["aiComponent_TANGENTS_AND_BITANGENTS"] = { type ='value', },
  ["aiComponent_COLORS"] = { type ='value', },
  ["aiComponent_TEXCOORDS"] = { type ='value', },
  ["aiComponent_BONEWEIGHTS"] = { type ='value', },
  ["aiComponent_ANIMATIONS"] = { type ='value', },
  ["aiComponent_TEXTURES"] = { type ='value', },
  ["aiComponent_LIGHTS"] = { type ='value', },
  ["aiComponent_CAMERAS"] = { type ='value', },
  ["aiComponent_MESHES"] = { type ='value', },
  ["aiComponent_MATERIALS"] = { type ='value', },
  ["aiLightSourceType_UNDEFINED"] = { type ='value', },
  ["aiLightSourceType_DIRECTIONAL"] = { type ='value', },
  ["aiLightSourceType_POINT"] = { type ='value', },
  ["aiLightSourceType_SPOT"] = { type ='value', },
  ["aiAnimBehaviour_DEFAULT"] = { type ='value', },
  ["aiAnimBehaviour_CONSTANT"] = { type ='value', },
  ["aiAnimBehaviour_LINEAR"] = { type ='value', },
  ["aiAnimBehaviour_REPEAT"] = { type ='value', },
  ["aiPrimitiveType_POINT"] = { type ='value', },
  ["aiPrimitiveType_LINE"] = { type ='value', },
  ["aiPrimitiveType_TRIANGLE"] = { type ='value', },
  ["aiPrimitiveType_POLYGON"] = { type ='value', },
  ["aiTextureOp_Multiply"] = { type ='value', },
  ["aiTextureOp_Add"] = { type ='value', },
  ["aiTextureOp_Subtract"] = { type ='value', },
  ["aiTextureOp_Divide"] = { type ='value', },
  ["aiTextureOp_SmoothAdd"] = { type ='value', },
  ["aiTextureOp_SignedAdd"] = { type ='value', },
  ["aiTextureMapMode_Wrap"] = { type ='value', },
  ["aiTextureMapMode_Clamp"] = { type ='value', },
  ["aiTextureMapMode_Decal"] = { type ='value', },
  ["aiTextureMapMode_Mirror"] = { type ='value', },
  ["aiTextureMapping_UV"] = { type ='value', },
  ["aiTextureMapping_SPHERE"] = { type ='value', },
  ["aiTextureMapping_CYLINDER"] = { type ='value', },
  ["aiTextureMapping_BOX"] = { type ='value', },
  ["aiTextureMapping_PLANE"] = { type ='value', },
  ["aiTextureMapping_OTHER"] = { type ='value', },
  ["aiTextureType_NONE"] = { type ='value', },
  ["aiTextureType_DIFFUSE"] = { type ='value', },
  ["aiTextureType_SPECULAR"] = { type ='value', },
  ["aiTextureType_AMBIENT"] = { type ='value', },
  ["aiTextureType_EMISSIVE"] = { type ='value', },
  ["aiTextureType_HEIGHT"] = { type ='value', },
  ["aiTextureType_NORMALS"] = { type ='value', },
  ["aiTextureType_SHININESS"] = { type ='value', },
  ["aiTextureType_OPACITY"] = { type ='value', },
  ["aiTextureType_DISPLACEMENT"] = { type ='value', },
  ["aiTextureType_LIGHTMAP"] = { type ='value', },
  ["aiTextureType_REFLECTION"] = { type ='value', },
  ["aiTextureType_UNKNOWN"] = { type ='value', },
  ["aiShadingMode_Flat"] = { type ='value', },
  ["aiShadingMode_Gouraud"] = { type ='value', },
  ["aiShadingMode_Phong"] = { type ='value', },
  ["aiShadingMode_Blinn"] = { type ='value', },
  ["aiShadingMode_Toon"] = { type ='value', },
  ["aiShadingMode_OrenNayar"] = { type ='value', },
  ["aiShadingMode_Minnaert"] = { type ='value', },
  ["aiShadingMode_CookTorrance"] = { type ='value', },
  ["aiShadingMode_NoShading"] = { type ='value', },
  ["aiShadingMode_Fresnel"] = { type ='value', },
  ["aiTextureFlags_Invert"] = { type ='value', },
  ["aiTextureFlags_UseAlpha"] = { type ='value', },
  ["aiTextureFlags_IgnoreAlpha"] = { type ='value', },
  ["aiBlendMode_Default"] = { type ='value', },
  ["aiBlendMode_Additive"] = { type ='value', },
  ["aiPTI_Float"] = { type ='value', },
  ["aiPTI_String"] = { type ='value', },
  ["aiPTI_Integer"] = { type ='value', },
  ["aiPTI_Buffer"] = { type ='value', },
  ["aiSceneFlags_INCOMPLETE"] = { type ='value', },
  ["aiSceneFlags_VALIDATED"] = { type ='value', },
  ["aiSceneFlags_VALIDATION_WARNING"] = { type ='value', },
  ["aiSceneFlags_NON_VERBOSE_FORMAT"] = { type ='value', },
  ["aiSceneFlags_FLAGS_TERRAIN"] = { type ='value', },
  ["aiProcess_CalcTangentSpace"] = { type ='value', },
  ["aiProcess_JoinIdenticalVertices"] = { type ='value', },
  ["aiProcess_MakeLeftHanded"] = { type ='value', },
  ["aiProcess_Triangulate"] = { type ='value', },
  ["aiProcess_RemoveComponent"] = { type ='value', },
  ["aiProcess_GenNormals"] = { type ='value', },
  ["aiProcess_GenSmoothNormals"] = { type ='value', },
  ["aiProcess_SplitLargeMeshes"] = { type ='value', },
  ["aiProcess_PreTransformVertices"] = { type ='value', },
  ["aiProcess_LimitBoneWeights"] = { type ='value', },
  ["aiProcess_ValidateDataStructure"] = { type ='value', },
  ["aiProcess_ImproveCacheLocality"] = { type ='value', },
  ["aiProcess_RemoveRedundantMaterials"] = { type ='value', },
  ["aiProcess_FixInfacingNormals"] = { type ='value', },
  ["aiProcess_SortByPType"] = { type ='value', },
  ["aiProcess_FindDegenerates"] = { type ='value', },
  ["aiProcess_FindInvalidData"] = { type ='value', },
  ["aiProcess_GenUVCoords"] = { type ='value', },
  ["aiProcess_TransformUVCoords"] = { type ='value', },
  ["aiProcess_FindInstances"] = { type ='value', },
  ["aiProcess_OptimizeMeshes"] = { type ='value', },
  ["aiProcess_OptimizeGraph"] = { type ='value', },
  ["aiProcess_FlipUVs"] = { type ='value', },
  ["aiProcess_FlipWindingOrder"] = { type ='value', },
  ["aiImportFile"] = { type ='function', 
    description = "", 
    returns = "(aiScene*)",
    valuetype = "ai.aiScene",
    args = "(char* pFile, uint pFile)", },
  ["aiImportFileEx"] = { type ='function', 
    description = "", 
    returns = "(aiScene*)",
    valuetype = "ai.aiScene",
    args = "(char* pFile, uint pFlags, aiFileIO* pFS)", },
  ["aiImportFileFromMemory"] = { type ='function', 
    description = "", 
    returns = "(aiScene*)",
    valuetype = "ai.aiScene",
    args = "(char* pBuffer, uint pLength, uint pFlags, char* pHint)", },
  ["aiApplyPostProcessing"] = { type ='function', 
    description = "", 
    returns = "(aiScene*)",
    valuetype = "ai.aiScene",
    args = "(aiScene* pScene, uint pFlags)", },
  ["aiGetPredefinedLogStream"] = { type ='function', 
    description = "", 
    returns = "(aiLogStream)",
    valuetype = "ai.aiLogStream",
    args = "(aiDefaultLogStream pStreams, char* file)", },
  ["aiAttachLogStream"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiLogStream* stream)", },
  ["aiEnableVerboseLogging"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiBool d)", },
  ["aiDetachLogStream"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiLogStream* stream)", },
  ["aiDetachAllLogStreams"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "()", },
  ["aiReleaseImport"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiScene* pScene)", },
  ["aiGetErrorString"] = { type ='function', 
    description = "", 
    returns = "(const char*)",
    valuetype = "string",
    args = "()", },
  ["aiIsExtensionSupported"] = { type ='function', 
    description = "", 
    returns = "(aiBool)",
    valuetype = nil,
    args = "(char* szExtension)", },
  ["aiGetExtensionList"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiString* szOut)", },
  ["aiGetMemoryRequirements"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiScene* pIn, aiMemoryInfo* info)", },
  ["aiSetImportPropertyInteger"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(char* szName, int value)", },
  ["aiSetImportPropertyFloat"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(char* szName, float value)", },
  ["aiSetImportPropertyString"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(char* szName, aiString* st)", },
  ["aiCreateQuaternionFromMatrix"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiQuaternion* quat, aiMatrix3x3* mat)", },
  ["aiDecomposeMatrix"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix4x4* mat, aiVector3D* scaling, aiQuaternion* rotation, aiVector3D* position)", },
  ["aiTransposeMatrix4"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix4x4* mat)", },
  ["aiTransposeMatrix3"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix3x3* mat)", },
  ["aiTransformVecByMatrix3"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiVector3D* vec, aiMatrix3x3* mat)", },
  ["aiTransformVecByMatrix4"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiVector3D* vec, aiMatrix4x4* mat)", },
  ["aiMultiplyMatrix4"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix4x4* dst, aiMatrix4x4* src)", },
  ["aiMultiplyMatrix3"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix3x3* dst, aiMatrix3x3* src)", },
  ["aiIdentityMatrix3"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix3x3* mat)", },
  ["aiIdentityMatrix4"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(aiMatrix4x4* mat)", },
  ["aiGetMaterialProperty"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiMaterialProperty** pPropOut)", },
  ["aiGetMaterialFloatArray"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* pMat, char* pKey, uint type, uint index, float* pOut, uint* pMax)", },
  ["aiGetMaterialIntegerArray"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* pMat, char* pKey, uint type, uint index, int* pOut, uint* pMax)", },
  ["aiGetMaterialColor"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiColor4D* pOut)", },
  ["aiGetMaterialString"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiString* pOut)", },
  ["aiGetMaterialTextureCount"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "(aiMaterial* pMat, aiTextureType type)", },
  ["aiGetMaterialTexture"] = { type ='function', 
    description = "", 
    returns = "(aiReturn)",
    valuetype = nil,
    args = "(aiMaterial* mat, aiTextureType type, uint index, aiString* path, aiTextureMapping* mapping , uint* uvindex , float* blend , aiTextureOp* op , aiTextureMapMode* mapmode)", },
  ["aiGetLegalString"] = { type ='function', 
    description = "", 
    returns = "(const char*)",
    valuetype = "string",
    args = "()", },
  ["aiGetVersionMinor"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "()", },
  ["aiGetVersionMajor"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "()", },
  ["aiGetVersionRevision"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "()", },
  ["aiGetCompileFlags"] = { type ='function', 
    description = "", 
    returns = "(uint)",
    valuetype = nil,
    args = "()", },
  ["aiLogStream"] = { type ='class', 
    description = "", 
    childs =     {
    ["callback"] = { type ='value', description = "aiLogStreamCallback", valuetype = nil, },
    ["user"] = { type ='value', description = "const char*", valuetype = "string", },
    }
  },
  ["aiString"] = { type ='class', 
    description = "", 
    childs =     {
    ["length"] = { type ='value', description = "size_t", valuetype = nil, },
    ["data"] = { type ='value', description = "char[1024]", valuetype = nil, },
    }
  },
  ["aiMemoryInfo"] = { type ='class', 
    description = "", 
    childs =     {
    ["textures"] = { type ='value', description = "uint", valuetype = nil, },
    ["materials"] = { type ='value', description = "uint", valuetype = nil, },
    ["meshes"] = { type ='value', description = "uint", valuetype = nil, },
    ["nodes"] = { type ='value', description = "uint", valuetype = nil, },
    ["animations"] = { type ='value', description = "uint", valuetype = nil, },
    ["cameras"] = { type ='value', description = "uint", valuetype = nil, },
    ["lights"] = { type ='value', description = "uint", valuetype = nil, },
    ["total"] = { type ='value', description = "uint", valuetype = nil, },
    }
  },
  ["aiVector2D"] = { type ='class', 
    description = "", 
    childs =     {
    ["x"] = { type ='value', description = "float", valuetype = nil, },
    ["y"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiVector3D"] = { type ='class', 
    description = "", 
    childs =     {
    ["x"] = { type ='value', description = "float", valuetype = nil, },
    ["y"] = { type ='value', description = "float", valuetype = nil, },
    ["z"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiQuaternion"] = { type ='class', 
    description = "", 
    childs =     {
    ["w"] = { type ='value', description = "float", valuetype = nil, },
    ["x"] = { type ='value', description = "float", valuetype = nil, },
    ["y"] = { type ='value', description = "float", valuetype = nil, },
    ["z"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiMatrix3x3"] = { type ='class', 
    description = "", 
    childs =     {
    ["a1"] = { type ='value', description = "float", valuetype = nil, },
    ["a2"] = { type ='value', description = "float", valuetype = nil, },
    ["a3"] = { type ='value', description = "float", valuetype = nil, },
    ["b1"] = { type ='value', description = "float", valuetype = nil, },
    ["b2"] = { type ='value', description = "float", valuetype = nil, },
    ["b3"] = { type ='value', description = "float", valuetype = nil, },
    ["c1"] = { type ='value', description = "float", valuetype = nil, },
    ["c2"] = { type ='value', description = "float", valuetype = nil, },
    ["c3"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiMatrix4x4"] = { type ='class', 
    description = "", 
    childs =     {
    ["a1"] = { type ='value', description = "float", valuetype = nil, },
    ["a2"] = { type ='value', description = "float", valuetype = nil, },
    ["a3"] = { type ='value', description = "float", valuetype = nil, },
    ["a4"] = { type ='value', description = "float", valuetype = nil, },
    ["b1"] = { type ='value', description = "float", valuetype = nil, },
    ["b2"] = { type ='value', description = "float", valuetype = nil, },
    ["b3"] = { type ='value', description = "float", valuetype = nil, },
    ["b4"] = { type ='value', description = "float", valuetype = nil, },
    ["c1"] = { type ='value', description = "float", valuetype = nil, },
    ["c2"] = { type ='value', description = "float", valuetype = nil, },
    ["c3"] = { type ='value', description = "float", valuetype = nil, },
    ["c4"] = { type ='value', description = "float", valuetype = nil, },
    ["d1"] = { type ='value', description = "float", valuetype = nil, },
    ["d2"] = { type ='value', description = "float", valuetype = nil, },
    ["d3"] = { type ='value', description = "float", valuetype = nil, },
    ["d4"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiPlane"] = { type ='class', 
    description = "", 
    childs =     {
    ["a"] = { type ='value', description = "float", valuetype = nil, },
    ["b"] = { type ='value', description = "float", valuetype = nil, },
    ["c"] = { type ='value', description = "float", valuetype = nil, },
    ["d"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiRay"] = { type ='class', 
    description = "", 
    childs =     {
    ["pos"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["dir"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    }
  },
  ["aiColor3D"] = { type ='class', 
    description = "", 
    childs =     {
    ["r"] = { type ='value', description = "float", valuetype = nil, },
    ["g"] = { type ='value', description = "float", valuetype = nil, },
    ["b"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiColor4D"] = { type ='class', 
    description = "", 
    childs =     {
    ["r"] = { type ='value', description = "float", valuetype = nil, },
    ["g"] = { type ='value', description = "float", valuetype = nil, },
    ["b"] = { type ='value', description = "float", valuetype = nil, },
    ["a"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiFileIO"] = { type ='class', 
    description = "", 
    childs =     {
    ["OpenProc"] = { type ='value', description = "aiFileOpenProc", valuetype = nil, },
    ["CloseProc"] = { type ='value', description = "aiFileCloseProc", valuetype = nil, },
    ["UserData"] = { type ='value', description = "aiUserData", valuetype = nil, },
    }
  },
  ["aiFile"] = { type ='class', 
    description = "", 
    childs =     {
    ["ReadProc"] = { type ='value', description = "aiFileReadProc", valuetype = nil, },
    ["WriteProc"] = { type ='value', description = "aiFileWriteProc", valuetype = nil, },
    ["TellProc"] = { type ='value', description = "aiFileTellProc", valuetype = nil, },
    ["FileSizeProc"] = { type ='value', description = "aiFileTellProc", valuetype = nil, },
    ["SeekProc"] = { type ='value', description = "aiFileSeek", valuetype = nil, },
    ["FlushProc"] = { type ='value', description = "aiFileFlushProc", valuetype = nil, },
    ["UserData"] = { type ='value', description = "aiUserData", valuetype = nil, },
    }
  },
  ["aiLight"] = { type ='class', 
    description = "", 
    childs =     {
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mType"] = { type ='value', description = "aiLightSourceType", valuetype = nil, },
    ["mPosition"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["mDirection"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["mAttenuationConstant"] = { type ='value', description = "float", valuetype = nil, },
    ["mAttenuationLinear"] = { type ='value', description = "float", valuetype = nil, },
    ["mAttenuationQuadratic"] = { type ='value', description = "float", valuetype = nil, },
    ["mColorDiffuse"] = { type ='value', description = "aiColor3D", valuetype = "ai.aiColor3D", },
    ["mColorSpecular"] = { type ='value', description = "aiColor3D", valuetype = "ai.aiColor3D", },
    ["mColorAmbient"] = { type ='value', description = "aiColor3D", valuetype = "ai.aiColor3D", },
    ["mAngleInnerCone"] = { type ='value', description = "float", valuetype = nil, },
    ["mAngleOuterCone"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiCamera"] = { type ='class', 
    description = "", 
    childs =     {
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mPosition"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["mUp"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["mLookAt"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    ["mHorizontalFOV"] = { type ='value', description = "float", valuetype = nil, },
    ["mClipPlaneNear"] = { type ='value', description = "float", valuetype = nil, },
    ["mClipPlaneFar"] = { type ='value', description = "float", valuetype = nil, },
    ["mAspect"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiVectorKey"] = { type ='class', 
    description = "", 
    childs =     {
    ["mTime"] = { type ='value', description = "double", valuetype = nil, },
    ["mValue"] = { type ='value', description = "aiVector3D", valuetype = "ai.aiVector3D", },
    }
  },
  ["aiQuatKey"] = { type ='class', 
    description = "", 
    childs =     {
    ["mTime"] = { type ='value', description = "double", valuetype = nil, },
    ["mValue"] = { type ='value', description = "aiQuaternion", valuetype = "ai.aiQuaternion", },
    }
  },
  ["aiNodeAnim"] = { type ='class', 
    description = "", 
    childs =     {
    ["mNodeName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mNumPositionKeys"] = { type ='value', description = "uint", valuetype = nil, },
    ["mPositionKeys"] = { type ='value', description = "aiVectorKey*", valuetype = "ai.aiVectorKey", },
    ["mNumRotationKeys"] = { type ='value', description = "uint", valuetype = nil, },
    ["mRotationKeys"] = { type ='value', description = "aiQuatKey*", valuetype = "ai.aiQuatKey", },
    ["mNumScalingKeys"] = { type ='value', description = "uint", valuetype = nil, },
    ["mScalingKeys"] = { type ='value', description = "aiVectorKey*", valuetype = "ai.aiVectorKey", },
    ["mPreState"] = { type ='value', description = "aiAnimBehaviour", valuetype = nil, },
    ["mPostState"] = { type ='value', description = "aiAnimBehaviour", valuetype = nil, },
    }
  },
  ["aiAnimation"] = { type ='class', 
    description = "", 
    childs =     {
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mDuration"] = { type ='value', description = "double", valuetype = nil, },
    ["mTicksPerSecond"] = { type ='value', description = "double", valuetype = nil, },
    ["mNumChannels"] = { type ='value', description = "uint", valuetype = nil, },
    ["mChannels"] = { type ='value', description = "aiNodeAnim**", valuetype = "ai.aiNodeAnim", },
    }
  },
  ["aiFace"] = { type ='class', 
    description = "", 
    childs =     {
    ["mNumIndices"] = { type ='value', description = "uint", valuetype = nil, },
    ["mIndices"] = { type ='value', description = "uint*", valuetype = nil, },
    }
  },
  ["aiVertexWeight"] = { type ='class', 
    description = "", 
    childs =     {
    ["mVertexId"] = { type ='value', description = "uint", valuetype = nil, },
    ["mWeight"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiBone"] = { type ='class', 
    description = "", 
    childs =     {
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mNumWeights"] = { type ='value', description = "uint", valuetype = nil, },
    ["mWeights"] = { type ='value', description = "aiVertexWeight*", valuetype = "ai.aiVertexWeight", },
    ["mOffsetMatrix"] = { type ='value', description = "aiMatrix4x4", valuetype = "ai.aiMatrix4x4", },
    }
  },
  ["aiAnimMesh"] = { type ='class', 
    description = "", 
    childs =     {
    ["mVertices"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mNormals"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mTangents"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mBitangents"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mColors"] = { type ='value', description = "aiColor4D*[0x4]", valuetype = "ai.aiColor4D", },
    ["mTextureCoords"] = { type ='value', description = "aiVector3D*[0x4]", valuetype = "ai.aiVector3D", },
    ["mNumVertices"] = { type ='value', description = "uint", valuetype = nil, },
    }
  },
  ["aiMesh"] = { type ='class', 
    description = "", 
    childs =     {
    ["mPrimitiveTypes"] = { type ='value', description = "uint", valuetype = nil, },
    ["mNumVertices"] = { type ='value', description = "uint", valuetype = nil, },
    ["mNumFaces"] = { type ='value', description = "uint", valuetype = nil, },
    ["mVertices"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mNormals"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mTangents"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mBitangents"] = { type ='value', description = "aiVector3D*", valuetype = "ai.aiVector3D", },
    ["mColors"] = { type ='value', description = "aiColor4D*[0x4]", valuetype = "ai.aiColor4D", },
    ["mTextureCoords"] = { type ='value', description = "aiVector3D*[0x4]", valuetype = "ai.aiVector3D", },
    ["mNumUVComponents"] = { type ='value', description = "uint[0x4]", valuetype = nil, },
    ["mFaces"] = { type ='value', description = "aiFace*", valuetype = "ai.aiFace", },
    ["mNumBones"] = { type ='value', description = "uint", valuetype = nil, },
    ["mBones"] = { type ='value', description = "aiBone**", valuetype = "ai.aiBone", },
    ["mMaterialIndex"] = { type ='value', description = "uint", valuetype = nil, },
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mNumAnimMeshes"] = { type ='value', description = "uint", valuetype = nil, },
    ["mAnimMeshes"] = { type ='value', description = "aiAnimMesh**", valuetype = "ai.aiAnimMesh", },
    }
  },
  ["aiUVTransform"] = { type ='class', 
    description = "", 
    childs =     {
    ["mTranslation"] = { type ='value', description = "aiVector2D", valuetype = "ai.aiVector2D", },
    ["mScaling"] = { type ='value', description = "aiVector2D", valuetype = "ai.aiVector2D", },
    ["mRotation"] = { type ='value', description = "float", valuetype = nil, },
    }
  },
  ["aiMaterialProperty"] = { type ='class', 
    description = "", 
    childs =     {
    ["mKey"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mSemantic"] = { type ='value', description = "uint", valuetype = nil, },
    ["mIndex"] = { type ='value', description = "uint", valuetype = nil, },
    ["mDataLength"] = { type ='value', description = "uint", valuetype = nil, },
    ["mType"] = { type ='value', description = "aiPropertyTypeInfo", valuetype = nil, },
    ["mData"] = { type ='value', description = "char*", valuetype = nil, },
    }
  },
  ["aiMaterial"] = { type ='class', 
    description = "", 
    childs =     {
    ["mProperties"] = { type ='value', description = "aiMaterialProperty**", valuetype = "ai.aiMaterialProperty", },
    ["mNumProperties"] = { type ='value', description = "uint", valuetype = nil, },
    ["mNumAllocated"] = { type ='value', description = "uint", valuetype = nil, },
    }
  },
  ["aiTexel"] = { type ='class', 
    description = "", 
    childs =     {
    ["b"] = { type ='value', description = "ubyte", valuetype = nil, },
    ["g"] = { type ='value', description = "ubyte", valuetype = nil, },
    ["r"] = { type ='value', description = "ubyte", valuetype = nil, },
    ["a"] = { type ='value', description = "ubyte", valuetype = nil, },
    }
  },
  ["aiTexture"] = { type ='class', 
    description = "", 
    childs =     {
    ["mWidth"] = { type ='value', description = "uint", valuetype = nil, },
    ["mHeight"] = { type ='value', description = "uint", valuetype = nil, },
    ["achFormatHint"] = { type ='value', description = "char[4]", valuetype = nil, },
    ["pcData"] = { type ='value', description = "aiTexel*", valuetype = "ai.aiTexel", },
    }
  },
  ["aiNode"] = { type ='class', 
    description = "", 
    childs =     {
    ["mName"] = { type ='value', description = "aiString", valuetype = "ai.aiString", },
    ["mTransformation"] = { type ='value', description = "aiMatrix4x4", valuetype = "ai.aiMatrix4x4", },
    ["mParent"] = { type ='value', description = "struct aiNode*", valuetype = "ai.aiNode", },
    ["mNumChildren"] = { type ='value', description = "uint", valuetype = nil, },
    ["mChildren"] = { type ='value', description = "struct aiNode**", valuetype = "ai.aiNode", },
    ["mNumMeshes"] = { type ='value', description = "int", valuetype = nil, },
    ["mMeshes"] = { type ='value', description = "uint*", valuetype = nil, },
    }
  },
  ["aiScene"] = { type ='class', 
    description = "", 
    childs =     {
    ["mFlags"] = { type ='value', description = "uint", valuetype = nil, },
    ["mRootNode"] = { type ='value', description = "aiNode*", valuetype = "ai.aiNode", },
    ["mNumMeshes"] = { type ='value', description = "uint", valuetype = nil, },
    ["mMeshes"] = { type ='value', description = "aiMesh**", valuetype = "ai.aiMesh", },
    ["mNumMaterials"] = { type ='value', description = "uint", valuetype = nil, },
    ["mMaterials"] = { type ='value', description = "aiMaterial**", valuetype = "ai.aiMaterial", },
    ["mNumAnimations"] = { type ='value', description = "uint", valuetype = nil, },
    ["mAnimations"] = { type ='value', description = "aiAnimation**", valuetype = "ai.aiAnimation", },
    ["mNumTextures"] = { type ='value', description = "uint", valuetype = nil, },
    ["mTextures"] = { type ='value', description = "aiTexture**", valuetype = "ai.aiTexture", },
    ["mNumLights"] = { type ='value', description = "uint", valuetype = nil, },
    ["mLights"] = { type ='value', description = "aiLight**", valuetype = "ai.aiLight", },
    ["mNumCameras"] = { type ='value', description = "uint", valuetype = nil, },
    ["mCameras"] = { type ='value', description = "aiCamera**", valuetype = "ai.aiCamera", },
    }
  },
  }
return {
  ai = {
    type = 'lib',
    description = "AssetImporter Model Loader Library",
    childs = api,
  },
  assimp = {
    type = 'lib',
    description = "AssetImporter Model Loader Library",
    childs = api,
  },
}
