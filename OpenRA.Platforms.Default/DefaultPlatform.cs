#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Platforms.Default
{
	public class DefaultPlatform : IPlatform
	{
		public IGraphicsDevice CreateGraphics(Size size, WindowMode windowMode, int batchSize)
		{
			// Run graphics rendering on a dedicated thread.
			// The calling thread will then have more time to process other tasks, since rendering happens in parallel.
			// If the calling thread is the main game thread, this means it can run more logic and render ticks.
			return new ThreadedGraphicsDevice(new Sdl2GraphicsDevice(size, windowMode), batchSize);
		}

		public ISoundEngine CreateSound(string device)
		{
			return new OpenAlSoundEngine(device);
		}
	}
}
