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
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Renderer.SdlCommon
{
	public static class ErrorHandler
	{
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
			Log.Write("graphics",  "Renderer: {0}", GL.GetString(StringName.Renderer));
			Log.Write("graphics",  "GL Version: {0}", GL.GetString(StringName.Version));
			Log.Write("graphics",  "Shader Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", GL.GetString(StringName.Extensions));
		}
	}
}
