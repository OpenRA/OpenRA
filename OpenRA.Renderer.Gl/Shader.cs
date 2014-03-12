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
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Renderer.SdlCommon;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Renderer.Glsl
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
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.vert".F(Path.DirectorySeparatorChar, name))))
				vertexCode = file.ReadToEnd();

			var v = GL.CreateShader(ShaderType.VertexShader);
			ErrorHandler.CheckGlError();
			GL.Arb.ShaderSource(v, 1, new string[] { vertexCode }, new int[0]);
			ErrorHandler.CheckGlError();
			GL.Arb.CompileShader(v);
			ErrorHandler.CheckGlError();

			int success;
			GL.Arb.GetObjectParameter(v, ArbShaderObjects.ObjectCompileStatusArb, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in {0}{1}.vert".F(Path.DirectorySeparatorChar, name));

			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name))))
				fragmentCode = file.ReadToEnd();

			var f = GL.CreateShader(ShaderType.FragmentShader);
			ErrorHandler.CheckGlError();
			GL.Arb.ShaderSource(f, 1, new string[] { fragmentCode }, new int[0]);
			ErrorHandler.CheckGlError();
			GL.Arb.CompileShader(f);
			ErrorHandler.CheckGlError();

			GL.Arb.GetObjectParameter(f, ArbShaderObjects.ObjectCompileStatusArb, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, name));

			// Assemble program
			program = GL.Arb.CreateProgramObject();
			ErrorHandler.CheckGlError();
			GL.Arb.AttachObject(program, v);
			ErrorHandler.CheckGlError();
			GL.Arb.AttachObject(program, f);
			ErrorHandler.CheckGlError();

			GL.Arb.LinkProgram(program);
			ErrorHandler.CheckGlError();

			GL.Arb.GetObjectParameter(program, ArbShaderObjects.ObjectLinkStatusArb, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Linking error in {0} shader".F(name));

			GL.Arb.UseProgramObject(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			GL.Arb.GetObjectParameter(program, ArbShaderObjects.ObjectActiveUniformsArb, out numUniforms);
			ErrorHandler.CheckGlError();

			int nextTexUnit = 0;
			for (var i = 0; i < numUniforms; i++)
			{
				int length, size;
				ArbShaderObjects type;
				var sb = new StringBuilder(128);

				GL.Arb.GetActiveUniform(program, i, 128, out length, out size, out type, sb);
				var sampler = sb.ToString();
				ErrorHandler.CheckGlError();

				if (type == ArbShaderObjects.Sampler2DArb)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = GL.Arb.GetUniformLocation(program, sampler);
					ErrorHandler.CheckGlError();
					GL.Arb.Uniform1(loc, nextTexUnit);
					ErrorHandler.CheckGlError();

					nextTexUnit++;
				}
			}
		}

		public void Render(Action a)
		{
			GL.Arb.UseProgramObject(program);

			// bind the textures
			foreach (var kv in textures)
			{
				GL.Arb.ActiveTexture(TextureUnit.Texture0 + kv.Key);
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
			GL.Arb.UseProgramObject(program);
			ErrorHandler.CheckGlError();
			var param = GL.Arb.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Arb.Uniform1(param, x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			GL.Arb.UseProgramObject(program);
			ErrorHandler.CheckGlError();
			var param = GL.Arb.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Arb.Uniform2(param, x, y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			var param = GL.Arb.GetUniformLocation(program, name);
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

			GL.Arb.UseProgramObject(program);
			ErrorHandler.CheckGlError();
			var param = GL.Arb.GetUniformLocation(program, name);
			ErrorHandler.CheckGlError();
			GL.Arb.UniformMatrix4(param, 1, false, mtx);
			ErrorHandler.CheckGlError();
		}
	}
}
