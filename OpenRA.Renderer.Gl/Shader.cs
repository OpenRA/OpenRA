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
			
			int v = Gl.glCreateShader(Gl.GL_VERTEX_SHADER);
			GraphicsDevice.CheckGlError();
			
			Gl.glShaderSource(v,1,new string[]{vertexCode},null);
			GraphicsDevice.CheckGlError();
			Gl.glCompileShader(v);
			GraphicsDevice.CheckGlError();
			
			// Fragment shader
			string fragmentCode;
			using (var file = new StreamReader(FileSystem.Open("glsl{0}{1}.frag".F(Path.DirectorySeparatorChar, type))))
				fragmentCode = file.ReadToEnd();
			int f = Gl.glCreateShader(Gl.GL_FRAGMENT_SHADER);
			GraphicsDevice.CheckGlError();
			Gl.glShaderSource(f,1,new string[]{fragmentCode},null);
			GraphicsDevice.CheckGlError();
			Gl.glCompileShader(f);
			GraphicsDevice.CheckGlError();
			
			// Assemble program
			program = Gl.glCreateProgram();
			GraphicsDevice.CheckGlError();
			Gl.glAttachShader(program,v);
			GraphicsDevice.CheckGlError();
			Gl.glAttachShader(program,f);
			GraphicsDevice.CheckGlError();
			
			Gl.glLinkProgram(program);
			StringBuilder foo = new StringBuilder();
			int[] l = new int[1];
            System.Text.StringBuilder log = new System.Text.StringBuilder(4024);
			
			Gl.glGetProgramInfoLog(program,4024,l,log);
			GraphicsDevice.CheckGlError();
			Console.WriteLine(log.ToString());

			int numAttribs;
			Gl.glGetProgramiv( program, Gl.GL_ACTIVE_UNIFORMS, out numAttribs );
			GraphicsDevice.CheckGlError();

			Gl.glUseProgram(program);
			GraphicsDevice.CheckGlError();

			int nextTexUnit = 1;
			for( int i = 0 ; i < numAttribs ; i++ )
			{
				int uLen, uSize, uType;
				var sb = new StringBuilder(4096);
				Gl.glGetActiveUniform( program, i, 4096, out uLen, out uSize, out uType, sb );
				GraphicsDevice.CheckGlError();
				if( uType == Gl.GL_SAMPLER_2D )
				{
					samplers.Add( sb.ToString(), nextTexUnit );
					Gl.glUniform1i( i, nextTexUnit );
					++nextTexUnit;
				}
			}
		}

		public void Render(Action a)
		{
			Gl.glUseProgram(program);
			GraphicsDevice.CheckGlError();
			a();
			GraphicsDevice.CheckGlError();
		}

		public void SetValue(string name, ITexture t)
		{
			if( t == null ) return;
			Gl.glUseProgram(program);
			GraphicsDevice.CheckGlError();
			var texture = (Texture)t;
			int texUnit;
			if( samplers.TryGetValue( name, out texUnit ) )
			{
				Gl.glActiveTexture( Gl.GL_TEXTURE0 + texUnit );
				GraphicsDevice.CheckGlError();
				Gl.glBindTexture( Gl.GL_TEXTURE_2D, texture.texture );
				GraphicsDevice.CheckGlError();
				Gl.glActiveTexture( Gl.GL_TEXTURE0 );
			}
		}
		
		public void SetValue(string name, float x, float y)
		{
			Gl.glUseProgram(program);
			GraphicsDevice.CheckGlError();
			int param = Gl.glGetUniformLocation(program, name);
			GraphicsDevice.CheckGlError();			
			Gl.glUniform2f(param,x,y);
			GraphicsDevice.CheckGlError();
		}

		public void Commit() { }
	}
}
