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

namespace OpenRA.Video
{
	public interface IVideo
	{
		ushort FrameCount { get; }
		byte Framerate { get; }
		ushort Width { get; }
		ushort Height { get; }

		/// <summary>
		/// Current frame color data in 32-bit BGRA.
		/// </summary>
		byte[] CurrentFrameData { get; }
		int CurrentFrameIndex { get; }
		void AdvanceFrame();

		bool HasAudio { get; }
		byte[] AudioData { get; }
		int AudioChannels { get; }
		int SampleBits { get; }
		int SampleRate { get; }

		void Reset();
	}
}
