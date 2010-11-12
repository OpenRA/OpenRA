#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;
using System.Text;
using System.Collections.Generic;

namespace OpenRA.Renderer.Glsl
{
	public class Shader : IShader
	{
		int program;
		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();

		public Shader(GraphicsDevice dev, string type)
		{			
			// Vertex shader
			string vertexCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.vert".F(Path.DirectorySeparatorChar, type))))
				vertexCode = file.ReadToEnd();
			
			int v = Gl.glCreateShaderObjectARB(Gl.GL_VERTEX_SHADER_ARB);
			GraphicsDevice.CheckGlError();
			Gl.glShaderSourceARB(v,1,new string[]{vertexCode},null);
			GraphicsDevice.CheckGlError();
			Gl.glCompileShaderARB(v);
			GraphicsDevice.CheckGlError();
			
			int success;
			Gl.glGetObjectParameterivARB(v, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			GraphicsDevice.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in {0}{1}.vert".F(Path.DirectorySeparatorChar, type));
			
			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, type))))
				fragmentCode = file.ReadToEnd();
			int f = Gl.glCreateShaderObjectARB(Gl.GL_FRAGMENT_SHADER_ARB);
			GraphicsDevice.CheckGlError();
			Gl.glShaderSourceARB(f,1,new string[]{fragmentCode},null);
			GraphicsDevice.CheckGlError();
			Gl.glCompileShaderARB(f);
			GraphicsDevice.CheckGlError();
			
			Gl.glGetObjectParameterivARB(f, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out success);
			GraphicsDevice.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Compile error in glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, type));
			
			
			// Assemble program
			program = Gl.glCreateProgramObjectARB();
			GraphicsDevice.CheckGlError();
			Gl.glAttachObjectARB(program,v);
			GraphicsDevice.CheckGlError();
			Gl.glAttachObjectARB(program,f);
			GraphicsDevice.CheckGlError();
			
			Gl.glLinkProgramARB(program);
			GraphicsDevice.CheckGlError();
			
			Gl.glGetObjectParameterivARB(program, Gl.GL_OBJECT_LINK_STATUS_ARB, out success);
			GraphicsDevice.CheckGlError();
			if (success == 0)
				throw new InvalidProgramException("Linking error in {0} shader".F(type));
			
			
			Gl.glUseProgramObjectARB(program);
			GraphicsDevice.CheckGlError();
			
			int numUniforms;
			Gl.glGetObjectParameterivARB( program, Gl.GL_ACTIVE_UNIFORMS, out numUniforms );
			GraphicsDevice.CheckGlError();

			int nextTexUnit = 1;
			for( int i = 0 ; i < numUniforms ; i++ )
			{
				int uLen, uSize, uType;
				var sb = new StringBuilder(128);
				Gl.glGetActiveUniformARB( program, i, 128, out uLen, out uSize, out uType, sb );
				GraphicsDevice.CheckGlError();
				if( uType == Gl.GL_SAMPLER_2D_ARB )
				{
					samplers.Add( sb.ToString(), nextTexUnit );
					Gl.glUniform1iARB( i, nextTexUnit );
					++nextTexUnit;
				}
			}
		}

		public void Render(Action a)
		{
			Gl.glUseProgramObjectARB(program);
			GraphicsDevice.CheckGlError();
			// Todo: Only enable alpha blending if we need it
			Gl.glEnable(Gl.GL_BLEND);
			GraphicsDevice.CheckGlError();
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			GraphicsDevice.CheckGlError();
			a();
			GraphicsDevice.CheckGlError();
			Gl.glDisable(Gl.GL_BLEND);
			GraphicsDevice.CheckGlError();
		}

		public void SetValue(string name, ITexture t)
		{
			if( t == null ) return;
			Gl.glUseProgramObjectARB(program);
			GraphicsDevice.CheckGlError();
			var texture = (Texture)t;
			int texUnit;
			if( samplers.TryGetValue( name, out texUnit ) )
			{
				Gl.glActiveTextureARB( Gl.GL_TEXTURE0_ARB + texUnit );
				GraphicsDevice.CheckGlError();
				Gl.glBindTexture( Gl.GL_TEXTURE_2D, texture.texture );
				GraphicsDevice.CheckGlError();
				Gl.glActiveTextureARB( Gl.GL_TEXTURE0_ARB );
			}
		}
		
		public void SetValue(string name, float x, float y)
		{
			Gl.glUseProgramObjectARB(program);
			GraphicsDevice.CheckGlError();
			int param = Gl.glGetUniformLocationARB(program, name);
			GraphicsDevice.CheckGlError();			
			Gl.glUniform2fARB(param,x,y);
			GraphicsDevice.CheckGlError();
		}

		public void Commit() { }
	}
}
