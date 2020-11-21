#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenRA.Platforms.Default
{
	class Shader : ThreadAffine, IShader
	{
		public const int VertexPosAttributeIndex = 0;
		public const int TexCoordAttributeIndex = 1;
		public const int TexMetadataAttributeIndex = 2;
		public const int TintAttributeIndex = 3;

		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, int> legacySizeUniforms = new Dictionary<int, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();
		readonly Queue<int> unbindTextures = new Queue<int>();
		readonly uint program;

		protected uint CompileShaderObject(int type, string name)
		{
			var ext = type == OpenGL.GL_VERTEX_SHADER ? "vert" : "frag";
			var filename = Path.Combine(Platform.EngineDir, "glsl", name + "." + ext);
			var code = File.ReadAllText(filename);

			var version = OpenGL.Profile == GLProfile.Embedded ? "300 es" :
				OpenGL.Profile == GLProfile.Legacy ? "120" : "140";

			code = code.Replace("{VERSION}", version);

			var shader = OpenGL.glCreateShader(type);
			OpenGL.CheckGLError();
			unsafe
			{
				var length = code.Length;
				OpenGL.glShaderSource(shader, 1, new string[] { code }, new IntPtr(&length));
			}

			OpenGL.CheckGLError();
			OpenGL.glCompileShader(shader);
			OpenGL.CheckGLError();
			OpenGL.glGetShaderiv(shader, OpenGL.GL_COMPILE_STATUS, out var success);
			OpenGL.CheckGLError();
			if (success == OpenGL.GL_FALSE)
			{
				OpenGL.glGetShaderiv(shader, OpenGL.GL_INFO_LOG_LENGTH, out var len);
				var log = new StringBuilder(len);
				OpenGL.glGetShaderInfoLog(shader, len, out _, log);

				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Compile error in shader object '{0}'".F(filename));
			}

			return shader;
		}

		public Shader(string name)
		{
			var vertexShader = CompileShaderObject(OpenGL.GL_VERTEX_SHADER, name);
			var fragmentShader = CompileShaderObject(OpenGL.GL_FRAGMENT_SHADER, name);

			// Assemble program
			program = OpenGL.glCreateProgram();
			OpenGL.CheckGLError();

			OpenGL.glBindAttribLocation(program, VertexPosAttributeIndex, "aVertexPosition");
			OpenGL.CheckGLError();
			OpenGL.glBindAttribLocation(program, TexCoordAttributeIndex, "aVertexTexCoord");
			OpenGL.CheckGLError();
			OpenGL.glBindAttribLocation(program, TexMetadataAttributeIndex, "aVertexTexMetadata");
			OpenGL.CheckGLError();
			OpenGL.glBindAttribLocation(program, TintAttributeIndex, "aVertexTint");
			OpenGL.CheckGLError();

			if (OpenGL.Profile == GLProfile.Modern)
			{
				OpenGL.glBindFragDataLocation(program, 0, "fragColor");
				OpenGL.CheckGLError();
			}

			OpenGL.glAttachShader(program, vertexShader);
			OpenGL.CheckGLError();
			OpenGL.glAttachShader(program, fragmentShader);
			OpenGL.CheckGLError();

			OpenGL.glLinkProgram(program);
			OpenGL.CheckGLError();
			OpenGL.glGetProgramiv(program, OpenGL.GL_LINK_STATUS, out var success);
			OpenGL.CheckGLError();
			if (success == OpenGL.GL_FALSE)
			{
				OpenGL.glGetProgramiv(program, OpenGL.GL_INFO_LOG_LENGTH, out var len);

				var log = new StringBuilder(len);
				OpenGL.glGetProgramInfoLog(program, len, out _, log);
				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Link error in shader program '{0}'".F(name));
			}

			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();

			OpenGL.glGetProgramiv(program, OpenGL.GL_ACTIVE_UNIFORMS, out var numUniforms);

			OpenGL.CheckGLError();

			var nextTexUnit = 0;
			for (var i = 0; i < numUniforms; i++)
			{
				var sb = new StringBuilder(128);
				OpenGL.glGetActiveUniform(program, i, 128, out _, out _, out var type, sb);
				var sampler = sb.ToString();
				OpenGL.CheckGLError();

				if (type == OpenGL.GL_SAMPLER_2D)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = OpenGL.glGetUniformLocation(program, sampler);
					OpenGL.CheckGLError();
					OpenGL.glUniform1i(loc, nextTexUnit);
					OpenGL.CheckGLError();

					if (OpenGL.Profile == GLProfile.Legacy)
					{
						var sizeLoc = OpenGL.glGetUniformLocation(program, sampler + "Size");
						if (sizeLoc >= 0)
							legacySizeUniforms.Add(nextTexUnit, sizeLoc);
					}

					nextTexUnit++;
				}
			}
		}

		public void PrepareRender()
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();

			// bind the textures
			foreach (var kv in textures)
			{
				var texture = (ITextureInternal)kv.Value;

				// Evict disposed textures from the cache
				if (OpenGL.glIsTexture(texture.ID))
				{
					OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + kv.Key);
					OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture.ID);

					// Work around missing textureSize GLSL function by explicitly tracking sizes in a uniform
					if (OpenGL.Profile == GLProfile.Legacy && legacySizeUniforms.TryGetValue(kv.Key, out var param))
					{
						OpenGL.glUniform2f(param, texture.Size.Width, texture.Size.Height);
						OpenGL.CheckGLError();
					}
				}
				else
					unbindTextures.Enqueue(kv.Key);
			}

			while (unbindTextures.Count > 0)
				textures.Remove(unbindTextures.Dequeue());

			OpenGL.CheckGLError();
		}

		public void SetTexture(string name, ITexture t)
		{
			VerifyThreadAffinity();
			if (t == null)
				return;

			if (samplers.TryGetValue(name, out var texUnit))
				textures[texUnit] = t;
		}

		public void SetBool(string name, bool value)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform1i(param, value ? 1 : 0);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform1f(param, x);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x, float y)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform2f(param, x, y);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x, float y, float z)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform3f(param, x, y, z);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			VerifyThreadAffinity();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			unsafe
			{
				fixed (float* pVec = vec)
				{
					var ptr = new IntPtr(pVec);
					switch (length)
					{
						case 1: OpenGL.glUniform1fv(param, 1, ptr); break;
						case 2: OpenGL.glUniform2fv(param, 1, ptr); break;
						case 3: OpenGL.glUniform3fv(param, 1, ptr); break;
						case 4: OpenGL.glUniform4fv(param, 1, ptr); break;
						default: throw new InvalidDataException("Invalid vector length");
					}
				}
			}

			OpenGL.CheckGLError();
		}

		public void SetMatrix(string name, float[] mtx)
		{
			VerifyThreadAffinity();
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();

			unsafe
			{
				fixed (float* pMtx = mtx)
					OpenGL.glUniformMatrix4fv(param, 1, false, new IntPtr(pMtx));
			}

			OpenGL.CheckGLError();
		}
	}
}
