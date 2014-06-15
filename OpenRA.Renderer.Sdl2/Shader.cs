#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Renderer.Sdl2
{
	public class Shader : IShader
	{
		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();
		int program;

		public Shader(string name)
		{
			// Vertex shader
			string vertexCode;
			using (var file = new StreamReader(GlobalFileSystem.Open("glsl{0}{1}.vert".F(Path.DirectorySeparatorChar, name))))
				vertexCode = file.ReadToEnd();

			var vertexShader = GL.CreateShader(ShaderType.VertexShader);
			ErrorHandler.CheckGlError();
			GL.ShaderSource(vertexShader, vertexCode);
			ErrorHandler.CheckGlError();
			GL.CompileShader(vertexShader);
			ErrorHandler.CheckGlError();
			int success;
			GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)All.False)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.vert".F(Path.DirectorySeparatorChar, name));

			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(GlobalFileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name))))
				fragmentCode = file.ReadToEnd();

			var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
			ErrorHandler.CheckGlError();
			GL.ShaderSource(fragmentShader, fragmentCode);
			ErrorHandler.CheckGlError();
			GL.CompileShader(fragmentShader);
			ErrorHandler.CheckGlError();
			GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)All.False)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name));

			// Assemble program
			program = GL.CreateProgram();
			ErrorHandler.CheckGlError();
			GL.AttachShader(program, vertexShader);
			ErrorHandler.CheckGlError();
			GL.AttachShader(program, fragmentShader);
			ErrorHandler.CheckGlError();

			GL.LinkProgram(program);
			ErrorHandler.CheckGlError();
			GL.GetProgram(program, ProgramParameter.LinkStatus, out success);
			ErrorHandler.CheckGlError();
			if (success == (int)All.False)
				throw new InvalidProgramException("Linking error in {0} shader".F(name));

			GL.UseProgram(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			GL.GetProgram(program, ProgramParameter.ActiveUniforms, out numUniforms);
			ErrorHandler.CheckGlError();

			int nextTexUnit = 0;
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
			if (t == null)
				return;

			int texUnit;
			if (samplers.TryGetValue(name, out texUnit))
				textures[texUnit] = t;
		}

		public void SetVec(string name, float x)
		{
			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Uniform1(param, x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			GL.UseProgram(program);
			ErrorHandler.CheckGlError();
			var param = GL.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Uniform2(param, x, y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
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
