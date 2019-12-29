#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;

namespace OpenRA.Platforms.Default
{
	sealed class DummySoundEngine : ISoundEngine
	{
		public bool Dummy { get { return true; } }

		public SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice(null, "Default Output"),
			};

			return defaultDevices;
		}

		public DummySoundEngine(string deviceName) { }

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new NullSoundSource();
		}

		public ISound Play2D(ISoundSource soundSource, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			return new NullSound();
		}

		public ISound Play2DStream(Stream stream, int channels, int sampleBits, int sampleRate, bool loop, bool relative, WPos pos, float volume)
		{
			return null;
		}

		public float Volume
		{
			get { return 0; }
			set { }
		}

		public void PauseSound(ISound sound, bool paused) { }
		public void SetAllSoundsPaused(bool paused) { }
		public void SetSoundVolume(float volume, ISound music, ISound video) { }
		public void StopSound(ISound sound) { }
		public void StopAllSounds() { }
		public void SetListenerPosition(WPos position) { }
		public void Dispose() { }
	}

	class NullSoundSource : ISoundSource
	{
		public void Dispose() { }
	}

	class NullSound : ISound
	{
		public float Volume { get; set; }
		public float SeekPosition { get { return 0; } }
		public bool Complete { get { return false; } }

		public void SetPosition(WPos position) { }
	}
}
