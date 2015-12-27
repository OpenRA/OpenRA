#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();
		readonly uint program;

		protected uint CompileShaderObject(int type, string name)
		{
			var ext = type == OpenGL.GL_VERTEX_SHADER ? "vert" : "frag";
			var filename = Path.Combine(Platform.GameDir, "glsl", name + "." + ext);
			var code = File.ReadAllText(filename);

			var shader = OpenGL.glCreateShader(type);
			ErrorHandler.CheckGlError();
			unsafe
			{
				var length = code.Length;
				OpenGL.glShaderSource(shader, 1, new string[] { code }, new IntPtr(&length));
			}

			ErrorHandler.CheckGlError();
			OpenGL.glCompileShader(shader);
			ErrorHandler.CheckGlError();
			int success;
			OpenGL.glGetShaderiv(shader, OpenGL.GL_COMPILE_STATUS, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)OpenGL.GL_FALSE)
			{
				int len;
				OpenGL.glGetShaderiv(shader, OpenGL.GL_INFO_LOG_LENGTH, out len);
				var log = new StringBuilder(len);
				int length;
				OpenGL.glGetShaderInfoLog(shader, len, out length, log);

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
			ErrorHandler.CheckGlError();

			OpenGL.glBindAttribLocation(program, VertexPosAttributeIndex, "aVertexPosition");
			ErrorHandler.CheckGlError();
			OpenGL.glBindAttribLocation(program, TexCoordAttributeIndex, "aVertexTexCoord");
			ErrorHandler.CheckGlError();

			OpenGL.glAttachShader(program, vertexShader);
			ErrorHandler.CheckGlError();
			OpenGL.glAttachShader(program, fragmentShader);
			ErrorHandler.CheckGlError();

			OpenGL.glLinkProgram(program);
			ErrorHandler.CheckGlError();
			int success;
			OpenGL.glGetProgramiv(program, OpenGL.GL_LINK_STATUS, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)OpenGL.GL_FALSE)
			{
				int len;
				OpenGL.glGetProgramiv(program, OpenGL.GL_INFO_LOG_LENGTH, out len);

				var log = new StringBuilder(len);
				int length;
				OpenGL.glGetProgramInfoLog(program, len, out length, log);
				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Link error in shader program '{0}'".F(name));
			}

			OpenGL.glUseProgram(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			OpenGL.glGetProgramiv(program, OpenGL.GL_ACTIVE_UNIFORMS, out numUniforms);

			ErrorHandler.CheckGlError();

			var nextTexUnit = 0;
			for (var i = 0; i < numUniforms; i++)
			{
				int length, size;
				int type;
				var sb = new StringBuilder(128);
				OpenGL.glGetActiveUniform(program, i, 128, out length, out size, out type, sb);
				var sampler = sb.ToString();
				ErrorHandler.CheckGlError();

				if (type == OpenGL.GL_SAMPLER_2D)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = OpenGL.glGetUniformLocation(program, sampler);
					ErrorHandler.CheckGlError();
					OpenGL.glUniform1i(loc, nextTexUnit);
					ErrorHandler.CheckGlError();

					nextTexUnit++;
				}
			}
		}

		public void Render(Action a)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);

			// bind the textures
			foreach (var kv in textures)
			{
				OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + kv.Key);
				OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, ((Texture)kv.Value).ID);
			}

			ErrorHandler.CheckGlError();
			a();
			ErrorHandler.CheckGlError();
		}

		public void SetTexture(string name, ITexture t)
		{
			VerifyThreadAffinity();
			if (t == null)
				return;

			int texUnit;
			if (samplers.TryGetValue(name, out texUnit))
				textures[texUnit] = t;
		}

		public void SetBool(string name, bool value)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			ErrorHandler.CheckGlError();
			var param = OpenGL.glGetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			OpenGL.glUniform1i(param, value ? 1 : 0);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			ErrorHandler.CheckGlError();
			var param = OpenGL.glGetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			OpenGL.glUniform1f(param, x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			ErrorHandler.CheckGlError();
			var param = OpenGL.glGetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			OpenGL.glUniform2f(param, x, y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			VerifyThreadAffinity();
			var param = OpenGL.glGetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
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

			ErrorHandler.CheckGlError();
		}

		public void SetMatrix(string name, float[] mtx)
		{
			VerifyThreadAffinity();
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			OpenGL.glUseProgram(program);
			ErrorHandler.CheckGlError();
			var param = OpenGL.glGetUniformLocation(program, name);
			ErrorHandler.CheckGlError();

			unsafe
			{
				fixed (float* pMtx = mtx)
					OpenGL.glUniformMatrix4fv(param, 1, false, new IntPtr(pMtx));
			}

			ErrorHandler.CheckGlError();
		}
	}
}
