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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenAL;

namespace OpenRA.Platforms.Default
{
	sealed class OpenAlSoundEngine : ISoundEngine
	{
		public SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice("Default", null, "Default Output"),
				new SoundDevice("Null", null, "Output Disabled")
			};

			var physicalDevices = PhysicalDevices().Select(d => new SoundDevice("Default", d, d));
			return defaultDevices.Concat(physicalDevices).ToArray();
		}

		class PoolSlot
		{
			public bool IsActive;
			public int FrameStarted;
			public WPos Pos;
			public bool IsRelative;
			public ISoundSource Sound;
		}

		const int MaxInstancesPerFrame = 3;
		const int GroupDistance = 2730;
		const int GroupDistanceSqr = GroupDistance * GroupDistance;
		const int PoolSize = 32;

		readonly Dictionary<uint, PoolSlot> sourcePool = new Dictionary<uint, PoolSlot>();
		float volume = 1f;
		IntPtr device;
		IntPtr context;

		static string[] QueryDevices(string label, int type)
		{
			// Clear error bit
			AL10.alGetError();

			var devices = new List<string>();
			var next = ALC10.alcGetString(IntPtr.Zero, type);
			if (next == IntPtr.Zero || AL10.alGetError() != AL10.AL_NO_ERROR)
			{
				Log.Write("sound", "Failed to query OpenAL device list using {0}", label);
				return new string[] { };
			}

			do
			{
				var str = Marshal.PtrToStringAuto(next);
				next += Encoding.Default.GetByteCount(str) + 1;
				devices.Add(str);
			} while (Marshal.ReadByte(next) != 0);

			return devices.ToArray();
		}

		static string[] PhysicalDevices()
		{
			// Returns all devices under Windows Vista and newer
			if (ALC11.alcIsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATE_ALL_EXT"))
				return QueryDevices("ALC_ENUMERATE_ALL_EXT", ALC11.ALC_ALL_DEVICES_SPECIFIER);

			if (ALC11.alcIsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATION_EXT"))
				return QueryDevices("ALC_ENUMERATION_EXT", ALC10.ALC_DEVICE_SPECIFIER);

			return new string[] { };
		}

		public OpenAlSoundEngine()
		{
			Console.WriteLine("Using OpenAL sound engine");

			if (Game.Settings.Sound.Device != null)
				Console.WriteLine("Using device `{0}`", Game.Settings.Sound.Device);
			else
				Console.WriteLine("Using default device");

			device = ALC10.alcOpenDevice(Game.Settings.Sound.Device);
			if (device == IntPtr.Zero)
			{
				Console.WriteLine("Failed to open device. Falling back to default");
				device = ALC10.alcOpenDevice(null);
				if (device == IntPtr.Zero)
					throw new InvalidOperationException("Can't create OpenAL device");
			}

			context = ALC10.alcCreateContext(device, null);
			if (context == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL context");
			ALC10.alcMakeContextCurrent(context);

			for (var i = 0; i < PoolSize; i++)
			{
				var source = 0U;
				AL10.alGenSources(new IntPtr(1), out source);
				if (AL10.alGetError() != AL10.AL_NO_ERROR)
				{
					Log.Write("sound", "Failed generating OpenAL source {0}", i);
					return;
				}

				sourcePool.Add(source, new PoolSlot() { IsActive = false });
			}
		}

		bool TryGetSourceFromPool(out uint source)
		{
			foreach (var kvp in sourcePool)
			{
				if (!kvp.Value.IsActive)
				{
					sourcePool[kvp.Key].IsActive = true;
					source = kvp.Key;
					return true;
				}
			}

			var freeSources = new List<uint>();
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
				if (state != AL10.AL_PLAYING && state != AL10.AL_PAUSED)
					freeSources.Add(key);
			}

			if (freeSources.Count == 0)
			{
				source = 0;
				return false;
			}

			foreach (var i in freeSources)
				sourcePool[i].IsActive = false;

			sourcePool[freeSources[0]].IsActive = true;

			source = freeSources[0];
			return true;
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
		}

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			if (sound == null)
			{
				Log.Write("sound", "Attempt to Play2D a null `ISoundSource`");
				return null;
			}

			var currFrame = Game.LocalTick;
			var atten = 1f;

			// Check if max # of instances-per-location reached:
			if (attenuateVolume)
			{
				int instances = 0, activeCount = 0;
				foreach (var s in sourcePool.Values)
				{
					if (!s.IsActive)
						continue;
					if (s.IsRelative != relative)
						continue;

					++activeCount;
					if (s.Sound != sound)
						continue;
					if (currFrame - s.FrameStarted >= 5)
						continue;

					// Too far away to count?
					var lensqr = (s.Pos - pos).LengthSquared;
					if (lensqr >= GroupDistanceSqr)
						continue;

					// If we are starting too many instances of the same sound within a short time then stop this one:
					if (++instances == MaxInstancesPerFrame)
						return null;
				}

				// Attenuate a little bit based on number of active sounds:
				atten = 0.66f * ((PoolSize - activeCount * 0.5f) / PoolSize);
			}

			uint source;
			if (!TryGetSourceFromPool(out source))
				return null;

			if (Game.Settings.Sound.Mute)
				Game.Sound.MuteAudio();

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.Sound = sound;
			slot.IsRelative = relative;
			return new OpenAlSound(source, ((OpenAlSoundSource)sound).Buffer, loop, relative, pos, volume * atten);
		}

		public float Volume
		{
			get { return volume; }
			set { AL10.alListenerf(AL10.AL_GAIN, volume = value); }
		}

		public void PauseSound(ISound sound, bool paused)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
			if (state == AL10.AL_PLAYING && paused)
				AL10.alSourcePause(key);
			else if (state == AL10.AL_PAUSED && !paused)
				AL10.alSourcePlay(key);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
				if (state == AL10.AL_PLAYING && paused)
					AL10.alSourcePause(key);
				else if (state == AL10.AL_PAUSED && !paused)
					AL10.alSourcePlay(key);
			}
		}

		public void SetSoundVolume(float volume, ISound music, ISound video)
		{
			var sounds = sourcePool.Keys.Where(key =>
			{
				int state;
				AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
				return (state == AL10.AL_PLAYING || state == AL10.AL_PAUSED) &&
					   (music == null || key != ((OpenAlSound)music).Source) &&
					   (video == null || key != ((OpenAlSound)video).Source);
			});

			foreach (var s in sounds)
				AL10.alSourcef(s, AL10.AL_GAIN, volume);
		}

		public void StopSound(ISound sound)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
			if (state == AL10.AL_PLAYING || state == AL10.AL_PAUSED)
				AL10.alSourceStop(key);
		}

		public void StopAllSounds()
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL10.alGetSourcei(key, AL10.AL_SOURCE_STATE, out state);
				if (state == AL10.AL_PLAYING || state == AL10.AL_PAUSED)
					AL10.alSourceStop(key);
			}
		}

		public void SetListenerPosition(WPos position)
		{
			// Move the listener out of the plane so that sounds near the middle of the screen aren't too positional
			AL10.alListener3f(AL10.AL_POSITION, position.X, position.Y, position.Z + 2133);

			var orientation = new[] { 0f, 0f, 1f, 0f, -1f, 0f };
			AL10.alListenerfv(AL10.AL_ORIENTATION, orientation);
			AL10.alListenerf(EFX.AL_METERS_PER_UNIT, .01f);
		}

		~OpenAlSoundEngine()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (context != IntPtr.Zero)
			{
				ALC10.alcMakeContextCurrent(IntPtr.Zero);
				ALC10.alcDestroyContext(context);
				context = IntPtr.Zero;
			}

			if (device != IntPtr.Zero)
			{
				ALC10.alcCloseDevice(device);
				device = IntPtr.Zero;
			}
		}
	}

	class OpenAlSoundSource : ISoundSource
	{
		public readonly uint Buffer;

		static int MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? AL10.AL_FORMAT_MONO16 : AL10.AL_FORMAT_MONO8;
			else
				return bits == 16 ? AL10.AL_FORMAT_STEREO16 : AL10.AL_FORMAT_STEREO8;
		}

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			AL10.alGenBuffers(new IntPtr(1), out Buffer);
			AL10.alBufferData(Buffer, MakeALFormat(channels, sampleBits), data, new IntPtr(data.Length), new IntPtr(sampleRate));
		}
	}

	class OpenAlSound : ISound
	{
		public readonly uint Source;
		float volume;

		public OpenAlSound(uint source, uint buffer, bool looping, bool relative, WPos pos, float volume)
		{
			Source = source;
			Volume = volume;

			AL10.alSourcef(source, AL10.AL_PITCH, 1f);
			AL10.alSource3f(source, AL10.AL_POSITION, pos.X, pos.Y, pos.Z);
			AL10.alSource3f(source, AL10.AL_VELOCITY, 0f, 0f, 0f);
			AL10.alSourcei(source, AL10.AL_BUFFER, (int)buffer);
			AL10.alSourcei(source, AL10.AL_LOOPING, looping ? 1 : 0);
			AL10.alSourcei(source, AL10.AL_SOURCE_RELATIVE, relative ? 1 : 0);

			AL10.alSourcef(source, AL10.AL_REFERENCE_DISTANCE, 6826);
			AL10.alSourcef(source, AL10.AL_MAX_DISTANCE, 136533);
			AL10.alSourcePlay(source);
		}

		public float Volume
		{
			get { return volume; }
			set { AL10.alSourcef(Source, AL10.AL_GAIN, volume = value); }
		}

		public float SeekPosition
		{
			get
			{
				int pos;
				AL10.alGetSourcei(Source, AL11.AL_SAMPLE_OFFSET, out pos);
				return pos / 22050f;
			}
		}

		public bool Playing
		{
			get
			{
				int state;
				AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
				return state == AL10.AL_PLAYING;
			}
		}
	}
}
