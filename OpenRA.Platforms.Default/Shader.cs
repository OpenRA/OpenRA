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
using System.IO;
using System.Text;
using OpenRA.Graphics;

namespace OpenRA.Platforms.Default
{
	sealed class Shader : ThreadAffine, IShader
	{
		readonly Dictionary<string, int> samplers = new();
		readonly Dictionary<string, int> uniformCache = new();
		readonly Dictionary<int, ITexture> textures = new();
		readonly Queue<int> unbindTextures = new();
		readonly IShaderBindings bindings;
		readonly uint program;

		static uint CompileShaderObject(int type, string code, string name)
		{
			var version = OpenGL.Profile == GLProfile.Embedded ? "300 es" : "140";

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

				Log.Write("graphics", $"GL Info Log:\n{log}");
				throw new InvalidProgramException($"Compile error in shader object {name}.");
			}

			return shader;
		}

		public Shader(IShaderBindings bindings)
		{
			var vertexShader = CompileShaderObject(OpenGL.GL_VERTEX_SHADER, bindings.VertexShaderCode, bindings.VertexShaderName);
			var fragmentShader = CompileShaderObject(OpenGL.GL_FRAGMENT_SHADER, bindings.FragmentShaderCode, bindings.FragmentShaderName);

			// Assemble program
			program = OpenGL.glCreateProgram();
			OpenGL.CheckGLError();

			this.bindings = bindings;
			for (ushort i = 0; i < bindings.Attributes.Length; i++)
			{
				OpenGL.glEnableVertexAttribArray(i);
				OpenGL.CheckGLError();
				OpenGL.glBindAttribLocation(program, i, bindings.Attributes[i].Name);
				OpenGL.CheckGLError();
			}

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
				Log.Write("graphics", $"GL Info Log:\n{log}");
				throw new InvalidProgramException($"Link error in shader program '{bindings.VertexShaderName}' and '{bindings.FragmentShaderName}'");
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
				OpenGL.CheckGLError();

				var sampler = sb.ToString();
				var loc = OpenGL.glGetUniformLocation(program, sampler);
				OpenGL.CheckGLError();
				uniformCache[sampler] = loc;

				if (type == OpenGL.GL_SAMPLER_2D)
				{
					samplers.Add(sampler, nextTexUnit);

					OpenGL.glUniform1i(loc, nextTexUnit);
					OpenGL.CheckGLError();
					nextTexUnit++;
				}
			}
		}

		public void Bind()
		{
			for (ushort i = 0; i < bindings.Attributes.Length; i++)
			{
				var attribute = bindings.Attributes[i];
				if (attribute.Type == ShaderVertexAttributeType.Float)
					OpenGL.glVertexAttribPointer(i, attribute.Components, OpenGL.GL_FLOAT, false, bindings.Stride, new IntPtr(attribute.Offset));
				else
					OpenGL.glVertexAttribIPointer(i, attribute.Components, (int)attribute.Type, bindings.Stride, new IntPtr(attribute.Offset));
				OpenGL.CheckGLError();
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
			OpenGL.glUniform1i(uniformCache[name], value ? 1 : 0);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			OpenGL.glUniform1f(uniformCache[name], x);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x, float y)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			OpenGL.glUniform2f(uniformCache[name], x, y);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float x, float y, float z)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			OpenGL.glUniform3f(uniformCache[name], x, y, z);
			OpenGL.CheckGLError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			VerifyThreadAffinity();
			var param = uniformCache[name];
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

			unsafe
			{
				fixed (float* pMtx = mtx)
					OpenGL.glUniformMatrix4fv(uniformCache[name], 1, false, new IntPtr(pMtx));
			}

			OpenGL.CheckGLError();
		}
	}
}
