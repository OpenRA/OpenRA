--[[// cugl cuGL | Cuda GL Interop
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

CUresult cuGLCtxCreate_v2(CUcontext *pCtx, unsigned int Flags, CUdevice device );
CUresult cuGraphicsGLRegisterBuffer(CUgraphicsResource *pCudaResource, GLuint buffer, unsigned int Flags);
CUresult cuGraphicsGLRegisterImage(CUgraphicsResource *pCudaResource, GLuint image, GLenum target, unsigned int Flags);

typedef void* HGPUNV;
CUresult cuWGLGetDevice(CUdevice *pDevice, HGPUNV hGpu);
]]
--auto-generated api from ffi headers

local api = {
  ["cuGLCtxCreate_v2"] = { type ='function', 
      description = "", 
      returns = "(CUresult)",
      args = "(CUcontext *pCtx, unsigned int Flags, CUdevice device)", },
  ["cuGraphicsGLRegisterBuffer"] = { type ='function', 
      description = "", 
      returns = "(CUresult)",
      args = "(CUgraphicsResource *pCudaResource, GLuint buffer, unsigned int Flags)", },
  ["cuGraphicsGLRegisterImage"] = { type ='function', 
      description = "", 
      returns = "(CUresult)",
      args = "(CUgraphicsResource *pCudaResource, GLuint image, GLenum target, unsigned int Flags)", },
  ["cuWGLGetDevice"] = { type ='function', 
      description = "", 
      returns = "(CUresult)",
      args = "(CUdevice *pDevice, HGPUNV hGpu)", },
}
cugl = {
	type = 'class',
	description = "Cuda GL Interop",
	childs = api,
}
cuGL = {
	type = 'class',
	description = "Cuda GL Interop",
	childs = api,
}
