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
using Tao.OpenGl;

namespace OpenRA.Renderer.Glsl
{
	public class Shader : IShader
	{
		int program;
		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();

		public Shader(GraphicsDevice dev, string type)
		{
			// Vertex shader
			string vertexCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.vert".F(Path.DirectorySeparatorChar, type))))
				vertexCode = file.ReadToEnd();

			int v = Gl.glCreateShaderObjectARB(Gl.GL_VERTEX_SHADER_ARB);
			ErrorHandler.CheckGlError();
			Gl.glShaderSourceARB(v,1,new string[]{vertexCode},null);
			ErrorHandler.CheckGlError();
			Gl.glCompileShaderARB(v);
			ErrorHandler.CheckGlError();

			int success;
			Gl.glGetObjectParameterivARB(v, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in {0}{1}.vert".F(Path.DirectorySeparatorChar, type));

			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, type))))
				fragmentCode = file.ReadToEnd();
			int f = Gl.glCreateShaderObjectARB(Gl.GL_FRAGMENT_SHADER_ARB);
			ErrorHandler.CheckGlError();
			Gl.glShaderSourceARB(f,1,new string[]{fragmentCode},null);
			ErrorHandler.CheckGlError();
			Gl.glCompileShaderARB(f);
			ErrorHandler.CheckGlError();

			Gl.glGetObjectParameterivARB(f, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, type));


			// Assemble program
			program = Gl.glCreateProgramObjectARB();
			ErrorHandler.CheckGlError();
			Gl.glAttachObjectARB(program,v);
			ErrorHandler.CheckGlError();
			Gl.glAttachObjectARB(program,f);
			ErrorHandler.CheckGlError();

			Gl.glLinkProgramARB(program);
			ErrorHandler.CheckGlError();

			Gl.glGetObjectParameterivARB(program, Gl.GL_OBJECT_LINK_STATUS_ARB, out success);
			ErrorHandler.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Linking error in {0} shader".F(type));


			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();

			int numUniforms;
			Gl.glGetObjectParameterivARB( program, Gl.GL_ACTIVE_UNIFORMS, out numUniforms );
			ErrorHandler.CheckGlError();

			int nextTexUnit = 0;
			for( int i = 0 ; i < numUniforms ; i++ )
			{
				int uLen, uSize, uType, loc;
				var sb = new StringBuilder(128);
				Gl.glGetActiveUniformARB( program, i, 128, out uLen, out uSize, out uType, sb );
				var sampler = sb.ToString();
				ErrorHandler.CheckGlError();
				if( uType == Gl.GL_SAMPLER_2D_ARB )
				{
					samplers.Add( sampler, nextTexUnit );
					loc = Gl.glGetUniformLocationARB(program, sampler);
					ErrorHandler.CheckGlError();
					Gl.glUniform1iARB( loc, nextTexUnit );
					ErrorHandler.CheckGlError();
					++nextTexUnit;
				}
			}

		}

		public void Render(Action a)
		{
			Gl.glUseProgramObjectARB(program);

			/* bind the textures */

			foreach (var kv in textures)
			{
				Gl.glActiveTextureARB(Gl.GL_TEXTURE0_ARB + kv.Key);
				Gl.glBindTexture(Gl.GL_TEXTURE_2D, ((Texture)kv.Value).ID);
			}

			/* configure blend state */
			ErrorHandler.CheckGlError();
			// TODO: Only enable alpha blending if we need it
			Gl.glEnable(Gl.GL_BLEND);
			ErrorHandler.CheckGlError();
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			ErrorHandler.CheckGlError();
			a();
			ErrorHandler.CheckGlError();
			Gl.glDisable(Gl.GL_BLEND);
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
			int param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniform1fARB(param,x);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float x, float y)
		{
			Gl.glUseProgramObjectARB(program);
			ErrorHandler.CheckGlError();
			int param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniform2fARB(param,x,y);
			ErrorHandler.CheckGlError();
		}

		public void SetVec(string name, float[] vec, int length)
		{
			int param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			switch(length)
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
			int param = Gl.glGetUniformLocationARB(program, name);
			ErrorHandler.CheckGlError();
			Gl.glUniformMatrix4fv(param, 1, Gl.GL_FALSE, mtx);
			ErrorHandler.CheckGlError();
		}
	}
}
