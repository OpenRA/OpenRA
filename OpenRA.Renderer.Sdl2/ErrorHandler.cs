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
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Renderer.Sdl2
{
	public static class ErrorHandler
	{
		static Version OpenGLversion;

		public static void CheckGlVersion()
		{
			var version = GL.GetString(StringName.Version).Split(' ')[0].Split('.');
			int major;
			Exts.TryParseIntegerInvariant(version[0], out major);
			int minor;
            Exts.TryParseIntegerInvariant(version[1], out minor);
			Console.WriteLine("Detected OpenGL version: {0}.{1}".F(major, minor));
			OpenGLversion = new Version(major, minor);
			if (major < 2)
			{
				WriteGraphicsLog("OpenRA requires OpenGL version 2.0 or greater.");
				throw new InvalidProgramException("OpenGL Version Error: See graphics.log for details.");
			}
		}

		public static void CheckGlError()
		{
			var n = GL.GetError();
			if (n != ErrorCode.NoError)
			{
				var error = "GL Error: {0}\n{1}".F(n, new StackTrace());
				WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}
		}

		public static void WriteGraphicsLog(string message)
		{
			Log.Write("graphics", message);
			Log.Write("graphics", "");
			Log.Write("graphics", "OpenGL Information:");
			Log.Write("graphics",  "Vendor: {0}", GL.GetString(StringName.Vendor));
			if (GL.GetString(StringName.Vendor).Contains("Microsoft"))
			{
				Log.Write("graphics", "Note:  The default driver provided by Microsoft does not include full OpenGL support.\n"
					+ "Please install the latest drivers from your graphics card manufacturer's website.\n");
			}
			Log.Write("graphics",  "Renderer: {0}", GL.GetString(StringName.Renderer));
			Log.Write("graphics",  "GL Version: {0}", GL.GetString(StringName.Version));
			if (OpenGLversion.Major < 2)
			{
				Log.Write("graphics", "Note: OpenRA requires OpenGL version 2.0+.\n"
					+"Please update your graphics card drivers to the latest version.\n");
			}
			Log.Write("graphics",  "Shader Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", GL.GetString(StringName.Extensions));
		}
	}
}