#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;

namespace OpenRA.Platforms.Default
{
	[SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore",
		Justification = "C-style naming is kept for consistency with the underlying native API.")]
	[SuppressMessage("Style", "IDE1006:Naming Styles",
		Justification = "C-style naming is kept for consistency with the underlying native API.")]
	static class OpenGL
	{
		[Flags]
		public enum GLFeatures
		{
			None = 0,
			DebugMessagesCallback = 1,
			ESReadFormatBGRA = 2,
		}

		public static GLProfile Profile { get; private set; }
		public static GLFeatures Features { get; private set; }

		public static string Version { get; private set; }

		#region Constants

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
		public const int GL_INVALID_ENUM = 0x0500;
		public const int GL_INVALID_VALUE = 0x0501;
		public const int GL_INVALID_OPERATION = 0x0502;
		public const int GL_STACK_OVERFLOW = 0x0503;
		public const int GL_STACK_UNDERFLOW = 0x0504;
		public const int GL_OUT_OF_MEMORY = 0x0505;
		public const int GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506;
		public const int GL_CONTEXT_LOST = 0x0507;
		public const int GL_TABLE_TOO_LARGE = 0x8031;

		static readonly Dictionary<int, string> ErrorToText = new()
		{
			{ GL_NO_ERROR, "No Error" },
			{ GL_INVALID_ENUM, "Invalid Enum" },
			{ GL_INVALID_VALUE, "Invalid Value" },
			{ GL_INVALID_OPERATION, "Invalid Operation" },
			{ GL_STACK_OVERFLOW, "Stack Overflow" },
			{ GL_STACK_UNDERFLOW, "Stack Underflow" },
			{ GL_OUT_OF_MEMORY, "Out Of Memory" },
			{ GL_INVALID_FRAMEBUFFER_OPERATION, "Invalid Framebuffer Operation" },
			{ GL_CONTEXT_LOST, "Context Lost" },
			{ GL_TABLE_TOO_LARGE, "Table Too Large" },
		};

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
		public const int GL_RGBA = 0x1908;
		public const int GL_BGRA = 0x80E1;
		public const int GL_RGBA8 = 0x8058;
		public const int GL_CLAMP_TO_EDGE = 0x812F;
		public const int GL_TEXTURE_BASE_LEVEL = 0x813C;
		public const int GL_TEXTURE_MAX_LEVEL = 0x813D;

		public const int GL_ARRAY_BUFFER = 0x8892;
		public const int GL_DYNAMIC_DRAW = 0x88E8;

		public const int GL_TEXTURE0 = 0x84C0;
		public const int GL_DEPTH_COMPONENT16 = 0x81A5;

		// OpenGL 2
		public const int GL_FRAGMENT_SHADER = 0x8B30;
		public const int GL_VERTEX_SHADER = 0x8B31;
		public const int GL_SAMPLER_2D = 0x8B5E;
		public const int GL_COMPILE_STATUS = 0x8B81;
		public const int GL_LINK_STATUS = 0x8B82;
		public const int GL_INFO_LOG_LENGTH = 0x8B84;
		public const int GL_ACTIVE_UNIFORMS = 0x8B86;

		public const int GL_RGBA16F = 0x881A;

		// OpenGL 4.3
		public const int GL_DEBUG_OUTPUT = 0x92E0;
		public const int GL_DEBUG_OUTPUT_SYNCHRONOUS = 0x8242;

		public const int GL_DEBUG_SOURCE_API = 0x8246;
		public const int GL_DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247;
		public const int GL_DEBUG_SOURCE_SHADER_COMPILER = 0x8248;
		public const int GL_DEBUG_SOURCE_THIRD_PARTY = 0x8249;
		public const int GL_DEBUG_SOURCE_APPLICATION = 0x824A;
		public const int GL_DEBUG_SOURCE_OTHER = 0x824B;

		static readonly Dictionary<int, string> DebugSourceToText = new()
		{
			{ GL_DEBUG_SOURCE_API, "API" },
			{ GL_DEBUG_SOURCE_WINDOW_SYSTEM, "Window System" },
			{ GL_DEBUG_SOURCE_SHADER_COMPILER, "Shader Compiler" },
			{ GL_DEBUG_SOURCE_THIRD_PARTY, "Third Party" },
			{ GL_DEBUG_SOURCE_APPLICATION, "Application" },
			{ GL_DEBUG_SOURCE_OTHER, "Other" }
		};

		public const int GL_DEBUG_TYPE_ERROR = 0x824C;
		public const int GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR = 0x824D;
		public const int GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR = 0x824E;
		public const int GL_DEBUG_TYPE_PORTABILITY = 0x824F;
		public const int GL_DEBUG_TYPE_PERFORMANCE = 0x8250;
		public const int GL_DEBUG_TYPE_MARKER = 0x8268;
		public const int GL_DEBUG_TYPE_PUSH_GROUP = 0x8269;
		public const int GL_DEBUG_TYPE_POP_GROUP = 0x826A;
		public const int GL_DEBUG_TYPE_OTHER = 0x8251;

		static readonly Dictionary<int, string> DebugTypeToText = new()
		{
			{ GL_DEBUG_TYPE_ERROR, "Error" },
			{ GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR, "Deprecated Behaviour" },
			{ GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR, "Undefined Behaviour" },
			{ GL_DEBUG_TYPE_PORTABILITY, "Portability" },
			{ GL_DEBUG_TYPE_PERFORMANCE, "Performance" },
			{ GL_DEBUG_TYPE_MARKER, "Marker" },
			{ GL_DEBUG_TYPE_PUSH_GROUP, "Push Group" },
			{ GL_DEBUG_TYPE_POP_GROUP, "Pop Group" },
			{ GL_DEBUG_TYPE_OTHER, "Other" }
		};

		public const int GL_DEBUG_SEVERITY_HIGH = 0x9146;
		public const int GL_DEBUG_SEVERITY_MEDIUM = 0x9147;
		public const int GL_DEBUG_SEVERITY_LOW = 0x9148;
		public const int GL_DEBUG_SEVERITY_NOTIFICATION = 0x826B;

		static readonly Dictionary<int, string> DebugSeverityToText = new()
		{
			{ GL_DEBUG_SEVERITY_HIGH, "High" },
			{ GL_DEBUG_SEVERITY_MEDIUM, "Medium" },
			{ GL_DEBUG_SEVERITY_LOW, "Low" },
			{ GL_DEBUG_SEVERITY_NOTIFICATION, "Notification" }
		};

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
		public const int GL_NUM_EXTENSIONS = 0x821D;

		public const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;

		// Framebuffers
		public const int GL_FRAMEBUFFER = 0x8D40;
		public const int GL_RENDERBUFFER = 0x8D41;
		public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
		public const int GL_DEPTH_ATTACHMENT = 0x8D00;
		public const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
		public const int GL_FRAMEBUFFER_BINDING = 0x8CA6;

		#endregion

		#region GL Delegates

		public delegate void DebugProc(int source, int type, uint id, int severity, int length, StringBuilder message,
			IntPtr userParam);
		static DebugProc DebugMessageHandle { get; set; }

		public delegate void DebugMessageCallback(DebugProc callback, IntPtr userParam);
		public static DebugMessageCallback glDebugMessageCallback { get; private set; }

		public delegate void DebugMessageInsert(int source, int type, uint id, int severity, int length, string message);
		public static DebugMessageInsert glDebugMessageInsert { get; private set; }

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

		delegate IntPtr GetStringi(int name, uint index);
		static GetStringi glGetStringiInternal;

		public static string glGetStringi(int name, uint index)
		{
			unsafe
			{
				return new string((sbyte*)glGetStringiInternal(name, index));
			}
		}

		public unsafe delegate int GetIntegerv(int pname, out int param);
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

		public delegate void GenVertexArrays(int n, out uint buffers);
		public static GenVertexArrays glGenVertexArrays { get; private set; }

		public delegate void BindVertexArray(uint buffer);
		public static BindVertexArray glBindVertexArray { get; private set; }

		public delegate void BufferData(int target, IntPtr size, IntPtr data, int usage);
		public static BufferData glBufferData { get; private set; }

		public delegate void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data);
		public static BufferSubData glBufferSubData { get; private set; }

		public delegate void DeleteBuffers(int n, ref uint buffers);
		public static DeleteBuffers glDeleteBuffers { get; private set; }

		public delegate void BindAttribLocation(uint program, int index, string name);
		public static BindAttribLocation glBindAttribLocation { get; private set; }

		public delegate void BindFragDataLocation(uint program, int colorNumber, string name);
		public static BindFragDataLocation glBindFragDataLocation { get; private set; }

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

		public delegate void BlendEquationSeparate(int modeRGB, int modeAlpha);
		public static BlendEquationSeparate glBlendEquationSeparate { get; private set; }

		public delegate void BlendFunc(int sfactor, int dfactor);
		public static BlendFunc glBlendFunc { get; private set; }

		public delegate void DepthFunc(int func);
		public static DepthFunc glDepthFunc { get; private set; }

		public delegate void Scissor(int x, int y, int width, int height);
		public static Scissor glScissor { get; private set; }

		public delegate void ReadPixels(int x, int y, int width, int height,
			int format, int type, IntPtr data);
		public static ReadPixels glReadPixels { get; private set; }

		public delegate void GenTextures(int n, out uint textures);
		public static GenTextures glGenTextures { get; private set; }

		public delegate void DeleteTextures(int n, ref uint textures);
		public static DeleteTextures glDeleteTextures { get; private set; }

		public delegate bool IsTexture(uint texture);
		public static IsTexture glIsTexture { get; private set; }

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

		#endregion

		public static void Initialize(bool preferLegacyProfile)
		{
			try
			{
				// First set up the bindings we need for error handling
				glEnable = Bind<Enable>("glEnable");
				glDisable = Bind<Disable>("glDisable");
				glGetError = Bind<GetError>("glGetError");
				glGetStringInternal = Bind<GetString>("glGetString");
				glGetStringiInternal = Bind<GetStringi>("glGetStringi");
				glGetIntegerv = Bind<GetIntegerv>("glGetIntegerv");
			}
			catch (Exception e)
			{
				throw new InvalidProgramException("Failed to initialize low-level OpenGL bindings. GPU information is not available.", e);
			}

			if (!DetectGLFeatures(preferLegacyProfile))
			{
				WriteGraphicsLog("Unsupported OpenGL version: " + glGetString(GL_VERSION));
				throw new InvalidProgramException("OpenGL Version Error: See graphics.log for details.");
			}

			// Allow users to force-disable the debug message callback feature to work around driver bugs
			if (Features.HasFlag(GLFeatures.DebugMessagesCallback) && Game.Settings.Graphics.DisableGLDebugMessageCallback)
				Features ^= GLFeatures.DebugMessagesCallback;

			// Force disable the debug message callback feature on Linux + AMD GPU to work around a startup freeze
			if (Features.HasFlag(GLFeatures.DebugMessagesCallback) && Platform.CurrentPlatform == PlatformType.Linux)
			{
				var renderer = glGetString(GL_RENDERER);
				if (renderer.Contains("AMD") || renderer.Contains("Radeon"))
					Features ^= GLFeatures.DebugMessagesCallback;
			}

			// Older Intel on Windows is broken too
			if (Features.HasFlag(GLFeatures.DebugMessagesCallback) && Platform.CurrentPlatform == PlatformType.Windows)
			{
				var renderer = glGetString(GL_RENDERER);
				if (renderer.Contains("HD Graphics") && !renderer.Contains("UHD Graphics"))
					Features ^= GLFeatures.DebugMessagesCallback;
			}

			// Setup the debug message callback handler
			if (Features.HasFlag(GLFeatures.DebugMessagesCallback))
			{
				try
				{
					var suffix = Profile == GLProfile.Embedded ? "KHR" : "";
					glDebugMessageCallback = Bind<DebugMessageCallback>("glDebugMessageCallback" + suffix);
					glDebugMessageInsert = Bind<DebugMessageInsert>("glDebugMessageInsert" + suffix);

					glEnable(GL_DEBUG_OUTPUT);
					glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);

					// Need to keep a reference to the callback so it doesn't get garbage collected
					DebugMessageHandle = DebugMessageHandler;
					glDebugMessageCallback(DebugMessageHandle, IntPtr.Zero);
				}
				catch (Exception e)
				{
					throw new InvalidProgramException("Failed to initialize an OpenGL debug message callback.", e);
				}
			}

			Console.WriteLine("OpenGL renderer: " + glGetString(GL_RENDERER));
			Console.WriteLine("OpenGL version: " + glGetString(GL_VERSION));

			try
			{
				glFlush = Bind<Flush>("glFlush");
				glViewport = Bind<Viewport>("glViewport");
				glClear = Bind<Clear>("glClear");
				glClearColor = Bind<ClearColor>("glClearColor");
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
				glBlendEquation = Bind<BlendEquation>("glBlendEquation");
				glBlendEquationSeparate = Bind<BlendEquationSeparate>("glBlendEquationSeparate");
				glBlendFunc = Bind<BlendFunc>("glBlendFunc");
				glDepthFunc = Bind<DepthFunc>("glDepthFunc");
				glScissor = Bind<Scissor>("glScissor");
				glReadPixels = Bind<ReadPixels>("glReadPixels");
				glGenTextures = Bind<GenTextures>("glGenTextures");
				glDeleteTextures = Bind<DeleteTextures>("glDeleteTextures");
				glIsTexture = Bind<IsTexture>("glIsTexture");
				glBindTexture = Bind<BindTexture>("glBindTexture");
				glActiveTexture = Bind<ActiveTexture>("glActiveTexture");
				glTexImage2D = Bind<TexImage2D>("glTexImage2D");
				glTexParameteri = Bind<TexParameteri>("glTexParameteri");
				glTexParameterf = Bind<TexParameterf>("glTexParameterf");

				if (Profile != GLProfile.Legacy)
				{
					if (Profile != GLProfile.Embedded)
					{
						glGetTexImage = Bind<GetTexImage>("glGetTexImage");
						glBindFragDataLocation = Bind<BindFragDataLocation>("glBindFragDataLocation");
					}
					else
					{
						glGetTexImage = null;
						glBindFragDataLocation = null;
					}

					glGenVertexArrays = Bind<GenVertexArrays>("glGenVertexArrays");
					glBindVertexArray = Bind<BindVertexArray>("glBindVertexArray");
					glGenFramebuffers = Bind<GenFramebuffers>("glGenFramebuffers");
					glBindFramebuffer = Bind<BindFramebuffer>("glBindFramebuffer");
					glFramebufferTexture2D = Bind<FramebufferTexture2D>("glFramebufferTexture2D");
					glDeleteFramebuffers = Bind<DeleteFramebuffers>("glDeleteFramebuffers");
					glGenRenderbuffers = Bind<GenRenderbuffers>("glGenRenderbuffers");
					glBindRenderbuffer = Bind<BindRenderbuffer>("glBindRenderbuffer");
					glRenderbufferStorage = Bind<RenderbufferStorage>("glRenderbufferStorage");
					glDeleteRenderbuffers = Bind<DeleteRenderbuffers>("glDeleteRenderbuffers");
					glFramebufferRenderbuffer = Bind<FramebufferRenderbuffer>("glFramebufferRenderbuffer");
					glCheckFramebufferStatus = Bind<CheckFramebufferStatus>("glCheckFramebufferStatus");
				}
				else
				{
					glGenVertexArrays = null;
					glBindVertexArray = null;
					glBindFragDataLocation = null;
					glGetTexImage = Bind<GetTexImage>("glGetTexImage");
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
			}
			catch (Exception e)
			{
				WriteGraphicsLog($"Failed to initialize OpenGL bindings.\nInner exception was: {e}");
				throw new InvalidProgramException("Failed to initialize OpenGL. See graphics.log for details.", e);
			}
		}

		static T Bind<T>(string name)
		{
			return (T)(object)Marshal.GetDelegateForFunctionPointer(SDL.SDL_GL_GetProcAddress(name), typeof(T));
		}

		public static bool DetectGLFeatures(bool preferLegacyProfile)
		{
			var hasValidConfiguration = false;
			try
			{
				Version = glGetString(GL_VERSION);

				var major = 0;
				var minor = 0;

				// Assume that the first numeric token corresponds to the GL version
				foreach (var t in Version.Split())
				{
					var split = t.Split('.');
					if (split.Length >= 2 && int.TryParse(split[0], out major) && int.TryParse(split[1], out minor))
						break;
				}

				// Core features are defined as the shared feature set of GL 3.2 and (GLES 3 + BGRA extension)
				var hasBGRA = SDL.SDL_GL_ExtensionSupported("GL_EXT_texture_format_BGRA8888") == SDL.SDL_bool.SDL_TRUE;
				if (Version.Contains(" ES") && hasBGRA && major >= 3)
				{
					hasValidConfiguration = true;
					Profile = GLProfile.Embedded;
					if (SDL.SDL_GL_ExtensionSupported("GL_EXT_read_format_bgra") == SDL.SDL_bool.SDL_TRUE)
						Features |= GLFeatures.ESReadFormatBGRA;
				}
				else if (major > 3 || (major == 3 && minor >= 2))
				{
					hasValidConfiguration = true;
					Profile = GLProfile.Modern;
				}

				// Debug callbacks were introduced in GL 4.3
				var hasDebugMessagesCallback = SDL.SDL_GL_ExtensionSupported("GL_KHR_debug") == SDL.SDL_bool.SDL_TRUE;
				if (hasDebugMessagesCallback)
					Features |= GLFeatures.DebugMessagesCallback;

				if (preferLegacyProfile || (major == 2 && minor == 1) || (major == 3 && minor < 2))
				{
					if (SDL.SDL_GL_ExtensionSupported("GL_EXT_framebuffer_object") == SDL.SDL_bool.SDL_TRUE)
					{
						hasValidConfiguration = true;
						Profile = GLProfile.Legacy;
					}
				}
			}
			catch (Exception) { }

			return hasValidConfiguration;
		}

		public static void WriteGraphicsLog(string message)
		{
			Log.Write("graphics", message);
			Log.Write("graphics", "");
			Log.Write("graphics", "OpenGL Information:");
			var vendor = glGetString(GL_VENDOR);
			Log.Write("graphics", $"Vendor: {vendor}");
			if (vendor.Contains("Microsoft"))
			{
				var msg = "";
				msg += "Note:  The default driver provided by Microsoft does not include full OpenGL support.\n";
				msg += "Please install the latest drivers from your graphics card manufacturer's website.\n";
				Log.Write("graphics", msg);
			}

			Log.Write("graphics", $"Renderer: {glGetString(GL_RENDERER)}");
			Log.Write("graphics", $"GL Version: {glGetString(GL_VERSION)}");
			Log.Write("graphics", $"Shader Version: {glGetString(GL_SHADING_LANGUAGE_VERSION)}");
			Log.Write("graphics", "Available extensions:");

			if (Profile != GLProfile.Legacy)
			{
				glGetIntegerv(GL_NUM_EXTENSIONS, out var extensionCount);
				for (var i = 0; i < extensionCount; i++)
					Log.Write("graphics", glGetStringi(GL_EXTENSIONS, (uint)i));
			}
			else
				Log.Write("graphics", glGetString(GL_EXTENSIONS));
		}

		public static void CheckGLError()
		{
			// Let the debug message handler log the errors instead.
			if ((Features & GLFeatures.DebugMessagesCallback) == GLFeatures.DebugMessagesCallback)
				return;

			var type = glGetError();
			if (type == GL_NO_ERROR)
				return;

			string errorText;
			errorText = ErrorToText.TryGetValue(type, out errorText) ? errorText : type.ToString("X");
			var error = $"GL Error: {errorText}\n{new StackTrace()}";

			WriteGraphicsLog(error);

			const string exceptionMessage = "OpenGL Error: See graphics.log for details.";

			if (type == GL_OUT_OF_MEMORY)
				throw new OutOfMemoryException(exceptionMessage);

			throw new InvalidOperationException(exceptionMessage);
		}

		static void DebugMessageHandler(int source, int type, uint id, int severity, int length, StringBuilder message, IntPtr userparam)
		{
			string error;

			switch (severity)
			{
				case GL_DEBUG_SEVERITY_HIGH:
					error = BuildErrorText(source, type, severity, message);
					WriteGraphicsLog(error);
					throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");

				case GL_DEBUG_SEVERITY_MEDIUM:
					error = BuildErrorText(source, type, severity, message);
					Console.WriteLine(error);
					break;
			}
		}

		static string BuildErrorText(int source, int type, int severity, StringBuilder message)
		{
			string sourceText;
			string typeText;
			string severityText;

			sourceText = DebugSourceToText.TryGetValue(source, out sourceText) ? sourceText : source.ToString("X");
			typeText = DebugTypeToText.TryGetValue(type, out typeText) ? typeText : type.ToString("X");
			severityText = DebugSeverityToText.TryGetValue(severity, out severityText) ? severityText : severity.ToString("X");
			var messageText = message.ToString();

			return $"{severityText} - GL Debug {sourceText} Output: {typeText} - {messageText}\n{new StackTrace()}";
		}
	}
}
