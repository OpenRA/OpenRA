#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		public IPlatformWindow CreateWindow(Size size, WindowMode windowMode, float scaleModifier, int batchSize, int videoDisplay, GLProfile profile, bool enableLegacyGL)
		{
			return new Sdl2PlatformWindow(size, windowMode, scaleModifier, batchSize, videoDisplay, profile, enableLegacyGL);
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
				return new DummySoundEngine();
			}
		}

		public IFont CreateFont(byte[] data)
		{
			return new FreeTypeFont(data);
		}
	}
}
