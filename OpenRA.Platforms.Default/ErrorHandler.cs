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
				OpenGL.WriteGraphicsLog("OpenRA requires OpenGL version 2.0 or greater and detected {0}.{1}".F(major, minor));
				throw new InvalidProgramException("OpenGL Version Error: See graphics.log for details.");
			}
		}

		public static void CheckGlError()
		{
			var n = OpenGL.glGetError();
			if (n != OpenGL.GL_NO_ERROR)
			{
				var error = "GL Error: {0}\n{1}".F(n, new StackTrace());
				OpenGL.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}
		}
	}
}