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
using System.IO;

namespace OpenRA
{
	public interface ISoundEngine : IDisposable
	{
		SoundDevice[] AvailableDevices();
		ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate);
		ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume);
		ISound Play2DStream(Stream stream, int channels, int sampleBits, int sampleRate, bool loop, bool relative, WPos pos, float volume);
		bool Dummy { get; }
		float Volume { get; set; }
		void PauseSound(ISound sound, bool paused);
		void StopSound(ISound sound);
		void SetAllSoundsPaused(bool paused);
		void StopAllSounds();
		void SetListenerPosition(WPos position);
		void SetSoundVolume(float volume, ISound music, ISound video);
		void SetSoundLooping(bool looping, ISound sound);
	}

	public class SoundDevice
	{
		public readonly string Device;
		public readonly string Label;

		public SoundDevice(string device, string label)
		{
			Device = device;
			Label = label;
		}
	}

	public interface ISoundSource : IDisposable { }

	public interface ISound
	{
		float Volume { get; set; }
		float SeekPosition { get; }
		bool Complete { get; }
		void SetPosition(WPos pos);
	}
}
