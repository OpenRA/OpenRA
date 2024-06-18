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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Creative;
using Silk.NET.OpenAL.Extensions.Enumeration;

namespace OpenRA.Platforms.Default
{
	sealed class OpenAlSoundEngine : ISoundEngine
	{
		public bool Dummy => false;

		public SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice(null, "Default Output"),
			};

			var physicalDevices = PhysicalDevices().Select(d => new SoundDevice(d, d));
			return defaultDevices.Concat(physicalDevices).ToArray();
		}

		sealed class PoolSlot
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

		// https://github.com/kcat/openal-soft/issues/580
		// https://github.com/kcat/openal-soft/blob/b6aa73b26004afe63d83097f2f91ecda9bc25cb9/alc/alc.cpp#L3191-L3203
		const int PoolSize = 256;

		static readonly AL Al;
		static readonly ALContext Alc;

		readonly Dictionary<uint, PoolSlot> sourcePool = new(PoolSize);
		float volume = 1f;
		unsafe Device* device;
		unsafe Context* context;

		static OpenAlSoundEngine()
		{
			Al = AL.GetApi();
			Alc = ALContext.GetApi();
		}

		static unsafe string[] PhysicalDevices()
		{
			// Clear error bit
			Al.GetError();

			// Returns all devices under Windows Vista and newer
			if (Alc.TryGetExtension<EnumerateAll>(null, out var enumerateAll))
			{
				var devices = enumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
				if (devices != null && Al.GetError() == AudioError.NoError)
					return devices.ToArray();
			}

			if (Alc.TryGetExtension<Enumeration>(null, out var enumeration))
			{
				var devices = enumeration.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers);
				if (devices != null && Al.GetError() == AudioError.NoError)
					return devices.ToArray();
			}

			return Array.Empty<string>();
		}

		internal static BufferFormat MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? BufferFormat.Mono16 : BufferFormat.Mono8;
			else
				return bits == 16 ? BufferFormat.Stereo16 : BufferFormat.Stereo8;
		}

		public unsafe OpenAlSoundEngine(string deviceName)
		{
			if (deviceName != null)
				Console.WriteLine("Using sound device `{0}`", deviceName);
			else
				Console.WriteLine("Using default sound device");

			device = Alc.OpenDevice(deviceName);
			if (device == null)
			{
				Console.WriteLine("Failed to open device. Falling back to default");
				device = Alc.OpenDevice(null);
				if (device == null)
					throw new InvalidOperationException("Can't create OpenAL device");
			}

			context = Alc.CreateContext(device, null);
			if (context == null)
				throw new InvalidOperationException("Can't create OpenAL context");
			Alc.MakeContextCurrent(context);

			for (var i = 0; i < PoolSize; i++)
			{
				var source = Al.GenSources(1)[0];
				if (Al.GetError() != AudioError.NoError)
				{
					Log.Write("sound", $"Failed generating OpenAL source {i}");
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
					Al.SourceRewind(freeSource);
					Al.SetSourceProperty(freeSource, SourceInteger.Buffer, 0);

					// Make sure we can accurately determine the end of the original sound,
					// even if the source is immediately reused.
					sound.UnbindSource();

					var slot = kv.Value;
					slot.SoundSource = null;
					slot.Sound = null;
					slot.IsActive = false;
				}
			}

			if (freeSources.Count == 0)
			{
				source = 0;
				return false;
			}

			source = freeSources[0];
			sourcePool[source].IsActive = true;
			return true;
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(Al, data, data.Length, channels, sampleBits, sampleRate);
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

			if (!TryGetSourceFromPool(out var source))
				return null;

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.IsRelative = relative;
			slot.SoundSource = alSoundSource;
			slot.Sound = new OpenAlSound(Al, source, loop, relative, pos, volume * atten, alSoundSource.SampleRate, alSoundSource.Buffer);
			return slot.Sound;
		}

		public ISound Play2DStream(Stream stream, int channels, int sampleBits, int sampleRate, bool loop, bool relative, WPos pos, float volume)
		{
			var currFrame = Game.LocalTick;

			if (!TryGetSourceFromPool(out var source))
				return null;

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.IsRelative = relative;
			slot.SoundSource = null;
			slot.Sound = new OpenAlAsyncLoadSound(Al, source, loop, relative, pos, volume, channels, sampleBits, sampleRate, stream);
			return slot.Sound;
		}

		public float Volume
		{
			get => volume;
			set => AL.GetApi().SetListenerProperty(ListenerFloat.Gain, volume = value);
		}

		public void PauseSound(ISound sound, bool paused)
		{
			if (sound == null || sound.Complete)
				return;

			var source = ((OpenAlSound)sound).Source;
			PauseSound(source, paused);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var source in sourcePool.Keys)
				PauseSound(source, paused);
		}

		static void PauseSound(uint source, bool paused)
		{
			Al.GetSourceProperty(source, GetSourceInteger.SourceState, out var state);
			if (paused)
			{
				if (state == (int)SourceState.Playing)
					Al.SourcePause(source);
				else if (state == (int)SourceState.Initial)
				{
					// If a sound hasn't started yet,
					// we indicate it should not play be transitioning it to the stopped state.
					Al.SourcePlay(source);
					Al.SourceStop(source);
				}
			}
			else if (!paused && state != (int)SourceState.Playing)
				Al.SourcePlay(source);
		}

		public void SetSoundVolume(float volume, ISound music, ISound video)
		{
			var sounds = sourcePool.Keys.Where(key =>
			{
				Al.GetSourceProperty(key, GetSourceInteger.SourceState, out var state);
				return (state == (int)SourceState.Playing || state == (int)SourceState.Paused) &&
					   (music == null || key != ((OpenAlSound)music).Source) &&
					   (video == null || key != ((OpenAlSound)video).Source);
			});

			foreach (var s in sounds)
				Al.SetSourceProperty(s, SourceFloat.Gain, volume);
		}

		public void StopSound(ISound sound)
		{
			((OpenAlSound)sound)?.Stop();
		}

		public void StopAllSounds()
		{
			foreach (var slot in sourcePool.Values)
				slot.Sound?.Stop();
		}

		public unsafe void SetListenerPosition(WPos position)
		{
			// Move the listener out of the plane so that sounds near the middle of the screen aren't too positional
			Al.SetListenerProperty(ListenerVector3.Position, position.X, position.Y, position.Z + 2133);

			var orientation = new[] { 0f, 0f, 1f, 0f, -1f, 0f };
			fixed (float* orientationPtr = &orientation[0])
				Al.SetListenerProperty(ListenerFloatArray.Orientation, orientationPtr);

			// TODO bugged in silk!
			Al.SetListenerProperty((ListenerFloat)(int)EFXListenerFloat.EfxMetersPerUnit, 0.1f);
			/*if (Al.TryGetExtension<EffectExtension>(out var effectExtension))
				effectExtension.SetListenerProperty(EFXListenerFloat.EfxMetersPerUnit, 0.1f);*/
		}

		public void SetSoundLooping(bool looping, ISound sound)
		{
			((OpenAlSound)sound)?.SetLooping(looping);
		}

		public void SetSoundPosition(ISound sound, WPos position)
		{
			((OpenAlSound)sound)?.SetPosition(position);
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

		unsafe void Dispose(bool disposing)
		{
			if (disposing)
				StopAllSounds();

			if (sourcePool.Count > 0)
			{
				var sources = sourcePool.Keys.ToArray();
				fixed (uint* sourcesPtr = &sources[0])
					Al.DeleteSources(PoolSize, sourcesPtr);
			}

			sourcePool.Clear();

			if (context != null)
			{
				Alc.MakeContextCurrent(IntPtr.Zero);
				Alc.DestroyContext(context);
				context = null;
			}

			if (device != null)
			{
				Alc.CloseDevice(device);
				device = null;
			}
		}
	}

	sealed class OpenAlSoundSource : ISoundSource
	{
		readonly AL al;
		bool disposed;

		public uint Buffer { get; }
		public int SampleRate { get; }

		public unsafe OpenAlSoundSource(AL al, byte[] data, int byteCount, int channels, int sampleBits, int sampleRate)
		{
			this.al = al;
			SampleRate = sampleRate;
			Buffer = al.GenBuffers(1)[0];
			fixed (byte* dataPtr = &data[0])
				al.BufferData(Buffer, OpenAlSoundEngine.MakeALFormat(channels, sampleBits), dataPtr, byteCount, sampleRate);
		}

		void Dispose(bool _)
		{
			if (!disposed)
			{
				al.DeleteBuffer(Buffer);
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
		internal uint Source { get; private set; }
		protected readonly float SampleRate;
		readonly AL al;

		bool done;

		public OpenAlSound(AL al, uint source, bool looping, bool relative, WPos pos, float volume, int sampleRate, uint buffer)
			: this(al, source, looping, relative, pos, volume, sampleRate)
		{
			al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
			al.SourcePlay(source);
		}

		protected OpenAlSound(AL al, uint source, bool looping, bool relative, WPos pos, float volume, int sampleRate)
		{
			this.al = al;
			Source = source;
			SampleRate = sampleRate;
			Volume = volume;

			al.SetSourceProperty(source, SourceFloat.Pitch, 1f);
			al.SetSourceProperty(source, SourceVector3.Position, pos.X, pos.Y, pos.Z);
			al.SetSourceProperty(source, SourceVector3.Velocity, 0f, 0f, 0f);
			al.SetSourceProperty(source, SourceBoolean.Looping, looping);
			al.SetSourceProperty(source, SourceBoolean.SourceRelative, relative);

			al.SetSourceProperty(source, SourceFloat.ReferenceDistance, 6826);
			al.SetSourceProperty(source, SourceFloat.MaxDistance, 136533);
		}

		internal void UnbindSource()
		{
			done = true;
			Source = uint.MaxValue;
		}

		public float Volume
		{
			get
			{
				if (done)
					return float.NaN;

				al.GetSourceProperty(Source, SourceFloat.Gain, out var volume);
				return volume;
			}

			set
			{
				if (done)
					return;

				al.SetSourceProperty(Source, SourceFloat.Gain, value);
			}
		}

		public virtual float SeekPosition
		{
			get
			{
				if (done)
					return float.NaN;

				al.GetSourceProperty(Source, GetSourceInteger.SampleOffset, out var sampleOffset);
				return sampleOffset / SampleRate;
			}
		}

		public virtual bool Complete
		{
			get
			{
				if (done)
					return true;

				al.GetSourceProperty(Source, GetSourceInteger.SourceState, out var state);
				return state == (int)SourceState.Stopped;
			}
		}

		public void SetPosition(WPos pos)
		{
			if (done)
				return;

			al.SetSourceProperty(Source, SourceVector3.Position, pos.X, pos.Y, pos.Z);
		}

		protected void StopSource()
		{
			if (done)
				return;

			al.GetSourceProperty(Source, GetSourceInteger.SourceState, out var state);
			if (state == (int)SourceState.Playing || state == (int)SourceState.Paused)
				al.SourceStop(Source);
		}

		public virtual void Stop()
		{
			if (done)
				return;

			StopSource();
			al.SetSourceProperty(Source, SourceInteger.Buffer, 0);
		}

		public void SetLooping(bool looping)
		{
			if (done)
				return;

			al.SetSourceProperty(Source, SourceBoolean.Looping, looping);
		}
	}

	sealed class OpenAlAsyncLoadSound : OpenAlSound
	{
		static readonly byte[] SilentData = new byte[2];
		readonly CancellationTokenSource cts = new();
		readonly Task playTask;

		public OpenAlAsyncLoadSound(AL al, uint source, bool looping, bool relative, WPos pos, float volume, int channels, int sampleBits, int sampleRate, Stream stream)
			: base(al, source, looping, relative, pos, volume, sampleRate)
		{
			// Load a silent buffer into the source. Without this,
			// attempting to change the state (i.e. play/pause) the source fails on some systems.
			var silentSource = new OpenAlSoundSource(al, SilentData, SilentData.Length, channels, sampleBits, sampleRate);
			al.SetSourceProperty(source, SourceInteger.Buffer, (int)silentSource.Buffer);

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
						al.SourceStop(source);
						al.SetSourceProperty(source, SourceInteger.Buffer, 0);
						silentSource.Dispose();
						return;
					}
				}

				var data = memoryStream.GetBuffer();
				var dataLength = (int)memoryStream.Length;
				var bytesPerSample = sampleBits / 8f;
				var lengthInSecs = dataLength / (channels * bytesPerSample * sampleRate);
				using (var soundSource = new OpenAlSoundSource(al, data, dataLength, channels, sampleBits, sampleRate))
				{
					// Need to stop the source, before attaching the real input and deleting the silent one.
					al.SourceStop(source);
					al.SetSourceProperty(source, SourceInteger.Buffer, (int)soundSource.Buffer);
					silentSource.Dispose();

					lock (cts)
					{
						if (!cts.IsCancellationRequested)
						{
							// TODO: A race condition can happen between the state check and playing/rewinding if a
							// user pauses/resumes at the right moment. The window of opportunity is small and the
							// consequences are minor, so for now we'll ignore it.
							al.GetSourceProperty(Source, GetSourceInteger.SourceState, out var state);
							if (state != (int)SourceState.Stopped)
								al.SourcePlay(source);
							else
							{
								// A stopped sound indicates it was paused before we finishing loaded.
								// We don't want to start playing it right away.
								// We rewind the source so when it is started, it plays from the beginning.
								al.SourceRewind(source);
							}
						}
					}

					while (!cts.IsCancellationRequested)
					{
						// Need to check seek before state. Otherwise, the music can stop after our state check at
						// which point the seek will be zero, meaning we'll wait the full track length before seeing it
						// has stopped.
						var currentSeek = SeekPosition;

						al.GetSourceProperty(Source, GetSourceInteger.SourceState, out var state);
						if (state == (int)SourceState.Stopped)
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

					al.SetSourceProperty(Source, SourceInteger.Buffer, 0);
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

		public override bool Complete => playTask.IsCompleted;
	}
}
