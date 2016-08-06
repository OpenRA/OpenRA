#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA;

namespace OpenRA.Platforms.Default
{
	public class DefaultPlatform : IPlatform
	{
		public IGraphicsDevice CreateGraphics(Size size, WindowMode windowMode)
		{
			return new Sdl2GraphicsDevice(size, windowMode);
		}

		public ISoundEngine CreateSound(string device)
		{
			return new OpenAlSoundEngine(device);
		}
	}
}
