#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using Tao.OpenGl;

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

			var v = Gl.glCreateShaderObjectARB(Gl.GL_VERTEX_SHADER_ARB);
			ErrorHandler.CheckGlError();
			Gl.glShaderSourceARB(v, 1, new string[] { vertexCode }, null);
			ErrorHandler.CheckGlError();
			Gl.glCompileShaderARB(v);
			ErrorHandler.CheckGlError();

			int success;
			Gl.glGetObjectParameterivARB(v, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in {0}{1}.vert".F(Path.DirectorySeparatorChar, name));

			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(GlobalFileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name))))
				fragmentCode = file.ReadToEnd();

			var f = Gl.glCreateShaderObjectARB(Gl.GL_FRAGMENT_SHADER_ARB);
			ErrorHandler.CheckGlError();
			Gl.glShaderSourceARB(f, 1, new string[] { fragmentCode }, null);
			ErrorHandler.CheckGlError();
			Gl.glCompileShaderARB(f);
			ErrorHandler.CheckGlError();

			Gl.glGetObjectParameterivARB(f, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name));

			// Assemble program
			program = Gl.glCreateProgramObjectARB();
			ErrorHandler.CheckGlError();
			Gl.glAttachObjectARB(program, v);
			ErrorHandler.CheckGlError();
			Gl.glAttachObjectARB(program, f);
			ErrorHandler.CheckGlError();

			Gl.glLinkProgramARB(program);
			ErrorHandler.CheckGlError();

			Gl.glGetObjectParameterivARB(program, Gl.GL_OBJECT_LINK_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Linking error in {0} shader".F(name));

			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			Gl.glGetObjectParameterivARB(program, Gl.GL_ACTIVE_UNIFORMS, out numUniforms);
			ErrorHandler.CheckGlError();

			int nextTexUnit = 0;
			for (var i = 0; i < numUniforms; i++)
			{
				int length, size, type;
				var sb = new StringBuilder(128);

				Gl.glGetActiveUniformARB(program, i, 128, out length, out size, out type, sb);
				var sampler = sb.ToString();
				ErrorHandler.CheckGlError();

				if (type == Gl.GL_SAMPLER_2D_ARB)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = Gl.glGetUniformLocationARB(program, sampler);
					ErrorHandler.CheckGlError();
					Gl.glUniform1iARB(loc, nextTexUnit);
					ErrorHandler.CheckGlError();

					nextTexUnit++;
				}
			}
		}

		public void Render(Action a)
		{
			Gl.glUseProgramObjectARB(program);

			// bind the textures
			foreach (var kv in textures)
			{
				Gl.glActiveTextureARB(Gl.GL_TEXTURE0_ARB + kv.Key);
				Gl.glBindTexture(Gl.GL_TEXTURE_2D, ((Texture)kv.Value).ID);
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
			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();
			var param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniform1fARB(param, x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();
			var param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniform2fARB(param, x, y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			var param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			switch (length)
			{
				case 1: Gl.glUniform1fv(param, 1, vec); break;
				case 2: Gl.glUniform2fv(param, 1, vec); break;
				case 3: Gl.glUniform3fv(param, 1, vec); break;
				case 4: Gl.glUniform4fv(param, 1, vec); break;
				default: throw new InvalidDataException("Invalid vector length");
			}

			ErrorHandler.CheckGlError();
		}

		public void SetMatrix(string name, float[] mtx)
		{
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();
			var param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniformMatrix4fv(param, 1, Gl.GL_FALSE, mtx);
			ErrorHandler.CheckGlError();
		}
	}
}
