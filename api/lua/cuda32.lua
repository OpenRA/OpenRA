--[[// cuda cu | Nvidia CUDA Driver API
/*
 * Copyright 1993-2010 NVIDIA Corporation.  All rights reserved.
 *
 * NOTICE TO USER:
 *
 * This source code is subject to NVIDIA ownership rights under U.S. and
 * international Copyright laws.  Users and possessors of this source code
 * are hereby granted a nonexclusive, royalty-free license to use this code
 * in individual and commercial software.
 *
 * NVIDIA MAKES NO REPRESENTATION ABOUT THE SUITABILITY OF THIS SOURCE
 * CODE FOR ANY PURPOSE.  IT IS PROVIDED "AS IS" WITHOUT EXPRESS OR
 * IMPLIED WARRANTY OF ANY KIND.  NVIDIA DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOURCE CODE, INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY, NONINFRINGEMENT, AND FITNESS FOR A PARTICULAR PURPOSE.
 * IN NO EVENT SHALL NVIDIA BE LIABLE FOR ANY SPECIAL, INDIRECT, INCIDENTAL,
 * OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
 * OF USE, DATA OR PROFITS,  WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
 * OR OTHER TORTIOUS ACTION,  ARISING OUT OF OR IN CONNECTION WITH THE USE
 * OR PERFORMANCE OF THIS SOURCE CODE.
 *
 * U.S. Government End Users.   This source code is a "commercial item" as
 * that term is defined at  48 C.F.R. 2.101 (OCT 1995), consisting  of
 * "commercial computer  software"  and "commercial computer software
 * documentation" as such terms are  used in 48 C.F.R. 12.212 (SEPT 1995)
 * and is provided to the U.S. Government only as a commercial end item.
 * Consistent with 48 C.F.R.12.212 and 48 C.F.R. 227.7202-1 through
 * 227.7202-4 (JUNE 1995), all U.S. Government End Users acquire the
 * source code with only those rights set forth herein.
 *
 * Any use of this source code in individual and commercial software must
 * include, in the user documentation and internal comments to the code,
 * the above Disclaimer and U.S. Government End Users Notice.
 */

typedef int CUdevice;                                     
typedef struct CUctx_st *CUcontext;                       
typedef struct CUmod_st *CUmodule;                        
typedef struct CUfunc_st *CUfunction;                     
typedef struct CUarray_st *CUarray;                       
typedef struct CUtexref_st *CUtexref;                     
typedef struct CUsurfref_st *CUsurfref;                   
typedef struct CUevent_st *CUevent;                       
typedef struct CUstream_st *CUstream;                     
typedef struct CUgraphicsResource_st *CUgraphicsResource; 

typedef struct CUuuid_st {                                
    char bytes[16];
} CUuuid;

typedef enum CUctx_flags_enum {
    CU_CTX_SCHED_AUTO  = 0,         
    CU_CTX_SCHED_SPIN  = 1,         
    CU_CTX_SCHED_YIELD = 2,         
    CU_CTX_SCHED_MASK  = 0x3,
    CU_CTX_BLOCKING_SYNC = 4,       
    CU_CTX_MAP_HOST = 8,            
    CU_CTX_LMEM_RESIZE_TO_MAX = 16, 
    CU_CTX_FLAGS_MASK  = 0x1f
} CUctx_flags;

typedef enum CUevent_flags_enum {
    CU_EVENT_DEFAULT        = 0, 
    CU_EVENT_BLOCKING_SYNC  = 1, 
    CU_EVENT_DISABLE_TIMING = 2  
} CUevent_flags;

typedef enum CUarray_format_enum {
    CU_AD_FORMAT_UNSIGNED_INT8  = 0x01, 
    CU_AD_FORMAT_UNSIGNED_INT16 = 0x02, 
    CU_AD_FORMAT_UNSIGNED_INT32 = 0x03, 
    CU_AD_FORMAT_SIGNED_INT8    = 0x08, 
    CU_AD_FORMAT_SIGNED_INT16   = 0x09, 
    CU_AD_FORMAT_SIGNED_INT32   = 0x0a, 
    CU_AD_FORMAT_HALF           = 0x10, 
    CU_AD_FORMAT_FLOAT          = 0x20  
} CUarray_format;

typedef enum CUaddress_mode_enum {
    CU_TR_ADDRESS_MODE_WRAP   = 0, 
    CU_TR_ADDRESS_MODE_CLAMP  = 1, 
    CU_TR_ADDRESS_MODE_MIRROR = 2, 
    CU_TR_ADDRESS_MODE_BORDER = 3  
} CUaddress_mode;

typedef enum CUfilter_mode_enum {
    CU_TR_FILTER_MODE_POINT  = 0, 
    CU_TR_FILTER_MODE_LINEAR = 1  
} CUfilter_mode;

typedef enum CUdevice_attribute_enum {
    CU_DEVICE_ATTRIBUTE_MAX_THREADS_PER_BLOCK = 1,              
    CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_X = 2,                    
    CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_Y = 3,                    
    CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_Z = 4,                    
    CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_X = 5,                     
    CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_Y = 6,                     
    CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_Z = 7,                     
    CU_DEVICE_ATTRIBUTE_MAX_SHARED_MEMORY_PER_BLOCK = 8,        
    CU_DEVICE_ATTRIBUTE_SHARED_MEMORY_PER_BLOCK = 8,            
    CU_DEVICE_ATTRIBUTE_TOTAL_CONSTANT_MEMORY = 9,              
    CU_DEVICE_ATTRIBUTE_WARP_SIZE = 10,                         
    CU_DEVICE_ATTRIBUTE_MAX_PITCH = 11,                         
    CU_DEVICE_ATTRIBUTE_MAX_REGISTERS_PER_BLOCK = 12,           
    CU_DEVICE_ATTRIBUTE_REGISTERS_PER_BLOCK = 12,               
    CU_DEVICE_ATTRIBUTE_CLOCK_RATE = 13,                        
    CU_DEVICE_ATTRIBUTE_TEXTURE_ALIGNMENT = 14,                 
    CU_DEVICE_ATTRIBUTE_GPU_OVERLAP = 15,                       
    CU_DEVICE_ATTRIBUTE_MULTIPROCESSOR_COUNT = 16,              
    CU_DEVICE_ATTRIBUTE_KERNEL_EXEC_TIMEOUT = 17,               
    CU_DEVICE_ATTRIBUTE_INTEGRATED = 18,                        
    CU_DEVICE_ATTRIBUTE_CAN_MAP_HOST_MEMORY = 19,               
    CU_DEVICE_ATTRIBUTE_COMPUTE_MODE = 20,                      
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE1D_WIDTH = 21,           
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_WIDTH = 22,           
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_HEIGHT = 23,          
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_WIDTH = 24,           
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_HEIGHT = 25,          
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_DEPTH = 26,           
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_WIDTH = 27,     
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_HEIGHT = 28,    
    CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_NUMSLICES = 29, 
    CU_DEVICE_ATTRIBUTE_SURFACE_ALIGNMENT = 30,                 
    CU_DEVICE_ATTRIBUTE_CONCURRENT_KERNELS = 31,                
    CU_DEVICE_ATTRIBUTE_ECC_ENABLED = 32,                       
    CU_DEVICE_ATTRIBUTE_PCI_BUS_ID = 33,                        
    CU_DEVICE_ATTRIBUTE_PCI_DEVICE_ID = 34,                     
    CU_DEVICE_ATTRIBUTE_TCC_DRIVER = 35                         
} CUdevice_attribute;

typedef struct CUdevprop_st {
    int maxThreadsPerBlock;     
    int maxThreadsDim[3];       
    int maxGridSize[3];         
    int sharedMemPerBlock;      
    int totalConstantMemory;    
    int SIMDWidth;              
    int memPitch;               
    int regsPerBlock;           
    int clockRate;              
    int textureAlign;           
} CUdevprop;

typedef enum CUfunction_attribute_enum {
    
    CU_FUNC_ATTRIBUTE_MAX_THREADS_PER_BLOCK = 0,
    CU_FUNC_ATTRIBUTE_SHARED_SIZE_BYTES = 1,
    CU_FUNC_ATTRIBUTE_CONST_SIZE_BYTES = 2,
    CU_FUNC_ATTRIBUTE_LOCAL_SIZE_BYTES = 3,
    CU_FUNC_ATTRIBUTE_NUM_REGS = 4,
    CU_FUNC_ATTRIBUTE_PTX_VERSION = 5,
    CU_FUNC_ATTRIBUTE_BINARY_VERSION = 6,

    CU_FUNC_ATTRIBUTE_MAX
} CUfunction_attribute;

typedef enum CUfunc_cache_enum {
    CU_FUNC_CACHE_PREFER_NONE    = 0x00, 
    CU_FUNC_CACHE_PREFER_SHARED  = 0x01, 
    CU_FUNC_CACHE_PREFER_L1      = 0x02  
} CUfunc_cache;

typedef enum CUmemorytype_enum {
    CU_MEMORYTYPE_HOST   = 0x01,    
    CU_MEMORYTYPE_DEVICE = 0x02,    
    CU_MEMORYTYPE_ARRAY  = 0x03     
} CUmemorytype;

typedef enum CUcomputemode_enum {
    CU_COMPUTEMODE_DEFAULT    = 0,  
    CU_COMPUTEMODE_EXCLUSIVE  = 1,  
    CU_COMPUTEMODE_PROHIBITED = 2   
} CUcomputemode;

typedef enum CUjit_option_enum
{
    
    CU_JIT_MAX_REGISTERS = 0,
    CU_JIT_THREADS_PER_BLOCK,
    CU_JIT_WALL_TIME,
    CU_JIT_INFO_LOG_BUFFER,
    CU_JIT_INFO_LOG_BUFFER_SIZE_BYTES,
    CU_JIT_ERROR_LOG_BUFFER,
    CU_JIT_ERROR_LOG_BUFFER_SIZE_BYTES,
    CU_JIT_OPTIMIZATION_LEVEL,
    CU_JIT_TARGET_FROM_CUCONTEXT,
    CU_JIT_TARGET,
    CU_JIT_FALLBACK_STRATEGY

} CUjit_option;

typedef enum CUjit_target_enum
{
    CU_TARGET_COMPUTE_10 = 0,   
    CU_TARGET_COMPUTE_11,       
    CU_TARGET_COMPUTE_12,       
    CU_TARGET_COMPUTE_13,       
    CU_TARGET_COMPUTE_20,       
    CU_TARGET_COMPUTE_21        
} CUjit_target;

typedef enum CUjit_fallback_enum
{
    CU_PREFER_PTX = 0,  

    CU_PREFER_BINARY    

} CUjit_fallback;

typedef enum CUgraphicsRegisterFlags_enum {
    CU_GRAPHICS_REGISTER_FLAGS_NONE  = 0x00
} CUgraphicsRegisterFlags;

typedef enum CUgraphicsMapResourceFlags_enum {
    CU_GRAPHICS_MAP_RESOURCE_FLAGS_NONE          = 0x00,
    CU_GRAPHICS_MAP_RESOURCE_FLAGS_READ_ONLY     = 0x01,
    CU_GRAPHICS_MAP_RESOURCE_FLAGS_WRITE_DISCARD = 0x02
} CUgraphicsMapResourceFlags;

typedef enum CUarray_cubemap_face_enum {
    CU_CUBEMAP_FACE_POSITIVE_X  = 0x00, 
    CU_CUBEMAP_FACE_NEGATIVE_X  = 0x01, 
    CU_CUBEMAP_FACE_POSITIVE_Y  = 0x02, 
    CU_CUBEMAP_FACE_NEGATIVE_Y  = 0x03, 
    CU_CUBEMAP_FACE_POSITIVE_Z  = 0x04, 
    CU_CUBEMAP_FACE_NEGATIVE_Z  = 0x05  
} CUarray_cubemap_face;

typedef enum CUlimit_enum {
    CU_LIMIT_STACK_SIZE        = 0x00, 
    CU_LIMIT_PRINTF_FIFO_SIZE  = 0x01, 
    CU_LIMIT_MALLOC_HEAP_SIZE  = 0x02  
} CUlimit;

typedef enum cudaError_enum {
    
    CUDA_SUCCESS                              = 0,
    CUDA_ERROR_INVALID_VALUE                  = 1,
    CUDA_ERROR_OUT_OF_MEMORY                  = 2,
    CUDA_ERROR_NOT_INITIALIZED                = 3,
    CUDA_ERROR_DEINITIALIZED                  = 4,
    
    CUDA_ERROR_NO_DEVICE                      = 100,
    CUDA_ERROR_INVALID_DEVICE                 = 101,
    
    CUDA_ERROR_INVALID_IMAGE                  = 200,
    CUDA_ERROR_INVALID_CONTEXT                = 201,
    CUDA_ERROR_CONTEXT_ALREADY_CURRENT        = 202,
    CUDA_ERROR_MAP_FAILED                     = 205,
    CUDA_ERROR_UNMAP_FAILED                   = 206,
    CUDA_ERROR_ARRAY_IS_MAPPED                = 207,
    CUDA_ERROR_ALREADY_MAPPED                 = 208,
    CUDA_ERROR_NO_BINARY_FOR_GPU              = 209,
    CUDA_ERROR_ALREADY_ACQUIRED               = 210,
    CUDA_ERROR_NOT_MAPPED                     = 211,
    CUDA_ERROR_NOT_MAPPED_AS_ARRAY            = 212,
    CUDA_ERROR_NOT_MAPPED_AS_POINTER          = 213,
    CUDA_ERROR_ECC_UNCORRECTABLE              = 214,
    CUDA_ERROR_UNSUPPORTED_LIMIT              = 215,
    
    CUDA_ERROR_INVALID_SOURCE                 = 300,
    CUDA_ERROR_FILE_NOT_FOUND                 = 301,
    CUDA_ERROR_SHARED_OBJECT_SYMBOL_NOT_FOUND = 302,
    CUDA_ERROR_SHARED_OBJECT_INIT_FAILED      = 303,
    CUDA_ERROR_OPERATING_SYSTEM               = 304,
    
    CUDA_ERROR_INVALID_HANDLE                 = 400,
    
    CUDA_ERROR_NOT_FOUND                      = 500,
    
    CUDA_ERROR_NOT_READY                      = 600,
    
    CUDA_ERROR_LAUNCH_FAILED                  = 700,
    CUDA_ERROR_LAUNCH_OUT_OF_RESOURCES        = 701,
    CUDA_ERROR_LAUNCH_TIMEOUT                 = 702,
    CUDA_ERROR_LAUNCH_INCOMPATIBLE_TEXTURING  = 703,
    
    CUDA_ERROR_UNKNOWN                        = 999
} CUresult;

enum {
CU_MEMHOSTALLOC_PORTABLE        = 0x01,
CU_MEMHOSTALLOC_DEVICEMAP       = 0x02,
CU_MEMHOSTALLOC_WRITECOMBINED   = 0x04,
};

typedef struct CUDA_MEMCPY2D_st {
    size_t srcXInBytes;         
    size_t srcY;                

    CUmemorytype srcMemoryType; 
    const void *srcHost;        
    CUdeviceptr srcDevice;      
    CUarray srcArray;           
    size_t srcPitch;            

    size_t dstXInBytes;         
    size_t dstY;                

    CUmemorytype dstMemoryType; 
    void *dstHost;              
    CUdeviceptr dstDevice;      
    CUarray dstArray;           
    size_t dstPitch;            

    size_t WidthInBytes;        
    size_t Height;              
} CUDA_MEMCPY2D;

typedef struct CUDA_MEMCPY3D_st {
    size_t srcXInBytes;         
    size_t srcY;                
    size_t srcZ;                
    size_t srcLOD;              
    CUmemorytype srcMemoryType; 
    const void *srcHost;        
    CUdeviceptr srcDevice;      
    CUarray srcArray;           
    void *reserved0;            
    size_t srcPitch;            
    size_t srcHeight;           

    size_t dstXInBytes;         
    size_t dstY;                
    size_t dstZ;                
    size_t dstLOD;              
    CUmemorytype dstMemoryType; 
    void *dstHost;              
    CUdeviceptr dstDevice;      
    CUarray dstArray;           
    void *reserved1;            
    size_t dstPitch;            
    size_t dstHeight;           

    size_t WidthInBytes;        
    size_t Height;              
    size_t Depth;               
} CUDA_MEMCPY3D;

typedef struct CUDA_ARRAY_DESCRIPTOR_st
{
    size_t Width;             
    size_t Height;            

    CUarray_format Format;    
    unsigned int NumChannels; 
} CUDA_ARRAY_DESCRIPTOR;

typedef struct CUDA_ARRAY3D_DESCRIPTOR_st
{
    size_t Width;             
    size_t Height;            
    size_t Depth;             

    CUarray_format Format;    
    unsigned int NumChannels; 
    unsigned int Flags;       
} CUDA_ARRAY3D_DESCRIPTOR;
enum {
	CUDA_ARRAY3D_2DARRAY        = 0x01,
	CUDA_ARRAY3D_SURFACE_LDST   = 0x02,
	CU_TRSA_OVERRIDE_FORMAT = 0x01,
	CU_TRSF_READ_AS_INTEGER         = 0x01,
	CU_TRSF_NORMALIZED_COORDINATES  = 0x02,
	CU_TRSF_SRGB  = 0x10,
	CU_PARAM_TR_DEFAULT = -1
};

CUresult  cuInit(unsigned int Flags);

 /* END CUDA_INITIALIZE */


CUresult  cuDriverGetVersion(int *driverVersion);

 /* END CUDA_VERSION */


CUresult  cuDeviceGet(CUdevice *device, int ordinal);
CUresult  cuDeviceGetCount(int *count);
CUresult  cuDeviceGetName(char *name, int len, CUdevice dev);
CUresult  cuDeviceComputeCapability(int *major, int *minor, CUdevice dev);

CUresult  cuDeviceTotalMem_v2(size_t *bytes, CUdevice dev);

CUresult  cuDeviceGetProperties(CUdevprop *prop, CUdevice dev);
CUresult  cuDeviceGetAttribute(int *pi, CUdevice_attribute attrib, CUdevice dev);

 /* END CUDA_DEVICE */

CUresult  cuCtxCreate_v2(CUcontext *pctx, unsigned int flags, CUdevice dev);
CUresult  cuCtxDestroy(CUcontext ctx);
CUresult  cuCtxAttach(CUcontext *pctx, unsigned int flags);
CUresult  cuCtxDetach(CUcontext ctx);
CUresult  cuCtxPushCurrent(CUcontext ctx );
CUresult  cuCtxPopCurrent(CUcontext *pctx);
CUresult  cuCtxGetDevice(CUdevice *device);
CUresult  cuCtxSynchronize(void);
CUresult  cuCtxSetLimit(CUlimit limit, size_t value);
CUresult  cuCtxGetLimit(size_t *pvalue, CUlimit limit);
CUresult  cuCtxGetCacheConfig(CUfunc_cache *pconfig);
CUresult  cuCtxSetCacheConfig(CUfunc_cache config);
CUresult  cuCtxGetApiVersion(CUcontext ctx, unsigned int *version);

 /* END CUDA_CTX */

CUresult  cuModuleLoad(CUmodule *module, const char *fname);
CUresult  cuModuleLoadData(CUmodule *module, const void *image);
CUresult  cuModuleLoadDataEx(CUmodule *module, const void *image, unsigned int numOptions, CUjit_option *options, void **optionValues);
CUresult  cuModuleLoadFatBinary(CUmodule *module, const void *fatCubin);
CUresult  cuModuleUnload(CUmodule hmod);
CUresult  cuModuleGetFunction(CUfunction *hfunc, CUmodule hmod, const char *name);
CUresult  cuModuleGetGlobal_v2(CUdeviceptr *dptr, size_t *bytes, CUmodule hmod, const char *name);
CUresult  cuModuleGetTexRef(CUtexref *pTexRef, CUmodule hmod, const char *name);
CUresult  cuModuleGetSurfRef(CUsurfref *pSurfRef, CUmodule hmod, const char *name);

 /* END CUDA_MODULE */

CUresult  cuMemGetInfo_v2(size_t *free, size_t *total);
CUresult  cuMemAlloc_v2(CUdeviceptr *dptr, size_t bytesize);
CUresult  cuMemAllocPitch_v2(CUdeviceptr *dptr, size_t *pPitch, size_t WidthInBytes, size_t Height, unsigned int ElementSizeBytes);
CUresult  cuMemFree_v2(CUdeviceptr dptr);
CUresult  cuMemGetAddressRange_v2(CUdeviceptr *pbase, size_t *psize, CUdeviceptr dptr);
CUresult  cuMemAllocHost_v2(void **pp, size_t bytesize);

CUresult  cuMemFreeHost(void *p);
CUresult  cuMemHostAlloc(void **pp, size_t bytesize, unsigned int Flags);
CUresult  cuMemHostGetDevicePointer_v2(CUdeviceptr *pdptr, void *p, unsigned int Flags);
CUresult  cuMemHostGetFlags(unsigned int *pFlags, void *p);

CUresult  cuMemcpyHtoD_v2(CUdeviceptr dstDevice, const void *srcHost, size_t ByteCount);
CUresult  cuMemcpyDtoH_v2(void *dstHost, CUdeviceptr srcDevice, size_t ByteCount);
CUresult  cuMemcpyDtoD_v2(CUdeviceptr dstDevice, CUdeviceptr srcDevice, size_t ByteCount);
CUresult  cuMemcpyDtoA_v2(CUarray dstArray, size_t dstOffset, CUdeviceptr srcDevice, size_t ByteCount);
CUresult  cuMemcpyAtoD_v2(CUdeviceptr dstDevice, CUarray srcArray, size_t srcOffset, size_t ByteCount);
CUresult  cuMemcpyHtoA_v2(CUarray dstArray, size_t dstOffset, const void *srcHost, size_t ByteCount);
CUresult  cuMemcpyAtoH_v2(void *dstHost, CUarray srcArray, size_t srcOffset, size_t ByteCount);
CUresult  cuMemcpyAtoA_v2(CUarray dstArray, size_t dstOffset, CUarray srcArray, size_t srcOffset, size_t ByteCount);
CUresult  cuMemcpy2D_v2(const CUDA_MEMCPY2D *pCopy);
CUresult  cuMemcpy2DUnaligned_v2(const CUDA_MEMCPY2D *pCopy);
CUresult  cuMemcpy3D_v2(const CUDA_MEMCPY3D *pCopy);
CUresult  cuMemcpyHtoDAsync_v2(CUdeviceptr dstDevice, const void *srcHost, size_t ByteCount, CUstream hStream);
CUresult  cuMemcpyDtoHAsync_v2(void *dstHost, CUdeviceptr srcDevice, size_t ByteCount, CUstream hStream);
CUresult  cuMemcpyDtoDAsync_v2(CUdeviceptr dstDevice, CUdeviceptr srcDevice, size_t ByteCount, CUstream hStream);
CUresult  cuMemcpyHtoAAsync_v2(CUarray dstArray, size_t dstOffset, const void *srcHost, size_t ByteCount, CUstream hStream);
CUresult  cuMemcpyAtoHAsync_v2(void *dstHost, CUarray srcArray, size_t srcOffset, size_t ByteCount, CUstream hStream);
CUresult  cuMemcpy2DAsync_v2(const CUDA_MEMCPY2D *pCopy, CUstream hStream);
CUresult  cuMemcpy3DAsync_v2(const CUDA_MEMCPY3D *pCopy, CUstream hStream);
CUresult  cuMemsetD8_v2(CUdeviceptr dstDevice, unsigned char uc, size_t N);
CUresult  cuMemsetD16_v2(CUdeviceptr dstDevice, unsigned short us, size_t N);
CUresult  cuMemsetD32_v2(CUdeviceptr dstDevice, unsigned int ui, size_t N);
CUresult  cuMemsetD2D8_v2(CUdeviceptr dstDevice, size_t dstPitch, unsigned char uc, size_t Width, size_t Height);
CUresult  cuMemsetD2D16_v2(CUdeviceptr dstDevice, size_t dstPitch, unsigned short us, size_t Width, size_t Height);
CUresult  cuMemsetD2D32_v2(CUdeviceptr dstDevice, size_t dstPitch, unsigned int ui, size_t Width, size_t Height);
CUresult  cuMemsetD8Async(CUdeviceptr dstDevice, unsigned char uc, size_t N, CUstream hStream);
CUresult  cuMemsetD16Async(CUdeviceptr dstDevice, unsigned short us, size_t N, CUstream hStream);
CUresult  cuMemsetD32Async(CUdeviceptr dstDevice, unsigned int ui, size_t N, CUstream hStream);
CUresult  cuMemsetD2D8Async(CUdeviceptr dstDevice, size_t dstPitch, unsigned char uc, size_t Width, size_t Height, CUstream hStream);
CUresult  cuMemsetD2D16Async(CUdeviceptr dstDevice, size_t dstPitch, unsigned short us, size_t Width, size_t Height, CUstream hStream);
CUresult  cuMemsetD2D32Async(CUdeviceptr dstDevice, size_t dstPitch, unsigned int ui, size_t Width, size_t Height, CUstream hStream);
CUresult  cuArrayCreate_v2(CUarray *pHandle, const CUDA_ARRAY_DESCRIPTOR *pAllocateArray);
CUresult  cuArrayGetDescriptor_v2(CUDA_ARRAY_DESCRIPTOR *pArrayDescriptor, CUarray hArray);

CUresult  cuArrayDestroy(CUarray hArray);
CUresult  cuArray3DCreate_v2(CUarray *pHandle, const CUDA_ARRAY3D_DESCRIPTOR *pAllocateArray);
CUresult  cuArray3DGetDescriptor_v2(CUDA_ARRAY3D_DESCRIPTOR *pArrayDescriptor, CUarray hArray);

 /* END CUDA_MEM */

CUresult  cuStreamCreate(CUstream *phStream, unsigned int Flags);
CUresult  cuStreamWaitEvent(CUstream hStream, CUevent hEvent, unsigned int Flags);
CUresult  cuStreamQuery(CUstream hStream);
CUresult  cuStreamSynchronize(CUstream hStream);
CUresult  cuStreamDestroy(CUstream hStream);

 /* END CUDA_STREAM */

CUresult  cuEventCreate(CUevent *phEvent, unsigned int Flags);
CUresult  cuEventRecord(CUevent hEvent, CUstream hStream);
CUresult  cuEventQuery(CUevent hEvent);
CUresult  cuEventSynchronize(CUevent hEvent);
CUresult  cuEventDestroy(CUevent hEvent);
CUresult  cuEventElapsedTime(float *pMilliseconds, CUevent hStart, CUevent hEnd);

 /* END CUDA_EVENT */

CUresult  cuFuncSetBlockShape(CUfunction hfunc, int x, int y, int z);
CUresult  cuFuncSetSharedSize(CUfunction hfunc, unsigned int bytes);
CUresult  cuFuncGetAttribute(int *pi, CUfunction_attribute attrib, CUfunction hfunc);
CUresult  cuFuncSetCacheConfig(CUfunction hfunc, CUfunc_cache config);
CUresult  cuParamSetSize(CUfunction hfunc, unsigned int numbytes);
CUresult  cuParamSeti(CUfunction hfunc, int offset, unsigned int value);
CUresult  cuParamSetf(CUfunction hfunc, int offset, float value);
CUresult  cuParamSetv(CUfunction hfunc, int offset, void *ptr, unsigned int numbytes);
CUresult  cuLaunch(CUfunction f);
CUresult  cuLaunchGrid(CUfunction f, int grid_width, int grid_height);
CUresult  cuLaunchGridAsync(CUfunction f, int grid_width, int grid_height, CUstream hStream);


CUresult  cuParamSetTexRef(CUfunction hfunc, int texunit, CUtexref hTexRef);
 /* END CUDA_EXEC_DEPRECATED */

 /* END CUDA_EXEC */

CUresult  cuTexRefSetArray(CUtexref hTexRef, CUarray hArray, unsigned int Flags);

CUresult  cuTexRefSetAddress_v2(size_t *ByteOffset, CUtexref hTexRef, CUdeviceptr dptr, size_t bytes);
CUresult  cuTexRefSetAddress2D_v2(CUtexref hTexRef, const CUDA_ARRAY_DESCRIPTOR *desc, CUdeviceptr dptr, size_t Pitch);
CUresult  cuTexRefSetFormat(CUtexref hTexRef, CUarray_format fmt, int NumPackedComponents);
CUresult  cuTexRefSetAddressMode(CUtexref hTexRef, int dim, CUaddress_mode am);
CUresult  cuTexRefSetFilterMode(CUtexref hTexRef, CUfilter_mode fm);
CUresult  cuTexRefSetFlags(CUtexref hTexRef, unsigned int Flags);
CUresult  cuTexRefGetAddress_v2(CUdeviceptr *pdptr, CUtexref hTexRef);
CUresult  cuTexRefGetArray(CUarray *phArray, CUtexref hTexRef);
CUresult  cuTexRefGetAddressMode(CUaddress_mode *pam, CUtexref hTexRef, int dim);
CUresult  cuTexRefGetFilterMode(CUfilter_mode *pfm, CUtexref hTexRef);
CUresult  cuTexRefGetFormat(CUarray_format *pFormat, int *pNumChannels, CUtexref hTexRef);
CUresult  cuTexRefGetFlags(unsigned int *pFlags, CUtexref hTexRef);


CUresult  cuTexRefCreate(CUtexref *pTexRef);
CUresult  cuTexRefDestroy(CUtexref hTexRef);

 /* END CUDA_TEXREF_DEPRECATED */

 /* END CUDA_TEXREF */

CUresult  cuSurfRefSetArray(CUsurfref hSurfRef, CUarray hArray, unsigned int Flags);
CUresult  cuSurfRefGetArray(CUarray *phArray, CUsurfref hSurfRef);

 /* END CUDA_SURFREF */

CUresult  cuGraphicsUnregisterResource(CUgraphicsResource resource);
CUresult  cuGraphicsSubResourceGetMappedArray(CUarray *pArray, CUgraphicsResource resource, unsigned int arrayIndex, unsigned int mipLevel);
CUresult  cuGraphicsResourceGetMappedPointer_v2(CUdeviceptr *pDevPtr, size_t *pSize, CUgraphicsResource resource);
CUresult  cuGraphicsResourceSetMapFlags(CUgraphicsResource resource, unsigned int flags);
CUresult  cuGraphicsMapResources(unsigned int count, CUgraphicsResource *resources, CUstream hStream);
CUresult  cuGraphicsUnmapResources(unsigned int count, CUgraphicsResource *resources, CUstream hStream);

 /* END CUDA_GRAPHICS */

CUresult  cuGetExportTable(const void **ppExportTable, const CUuuid *pExportTableId);
 /* END CUDA_DRIVER */
]]  
--auto-generated api from ffi headers
local api =
  {
  ["CUjit_option"] = { type ='value', description = "", },
  ["CUjit_target"] = { type ='value', description = "", },
  ["CUjit_fallback"] = { type ='value', description = "", },
  ["CUDA_ARRAY_DESCRIPTOR"] = { type ='value', description = "", },
  ["CUDA_ARRAY3D_DESCRIPTOR"] = { type ='value', description = "", },
  ["CU_CTX_SCHED_AUTO"] = { type ='value', },
  ["CU_CTX_SCHED_SPIN"] = { type ='value', },
  ["CU_CTX_SCHED_YIELD"] = { type ='value', },
  ["CU_CTX_SCHED_MASK"] = { type ='value', },
  ["CU_CTX_BLOCKING_SYNC"] = { type ='value', },
  ["CU_CTX_MAP_HOST"] = { type ='value', },
  ["CU_CTX_LMEM_RESIZE_TO_MAX"] = { type ='value', },
  ["CU_CTX_FLAGS_MASK"] = { type ='value', },
  ["CU_EVENT_DEFAULT"] = { type ='value', },
  ["CU_EVENT_BLOCKING_SYNC"] = { type ='value', },
  ["CU_EVENT_DISABLE_TIMING"] = { type ='value', },
  ["CU_AD_FORMAT_UNSIGNED_INT8"] = { type ='value', },
  ["CU_AD_FORMAT_UNSIGNED_INT16"] = { type ='value', },
  ["CU_AD_FORMAT_UNSIGNED_INT32"] = { type ='value', },
  ["CU_AD_FORMAT_SIGNED_INT8"] = { type ='value', },
  ["CU_AD_FORMAT_SIGNED_INT16"] = { type ='value', },
  ["CU_AD_FORMAT_SIGNED_INT32"] = { type ='value', },
  ["CU_AD_FORMAT_HALF"] = { type ='value', },
  ["CU_AD_FORMAT_FLOAT"] = { type ='value', },
  ["CU_TR_ADDRESS_MODE_WRAP"] = { type ='value', },
  ["CU_TR_ADDRESS_MODE_CLAMP"] = { type ='value', },
  ["CU_TR_ADDRESS_MODE_MIRROR"] = { type ='value', },
  ["CU_TR_ADDRESS_MODE_BORDER"] = { type ='value', },
  ["CU_TR_FILTER_MODE_POINT"] = { type ='value', },
  ["CU_TR_FILTER_MODE_LINEAR"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_THREADS_PER_BLOCK"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_X"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_Y"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_BLOCK_DIM_Z"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_X"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_Y"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_GRID_DIM_Z"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_SHARED_MEMORY_PER_BLOCK"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_SHARED_MEMORY_PER_BLOCK"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_TOTAL_CONSTANT_MEMORY"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_WARP_SIZE"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_PITCH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAX_REGISTERS_PER_BLOCK"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_REGISTERS_PER_BLOCK"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_CLOCK_RATE"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_TEXTURE_ALIGNMENT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_GPU_OVERLAP"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MULTIPROCESSOR_COUNT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_KERNEL_EXEC_TIMEOUT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_INTEGRATED"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_CAN_MAP_HOST_MEMORY"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_COMPUTE_MODE"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE1D_WIDTH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_WIDTH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_HEIGHT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_WIDTH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_HEIGHT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE3D_DEPTH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_WIDTH"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_HEIGHT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_MAXIMUM_TEXTURE2D_ARRAY_NUMSLICES"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_SURFACE_ALIGNMENT"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_CONCURRENT_KERNELS"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_ECC_ENABLED"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_PCI_BUS_ID"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_PCI_DEVICE_ID"] = { type ='value', },
  ["CU_DEVICE_ATTRIBUTE_TCC_DRIVER"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_MAX_THREADS_PER_BLOCK"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_SHARED_SIZE_BYTES"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_CONST_SIZE_BYTES"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_LOCAL_SIZE_BYTES"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_NUM_REGS"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_PTX_VERSION"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_BINARY_VERSION"] = { type ='value', },
  ["CU_FUNC_ATTRIBUTE_MAX"] = { type ='value', },
  ["CU_FUNC_CACHE_PREFER_NONE"] = { type ='value', },
  ["CU_FUNC_CACHE_PREFER_SHARED"] = { type ='value', },
  ["CU_FUNC_CACHE_PREFER_L1"] = { type ='value', },
  ["CU_MEMORYTYPE_HOST"] = { type ='value', },
  ["CU_MEMORYTYPE_DEVICE"] = { type ='value', },
  ["CU_MEMORYTYPE_ARRAY"] = { type ='value', },
  ["CU_COMPUTEMODE_DEFAULT"] = { type ='value', },
  ["CU_COMPUTEMODE_EXCLUSIVE"] = { type ='value', },
  ["CU_COMPUTEMODE_PROHIBITED"] = { type ='value', },
  ["CU_JIT_MAX_REGISTERS"] = { type ='value', },
  ["CU_JIT_THREADS_PER_BLOCK"] = { type ='value', },
  ["CU_JIT_WALL_TIME"] = { type ='value', },
  ["CU_JIT_INFO_LOG_BUFFER"] = { type ='value', },
  ["CU_JIT_INFO_LOG_BUFFER_SIZE_BYTES"] = { type ='value', },
  ["CU_JIT_ERROR_LOG_BUFFER"] = { type ='value', },
  ["CU_JIT_ERROR_LOG_BUFFER_SIZE_BYTES"] = { type ='value', },
  ["CU_JIT_OPTIMIZATION_LEVEL"] = { type ='value', },
  ["CU_JIT_TARGET_FROM_CUCONTEXT"] = { type ='value', },
  ["CU_JIT_TARGET"] = { type ='value', },
  ["CU_JIT_FALLBACK_STRATEGY"] = { type ='value', },
  ["CU_TARGET_COMPUTE_10"] = { type ='value', },
  ["CU_TARGET_COMPUTE_11"] = { type ='value', },
  ["CU_TARGET_COMPUTE_12"] = { type ='value', },
  ["CU_TARGET_COMPUTE_13"] = { type ='value', },
  ["CU_TARGET_COMPUTE_20"] = { type ='value', },
  ["CU_TARGET_COMPUTE_21"] = { type ='value', },
  ["CU_PREFER_PTX"] = { type ='value', },
  ["CU_PREFER_BINARY"] = { type ='value', },
  ["CU_GRAPHICS_REGISTER_FLAGS_NONE"] = { type ='value', },
  ["CU_GRAPHICS_MAP_RESOURCE_FLAGS_NONE"] = { type ='value', },
  ["CU_GRAPHICS_MAP_RESOURCE_FLAGS_READ_ONLY"] = { type ='value', },
  ["CU_GRAPHICS_MAP_RESOURCE_FLAGS_WRITE_DISCARD"] = { type ='value', },
  ["CU_CUBEMAP_FACE_POSITIVE_X"] = { type ='value', },
  ["CU_CUBEMAP_FACE_NEGATIVE_X"] = { type ='value', },
  ["CU_CUBEMAP_FACE_POSITIVE_Y"] = { type ='value', },
  ["CU_CUBEMAP_FACE_NEGATIVE_Y"] = { type ='value', },
  ["CU_CUBEMAP_FACE_POSITIVE_Z"] = { type ='value', },
  ["CU_CUBEMAP_FACE_NEGATIVE_Z"] = { type ='value', },
  ["CU_LIMIT_STACK_SIZE"] = { type ='value', },
  ["CU_LIMIT_PRINTF_FIFO_SIZE"] = { type ='value', },
  ["CU_LIMIT_MALLOC_HEAP_SIZE"] = { type ='value', },
  ["CUDA_SUCCESS"] = { type ='value', },
  ["CUDA_ERROR_INVALID_VALUE"] = { type ='value', },
  ["CUDA_ERROR_OUT_OF_MEMORY"] = { type ='value', },
  ["CUDA_ERROR_NOT_INITIALIZED"] = { type ='value', },
  ["CUDA_ERROR_DEINITIALIZED"] = { type ='value', },
  ["CUDA_ERROR_NO_DEVICE"] = { type ='value', },
  ["CUDA_ERROR_INVALID_DEVICE"] = { type ='value', },
  ["CUDA_ERROR_INVALID_IMAGE"] = { type ='value', },
  ["CUDA_ERROR_INVALID_CONTEXT"] = { type ='value', },
  ["CUDA_ERROR_CONTEXT_ALREADY_CURRENT"] = { type ='value', },
  ["CUDA_ERROR_MAP_FAILED"] = { type ='value', },
  ["CUDA_ERROR_UNMAP_FAILED"] = { type ='value', },
  ["CUDA_ERROR_ARRAY_IS_MAPPED"] = { type ='value', },
  ["CUDA_ERROR_ALREADY_MAPPED"] = { type ='value', },
  ["CUDA_ERROR_NO_BINARY_FOR_GPU"] = { type ='value', },
  ["CUDA_ERROR_ALREADY_ACQUIRED"] = { type ='value', },
  ["CUDA_ERROR_NOT_MAPPED"] = { type ='value', },
  ["CUDA_ERROR_NOT_MAPPED_AS_ARRAY"] = { type ='value', },
  ["CUDA_ERROR_NOT_MAPPED_AS_POINTER"] = { type ='value', },
  ["CUDA_ERROR_ECC_UNCORRECTABLE"] = { type ='value', },
  ["CUDA_ERROR_UNSUPPORTED_LIMIT"] = { type ='value', },
  ["CUDA_ERROR_INVALID_SOURCE"] = { type ='value', },
  ["CUDA_ERROR_FILE_NOT_FOUND"] = { type ='value', },
  ["CUDA_ERROR_SHARED_OBJECT_SYMBOL_NOT_FOUND"] = { type ='value', },
  ["CUDA_ERROR_SHARED_OBJECT_INIT_FAILED"] = { type ='value', },
  ["CUDA_ERROR_OPERATING_SYSTEM"] = { type ='value', },
  ["CUDA_ERROR_INVALID_HANDLE"] = { type ='value', },
  ["CUDA_ERROR_NOT_FOUND"] = { type ='value', },
  ["CUDA_ERROR_NOT_READY"] = { type ='value', },
  ["CUDA_ERROR_LAUNCH_FAILED"] = { type ='value', },
  ["CUDA_ERROR_LAUNCH_OUT_OF_RESOURCES"] = { type ='value', },
  ["CUDA_ERROR_LAUNCH_TIMEOUT"] = { type ='value', },
  ["CUDA_ERROR_LAUNCH_INCOMPATIBLE_TEXTURING"] = { type ='value', },
  ["CUDA_ERROR_UNKNOWN"] = { type ='value', },
  ["CU_MEMHOSTALLOC_PORTABLE"] = { type ='value', },
  ["CU_MEMHOSTALLOC_DEVICEMAP"] = { type ='value', },
  ["CU_MEMHOSTALLOC_WRITECOMBINED"] = { type ='value', },
  ["CUDA_ARRAY3D_2DARRAY"] = { type ='value', },
  ["CUDA_ARRAY3D_SURFACE_LDST"] = { type ='value', },
  ["CU_TRSA_OVERRIDE_FORMAT"] = { type ='value', },
  ["CU_TRSF_READ_AS_INTEGER"] = { type ='value', },
  ["CU_TRSF_NORMALIZED_COORDINATES"] = { type ='value', },
  ["CU_TRSF_SRGB"] = { type ='value', },
  ["CU_PARAM_TR_DEFAULT"] = { type ='value', },
  ["cuInit"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(unsigned int Flags)", },
  ["cuDriverGetVersion"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(int *driverVersion)", },
  ["cuDeviceGet"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdevice *device, int ordinal)", },
  ["cuDeviceGetCount"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(int *count)", },
  ["cuDeviceGetName"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(char *name, int len, CUdevice dev)", },
  ["cuDeviceComputeCapability"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(int *major, int *minor, CUdevice dev)", },
  ["cuDeviceTotalMem_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(size_t *bytes, CUdevice dev)", },
  ["cuDeviceGetProperties"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdevprop *prop, CUdevice dev)", },
  ["cuDeviceGetAttribute"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(int *pi, CUdevice_attribute attrib, CUdevice dev)", },
  ["cuCtxCreate_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext *pctx, unsigned int flags, CUdevice dev)", },
  ["cuCtxDestroy"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext ctx)", },
  ["cuCtxAttach"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext *pctx, unsigned int flags)", },
  ["cuCtxDetach"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext ctx)", },
  ["cuCtxPushCurrent"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext ctx)", },
  ["cuCtxPopCurrent"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext *pctx)", },
  ["cuCtxGetDevice"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdevice *device)", },
  ["cuCtxSynchronize"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void)", },
  ["cuCtxSetLimit"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUlimit limit, size_t value)", },
  ["cuCtxGetLimit"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(size_t *pvalue, CUlimit limit)", },
  ["cuCtxGetCacheConfig"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunc_cache *pconfig)", },
  ["cuCtxSetCacheConfig"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunc_cache config)", },
  ["cuCtxGetApiVersion"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUcontext ctx, unsigned int *version)", },
  ["cuModuleLoad"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUmodule *module, const char *fname)", },
  ["cuModuleLoadData"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUmodule *module, const void *image)", },
  ["cuModuleLoadDataEx"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUmodule *module, const void *image, unsigned int numOptions, CUjit_option *options, void **optionValues)", },
  ["cuModuleLoadFatBinary"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUmodule *module, const void *fatCubin)", },
  ["cuModuleUnload"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUmodule hmod)", },
  ["cuModuleGetFunction"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction *hfunc, CUmodule hmod, const char *name)", },
  ["cuModuleGetGlobal_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *dptr, size_t *bytes, CUmodule hmod, const char *name)", },
  ["cuModuleGetTexRef"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref *pTexRef, CUmodule hmod, const char *name)", },
  ["cuModuleGetSurfRef"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUsurfref *pSurfRef, CUmodule hmod, const char *name)", },
  ["cuMemGetInfo_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(size_t *free, size_t *total)", },
  ["cuMemAlloc_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *dptr, size_t bytesize)", },
  ["cuMemAllocPitch_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *dptr, size_t *pPitch, size_t WidthInBytes, size_t Height, unsigned int ElementSizeBytes)", },
  ["cuMemFree_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dptr)", },
  ["cuMemGetAddressRange_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *pbase, size_t *psize, CUdeviceptr dptr)", },
  ["cuMemAllocHost_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void **pp, size_t bytesize)", },
  ["cuMemFreeHost"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void *p)", },
  ["cuMemHostAlloc"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void **pp, size_t bytesize, unsigned int Flags)", },
  ["cuMemHostGetDevicePointer_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *pdptr, void *p, unsigned int Flags)", },
  ["cuMemHostGetFlags"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(unsigned int *pFlags, void *p)", },
  ["cuMemcpyHtoD_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, const void *srcHost, size_t ByteCount)", },
  ["cuMemcpyDtoH_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void *dstHost, CUdeviceptr srcDevice, size_t ByteCount)", },
  ["cuMemcpyDtoD_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, CUdeviceptr srcDevice, size_t ByteCount)", },
  ["cuMemcpyDtoA_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray dstArray, size_t dstOffset, CUdeviceptr srcDevice, size_t ByteCount)", },
  ["cuMemcpyAtoD_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, CUarray srcArray, size_t srcOffset, size_t ByteCount)", },
  ["cuMemcpyHtoA_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray dstArray, size_t dstOffset, const void *srcHost, size_t ByteCount)", },
  ["cuMemcpyAtoH_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void *dstHost, CUarray srcArray, size_t srcOffset, size_t ByteCount)", },
  ["cuMemcpyAtoA_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray dstArray, size_t dstOffset, CUarray srcArray, size_t srcOffset, size_t ByteCount)", },
  ["cuMemcpy2D_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const CUDA_MEMCPY2D *pCopy)", },
  ["cuMemcpy2DUnaligned_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const CUDA_MEMCPY2D *pCopy)", },
  ["cuMemcpy3D_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const CUDA_MEMCPY3D *pCopy)", },
  ["cuMemcpyHtoDAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, const void *srcHost, size_t ByteCount, CUstream hStream)", },
  ["cuMemcpyDtoHAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void *dstHost, CUdeviceptr srcDevice, size_t ByteCount, CUstream hStream)", },
  ["cuMemcpyDtoDAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, CUdeviceptr srcDevice, size_t ByteCount, CUstream hStream)", },
  ["cuMemcpyHtoAAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray dstArray, size_t dstOffset, const void *srcHost, size_t ByteCount, CUstream hStream)", },
  ["cuMemcpyAtoHAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(void *dstHost, CUarray srcArray, size_t srcOffset, size_t ByteCount, CUstream hStream)", },
  ["cuMemcpy2DAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const CUDA_MEMCPY2D *pCopy, CUstream hStream)", },
  ["cuMemcpy3DAsync_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const CUDA_MEMCPY3D *pCopy, CUstream hStream)", },
  ["cuMemsetD8_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned char uc, size_t N)", },
  ["cuMemsetD16_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned short us, size_t N)", },
  ["cuMemsetD32_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned int ui, size_t N)", },
  ["cuMemsetD2D8_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned char uc, size_t Width, size_t Height)", },
  ["cuMemsetD2D16_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned short us, size_t Width, size_t Height)", },
  ["cuMemsetD2D32_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned int ui, size_t Width, size_t Height)", },
  ["cuMemsetD8Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned char uc, size_t N, CUstream hStream)", },
  ["cuMemsetD16Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned short us, size_t N, CUstream hStream)", },
  ["cuMemsetD32Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, unsigned int ui, size_t N, CUstream hStream)", },
  ["cuMemsetD2D8Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned char uc, size_t Width, size_t Height, CUstream hStream)", },
  ["cuMemsetD2D16Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned short us, size_t Width, size_t Height, CUstream hStream)", },
  ["cuMemsetD2D32Async"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr dstDevice, size_t dstPitch, unsigned int ui, size_t Width, size_t Height, CUstream hStream)", },
  ["cuArrayCreate_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray *pHandle, const CUDA_ARRAY_DESCRIPTOR *pAllocateArray)", },
  ["cuArrayGetDescriptor_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUDA_ARRAY_DESCRIPTOR *pArrayDescriptor, CUarray hArray)", },
  ["cuArrayDestroy"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray hArray)", },
  ["cuArray3DCreate_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray *pHandle, const CUDA_ARRAY3D_DESCRIPTOR *pAllocateArray)", },
  ["cuArray3DGetDescriptor_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUDA_ARRAY3D_DESCRIPTOR *pArrayDescriptor, CUarray hArray)", },
  ["cuStreamCreate"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUstream *phStream, unsigned int Flags)", },
  ["cuStreamWaitEvent"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUstream hStream, CUevent hEvent, unsigned int Flags)", },
  ["cuStreamQuery"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUstream hStream)", },
  ["cuStreamSynchronize"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUstream hStream)", },
  ["cuStreamDestroy"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUstream hStream)", },
  ["cuEventCreate"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUevent *phEvent, unsigned int Flags)", },
  ["cuEventRecord"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUevent hEvent, CUstream hStream)", },
  ["cuEventQuery"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUevent hEvent)", },
  ["cuEventSynchronize"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUevent hEvent)", },
  ["cuEventDestroy"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUevent hEvent)", },
  ["cuEventElapsedTime"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(float *pMilliseconds, CUevent hStart, CUevent hEnd)", },
  ["cuFuncSetBlockShape"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, int x, int y, int z)", },
  ["cuFuncSetSharedSize"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, unsigned int bytes)", },
  ["cuFuncGetAttribute"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(int *pi, CUfunction_attribute attrib, CUfunction hfunc)", },
  ["cuFuncSetCacheConfig"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, CUfunc_cache config)", },
  ["cuParamSetSize"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, unsigned int numbytes)", },
  ["cuParamSeti"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, int offset, unsigned int value)", },
  ["cuParamSetf"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, int offset, float value)", },
  ["cuParamSetv"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, int offset, void *ptr, unsigned int numbytes)", },
  ["cuLaunch"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction f)", },
  ["cuLaunchGrid"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction f, int grid_width, int grid_height)", },
  ["cuLaunchGridAsync"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction f, int grid_width, int grid_height, CUstream hStream)", },
  ["cuParamSetTexRef"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfunction hfunc, int texunit, CUtexref hTexRef)", },
  ["cuTexRefSetArray"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, CUarray hArray, unsigned int Flags)", },
  ["cuTexRefSetAddress_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(size_t *ByteOffset, CUtexref hTexRef, CUdeviceptr dptr, size_t bytes)", },
  ["cuTexRefSetAddress2D_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, const CUDA_ARRAY_DESCRIPTOR *desc, CUdeviceptr dptr, size_t Pitch)", },
  ["cuTexRefSetFormat"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, CUarray_format fmt, int NumPackedComponents)", },
  ["cuTexRefSetAddressMode"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, int dim, CUaddress_mode am)", },
  ["cuTexRefSetFilterMode"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, CUfilter_mode fm)", },
  ["cuTexRefSetFlags"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef, unsigned int Flags)", },
  ["cuTexRefGetAddress_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *pdptr, CUtexref hTexRef)", },
  ["cuTexRefGetArray"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray *phArray, CUtexref hTexRef)", },
  ["cuTexRefGetAddressMode"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUaddress_mode *pam, CUtexref hTexRef, int dim)", },
  ["cuTexRefGetFilterMode"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUfilter_mode *pfm, CUtexref hTexRef)", },
  ["cuTexRefGetFormat"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray_format *pFormat, int *pNumChannels, CUtexref hTexRef)", },
  ["cuTexRefGetFlags"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(unsigned int *pFlags, CUtexref hTexRef)", },
  ["cuTexRefCreate"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref *pTexRef)", },
  ["cuTexRefDestroy"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUtexref hTexRef)", },
  ["cuSurfRefSetArray"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUsurfref hSurfRef, CUarray hArray, unsigned int Flags)", },
  ["cuSurfRefGetArray"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray *phArray, CUsurfref hSurfRef)", },
  ["cuGraphicsUnregisterResource"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUgraphicsResource resource)", },
  ["cuGraphicsSubResourceGetMappedArray"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUarray *pArray, CUgraphicsResource resource, unsigned int arrayIndex, unsigned int mipLevel)", },
  ["cuGraphicsResourceGetMappedPointer_v2"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUdeviceptr *pDevPtr, size_t *pSize, CUgraphicsResource resource)", },
  ["cuGraphicsResourceSetMapFlags"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(CUgraphicsResource resource, unsigned int flags)", },
  ["cuGraphicsMapResources"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(unsigned int count, CUgraphicsResource *resources, CUstream hStream)", },
  ["cuGraphicsUnmapResources"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(unsigned int count, CUgraphicsResource *resources, CUstream hStream)", },
  ["cuGetExportTable"] = { type ='function', 
    description = "", 
    returns = "(CUresult )",
    args = "(const void **ppExportTable, const CUuuid *pExportTableId)", },
  ["CUuuid"] = { type ='class', 
    description = "", 
    
  },
  ["CUdevprop"] = { type ='class', 
    description = "", 
    childs =     {
    ["maxThreadsPerBlock"] = { type ='value', description = "int", },
    ["sharedMemPerBlock"] = { type ='value', description = "int", },
    ["totalConstantMemory"] = { type ='value', description = "int", },
    ["SIMDWidth"] = { type ='value', description = "int", },
    ["memPitch"] = { type ='value', description = "int", },
    ["regsPerBlock"] = { type ='value', description = "int", },
    ["clockRate"] = { type ='value', description = "int", },
    ["textureAlign"] = { type ='value', description = "int", },
    }
  },
  ["CUDA_MEMCPY2D"] = { type ='class', 
    description = "", 
    childs =     {
    ["srcXInBytes"] = { type ='value', description = "size_t", },
    ["srcY"] = { type ='value', description = "size_t", },
    ["srcMemoryType"] = { type ='value', description = "CUmemorytype", },
    ["srcHost"] = { type ='value', description = "const void *", },
    ["srcDevice"] = { type ='value', description = "CUdeviceptr", },
    ["srcArray"] = { type ='value', description = "CUarray", },
    ["srcPitch"] = { type ='value', description = "size_t", },
    ["dstXInBytes"] = { type ='value', description = "size_t", },
    ["dstY"] = { type ='value', description = "size_t", },
    ["dstMemoryType"] = { type ='value', description = "CUmemorytype", },
    ["dstHost"] = { type ='value', description = "void *", },
    ["dstDevice"] = { type ='value', description = "CUdeviceptr", },
    ["dstArray"] = { type ='value', description = "CUarray", },
    ["dstPitch"] = { type ='value', description = "size_t", },
    ["WidthInBytes"] = { type ='value', description = "size_t", },
    ["Height"] = { type ='value', description = "size_t", },
    }
  },
  ["CUDA_MEMCPY3D"] = { type ='class', 
    description = "", 
    childs =     {
    ["srcXInBytes"] = { type ='value', description = "size_t", },
    ["srcY"] = { type ='value', description = "size_t", },
    ["srcZ"] = { type ='value', description = "size_t", },
    ["srcLOD"] = { type ='value', description = "size_t", },
    ["srcMemoryType"] = { type ='value', description = "CUmemorytype", },
    ["srcHost"] = { type ='value', description = "const void *", },
    ["srcDevice"] = { type ='value', description = "CUdeviceptr", },
    ["srcArray"] = { type ='value', description = "CUarray", },
    ["reserved0"] = { type ='value', description = "void *", },
    ["srcPitch"] = { type ='value', description = "size_t", },
    ["srcHeight"] = { type ='value', description = "size_t", },
    ["dstXInBytes"] = { type ='value', description = "size_t", },
    ["dstY"] = { type ='value', description = "size_t", },
    ["dstZ"] = { type ='value', description = "size_t", },
    ["dstLOD"] = { type ='value', description = "size_t", },
    ["dstMemoryType"] = { type ='value', description = "CUmemorytype", },
    ["dstHost"] = { type ='value', description = "void *", },
    ["dstDevice"] = { type ='value', description = "CUdeviceptr", },
    ["dstArray"] = { type ='value', description = "CUarray", },
    ["reserved1"] = { type ='value', description = "void *", },
    ["dstPitch"] = { type ='value', description = "size_t", },
    ["dstHeight"] = { type ='value', description = "size_t", },
    ["WidthInBytes"] = { type ='value', description = "size_t", },
    ["Height"] = { type ='value', description = "size_t", },
    ["Depth"] = { type ='value', description = "size_t", },
    }
  },
  ["CUDA_ARRAY_DESCRIPTOR"] = { type ='class', 
    description = "", 
    childs =     {
    ["Width"] = { type ='value', description = "size_t", },
    ["Height"] = { type ='value', description = "size_t", },
    ["Format"] = { type ='value', description = "CUarray_format", },
    ["NumChannels"] = { type ='value', description = "unsigned int", },
    }
  },
  ["CUDA_ARRAY3D_DESCRIPTOR"] = { type ='class', 
    description = "", 
    childs =     {
    ["Width"] = { type ='value', description = "size_t", },
    ["Height"] = { type ='value', description = "size_t", },
    ["Depth"] = { type ='value', description = "size_t", },
    ["Format"] = { type ='value', description = "CUarray_format", },
    ["NumChannels"] = { type ='value', description = "unsigned int", },
    ["Flags"] = { type ='value', description = "unsigned int", },
    }
  },
  }
return {
  cuda = {
    type = 'lib',
    description = "Nvidia CUDA Driver API",
    childs = api,
  },
  cu = {
    type = 'lib',
    description = "Nvidia CUDA Driver API",
    childs = api,
  },
}
