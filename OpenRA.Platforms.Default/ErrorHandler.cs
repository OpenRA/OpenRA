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
using System.Diagnostics;

namespace OpenRA.Platforms.Default
{
	static class ErrorHandler
	{
		public static void CheckGlVersion()
		{
			var versionString = OpenGL.glGetString(OpenGL.GL_VERSION);
			var version = versionString.Contains(" ") ? versionString.Split(' ')[0].Split('.') : versionString.Split('.');

			var major = 0;
			if (version.Length > 0)
				int.TryParse(version[0], out major);

			var minor = 0;
			if (version.Length > 1)
				int.TryParse(version[1], out minor);

			Console.WriteLine("Detected OpenGL version: {0}.{1}".F(major, minor));
			if (major < 2)
			{
				WriteGraphicsLog("OpenRA requires OpenGL version 2.0 or greater and detected {0}.{1}".F(major, minor));
				throw new InvalidProgramException("OpenGL Version Error: See graphics.log for details.");
			}
		}

		public static void CheckGlError()
		{
			var n = OpenGL.glGetError();
			if (n != OpenGL.GL_NO_ERROR)
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
			var vendor = OpenGL.glGetString(OpenGL.GL_VENDOR);
			Log.Write("graphics", "Vendor: {0}", vendor);
			if (vendor.Contains("Microsoft"))
			{
				var msg = "";
				msg += "Note:  The default driver provided by Microsoft does not include full OpenGL support.\n";
				msg += "Please install the latest drivers from your graphics card manufacturer's website.\n";
				Log.Write("graphics", msg);
			}

			Log.Write("graphics", "Renderer: {0}", OpenGL.glGetString(OpenGL.GL_RENDERER));
			Log.Write("graphics", "GL Version: {0}", OpenGL.glGetString(OpenGL.GL_VERSION));
			Log.Write("graphics", "Shader Version: {0}", OpenGL.glGetString(OpenGL.GL_SHADING_LANGUAGE_VERSION));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", OpenGL.glGetString(OpenGL.GL_EXTENSIONS));
		}
	}
}