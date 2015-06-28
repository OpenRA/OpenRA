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
	#pragma warning disable 0649
	#pragma warning disable 3019
	#pragma warning disable 1591

	partial class GL
	{
		internal static partial class Delegates
		{
			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void EnableClientState(OpenTK.Graphics.OpenGL.ArrayCap array);
			internal static EnableClientState glEnableClientState;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void DrawArrays(OpenTK.Graphics.OpenGL.BeginMode mode, Int32 first, Int32 count);
			internal static DrawArrays glDrawArrays;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void ClearColor(Single red, Single green, Single blue, Single alpha);
			internal static ClearColor glClearColor;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask);
			internal static Clear glClear;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Enable(OpenTK.Graphics.OpenGL.EnableCap cap);
			internal static Enable glEnable;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Disable(OpenTK.Graphics.OpenGL.EnableCap cap);
			internal static Disable glDisable;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BlendEquation(OpenTK.Graphics.OpenGL.BlendEquationMode mode);
			internal static BlendEquation glBlendEquation;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc sfactor, OpenTK.Graphics.OpenGL.BlendingFactorDest dfactor);
			internal static BlendFunc glBlendFunc;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Scissor(Int32 x, Int32 y, Int32 width, Int32 height);
			internal static Scissor glScissor;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void LineWidth(Single width);
			internal static LineWidth glLineWidth;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate Int32 CreateShader(OpenTK.Graphics.OpenGL.ShaderType type);
			internal static CreateShader glCreateShader;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void CompileShader(UInt32 shader);
			internal static CompileShader glCompileShader;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetShaderiv(UInt32 shader, OpenTK.Graphics.OpenGL.ShaderParameter pname, [OutAttribute] Int32* @params);
			internal unsafe static GetShaderiv glGetShaderiv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetShaderInfoLog(UInt32 shader, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] StringBuilder infoLog);
			internal unsafe static GetShaderInfoLog glGetShaderInfoLog;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetProgramInfoLog(UInt32 program, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] StringBuilder infoLog);
			internal unsafe static GetProgramInfoLog glGetProgramInfoLog;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate Int32 CreateProgram();
			internal static CreateProgram glCreateProgram;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void AttachShader(UInt32 program, UInt32 shader);
			internal static AttachShader glAttachShader;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void LinkProgram(UInt32 program);
			internal static LinkProgram glLinkProgram;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetProgramiv(UInt32 program, OpenTK.Graphics.OpenGL.ProgramParameter pname, [OutAttribute] Int32* @params);
			internal unsafe static GetProgramiv glGetProgramiv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void UseProgram(UInt32 program);
			internal static UseProgram glUseProgram;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetActiveUniform(UInt32 program, UInt32 index, Int32 bufSize, [OutAttribute] Int32* length, [OutAttribute] Int32* size, [OutAttribute] OpenTK.Graphics.OpenGL.ActiveUniformType* type, [OutAttribute] StringBuilder name);
			internal unsafe static GetActiveUniform glGetActiveUniform;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate Int32 GetUniformLocation(UInt32 program, String name);
			internal static GetUniformLocation glGetUniformLocation;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Uniform1i(Int32 location, Int32 v0);
			internal static Uniform1i glUniform1i;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Uniform1f(Int32 location, Single v0);
			internal static Uniform1f glUniform1f;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Uniform2f(Int32 location, Single v0, Single v1);
			internal static Uniform2f glUniform2f;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void Uniform1fv(Int32 location, Int32 count, Single* value);
			internal unsafe static Uniform1fv glUniform1fv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void Uniform2fv(Int32 location, Int32 count, Single* value);
			internal unsafe static Uniform2fv glUniform2fv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void Uniform3fv(Int32 location, Int32 count, Single* value);
			internal unsafe static Uniform3fv glUniform3fv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void Uniform4fv(Int32 location, Int32 count, Single* value);
			internal unsafe static Uniform4fv glUniform4fv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void UniformMatrix4fv(Int32 location, Int32 count, bool transpose, Single* value);
			internal unsafe static UniformMatrix4fv glUniformMatrix4fv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void ActiveTexture(OpenTK.Graphics.OpenGL.TextureUnit texture);
			internal static ActiveTexture glActiveTexture;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BindTexture(OpenTK.Graphics.OpenGL.TextureTarget target, UInt32 texture);
			internal static BindTexture glBindTexture;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate IntPtr GetString(OpenTK.Graphics.OpenGL.StringName name);
			internal static GetString glGetString;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate OpenTK.Graphics.OpenGL.ErrorCode GetError();
			internal static GetError glGetError;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GenFramebuffersEXT(Int32 n, [OutAttribute] UInt32* framebuffers);
			internal unsafe static GenFramebuffersEXT glGenFramebuffersEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BindFramebufferEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, UInt32 framebuffer);
			internal static BindFramebufferEXT glBindFramebufferEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void FramebufferTexture2DEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, OpenTK.Graphics.OpenGL.FramebufferAttachment attachment, OpenTK.Graphics.OpenGL.TextureTarget textarget, UInt32 texture, Int32 level);
			internal static FramebufferTexture2DEXT glFramebufferTexture2DEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GenRenderbuffersEXT(Int32 n, [OutAttribute] UInt32* renderbuffers);
			internal unsafe static GenRenderbuffersEXT glGenRenderbuffersEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BindRenderbufferEXT(OpenTK.Graphics.OpenGL.RenderbufferTarget target, UInt32 renderbuffer);
			internal static BindRenderbufferEXT glBindRenderbufferEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void RenderbufferStorageEXT(OpenTK.Graphics.OpenGL.RenderbufferTarget target, OpenTK.Graphics.OpenGL.RenderbufferStorage internalformat, Int32 width, Int32 height);
			internal static RenderbufferStorageEXT glRenderbufferStorageEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void FramebufferRenderbufferEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target, OpenTK.Graphics.OpenGL.FramebufferAttachment attachment, OpenTK.Graphics.OpenGL.RenderbufferTarget renderbuffertarget, UInt32 renderbuffer);
			internal static FramebufferRenderbufferEXT glFramebufferRenderbufferEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate OpenTK.Graphics.OpenGL.FramebufferErrorCode CheckFramebufferStatusEXT(OpenTK.Graphics.OpenGL.FramebufferTarget target);
			internal static CheckFramebufferStatusEXT glCheckFramebufferStatusEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void DeleteFramebuffersEXT(Int32 n, UInt32* framebuffers);
			internal unsafe static DeleteFramebuffersEXT glDeleteFramebuffersEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void DeleteRenderbuffersEXT(Int32 n, UInt32* renderbuffers);
			internal unsafe static DeleteRenderbuffersEXT glDeleteRenderbuffersEXT;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GetIntegerv(OpenTK.Graphics.OpenGL.GetPName pname, [OutAttribute] Int32* @params);
			internal unsafe static GetIntegerv glGetIntegerv;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Flush();
			internal static Flush glFlush;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Viewport(Int32 x, Int32 y, Int32 width, Int32 height);
			internal static Viewport glViewport;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GenTextures(Int32 n, [OutAttribute] UInt32* textures);
			internal unsafe static GenTextures glGenTextures;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void TexParameteri(OpenTK.Graphics.OpenGL.TextureTarget target, OpenTK.Graphics.OpenGL.TextureParameterName pname, Int32 param);
			internal static TexParameteri glTexParameteri;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void TexParameterf(OpenTK.Graphics.OpenGL.TextureTarget target, OpenTK.Graphics.OpenGL.TextureParameterName pname, Single param);
			internal static TexParameterf glTexParameterf;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget target, Int32 level, OpenTK.Graphics.OpenGL.PixelInternalFormat internalformat, Int32 width, Int32 height, Int32 border, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, IntPtr pixels);
			internal static TexImage2D glTexImage2D;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void GetTexImage(OpenTK.Graphics.OpenGL.TextureTarget target, Int32 level, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, [OutAttribute] IntPtr pixels);
			internal static GetTexImage glGetTexImage;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void DeleteTextures(Int32 n, UInt32* textures);
			internal unsafe static DeleteTextures glDeleteTextures;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void GenBuffers(Int32 n, [OutAttribute] UInt32* buffers);
			internal unsafe static GenBuffers glGenBuffers;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BufferData(OpenTK.Graphics.OpenGL.BufferTarget target, IntPtr size, IntPtr data, OpenTK.Graphics.OpenGL.BufferUsageHint usage);
			internal static BufferData glBufferData;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BufferSubData(OpenTK.Graphics.OpenGL.BufferTarget target, IntPtr offset, IntPtr size, IntPtr data);
			internal static BufferSubData glBufferSubData;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget target, UInt32 buffer);
			internal static BindBuffer glBindBuffer;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void VertexPointer(Int32 size, OpenTK.Graphics.OpenGL.VertexPointerType type, Int32 stride, IntPtr pointer);
			internal static VertexPointer glVertexPointer;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void TexCoordPointer(Int32 size, OpenTK.Graphics.OpenGL.TexCoordPointerType type, Int32 stride, IntPtr pointer);
			internal static TexCoordPointer glTexCoordPointer;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void DeleteBuffers(Int32 n, UInt32* buffers);
			internal unsafe static DeleteBuffers glDeleteBuffers;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal unsafe delegate void ShaderSource(UInt32 shader, Int32 count, String[] @string, Int32* length);
			internal unsafe static ShaderSource glShaderSource;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void PushClientAttrib(OpenTK.Graphics.OpenGL.ClientAttribMask mask);
			internal static PushClientAttrib glPushClientAttrib;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void PixelStoref(OpenTK.Graphics.OpenGL.PixelStoreParameter pname, Single param);
			internal static PixelStoref glPixelStoref;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void PixelStorei(OpenTK.Graphics.OpenGL.PixelStoreParameter pname, Int32 param);
			internal static PixelStorei glPixelStorei;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void ReadPixels(Int32 x, Int32 y, Int32 width, Int32 height, OpenTK.Graphics.OpenGL.PixelFormat format, OpenTK.Graphics.OpenGL.PixelType type, [OutAttribute] IntPtr pixels);
			internal static ReadPixels glReadPixels;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void Finish();
			internal static Finish glFinish;

			[System.Security.SuppressUnmanagedCodeSecurity()]
			internal delegate void PopClientAttrib();
			internal static PopClientAttrib glPopClientAttrib;
		}
	}
}
