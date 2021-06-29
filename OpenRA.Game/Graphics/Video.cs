#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Video
{
	public interface IVideo
	{
		ushort Frames { get; }
		byte Framerate { get; }
		ushort Width { get; }
		ushort Height { get; }
		uint[,] FrameData { get; }

		int CurrentFrame { get; }
		void AdvanceFrame();

		bool HasAudio { get; }
		byte[] AudioData { get; }
		int AudioChannels { get; }
		int SampleBits { get; }
		int SampleRate { get; }

		void Reset();
	}
}
