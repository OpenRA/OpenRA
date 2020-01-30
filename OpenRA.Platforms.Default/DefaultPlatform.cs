#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	public class DefaultPlatform : IPlatform
	{
		public IPlatformWindow CreateWindow(Size size, WindowMode windowMode, float scaleModifier, int batchSize)
		{
			return new Sdl2PlatformWindow(size, windowMode, scaleModifier, batchSize);
		}

		public ISoundEngine CreateSound(string device)
		{
			try
			{
				return new OpenAlSoundEngine(device);
			}
			catch (InvalidOperationException e)
			{
				Log.Write("sound", "Failed to initialize OpenAL device. Error was {0}", e);
				return new DummySoundEngine(device);
			}
		}

		public IFont CreateFont(byte[] data)
		{
			return new FreeTypeFont(data);
		}
	}
}
