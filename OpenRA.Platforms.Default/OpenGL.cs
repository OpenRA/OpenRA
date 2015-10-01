#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;

namespace OpenRA.Platforms.Default
{
	[SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter",
		Justification = "C-style naming is kept for consistency with the underlying native API.")]
	[SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore",
		Justification = "C-style naming is kept for consistency with the underlying native API.")]
	internal static class OpenGL
	{
		public enum GLFeatures
		{
			None = 0,
			GL2OrGreater = 1,
			FramebufferExt = 4,
		}

		public static GLFeatures Features { get; private set; }

		public static string Version { get; private set; }

		public const int GL_FALSE = 0;

		// ClearBufferMask
		public const int GL_COLOR_BUFFER_BIT = 0x4000;
		public const int GL_DEPTH_BUFFER_BIT = 0x0100;
		public const int GL_STENCIL_BUFFER_BIT = 0x0400;

		// Data types
		public const int GL_UNSIGNED_BYTE = 0x1401;
		public const int GL_FLOAT = 0x1406;

		// Errors
		public const int GL_NO_ERROR = 0;

		// BeginMode
		public const int GL_POINTS = 0;
		public const int GL_LINES = 0x0001;
		public const int GL_TRIANGLES = 0x0004;

		// EnableCap
		public const int GL_ALPHA_TEST = 0x0BC0;
		public const int GL_BLEND = 0x0BE2;
		public const int GL_STENCIL_TEST = 0x0B90;
		public const int GL_DEPTH_TEST = 0x0B71;
		public const int GL_SCISSOR_TEST = 0x0C11;

		// Texture mapping
		public const int GL_TEXTURE_2D = 0x0DE1;
		public const int GL_TEXTURE_WRAP_S = 0x2802;
		public const int GL_TEXTURE_WRAP_T = 0x2803;
		public const int GL_TEXTURE_MAG_FILTER = 0x2800;
		public const int GL_TEXTURE_MIN_FILTER = 0x2801;
		public const int GL_NEAREST = 0x2600;
		public const int GL_LINEAR = 0x2601;

		// Depth buffer
		public const int GL_DEPTH_COMPONENT = 0x1902;
		public const int GL_LEQUAL = 0x0203;

		// BlendingFactorDest
		public const int GL_ZERO = 0;
		public const int GL_ONE = 1;
		public const int GL_SRC_COLOR = 0x0300;
		public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
		public const int GL_SRC_ALPHA = 0x0302;
		public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
		public const int GL_DST_ALPHA = 0x0304;
		public const int GL_ONE_MINUS_DST_ALPHA = 0x0305;
		public const int GL_DST_COLOR = 0x0306;
		public const int GL_ONE_MINUS_DST_COLOR = 0x0307;

		// GL_ARB_imaging
		public const int GL_FUNC_ADD = 0x8006;
		public const int GL_FUNC_SUBTRACT = 0x800A;
		public const int GL_FUNC_REVERSE_SUBTRACT = 0x800B;
		public const int GL_BLEND_COLOR = 0x8005;

		// OpenGL 1.1 - 1.5
		public const int GL_CLIENT_PIXEL_STORE_BIT = 0x0001;
		public const int GL_BGRA = 0x80E1;
		public const int GL_RGBA8 = 0x8058;
		public const int GL_CLAMP_TO_EDGE = 0x812F;
		public const int GL_TEXTURE_BASE_LEVEL = 0x813C;
		public const int GL_TEXTURE_MAX_LEVEL = 0x813D;

		public const int GL_ARRAY_BUFFER = 0x8892;
		public const int GL_DYNAMIC_DRAW = 0x88E8;

		public const int GL_TEXTURE0 = 0x84C0;

		// OpenGL 2
		public const int GL_FRAGMENT_SHADER = 0x8B30;
		public const int GL_VERTEX_SHADER = 0x8B31;
		public const int GL_SAMPLER_2D = 0x8B5E;
		public const int GL_COMPILE_STATUS = 0x8B81;
		public const int GL_LINK_STATUS = 0x8B82;
		public const int GL_INFO_LOG_LENGTH = 0x8B84;
		public const int GL_ACTIVE_UNIFORMS = 0x8B86;

		// Pixel Mode / Transfer
		public const int GL_PACK_ROW_LENGTH = 0x0D02;
		public const int GL_PACK_ALIGNMENT = 0x0D05;

		// Gets
		public const int GL_VIEWPORT = 0x0BA2;

		// Utility
		public const int GL_VENDOR = 0x1F00;
		public const int GL_RENDERER = 0x1F01;
		public const int GL_VERSION = 0x1F02;
		public const int GL_EXTENSIONS = 0x1F03;
		public const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;

		// Framebuffers
		public const int FRAMEBUFFER_EXT = 0x8D40;
		public const int RENDERBUFFER_EXT = 0x8D41;
		public const int COLOR_ATTACHMENT0_EXT = 0x8CE0;
		public const int DEPTH_ATTACHMENT_EXT = 0x8D00;
		public const int FRAMEBUFFER_COMPLETE_EXT = 0x8CD5;

		public delegate void Flush();
		public static Flush glFlush { get; private set; }

		public delegate void Viewport(int x, int y, int width, int height);
		public static Viewport glViewport { get; private set; }

		public delegate void Clear(int mask);
		public static Clear glClear { get; private set; }

		public delegate void ClearColor(float red, float green, float blue, float alpha);
		public static ClearColor glClearColor { get; private set; }

		public delegate int GetError();
		public static GetError glGetError { get; private set; }

		delegate IntPtr GetString(int name);
		static GetString glGetStringInternal;

		public static string glGetString(int name)
		{
			unsafe
			{
				return new string((sbyte*)glGetStringInternal(name));
			}
		}

		public unsafe delegate int GetIntegerv(int pname, int* param);
		public static GetIntegerv glGetIntegerv { get; private set; }

		public delegate void Finish();
		public static Finish glFinish { get; private set; }

		public delegate uint CreateProgram();
		public static CreateProgram glCreateProgram { get; private set; }

		public delegate void UseProgram(uint program);
		public static UseProgram glUseProgram { get; private set; }

		public delegate void GetProgramiv(uint program, int pname, out int param);
		public static GetProgramiv glGetProgramiv { get; private set; }

		public delegate uint CreateShader(int shaderType);
		public static CreateShader glCreateShader { get; private set; }

		public delegate void ShaderSource(uint shader, int count, string[] str, IntPtr length);
		public static ShaderSource glShaderSource { get; private set; }

		public delegate void CompileShader(uint shader);
		public static CompileShader glCompileShader { get; private set; }

		public delegate int GetShaderiv(uint shader, int name, out int param);
		public static GetShaderiv glGetShaderiv { get; private set; }

		public delegate void AttachShader(uint program, uint shader);
		public static AttachShader glAttachShader { get; private set; }

		public delegate void GetShaderInfoLog(uint shader, int maxLength, out int length, StringBuilder infoLog);
		public static GetShaderInfoLog glGetShaderInfoLog { get; private set; }

		public delegate void LinkProgram(uint program);
		public static LinkProgram glLinkProgram { get; private set; }

		public delegate void GetProgramInfoLog(uint program, int maxLength, out int length, StringBuilder infoLog);
		public static GetProgramInfoLog glGetProgramInfoLog { get; private set; }

		public delegate int GetUniformLocation(uint program, string name);
		public static GetUniformLocation glGetUniformLocation { get; private set; }

		public delegate void GetActiveUniform(uint program, int index, int bufSize,
			out int length, out int size, out int type, StringBuilder name);
		public static GetActiveUniform glGetActiveUniform { get; private set; }

		public delegate void Uniform1i(int location, int v0);
		public static Uniform1i glUniform1i { get; private set; }

		public delegate void Uniform1f(int location, float v0);
		public static Uniform1f glUniform1f { get; private set; }

		public delegate void Uniform2f(int location, float v0, float v1);
		public static Uniform2f glUniform2f { get; private set; }

		public delegate void Uniform3f(int location, float v0, float v1, float v2);
		public static Uniform3f glUniform3f { get; private set; }

		public delegate void Uniform1fv(int location, int count, IntPtr value);
		public static Uniform1fv glUniform1fv { get; private set; }

		public delegate void Uniform2fv(int location, int count, IntPtr value);
		public static Uniform2fv glUniform2fv { get; private set; }

		public delegate void Uniform3fv(int location, int count, IntPtr value);
		public static Uniform3fv glUniform3fv { get; private set; }

		public delegate void Uniform4fv(int location, int count, IntPtr value);
		public static Uniform4fv glUniform4fv { get; private set; }

		public delegate void UniformMatrix4fv(int location, int count, bool transpose, IntPtr value);
		public static UniformMatrix4fv glUniformMatrix4fv { get; private set; }

		public delegate void GenBuffers(int n, out uint buffers);
		public static GenBuffers glGenBuffers { get; private set; }

		public delegate void BindBuffer(int target, uint buffer);
		public static BindBuffer glBindBuffer { get; private set; }

		public delegate void BufferData(int target, IntPtr size, IntPtr data, int usage);
		public static BufferData glBufferData { get; private set; }

		public delegate void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data);
		public static BufferSubData glBufferSubData { get; private set; }

		public delegate void DeleteBuffers(int n, ref uint buffers);
		public static DeleteBuffers glDeleteBuffers { get; private set; }

		public delegate void BindAttribLocation(uint program, int index, string name);
		public static BindAttribLocation glBindAttribLocation { get; private set; }

		public delegate void VertexAttribPointer(int index, int size, int type, bool normalized,
			int stride, IntPtr pointer);
		public static VertexAttribPointer glVertexAttribPointer { get; private set; }

		public delegate void EnableVertexAttribArray(int index);
		public static EnableVertexAttribArray glEnableVertexAttribArray { get; private set; }

		public delegate void DisableVertexAttribArray(int index);
		public static DisableVertexAttribArray glDisableVertexAttribArray { get; private set; }

		public delegate void DrawArrays(int mode, int first, int count);
		public static DrawArrays glDrawArrays { get; private set; }

		public delegate void Enable(int cap);
		public static Enable glEnable { get; private set; }

		public delegate void Disable(int cap);
		public static Disable glDisable { get; private set; }

		public delegate void BlendEquation(int mode);
		public static BlendEquation glBlendEquation { get; private set; }

		public delegate void BlendFunc(int sfactor, int dfactor);
		public static BlendFunc glBlendFunc { get; private set; }

		public delegate void DepthFunc(int func);
		public static DepthFunc glDepthFunc { get; private set; }

		public delegate void Scissor(int x, int y, int width, int height);
		public static Scissor glScissor { get; private set; }

		public delegate void PushClientAttrib(int mask);
		public static PushClientAttrib glPushClientAttrib { get; private set; }

		public delegate void PopClientAttrib();
		public static PopClientAttrib glPopClientAttrib { get; private set; }

		public delegate void PixelStoref(int param, float pname);
		public static PixelStoref glPixelStoref { get; private set; }

		public delegate void ReadPixels(int x, int y, int width, int height,
			int format, int type, IntPtr data);
		public static ReadPixels glReadPixels { get; private set; }

		public delegate void GenTextures(int n, out uint textures);
		public static GenTextures glGenTextures { get; private set; }

		public delegate void DeleteTextures(int n, ref uint textures);
		public static DeleteTextures glDeleteTextures { get; private set; }

		public delegate void BindTexture(int target, uint texture);
		public static BindTexture glBindTexture { get; private set; }

		public delegate void ActiveTexture(int texture);
		public static ActiveTexture glActiveTexture { get; private set; }

		public delegate void TexImage2D(int target, int level, int internalFormat,
			int width, int height, int border, int format, int type, IntPtr pixels);
		public static TexImage2D glTexImage2D { get; private set; }

		public delegate void GetTexImage(int target, int level,
			int format, int type, IntPtr pixels);
		public static GetTexImage glGetTexImage { get; private set; }

		public delegate void TexParameteri(int target, int pname, int param);
		public static TexParameteri glTexParameteri { get; private set; }

		public delegate void TexParameterf(int target, int pname, float param);
		public static TexParameterf glTexParameterf { get; private set; }

		public delegate void GenFramebuffers(int n, out uint framebuffers);
		public static GenFramebuffers glGenFramebuffers { get; private set; }

		public delegate void BindFramebuffer(int target, uint framebuffer);
		public static BindFramebuffer glBindFramebuffer { get; private set; }

		public delegate void FramebufferTexture2D(int target, int attachment,
			int textarget, uint texture, int level);
		public static FramebufferTexture2D glFramebufferTexture2D { get; private set; }

		public delegate void DeleteFramebuffers(int n, ref uint framebuffers);
		public static DeleteFramebuffers glDeleteFramebuffers { get; private set; }

		public delegate void GenRenderbuffers(int n, out uint renderbuffers);
		public static GenRenderbuffers glGenRenderbuffers { get; private set; }

		public delegate void BindRenderbuffer(int target, uint renderbuffer);
		public static BindRenderbuffer glBindRenderbuffer { get; private set; }

		public delegate void RenderbufferStorage(int target, int internalformat,
			int width, int height);
		public static RenderbufferStorage glRenderbufferStorage { get; private set; }

		public delegate void DeleteRenderbuffers(int n, ref uint renderbuffers);
		public static DeleteRenderbuffers glDeleteRenderbuffers { get; private set; }

		public delegate void FramebufferRenderbuffer(int target, int attachment,
			int renderbuffertarget, uint renderbuffer);
		public static FramebufferRenderbuffer glFramebufferRenderbuffer { get; private set; }

		public delegate int CheckFramebufferStatus(int target);
		public static CheckFramebufferStatus glCheckFramebufferStatus { get; private set; }

		public static void Initialize()
		{
			// glGetError and glGetString are used in our error handlers
			// so we want these to be available early.
			try
			{
				glGetError = Bind<GetError>("glGetError");
				glGetStringInternal = Bind<GetString>("glGetString");
			}
			catch (Exception)
			{
				throw new InvalidProgramException("Failed to initialize low-level OpenGL bindings. GPU information is not available");
			}

			DetectGLFeatures();
			if (!Features.HasFlag(GLFeatures.GL2OrGreater) || !Features.HasFlag(GLFeatures.FramebufferExt))
			{
				WriteGraphicsLog("Unsupported OpenGL version: " + glGetString(GL_VERSION));
				throw new InvalidProgramException("OpenGL Version Error: See graphics.log for details.");
			}
			else
				Console.WriteLine("OpenGL version: " + glGetString(GL_VERSION));

			try
			{
				glFlush = Bind<Flush>("glFlush");
				glViewport = Bind<Viewport>("glViewport");
				glClear = Bind<Clear>("glClear");
				glClearColor = Bind<ClearColor>("glClearColor");
				glGetIntegerv = Bind<GetIntegerv>("glGetIntegerv");
				glFinish = Bind<Finish>("glFinish");
				glCreateProgram = Bind<CreateProgram>("glCreateProgram");
				glUseProgram = Bind<UseProgram>("glUseProgram");
				glGetProgramiv = Bind<GetProgramiv>("glGetProgramiv");
				glCreateShader = Bind<CreateShader>("glCreateShader");
				glShaderSource = Bind<ShaderSource>("glShaderSource");
				glCompileShader = Bind<CompileShader>("glCompileShader");
				glGetShaderiv = Bind<GetShaderiv>("glGetShaderiv");
				glAttachShader = Bind<AttachShader>("glAttachShader");
				glGetShaderInfoLog = Bind<GetShaderInfoLog>("glGetShaderInfoLog");
				glLinkProgram = Bind<LinkProgram>("glLinkProgram");
				glGetProgramInfoLog = Bind<GetProgramInfoLog>("glGetProgramInfoLog");
				glGetUniformLocation = Bind<GetUniformLocation>("glGetUniformLocation");
				glGetActiveUniform = Bind<GetActiveUniform>("glGetActiveUniform");
				glUniform1i = Bind<Uniform1i>("glUniform1i");
				glUniform1f = Bind<Uniform1f>("glUniform1f");
				glUniform2f = Bind<Uniform2f>("glUniform2f");
				glUniform3f = Bind<Uniform3f>("glUniform3f");
				glUniform1fv = Bind<Uniform1fv>("glUniform1fv");
				glUniform2fv = Bind<Uniform2fv>("glUniform2fv");
				glUniform3fv = Bind<Uniform3fv>("glUniform3fv");
				glUniform4fv = Bind<Uniform4fv>("glUniform4fv");
				glUniformMatrix4fv = Bind<UniformMatrix4fv>("glUniformMatrix4fv");
				glGenBuffers = Bind<GenBuffers>("glGenBuffers");
				glBindBuffer = Bind<BindBuffer>("glBindBuffer");
				glBufferData = Bind<BufferData>("glBufferData");
				glBufferSubData = Bind<BufferSubData>("glBufferSubData");
				glDeleteBuffers = Bind<DeleteBuffers>("glDeleteBuffers");
				glBindAttribLocation = Bind<BindAttribLocation>("glBindAttribLocation");
				glVertexAttribPointer = Bind<VertexAttribPointer>("glVertexAttribPointer");
				glEnableVertexAttribArray = Bind<EnableVertexAttribArray>("glEnableVertexAttribArray");
				glDisableVertexAttribArray = Bind<DisableVertexAttribArray>("glDisableVertexAttribArray");
				glDrawArrays = Bind<DrawArrays>("glDrawArrays");
				glEnable = Bind<Enable>("glEnable");
				glDisable = Bind<Disable>("glDisable");
				glBlendEquation = Bind<BlendEquation>("glBlendEquation");
				glBlendFunc = Bind<BlendFunc>("glBlendFunc");
				glDepthFunc = Bind<DepthFunc>("glDepthFunc");
				glScissor = Bind<Scissor>("glScissor");
				glPushClientAttrib = Bind<PushClientAttrib>("glPushClientAttrib");
				glPopClientAttrib = Bind<PopClientAttrib>("glPopClientAttrib");
				glPixelStoref = Bind<PixelStoref>("glPixelStoref");
				glReadPixels = Bind<ReadPixels>("glReadPixels");
				glGenTextures = Bind<GenTextures>("glGenTextures");
				glDeleteTextures = Bind<DeleteTextures>("glDeleteTextures");
				glBindTexture = Bind<BindTexture>("glBindTexture");
				glActiveTexture = Bind<ActiveTexture>("glActiveTexture");
				glTexImage2D = Bind<TexImage2D>("glTexImage2D");
				glGetTexImage = Bind<GetTexImage>("glGetTexImage");
				glTexParameteri = Bind<TexParameteri>("glTexParameteri");
				glTexParameterf = Bind<TexParameterf>("glTexParameterf");
				glGenFramebuffers = Bind<GenFramebuffers>("glGenFramebuffersEXT");
				glBindFramebuffer = Bind<BindFramebuffer>("glBindFramebufferEXT");
				glFramebufferTexture2D = Bind<FramebufferTexture2D>("glFramebufferTexture2DEXT");
				glDeleteFramebuffers = Bind<DeleteFramebuffers>("glDeleteFramebuffersEXT");
				glGenRenderbuffers = Bind<GenRenderbuffers>("glGenRenderbuffersEXT");
				glBindRenderbuffer = Bind<BindRenderbuffer>("glBindRenderbufferEXT");
				glRenderbufferStorage = Bind<RenderbufferStorage>("glRenderbufferStorageEXT");
				glDeleteRenderbuffers = Bind<DeleteRenderbuffers>("glDeleteRenderbuffersEXT");
				glFramebufferRenderbuffer = Bind<FramebufferRenderbuffer>("glFramebufferRenderbufferEXT");
				glCheckFramebufferStatus = Bind<CheckFramebufferStatus>("glCheckFramebufferStatusEXT");
			}
			catch (Exception e)
			{
				WriteGraphicsLog("Failed to initialize OpenGL bindings.\nInner exception was: {0}".F(e));
				throw new InvalidProgramException("Failed to initialize OpenGL. See graphics.log for details.");
			}
		}

		static T Bind<T>(string name)
		{
			return (T)(object)Marshal.GetDelegateForFunctionPointer(SDL.SDL_GL_GetProcAddress(name), typeof(T));
		}

		public static void DetectGLFeatures()
		{
			try
			{
				Version = glGetString(GL_VERSION);
				var version = Version.Contains(" ") ? Version.Split(' ')[0].Split('.') : Version.Split('.');

				var major = 0;
				if (version.Length > 0)
					int.TryParse(version[0], out major);

				var minor = 0;
				if (version.Length > 1)
					int.TryParse(version[1], out minor);

				if (major >= 2 && minor >= 0)
					Features |= GLFeatures.GL2OrGreater;

				var hasFramebufferExt = SDL.SDL_GL_ExtensionSupported("GL_EXT_framebuffer_object") == SDL.SDL_bool.SDL_TRUE;
				if (hasFramebufferExt)
					Features |= GLFeatures.FramebufferExt;
			}
			catch (Exception) { }
		}

		public static void CheckGLError()
		{
			var n = glGetError();
			if (n != GL_NO_ERROR)
			{
				var error = "GL Error: {0}\n{1}".F(n, new StackTrace());
				WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}
		}

		public static void WriteGraphicsLog(string message)
		{
			Log.Write("graphics", message);
			Log.Write("graphics", "");
			Log.Write("graphics", "OpenGL Information:");
			var vendor = glGetString(GL_VENDOR);
			Log.Write("graphics", "Vendor: {0}", vendor);
			if (vendor.Contains("Microsoft"))
			{
				var msg = "";
				msg += "Note:  The default driver provided by Microsoft does not include full OpenGL support.\n";
				msg += "Please install the latest drivers from your graphics card manufacturer's website.\n";
				Log.Write("graphics", msg);
			}

			Log.Write("graphics", "Renderer: {0}", glGetString(GL_RENDERER));
			Log.Write("graphics", "GL Version: {0}", glGetString(GL_VERSION));
			Log.Write("graphics", "Shader Version: {0}", glGetString(GL_SHADING_LANGUAGE_VERSION));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", glGetString(GL_EXTENSIONS));
		}
	}
}
