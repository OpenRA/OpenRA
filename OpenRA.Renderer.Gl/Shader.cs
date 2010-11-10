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

namespace OpenRA.Renderer.Glsl
{
	public class Shader : IShader
	{
		int program;
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
			Console.WriteLine(log.ToString());
			
			GraphicsDevice.CheckGlError();
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
			Gl.glUseProgram(program);
			GraphicsDevice.CheckGlError();
			var texture = (Texture)t;
			int param = Gl.glGetUniformLocation(program, name);
			GraphicsDevice.CheckGlError();
			
			if (texture != null && param >= 0)
			{
				Gl.glUniform1i(param, texture.texture);
				GraphicsDevice.CheckGlError();
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
