--[[// assimp ai | AssetImporter Model Loader Library
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
  char* user;
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
void aiLogStreamCallback( char* message, char* user );
size_t aiFileWriteProc( aiFile*, char*, size_t, size_t );
size_t aiFileReadProc( aiFile*, char*, size_t, size_t );
size_t aiFileTellProc( aiFile* );
void aiFileFlushProc( aiFile* );
aiReturn aiFileSeek( aiFile*, size_t, aiOrigin );
aiFile* aiFileOpenProc( aiFileIO*, char*, char* );
void aiFileCloseProc( aiFileIO*, aiFile* );
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
char* aiGetErrorString();
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
char* aiGetLegalString();
uint aiGetVersionMinor();
uint aiGetVersionMajor();
uint aiGetVersionRevision();
uint aiGetCompileFlags();
]]
--auto-generated api from ffi headers

local api = {
  ["ASSIMP_CFLAGS_SHARED"] = { type ='value', description = "0x1", },
  ["ASSIMP_CFLAGS_STLPORT"] = { type ='value', description = "0x2", },
  ["ASSIMP_CFLAGS_DEBUG"] = { type ='value', description = "0x4", },
  ["ASSIMP_CFLAGS_NOBOOST"] = { type ='value', description = "0x8", },
  ["ASSIMP_CFLAGS_SINGLETHREADED"] = { type ='value', description = "0x10", },
  ["AI_TYPES_MAXLEN"] = { type ='value', description = "1024", },
  ["AI_SLM_DEFAULT_MAX_TRIANGLES"] = { type ='value', description = "1000000", },
  ["AI_SLM_DEFAULT_MAX_VERTICES"] = { type ='value', description = "1000000", },
  ["AI_LMW_MAX_WEIGHTS"] = { type ='value', description = "0x4", },
  ["AI_UVTRAFO_SCALING"] = { type ='value', description = "0x1", },
  ["AI_UVTRAFO_ROTATION"] = { type ='value', description = "0x2", },
  ["AI_UVTRAFO_TRANSLATION"] = { type ='value', description = "0x4", },
  ["AI_MAX_FACE_INDICES"] = { type ='value', description = "0x7fff", },
  ["AI_MAX_BONE_WEIGHTS"] = { type ='value', description = "0x7fffffff", },
  ["AI_MAX_VERTICES"] = { type ='value', description = "0x7fffffff", },
  ["AI_MAX_FACES"] = { type ='value', description = "0x7fffffff", },
  ["AI_MAX_NUMBER_OF_COLOR_SETS"] = { type ='value', description = "0x4", },
  ["AI_MAX_NUMBER_OF_TEXTURECOORDS"] = { type ='value', description = "0x4", },
  ["aiBool_FALSE"] = { type ='keyword', },
  ["aiBool_TRUE"] = { type ='keyword', },
  ["aiReturn_SUCCESS"] = { type ='keyword', },
  ["aiReturn_FAILURE"] = { type ='keyword', },
  ["aiReturn_OUTOFMEMORY"] = { type ='keyword', },
  ["aiOrigin_SET"] = { type ='keyword', },
  ["aiOrigin_CUR"] = { type ='keyword', },
  ["aiOrigin_END"] = { type ='keyword', },
  ["aiDefaultLogStream_FILE"] = { type ='keyword', },
  ["aiDefaultLogStream_STDOUT"] = { type ='keyword', },
  ["aiDefaultLogStream_STDERR"] = { type ='keyword', },
  ["aiDefaultLogStream_DEBUGGER"] = { type ='keyword', },
  ["aiComponent_NORMALS"] = { type ='keyword', },
  ["aiComponent_TANGENTS_AND_BITANGENTS"] = { type ='keyword', },
  ["aiComponent_COLORS"] = { type ='keyword', },
  ["aiComponent_TEXCOORDS"] = { type ='keyword', },
  ["aiComponent_BONEWEIGHTS"] = { type ='keyword', },
  ["aiComponent_ANIMATIONS"] = { type ='keyword', },
  ["aiComponent_TEXTURES"] = { type ='keyword', },
  ["aiComponent_LIGHTS"] = { type ='keyword', },
  ["aiComponent_CAMERAS"] = { type ='keyword', },
  ["aiComponent_MESHES"] = { type ='keyword', },
  ["aiComponent_MATERIALS"] = { type ='keyword', },
  ["aiLightSourceType_UNDEFINED"] = { type ='keyword', },
  ["aiLightSourceType_DIRECTIONAL"] = { type ='keyword', },
  ["aiLightSourceType_POINT"] = { type ='keyword', },
  ["aiLightSourceType_SPOT"] = { type ='keyword', },
  ["aiAnimBehaviour_DEFAULT"] = { type ='keyword', },
  ["aiAnimBehaviour_CONSTANT"] = { type ='keyword', },
  ["aiAnimBehaviour_LINEAR"] = { type ='keyword', },
  ["aiAnimBehaviour_REPEAT"] = { type ='keyword', },
  ["aiPrimitiveType_POINT"] = { type ='keyword', },
  ["aiPrimitiveType_LINE"] = { type ='keyword', },
  ["aiPrimitiveType_TRIANGLE"] = { type ='keyword', },
  ["aiPrimitiveType_POLYGON"] = { type ='keyword', },
  ["aiTextureOp_Multiply"] = { type ='keyword', },
  ["aiTextureOp_Add"] = { type ='keyword', },
  ["aiTextureOp_Subtract"] = { type ='keyword', },
  ["aiTextureOp_Divide"] = { type ='keyword', },
  ["aiTextureOp_SmoothAdd"] = { type ='keyword', },
  ["aiTextureOp_SignedAdd"] = { type ='keyword', },
  ["aiTextureMapMode_Wrap"] = { type ='keyword', },
  ["aiTextureMapMode_Clamp"] = { type ='keyword', },
  ["aiTextureMapMode_Decal"] = { type ='keyword', },
  ["aiTextureMapMode_Mirror"] = { type ='keyword', },
  ["aiTextureMapping_UV"] = { type ='keyword', },
  ["aiTextureMapping_SPHERE"] = { type ='keyword', },
  ["aiTextureMapping_CYLINDER"] = { type ='keyword', },
  ["aiTextureMapping_BOX"] = { type ='keyword', },
  ["aiTextureMapping_PLANE"] = { type ='keyword', },
  ["aiTextureMapping_OTHER"] = { type ='keyword', },
  ["aiTextureType_NONE"] = { type ='keyword', },
  ["aiTextureType_DIFFUSE"] = { type ='keyword', },
  ["aiTextureType_SPECULAR"] = { type ='keyword', },
  ["aiTextureType_AMBIENT"] = { type ='keyword', },
  ["aiTextureType_EMISSIVE"] = { type ='keyword', },
  ["aiTextureType_HEIGHT"] = { type ='keyword', },
  ["aiTextureType_NORMALS"] = { type ='keyword', },
  ["aiTextureType_SHININESS"] = { type ='keyword', },
  ["aiTextureType_OPACITY"] = { type ='keyword', },
  ["aiTextureType_DISPLACEMENT"] = { type ='keyword', },
  ["aiTextureType_LIGHTMAP"] = { type ='keyword', },
  ["aiTextureType_REFLECTION"] = { type ='keyword', },
  ["aiTextureType_UNKNOWN"] = { type ='keyword', },
  ["aiShadingMode_Flat"] = { type ='keyword', },
  ["aiShadingMode_Gouraud"] = { type ='keyword', },
  ["aiShadingMode_Phong"] = { type ='keyword', },
  ["aiShadingMode_Blinn"] = { type ='keyword', },
  ["aiShadingMode_Toon"] = { type ='keyword', },
  ["aiShadingMode_OrenNayar"] = { type ='keyword', },
  ["aiShadingMode_Minnaert"] = { type ='keyword', },
  ["aiShadingMode_CookTorrance"] = { type ='keyword', },
  ["aiShadingMode_NoShading"] = { type ='keyword', },
  ["aiShadingMode_Fresnel"] = { type ='keyword', },
  ["aiTextureFlags_Invert"] = { type ='keyword', },
  ["aiTextureFlags_UseAlpha"] = { type ='keyword', },
  ["aiTextureFlags_IgnoreAlpha"] = { type ='keyword', },
  ["aiBlendMode_Default"] = { type ='keyword', },
  ["aiBlendMode_Additive"] = { type ='keyword', },
  ["aiPTI_Float"] = { type ='keyword', },
  ["aiPTI_String"] = { type ='keyword', },
  ["aiPTI_Integer"] = { type ='keyword', },
  ["aiPTI_Buffer"] = { type ='keyword', },
  ["aiSceneFlags_INCOMPLETE"] = { type ='keyword', },
  ["aiSceneFlags_VALIDATED"] = { type ='keyword', },
  ["aiSceneFlags_VALIDATION_WARNING"] = { type ='keyword', },
  ["aiSceneFlags_NON_VERBOSE_FORMAT"] = { type ='keyword', },
  ["aiSceneFlags_FLAGS_TERRAIN"] = { type ='keyword', },
  ["aiProcess_CalcTangentSpace"] = { type ='keyword', },
  ["aiProcess_JoinIdenticalVertices"] = { type ='keyword', },
  ["aiProcess_MakeLeftHanded"] = { type ='keyword', },
  ["aiProcess_Triangulate"] = { type ='keyword', },
  ["aiProcess_RemoveComponent"] = { type ='keyword', },
  ["aiProcess_GenNormals"] = { type ='keyword', },
  ["aiProcess_GenSmoothNormals"] = { type ='keyword', },
  ["aiProcess_SplitLargeMeshes"] = { type ='keyword', },
  ["aiProcess_PreTransformVertices"] = { type ='keyword', },
  ["aiProcess_LimitBoneWeights"] = { type ='keyword', },
  ["aiProcess_ValidateDataStructure"] = { type ='keyword', },
  ["aiProcess_ImproveCacheLocality"] = { type ='keyword', },
  ["aiProcess_RemoveRedundantMaterials"] = { type ='keyword', },
  ["aiProcess_FixInfacingNormals"] = { type ='keyword', },
  ["aiProcess_SortByPType"] = { type ='keyword', },
  ["aiProcess_FindDegenerates"] = { type ='keyword', },
  ["aiProcess_FindInvalidData"] = { type ='keyword', },
  ["aiProcess_GenUVCoords"] = { type ='keyword', },
  ["aiProcess_TransformUVCoords"] = { type ='keyword', },
  ["aiProcess_FindInstances"] = { type ='keyword', },
  ["aiProcess_OptimizeMeshes"] = { type ='keyword', },
  ["aiProcess_OptimizeGraph"] = { type ='keyword', },
  ["aiProcess_FlipUVs"] = { type ='keyword', },
  ["aiProcess_FlipWindingOrder"] = { type ='keyword', },
  ["aiLogStreamCallback"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(char* message, char* user)", },
  ["aiFileWriteProc"] = { type ='function', 
      description = "", 
      returns = "(size_t)",
      args = "(aiFile*, char*, size_t, size_t)", },
  ["aiFileReadProc"] = { type ='function', 
      description = "", 
      returns = "(size_t)",
      args = "(aiFile*, char*, size_t, size_t)", },
  ["aiFileTellProc"] = { type ='function', 
      description = "", 
      returns = "(size_t)",
      args = "(aiFile*)", },
  ["aiFileFlushProc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiFile*)", },
  ["aiFileSeek"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiFile*, size_t, aiOrigin)", },
  ["aiFileCloseProc"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiFileIO*, aiFile*)", },
  ["aiGetPredefinedLogStream"] = { type ='function', 
      description = "", 
      returns = "(aiLogStream)",
      args = "(aiDefaultLogStream pStreams, char* file)", },
  ["aiAttachLogStream"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiLogStream* stream)", },
  ["aiEnableVerboseLogging"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiBool d)", },
  ["aiDetachLogStream"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiLogStream* stream)", },
  ["aiDetachAllLogStreams"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "()", },
  ["aiReleaseImport"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiScene* pScene)", },
  ["aiIsExtensionSupported"] = { type ='function', 
      description = "", 
      returns = "(aiBool)",
      args = "(char* szExtension)", },
  ["aiGetExtensionList"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiString* szOut)", },
  ["aiGetMemoryRequirements"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiScene* pIn, aiMemoryInfo* info)", },
  ["aiSetImportPropertyInteger"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(char* szName, int value)", },
  ["aiSetImportPropertyFloat"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(char* szName, float value)", },
  ["aiSetImportPropertyString"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(char* szName, aiString* st)", },
  ["aiCreateQuaternionFromMatrix"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiQuaternion* quat, aiMatrix3x3* mat)", },
  ["aiDecomposeMatrix"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix4x4* mat, aiVector3D* scaling, aiQuaternion* rotation, aiVector3D* position)", },
  ["aiTransposeMatrix4"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix4x4* mat)", },
  ["aiTransposeMatrix3"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix3x3* mat)", },
  ["aiTransformVecByMatrix3"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiVector3D* vec, aiMatrix3x3* mat)", },
  ["aiTransformVecByMatrix4"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiVector3D* vec, aiMatrix4x4* mat)", },
  ["aiMultiplyMatrix4"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix4x4* dst, aiMatrix4x4* src)", },
  ["aiMultiplyMatrix3"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix3x3* dst, aiMatrix3x3* src)", },
  ["aiIdentityMatrix3"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix3x3* mat)", },
  ["aiIdentityMatrix4"] = { type ='function', 
      description = "", 
      returns = "()",
      args = "(aiMatrix4x4* mat)", },
  ["aiGetMaterialProperty"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiMaterialProperty** pPropOut)", },
  ["aiGetMaterialFloatArray"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* pMat, char* pKey, uint type, uint index, float* pOut, uint* pMax)", },
  ["aiGetMaterialIntegerArray"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* pMat, char* pKey, uint type, uint index, int* pOut, uint* pMax)", },
  ["aiGetMaterialColor"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiColor4D* pOut)", },
  ["aiGetMaterialString"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* pMat, char* pKey, uint type, uint index, aiString* pOut)", },
  ["aiGetMaterialTextureCount"] = { type ='function', 
      description = "", 
      returns = "(uint)",
      args = "(aiMaterial* pMat, aiTextureType type)", },
  ["aiGetMaterialTexture"] = { type ='function', 
      description = "", 
      returns = "(aiReturn)",
      args = "(aiMaterial* mat, aiTextureType type, uint index, aiString* path, aiTextureMapping* mapping , uint* uvindex , float* blend , aiTextureOp* op , aiTextureMapMode* mapmode)", },
  ["aiGetVersionMinor"] = { type ='function', 
      description = "", 
      returns = "(uint)",
      args = "()", },
  ["aiGetVersionMajor"] = { type ='function', 
      description = "", 
      returns = "(uint)",
      args = "()", },
  ["aiGetVersionRevision"] = { type ='function', 
      description = "", 
      returns = "(uint)",
      args = "()", },
  ["aiGetCompileFlags"] = { type ='function', 
      description = "", 
      returns = "(uint)",
      args = "()", },
}
assimp = {
	type = 'class',
	description = "AssetImporter Model Loader Library",
	childs = api,
}
ai = {
	type = 'class',
	description = "AssetImporter Model Loader Library",
	childs = api,
}
