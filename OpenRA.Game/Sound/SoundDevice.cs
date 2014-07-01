#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA
{
	interface ISoundEngine
	{
		ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate);
		ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume);
		float Volume { get; set; }
		void PauseSound(ISound sound, bool paused);
		void StopSound(ISound sound);
		void SetAllSoundsPaused(bool paused);
		void StopAllSounds();
		void SetListenerPosition(WPos position);
		void SetSoundVolume(float volume, ISound music, ISound video);
	}

	public class SoundDevice
	{
		public readonly string Engine;
		public readonly string Device;
		public readonly string Label;

		public SoundDevice(string engine, string device, string label)
		{
			Engine = engine;
			Device = device;
			Label = label;

			// Limit label to 32 characters
			if (Label.Length > 32)
				Label = "..." + Label.Substring(Label.Length - 32);
		}
	}

	interface ISoundSource { }

	public interface ISound
	{
		float Volume { get; set; }
		float SeekPosition { get; }
		bool Playing { get; }
	}
}
