#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA
{
	class NullSoundEngine : ISoundEngine
	{
		public NullSoundEngine()
		{
			Console.WriteLine("Using Null sound engine which disables SFX completely");
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new NullSoundSource();
		}

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			return new NullSound();
		}

		public void PauseSound(ISound sound, bool paused) { }
		public void StopSound(ISound sound) { }
		public void SetAllSoundsPaused(bool paused) { }
		public void StopAllSounds() { }
		public void SetListenerPosition(WPos position) { }
		public void SetSoundVolume(float volume, ISound music, ISound video) { }

		public float Volume { get; set; }
	}

	class NullSoundSource : ISoundSource { }

	class NullSound : ISound
	{
		public float Volume { get; set; }
		public float SeekPosition { get { return 0; } }
		public bool Playing { get { return false; } }
	}
}
