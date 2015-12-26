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
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Platforms.Default
{
	class Shader : ThreadAffine, IShader
	{
		public const int VertexPosAttributeIndex = 0;
		public const int TexCoordAttributeIndex = 1;

		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();
		readonly int program;

		protected int CompileShaderObject(ShaderType type, string name)
		{
			var ext = type == ShaderType.VertexShader ? "vert" : "frag";
			var filename = Path.Combine(Platform.GameDir, "glsl", name + "." + ext);
			var code = File.ReadAllText(filename);

			var shader = GL.CreateShader(type);
			ErrorHandler.CheckGlError();
			GL.ShaderSource(shader, code);
			ErrorHandler.CheckGlError();
			GL.CompileShader(shader);
			ErrorHandler.CheckGlError();
			int success;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)All.False)
			{
				int len;
				GL.GetShader(shader, ShaderParameter.InfoLogLength, out len);
				var log = new StringBuilder(len);
				unsafe
				{
					GL.GetShaderInfoLog(shader, len, null, log);
				}

				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Compile error in shader object '{0}'".F(filename));
			}

			return shader;
		}

		public Shader(string name)
		{
			var vertexShader = CompileShaderObject(ShaderType.VertexShader, name);
			var fragmentShader = CompileShaderObject(ShaderType.FragmentShader, name);

			// Assemble program
			program = GL.CreateProgram();
			ErrorHandler.CheckGlError();

			GL.BindAttribLocation(program, VertexPosAttributeIndex, "aVertexPosition");
			ErrorHandler.CheckGlError();
			GL.BindAttribLocation(program, TexCoordAttributeIndex, "aVertexTexCoord");
			ErrorHandler.CheckGlError();

			GL.AttachShader(program, vertexShader);
			ErrorHandler.CheckGlError();
			GL.AttachShader(program, fragmentShader);
			ErrorHandler.CheckGlError();

			GL.LinkProgram(program);
			ErrorHandler.CheckGlError();
			int success;
			GL.GetProgram(program, ProgramParameter.LinkStatus, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)All.False)
			{
				int len;
				GL.GetProgram(program, ProgramParameter.InfoLogLength, out len);
				var log = new StringBuilder(len);
				unsafe
				{
					GL.GetProgramInfoLog(program, len, null, log);
				}

				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Link error in shader program '{0}'".F(name));
			}

			GL.UseProgram(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			GL.GetProgram(program, ProgramParameter.ActiveUniforms, out numUniforms);
			ErrorHandler.CheckGlError();

			var nextTexUnit = 0;
			for (var i = 0; i < numUniforms; i++)
			{
				int length, size;
				ActiveUniformType type;
				var sb = new StringBuilder(128);
				GL.GetActiveUniform(program, i, 128, out length, out size, out type, sb);
				var sampler = sb.ToString();
				ErrorHandler.CheckGlError();

				if (type == ActiveUniformType.Sampler2D)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = GL.GetUniformLocation(program, sampler);
					ErrorHandler.CheckGlError();
					GL.Uniform1(loc, nextTexUnit);
					ErrorHandler.CheckGlError();

					nextTexUnit++;
				}
			}
		}

		public void Render(Action a)
		{
			VerifyThreadAffinity();
			GL.UseProgram(program);

			// bind the textures
			foreach (var kv in textures)
			{
				GL.ActiveTexture(TextureUnit.Texture0 + kv.Key);
				GL.BindTexture(TextureTarget.Texture2D, ((Texture)kv.Value).ID);
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
			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Uniform1(param, value ? 1 : 0);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x)
		{
			VerifyThreadAffinity();
			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Uniform1(param, x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			VerifyThreadAffinity();
			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Uniform2(param, x, y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			VerifyThreadAffinity();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			switch (length)
			{
				case 1: GL.Uniform1(param, 1, vec); break;
				case 2: GL.Uniform2(param, 1, vec); break;
				case 3: GL.Uniform3(param, 1, vec); break;
				case 4: GL.Uniform4(param, 1, vec); break;
				default: throw new InvalidDataException("Invalid vector length");
			}

			ErrorHandler.CheckGlError();
		}

		public void SetMatrix(string name, float[] mtx)
		{
			VerifyThreadAffinity();
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.UniformMatrix4(param, 1, false, mtx);
			ErrorHandler.CheckGlError();
		}
	}
}
