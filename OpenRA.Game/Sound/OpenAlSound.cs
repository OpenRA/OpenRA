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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenTK;
using OpenTK.Audio.OpenAL;

namespace OpenRA
{
	class OpenAlSoundEngine : ISoundEngine
	{
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

		float volume = 1f;
		Dictionary<int, PoolSlot> sourcePool = new Dictionary<int, PoolSlot>();

		static string[] QueryDevices(string label, AlcGetStringList type)
		{
			// Clear error bit
			AL.GetError();

			var devices = Alc.GetString(IntPtr.Zero, type).ToArray();
			if (AL.GetError() != ALError.NoError)
			{
				Log.Write("sound", "Failed to query OpenAL device list using {0}", label);
				return new string[] { };
			}

			return devices;
		}

		public static string[] AvailableDevices()
		{
			// Returns all devices under Windows Vista and newer
			if (Alc.IsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATE_ALL_EXT"))
				return QueryDevices("ALC_ENUMERATE_ALL_EXT", AlcGetStringList.AllDevicesSpecifier);

			if (Alc.IsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATION_EXT"))
				return QueryDevices("ALC_ENUMERATION_EXT", AlcGetStringList.DeviceSpecifier);

			return new string[] { };
		}

		public OpenAlSoundEngine()
		{
			Console.WriteLine("Using OpenAL sound engine");

			if (Game.Settings.Sound.Device != null)
				Console.WriteLine("Using device `{0}`", Game.Settings.Sound.Device);
			else
				Console.WriteLine("Using default device");

			var dev = Alc.OpenDevice(Game.Settings.Sound.Device);
			if (dev == IntPtr.Zero)
			{
				Console.WriteLine("Failed to open device. Falling back to default");
				dev = Alc.OpenDevice(null);
				if (dev == IntPtr.Zero)
					throw new InvalidOperationException("Can't create OpenAL device");
			}

			var ctx = Alc.CreateContext(dev, (int[])null);
			if (ctx == ContextHandle.Zero)
				throw new InvalidOperationException("Can't create OpenAL context");
			Alc.MakeContextCurrent(ctx);

			for (var i = 0; i < PoolSize; i++)
			{
				var source = 0;
				AL.GenSources(1, out source);
				if (0 != AL.GetError())
				{
					Log.Write("sound", "Failed generating OpenAL source {0}", i);
					return;
				}

				sourcePool.Add(source, new PoolSlot() { IsActive = false });
			}
		}

		int GetSourceFromPool()
		{
			foreach (var kvp in sourcePool)
			{
				if (!kvp.Value.IsActive)
				{
					sourcePool[kvp.Key].IsActive = true;
					return kvp.Key;
				}
			}

			var freeSources = new List<int>();
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL.GetSource(key, ALGetSourcei.SourceState, out state);
				if (state != (int)ALSourceState.Playing && state != (int)ALSourceState.Paused)
					freeSources.Add(key);
			}

			if (freeSources.Count == 0)
				return -1;

			foreach (var i in freeSources)
				sourcePool[i].IsActive = false;

			sourcePool[freeSources[0]].IsActive = true;

			return freeSources[0];
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

			var currFrame = Game.orderManager.LocalFrameNumber;
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

			var source = GetSourceFromPool();
			if (source == -1)
				return null;

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
			set { AL.Listener(ALListenerf.Gain, volume = value); }
		}

		public void PauseSound(ISound sound, bool paused)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			AL.GetSource(key, ALGetSourcei.SourceState, out state);
			if (state == (int)ALSourceState.Playing && paused)
				AL.SourcePause(key);
			else if (state ==  (int)ALSourceState.Paused && !paused)
				AL.SourcePlay(key);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL.GetSource(key, ALGetSourcei.SourceState, out state);
				if (state == (int)ALSourceState.Playing && paused)
					AL.SourcePause(key);
				else if (state ==  (int)ALSourceState.Paused && !paused)
					AL.SourcePlay(key);
			}
		}

		public void SetSoundVolume(float volume, ISound music, ISound video)
		{
			var sounds = sourcePool.Select(s => s.Key).Where(b =>
			{
				int state;
				AL.GetSource(b, ALGetSourcei.SourceState, out state);
				return (state == (int)ALSourceState.Playing || state == (int)ALSourceState.Paused) &&
					   (music == null || b != ((OpenAlSound)music).Source) &&
					   (video == null || b != ((OpenAlSound)video).Source);
			});

			foreach (var s in sounds)
				AL.Source(s, ALSourcef.Gain, volume);
		}

		public void StopSound(ISound sound)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			AL.GetSource(key, ALGetSourcei.SourceState, out state);
			if (state == (int)ALSourceState.Playing || state == (int)ALSourceState.Paused)
				AL.SourceStop(key);
		}

		public void StopAllSounds()
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				AL.GetSource(key, ALGetSourcei.SourceState, out state);
				if (state == (int)ALSourceState.Playing || state == (int)ALSourceState.Paused)
					AL.SourceStop(key);
			}
		}

		public void SetListenerPosition(WPos position)
		{
			// Move the listener out of the plane so that sounds near the middle of the screen aren't too positional
			AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z + 2133);

			var orientation = new[] { 0f, 0f, 1f, 0f, -1f, 0f };
			AL.Listener(ALListenerfv.Orientation, ref orientation);
			AL.Listener(ALListenerf.EfxMetersPerUnit, .01f);
		}
	}

	class OpenAlSoundSource : ISoundSource
	{
		public readonly int Buffer;

		static ALFormat MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? ALFormat.Mono16 : ALFormat.Mono8;
			else
				return bits == 16 ? ALFormat.Stereo16 : ALFormat.Stereo8;
		}

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			AL.GenBuffers(1, out Buffer);
			AL.BufferData(Buffer, MakeALFormat(channels, sampleBits), data, data.Length, sampleRate);
		}
	}

	class OpenAlSound : ISound
	{
		public readonly int Source = -1;
		float volume = 1f;

		public OpenAlSound(int source, int buffer, bool looping, bool relative, WPos pos, float volume)
		{
			if (source == -1)
				return;

			Source = source;
			Volume = volume;

			AL.Source(source, ALSourcef.Pitch, 1f);
			AL.Source(source, ALSource3f.Position, pos.X, pos.Y, pos.Z);
			AL.Source(source, ALSource3f.Velocity, 0f, 0f, 0f);
			AL.Source(source, ALSourcei.Buffer, buffer);
			AL.Source(source, ALSourceb.Looping, looping);
			AL.Source(source, ALSourceb.SourceRelative, relative);

			AL.Source(source, ALSourcef.ReferenceDistance, 6826);
			AL.Source(source, ALSourcef.MaxDistance, 136533);
			AL.SourcePlay(source);
		}

		public float Volume
		{
			get
			{
				return volume;
			}

			set
			{
				if (Source != -1)
					AL.Source(Source, ALSourcef.Gain, volume = value);
			}
		}

		public float SeekPosition
		{
			get
			{
				int pos;
				AL.GetSource(Source, ALGetSourcei.SampleOffset, out pos);
				return pos / 22050f;
			}
		}

		public bool Playing
		{
			get
			{
				int state;
				AL.GetSource(Source, ALGetSourcei.SourceState, out state);
				return state == (int)ALSourceState.Playing;
			}
		}
	}
}
