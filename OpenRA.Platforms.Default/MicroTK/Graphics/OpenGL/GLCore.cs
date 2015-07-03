// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2010 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

namespace OpenTK.Graphics.OpenGL
{
	using System;
	using System.Text;
	using System.Runtime.InteropServices;

	partial class GL
	{
		internal static partial class Core
		{
			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glEnableClientState", ExactSpelling = true)]
			internal extern static void EnableClientState(OpenTK.Graphics.OpenGL.ArrayCap array);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDrawArrays", ExactSpelling = true)]
			internal extern static void DrawArrays(OpenTK.Graphics.OpenGL.BeginMode mode, Int32 first, Int32 count);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glClearColor", ExactSpelling = true)]
			internal extern static void ClearColor(Single red, Single green, Single blue, Single alpha);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glClear", ExactSpelling = true)]
			internal extern static void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glEnable", ExactSpelling = true)]
			internal extern static void Enable(OpenTK.Graphics.OpenGL.EnableCap cap);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDisable", ExactSpelling = true)]
			internal extern static void Disable(OpenTK.Graphics.OpenGL.EnableCap cap);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBlendEquation", ExactSpelling = true)]
			internal extern static void BlendEquation(OpenTK.Graphics.OpenGL.BlendEquationMode mode);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBlendFunc", ExactSpelling = true)]
			internal extern static void BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc sfactor, OpenTK.Graphics.OpenGL.BlendingFactorDest dfactor);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glScissor", ExactSpelling = true)]
			internal extern static void Scissor(Int32 x, Int32 y, Int32 width, Int32 height);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glLineWidth", ExactSpelling = true)]
			internal extern static void LineWidth(Single width);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glCreateShader", ExactSpelling = true)]
			internal extern static Int32 CreateShader(OpenTK.Graphics.OpenGL.ShaderType type);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glCompileShader", ExactSpelling = true)]
			internal extern static void CompileShader(UInt32 shader);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetShaderiv", ExactSpelling = true)]
			internal extern static unsafe void GetShaderiv(UInt32 shader, OpenTK.Graphics.OpenGL.ShaderParameter pname, [OutAttribute] Int32* @params);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetShaderInfoLog", ExactSpelling = true)]
			internal extern static unsafe void GetShaderInfoLog(UInt32 shader, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] StringBuilder infoLog);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetProgramInfoLog", ExactSpelling = true)]
			internal extern static unsafe void GetProgramInfoLog(UInt32 program, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] StringBuilder infoLog);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glCreateProgram", ExactSpelling = true)]
			internal extern static Int32 CreateProgram();

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glAttachShader", ExactSpelling = true)]
			internal extern static void AttachShader(UInt32 program, UInt32 shader);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glLinkProgram", ExactSpelling = true)]
			internal extern static void LinkProgram(UInt32 program);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetProgramiv", ExactSpelling = true)]
			internal extern static unsafe void GetProgramiv(UInt32 program, OpenTK.Graphics.OpenGL.ProgramParameter pname, [OutAttribute] Int32* @params);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUseProgram", ExactSpelling = true)]
			internal extern static void UseProgram(UInt32 program);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetActiveUniform", ExactSpelling = true)]
			internal extern static unsafe void GetActiveUniform(UInt32 program, UInt32 index, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] Int32* size, [OutAttribute] OpenTK.Graphics.OpenGL.ActiveUniformType* type, [OutAttribute] StringBuilder name);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetUniformLocation", ExactSpelling = true)]
			internal extern static Int32 GetUniformLocation(UInt32 program, String name);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform1i", ExactSpelling = true)]
			internal extern static void Uniform1i(Int32 location, Int32 v0);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform1f", ExactSpelling = true)]
			internal extern static void Uniform1f(Int32 location, Single v0);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform2f", ExactSpelling = true)]
			internal extern static void Uniform2f(Int32 location, Single v0, Single v1);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform1fv", ExactSpelling = true)]
			internal extern static unsafe void Uniform1fv(Int32 location, Int32 count, Single* value);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform2fv", ExactSpelling = true)]
			internal extern static unsafe void Uniform2fv(Int32 location, Int32 count, Single* value);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform3fv", ExactSpelling = true)]
			internal extern static unsafe void Uniform3fv(Int32 location, Int32 count, Single* value);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniform4fv", ExactSpelling = true)]
			internal extern static unsafe void Uniform4fv(Int32 location, Int32 count, Single* value);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glUniformMatrix4fv", ExactSpelling = true)]
			internal extern static unsafe void UniformMatrix4fv(Int32 location, Int32 count, bool transpose, Single* value);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glActiveTexture", ExactSpelling = true)]
			internal extern static void ActiveTexture(OpenTK.Graphics.OpenGL.TextureUnit texture);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetString", ExactSpelling = true)]
			internal extern static IntPtr GetString(OpenTK.Graphics.OpenGL.StringName name);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetError", ExactSpelling = true)]
			internal extern static OpenTK.Graphics.OpenGL.ErrorCode GetError();

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGenFramebuffersEXT", ExactSpelling = true)]
			internal extern static unsafe void GenFramebuffersEXT(Int32 n, [OutAttribute] UInt32* framebuffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBindFramebufferEXT", ExactSpelling = true)]
			internal extern static void BindFramebufferEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, UInt32 framebuffer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glFramebufferTexture2DEXT", ExactSpelling = true)]
			internal extern static void FramebufferTexture2DEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, OpenTK.Graphics.OpenGL.FramebufferAttachment attachment, OpenTK.Graphics.OpenGL.TextureTarget textarget, UInt32 texture, Int32 level);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGenRenderbuffersEXT", ExactSpelling = true)]
			internal extern static unsafe void GenRenderbuffersEXT(Int32 n, [OutAttribute] UInt32* renderbuffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBindRenderbufferEXT", ExactSpelling = true)]
			internal extern static void BindRenderbufferEXT(OpenTK.Graphics.OpenGL.RenderbufferTarget target, UInt32 renderbuffer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glRenderbufferStorageEXT", ExactSpelling = true)]
			internal extern static void RenderbufferStorageEXT(OpenTK.Graphics.OpenGL.RenderbufferTarget target, OpenTK.Graphics.OpenGL.RenderbufferStorage internalformat, Int32 width, Int32 height);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glFramebufferRenderbufferEXT", ExactSpelling = true)]
			internal extern static void FramebufferRenderbufferEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, OpenTK.Graphics.OpenGL.FramebufferAttachment attachment, OpenTK.Graphics.OpenGL.RenderbufferTarget renderbuffertarget, UInt32 renderbuffer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glCheckFramebufferStatusEXT", ExactSpelling = true)]
			internal extern static OpenTK.Graphics.OpenGL.FramebufferErrorCode CheckFramebufferStatusEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDeleteFramebuffersEXT", ExactSpelling = true)]
			internal extern static unsafe void DeleteFramebuffersEXT(Int32 n, UInt32* framebuffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDeleteRenderbuffersEXT", ExactSpelling = true)]
			internal extern static unsafe void DeleteRenderbuffersEXT(Int32 n, UInt32* renderbuffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetIntegerv", ExactSpelling = true)]
			internal extern static unsafe void GetIntegerv(OpenTK.Graphics.OpenGL.GetPName pname, [OutAttribute] Int32* @params);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glFlush", ExactSpelling = true)]
			internal extern static void Flush();

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glViewport", ExactSpelling = true)]
			internal extern static void Viewport(Int32 x, Int32 y, Int32 width, Int32 height);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGenTextures", ExactSpelling = true)]
			internal extern static unsafe void GenTextures(Int32 n, [OutAttribute] UInt32* textures);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glTexParameteri", ExactSpelling = true)]
			internal extern static void TexParameteri(OpenTK.Graphics.OpenGL.TextureTarget target, OpenTK.Graphics.OpenGL.TextureParameterName pname, Int32 param);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glTexParameterf", ExactSpelling = true)]
			internal extern static void TexParameterf(OpenTK.Graphics.OpenGL.TextureTarget target, OpenTK.Graphics.OpenGL.TextureParameterName pname, Single param);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glTexImage2D", ExactSpelling = true)]
			internal extern static void TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget target, Int32 level, OpenTK.Graphics.OpenGL.PixelInternalFormat internalformat, Int32 width, Int32 height, Int32 border, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, IntPtr pixels);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGetTexImage", ExactSpelling = true)]
			internal extern static void GetTexImage(OpenTK.Graphics.OpenGL.TextureTarget target, Int32 level, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, [OutAttribute] IntPtr pixels);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDeleteTextures", ExactSpelling = true)]
			internal extern static unsafe void DeleteTextures(Int32 n, UInt32* textures);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glGenBuffers", ExactSpelling = true)]
			internal extern static unsafe void GenBuffers(Int32 n, [OutAttribute] UInt32* buffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBufferData", ExactSpelling = true)]
			internal extern static void BufferData(OpenTK.Graphics.OpenGL.BufferTarget target, IntPtr size, IntPtr data, OpenTK.Graphics.OpenGL.BufferUsageHint usage);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBufferSubData", ExactSpelling = true)]
			internal extern static void BufferSubData(OpenTK.Graphics.OpenGL.BufferTarget target, IntPtr offset, IntPtr size, IntPtr data);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glBindBuffer", ExactSpelling = true)]
			internal extern static void BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget target, UInt32 buffer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glVertexPointer", ExactSpelling = true)]
			internal extern static void VertexPointer(Int32 size, OpenTK.Graphics.OpenGL.VertexPointerType type, Int32 stride, IntPtr pointer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glTexCoordPointer", ExactSpelling = true)]
			internal extern static void TexCoordPointer(Int32 size, OpenTK.Graphics.OpenGL.TexCoordPointerType type, Int32 stride, IntPtr pointer);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glDeleteBuffers", ExactSpelling = true)]
			internal extern static unsafe void DeleteBuffers(Int32 n, UInt32* buffers);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glShaderSource", ExactSpelling = true)]
			internal extern static unsafe void ShaderSource(UInt32 shader, Int32 count, String[] @string, Int32* length);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glPushClientAttrib", ExactSpelling = true)]
			internal extern static void PushClientAttrib(OpenTK.Graphics.OpenGL.ClientAttribMask mask);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glPixelStoref", ExactSpelling = true)]
			internal extern static void PixelStoref(OpenTK.Graphics.OpenGL.PixelStoreParameter pname, Single param);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glPixelStorei", ExactSpelling = true)]
			internal extern static void PixelStorei(OpenTK.Graphics.OpenGL.PixelStoreParameter pname, Int32 param);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glReadPixels", ExactSpelling = true)]
			internal extern static void ReadPixels(Int32 x, Int32 y, Int32 width, Int32 height, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, [OutAttribute] IntPtr pixels);

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glFinish", ExactSpelling = true)]
			internal extern static void Finish();

			[System.Security.SuppressUnmanagedCodeSecurity()]
			[System.Runtime.InteropServices.DllImport(GL.Library, EntryPoint = "glPopClientAttrib", ExactSpelling = true)]
			internal extern static void PopClientAttrib();
		}
	}
}
