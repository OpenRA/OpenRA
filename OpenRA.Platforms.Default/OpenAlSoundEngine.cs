#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenAL;

namespace OpenRA.Platforms.Default
{
	sealed class OpenAlSoundEngine : ISoundEngine
	{
		public SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice(null, "Default Output"),
			};

			var physicalDevices = PhysicalDevices().Select(d => new SoundDevice(d, d));
			return defaultDevices.Concat(physicalDevices).ToArray();
		}

		class PoolSlot
		{
			public bool IsActive;
			public int FrameStarted;
			public WPos Pos;
			public bool IsRelative;
			public OpenAlSoundSource SoundSource;
			public OpenAlSound Sound;
		}

		const int MaxInstancesPerFrame = 3;
		const int GroupDistance = 2730;
		const int GroupDistanceSqr = GroupDistance * GroupDistance;
		const int PoolSize = 32;

		readonly Dictionary<uint, PoolSlot> sourcePool = new Dictionary<uint, PoolSlot>(PoolSize);
		float volume = 1f;
		IntPtr device;
		IntPtr context;

		static string[] QueryDevices(string label, int type)
		{
			// Clear error bit
			AL10.alGetError();

			// Returns a null separated list of strings, terminated by two nulls.
			var devicesPtr = ALC10.alcGetString(IntPtr.Zero, type);
			if (devicesPtr == IntPtr.Zero || AL10.alGetError() != AL10.AL_NO_ERROR)
			{
				Log.Write("sound", "Failed to query OpenAL device list using {0}", label);
				return new string[0];
			}

			var devices = new List<string>();
			var buffer = new List<byte>();
			var offset = 0;

			while (true)
			{
				var b = Marshal.ReadByte(devicesPtr, offset++);
				if (b != 0)
				{
					buffer.Add(b);
					continue;
				}

				// A null indicates termination of that string, so add that to our list.
				devices.Add(Encoding.UTF8.GetString(buffer.ToArray()));
				buffer.Clear();

				// Two successive nulls indicates the end of the list.
				if (Marshal.ReadByte(devicesPtr, offset) == 0)
					break;
			}

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

		internal static int MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? AL10.AL_FORMAT_MONO16 : AL10.AL_FORMAT_MONO8;
			else
				return bits == 16 ? AL10.AL_FORMAT_STEREO16 : AL10.AL_FORMAT_STEREO8;
		}

		public OpenAlSoundEngine(string deviceName)
		{
			if (deviceName != null)
				Console.WriteLine("Using sound device `{0}`", deviceName);
			else
				Console.WriteLine("Using default sound device");

			device = ALC10.alcOpenDevice(deviceName);
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
			foreach (var kv in sourcePool)
			{
				if (!kv.Value.IsActive)
				{
					sourcePool[kv.Key].IsActive = true;
					source = kv.Key;
					return true;
				}
			}

			var freeSources = new List<uint>();
			foreach (var kv in sourcePool)
			{
				var sound = kv.Value.Sound;
				if (sound != null && sound.Complete)
				{
					var freeSource = kv.Key;
					freeSources.Add(freeSource);
					AL10.alSourceRewind(freeSource);
					AL10.alSourcei(freeSource, AL10.AL_BUFFER, 0);
				}
			}

			if (freeSources.Count == 0)
			{
				source = 0;
				return false;
			}

			foreach (var freeSource in freeSources)
			{
				var slot = sourcePool[freeSource];
				slot.SoundSource = null;
				slot.Sound = null;
				slot.IsActive = false;
			}

			source = freeSources[0];
			sourcePool[source].IsActive = true;
			return true;
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(data, data.Length, channels, sampleBits, sampleRate);
		}

		public ISound Play2D(ISoundSource soundSource, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			if (soundSource == null)
			{
				Log.Write("sound", "Attempt to Play2D a null `ISoundSource`");
				return null;
			}

			var alSoundSource = (OpenAlSoundSource)soundSource;

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
					if (s.SoundSource != alSoundSource)
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

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.IsRelative = relative;
			slot.SoundSource = alSoundSource;
			slot.Sound = new OpenAlSound(source, loop, relative, pos, volume * atten, alSoundSource.SampleRate, alSoundSource.Buffer);
			return slot.Sound;
		}

		public ISound Play2DStream(Stream stream, int channels, int sampleBits, int sampleRate, bool loop, bool relative, WPos pos, float volume)
		{
			var currFrame = Game.LocalTick;

			uint source;
			if (!TryGetSourceFromPool(out source))
				return null;

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.IsRelative = relative;
			slot.SoundSource = null;
			slot.Sound = new OpenAlAsyncLoadSound(source, loop, relative, pos, volume, channels, sampleBits, sampleRate, stream);
			return slot.Sound;
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

			var source = ((OpenAlSound)sound).Source;
			PauseSound(source, paused);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var source in sourcePool.Keys)
				PauseSound(source, paused);
		}

		void PauseSound(uint source, bool paused)
		{
			int state;
			AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out state);
			if (paused)
			{
				if (state == AL10.AL_PLAYING)
					AL10.alSourcePause(source);
				else if (state == AL10.AL_INITIAL)
				{
					// If a sound hasn't started yet,
					// we indicate it should not play be transitioning it to the stopped state.
					AL10.alSourcePlay(source);
					AL10.alSourceStop(source);
				}
			}
			else if (!paused && state != AL10.AL_PLAYING)
				AL10.alSourcePlay(source);
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

			((OpenAlSound)sound).Stop();
		}

		public void StopAllSounds()
		{
			foreach (var slot in sourcePool.Values)
				if (slot.Sound != null)
					slot.Sound.Stop();
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
			StopAllSounds();

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
		uint buffer;
		bool disposed;

		public uint Buffer { get { return buffer; } }
		public int SampleRate { get; private set; }

		public OpenAlSoundSource(byte[] data, int byteCount, int channels, int sampleBits, int sampleRate)
		{
			SampleRate = sampleRate;
			AL10.alGenBuffers(new IntPtr(1), out buffer);
			AL10.alBufferData(buffer, OpenAlSoundEngine.MakeALFormat(channels, sampleBits), data, new IntPtr(byteCount), new IntPtr(sampleRate));
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				AL10.alDeleteBuffers(new IntPtr(1), ref buffer);
				disposed = true;
			}
		}

		~OpenAlSoundSource()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	class OpenAlSound : ISound
	{
		public readonly uint Source;
		protected readonly float SampleRate;

		public OpenAlSound(uint source, bool looping, bool relative, WPos pos, float volume, int sampleRate, uint buffer)
			: this(source, looping, relative, pos, volume, sampleRate)
		{
			AL10.alSourcei(source, AL10.AL_BUFFER, (int)buffer);
			AL10.alSourcePlay(source);
		}

		protected OpenAlSound(uint source, bool looping, bool relative, WPos pos, float volume, int sampleRate)
		{
			Source = source;
			SampleRate = sampleRate;
			Volume = volume;

			AL10.alSourcef(source, AL10.AL_PITCH, 1f);
			AL10.alSource3f(source, AL10.AL_POSITION, pos.X, pos.Y, pos.Z);
			AL10.alSource3f(source, AL10.AL_VELOCITY, 0f, 0f, 0f);
			AL10.alSourcei(source, AL10.AL_LOOPING, looping ? 1 : 0);
			AL10.alSourcei(source, AL10.AL_SOURCE_RELATIVE, relative ? 1 : 0);

			AL10.alSourcef(source, AL10.AL_REFERENCE_DISTANCE, 6826);
			AL10.alSourcef(source, AL10.AL_MAX_DISTANCE, 136533);
		}

		public float Volume
		{
			get { float volume; AL10.alGetSourcef(Source, AL10.AL_GAIN, out volume); return volume; }
			set { AL10.alSourcef(Source, AL10.AL_GAIN, value); }
		}

		public virtual float SeekPosition
		{
			get
			{
				int sampleOffset;
				AL10.alGetSourcei(Source, AL11.AL_SAMPLE_OFFSET, out sampleOffset);
				return sampleOffset / SampleRate;
			}
		}

		public virtual bool Complete
		{
			get
			{
				int state;
				AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
				return state == AL10.AL_STOPPED;
			}
		}

		public void SetPosition(WPos pos)
		{
			AL10.alSource3f(Source, AL10.AL_POSITION, pos.X, pos.Y, pos.Z);
		}

		protected void StopSource()
		{
			int state;
			AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
			if (state == AL10.AL_PLAYING || state == AL10.AL_PAUSED)
				AL10.alSourceStop(Source);
		}

		public virtual void Stop()
		{
			StopSource();
			AL10.alSourcei(Source, AL10.AL_BUFFER, 0);
		}
	}

	class OpenAlAsyncLoadSound : OpenAlSound
	{
		static readonly byte[] SilentData = new byte[2];
		readonly CancellationTokenSource cts = new CancellationTokenSource();
		readonly Task playTask;

		public OpenAlAsyncLoadSound(uint source, bool looping, bool relative, WPos pos, float volume, int channels, int sampleBits, int sampleRate, Stream stream)
			: base(source, looping, relative, pos, volume, sampleRate)
		{
			// Load a silent buffer into the source. Without this,
			// attempting to change the state (i.e. play/pause) the source fails on some systems.
			var silentSource = new OpenAlSoundSource(SilentData, SilentData.Length, channels, sampleBits, sampleRate);
			AL10.alSourcei(source, AL10.AL_BUFFER, (int)silentSource.Buffer);

			playTask = Task.Run(async () =>
			{
				MemoryStream memoryStream;
				using (stream)
				{
					try
					{
						memoryStream = new MemoryStream((int)stream.Length);
					}
					catch (NotSupportedException)
					{
						// Fallback for stream types that don't support Length.
						memoryStream = new MemoryStream();
					}

					try
					{
						await stream.CopyToAsync(memoryStream, 81920, cts.Token);
					}
					catch (TaskCanceledException)
					{
						// Sound was stopped early, cleanup the unused buffer and exit.
						AL10.alSourceStop(source);
						AL10.alSourcei(source, AL10.AL_BUFFER, 0);
						silentSource.Dispose();
						return;
					}
				}

				var data = memoryStream.GetBuffer();
				var dataLength = (int)memoryStream.Length;
				var bytesPerSample = sampleBits / 8f;
				var lengthInSecs = dataLength / (channels * bytesPerSample * sampleRate);
				using (var soundSource = new OpenAlSoundSource(data, dataLength, channels, sampleBits, sampleRate))
				{
					// Need to stop the source, before attaching the real input and deleting the silent one.
					AL10.alSourceStop(source);
					AL10.alSourcei(source, AL10.AL_BUFFER, (int)soundSource.Buffer);
					silentSource.Dispose();

					lock (cts)
					{
						if (!cts.IsCancellationRequested)
						{
							// TODO: A race condition can happen between the state check and playing/rewinding if a
							// user pauses/resumes at the right moment. The window of opportunity is small and the
							// consequences are minor, so for now we'll ignore it.
							int state;
							AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
							if (state != AL10.AL_STOPPED)
								AL10.alSourcePlay(source);
							else
							{
								// A stopped sound indicates it was paused before we finishing loaded.
								// We don't want to start playing it right away.
								// We rewind the source so when it is started, it plays from the beginning.
								AL10.alSourceRewind(source);
							}
						}
					}

					while (!cts.IsCancellationRequested)
					{
						// Need to check seek before state. Otherwise, the music can stop after our state check at
						// which point the seek will be zero, meaning we'll wait the full track length before seeing it
						// has stopped.
						var currentSeek = SeekPosition;

						int state;
						AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
						if (state == AL10.AL_STOPPED)
							break;

						try
						{
							// Wait until the track is due to complete, and at most 60 times a second to prevent a
							// busy-wait.
							var delaySecs = Math.Max(lengthInSecs - currentSeek, 1 / 60f);
							await Task.Delay(TimeSpan.FromSeconds(delaySecs), cts.Token);
						}
						catch (TaskCanceledException)
						{
							// Sound was stopped early, allow normal cleanup to occur.
						}
					}

					AL10.alSourcei(Source, AL10.AL_BUFFER, 0);
				}
			});
		}

		public override void Stop()
		{
			lock (cts)
			{
				StopSource();
				cts.Cancel();
			}

			try
			{
				playTask.Wait();
			}
			catch (AggregateException)
			{
			}
		}

		public override bool Complete
		{
			get { return playTask.IsCompleted; }
		}
	}
}
