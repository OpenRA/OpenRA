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
using System.Diagnostics;
using Tao.OpenGl;

namespace OpenRA.Renderer.SdlCommon
{
	public static class ErrorHandler
	{
		public enum GlError
		{
			GL_NO_ERROR = Gl.GL_NO_ERROR,
			GL_INVALID_ENUM = Gl.GL_INVALID_ENUM,
			GL_INVALID_VALUE = Gl.GL_INVALID_VALUE,
			GL_STACK_OVERFLOW = Gl.GL_STACK_OVERFLOW,
			GL_STACK_UNDERFLOW = Gl.GL_STACK_UNDERFLOW,
			GL_OUT_OF_MEMORY = Gl.GL_OUT_OF_MEMORY,
			GL_TABLE_TOO_LARGE = Gl.GL_TABLE_TOO_LARGE,
			GL_INVALID_OPERATION = Gl.GL_INVALID_OPERATION,
		}

		public static void CheckGlError()
		{
			var n = Gl.glGetError();
			if (n != Gl.GL_NO_ERROR)
			{
				var error = "GL Error: {0}\n{1}".F((GlError)n, new StackTrace());
				WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}
		}

		public static void WriteGraphicsLog(string message)
		{
			Log.AddChannel("graphics", "graphics.log");
			Log.Write("graphics", message);
			Log.Write("graphics", "");
			Log.Write("graphics", "OpenGL Information:");
			Log.Write("graphics",  "Vendor: {0}", Gl.glGetString(Gl.GL_VENDOR));
			Log.Write("graphics",  "Renderer: {0}", Gl.glGetString(Gl.GL_RENDERER));
			Log.Write("graphics",  "GL Version: {0}", Gl.glGetString(Gl.GL_VERSION));
			Log.Write("graphics",  "Shader Version: {0}", Gl.glGetString(Gl.GL_SHADING_LANGUAGE_VERSION));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", Gl.glGetString(Gl.GL_EXTENSIONS));
		}
	}
}