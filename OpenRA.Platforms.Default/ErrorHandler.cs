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