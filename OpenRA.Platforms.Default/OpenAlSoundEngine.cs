#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
				devices.Add(Encoding.Default.GetString(buffer.ToArray()));
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
			return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
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
			slot.Sound = new OpenAlStreamingSound(source, loop, relative, pos, volume, channels, sampleBits, sampleRate, stream);
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
			int state;
			AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out state);
			if (state == AL10.AL_PLAYING && paused)
				AL10.alSourcePause(source);
			else if (state == AL10.AL_PAUSED && !paused)
				AL10.alSourcePlay(source);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var source in sourcePool.Keys)
			{
				int state;
				AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out state);
				if (state == AL10.AL_PLAYING && paused)
					AL10.alSourcePause(source);
				else if (state == AL10.AL_PAUSED && !paused)
					AL10.alSourcePlay(source);
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

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			SampleRate = sampleRate;
			AL10.alGenBuffers(new IntPtr(1), out buffer);
			AL10.alBufferData(buffer, OpenAlSoundEngine.MakeALFormat(channels, sampleBits), data, new IntPtr(data.Length), new IntPtr(sampleRate));
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

	class OpenAlStreamingSound : OpenAlSound
	{
		const int BufferCount = 3;
		const int BufferSizeInSecs = 1;
		readonly object bufferDequeueLock = new object();
		readonly CancellationTokenSource cts = new CancellationTokenSource();
		readonly Task streamTask;
		readonly Stack<uint> freeBuffers = new Stack<uint>(BufferCount);
		int totalSamplesPlayed;

		public OpenAlStreamingSound(uint source, bool looping, bool relative, WPos pos, float volume, int channels, int sampleBits, int sampleRate, Stream stream)
			: base(source, looping, relative, pos, volume, sampleRate)
		{
			streamTask = Task.Run(async () =>
			{
				var format = OpenAlSoundEngine.MakeALFormat(channels, sampleBits);
				var bytesPerSample = sampleBits / 8;

				var buffers = new uint[BufferCount];
				AL10.alGenBuffers(new IntPtr(buffers.Length), buffers);
				try
				{
					foreach (var buffer in buffers)
						freeBuffers.Push(buffer);
					var data = new byte[sampleRate * bytesPerSample * BufferSizeInSecs];
					var streamEnd = false;

					while (!streamEnd && !cts.IsCancellationRequested)
					{
						// Fill the data array as fully as possible.
						var count = await ReadFillingBuffer(stream, data);
						streamEnd = count < data.Length;

						// Fill a buffer and queue it for playback.
						var nextBuffer = freeBuffers.Pop();
						AL10.alBufferData(nextBuffer, format, data, new IntPtr(count), new IntPtr(sampleRate));
						AL10.alSourceQueueBuffers(source, new IntPtr(1), ref nextBuffer);

						lock (cts)
						{
							if (!cts.IsCancellationRequested)
							{
								// Once we have at least one buffer filled, we can actually start.
								// If streaming fell behind, the source will have stopped,
								// we also need to play it in this case to resume audio.
								int state;
								AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out state);
								if (state == AL10.AL_INITIAL || state == AL10.AL_STOPPED)
								{
									// If we resume playback from the stopped state, it resets to the beginning!
									// To avoid replaying the same audio, we need to dequeue the processed buffers first.
									if (state == AL10.AL_STOPPED)
										DequeueBuffers();

									AL10.alSourcePlay(source);
								}
							}
						}

						// Try and dequeue buffers as they become available to be reused.
						// When the stream ends, wait for all the buffers to be processed
						while (freeBuffers.Count < (streamEnd ? buffers.Length : 1))
						{
							await Task.Delay(TimeSpan.FromSeconds(BufferSizeInSecs), cts.Token).ConfigureAwait(false);
							DequeueBuffers();
						}
					}
				}
				catch (TaskCanceledException)
				{
					// Streaming has been cancelled, we'll need to perform some cleanup.
				}
				finally
				{
					// If we never actually started the source, we need to start it and then stop it.
					// Otherwise it is left in the initial state and never returned to the source pool.
					int state;
					AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);
					if (state == AL10.AL_INITIAL)
						AL10.alSourcePlay(Source);

					// Ensure the source is stopped, which will mark all buffers as processed.
					// Dequeue these, so they can then be freed.
					AL10.alSourceStop(Source);
					lock (bufferDequeueLock)
					{
						int buffersProcessed;
						AL10.alGetSourcei(source, AL10.AL_BUFFERS_PROCESSED, out buffersProcessed);
						for (var i = 0; i < buffersProcessed; i++)
						{
							var dequeued = 0U;
							AL10.alSourceUnqueueBuffers(source, new IntPtr(1), ref dequeued);
						}
					}

					AL10.alDeleteBuffers(new IntPtr(buffers.Length), buffers);
					stream.Dispose();
				}
			}).ContinueWith(task =>
			{
				Game.RunAfterTick(() =>
				{
					throw new Exception("Failed to stream a sound.", task.Exception);
				});
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		async Task<int> ReadFillingBuffer(Stream stream, byte[] buffer)
		{
			var offset = 0;
			int count;
			while (offset < buffer.Length &&
				(count = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cts.Token).ConfigureAwait(false)) > 0)
				offset += count;
			return offset;
		}

		void DequeueBuffers()
		{
			lock (bufferDequeueLock)
			{
				// Check for any processed buffers, and dequeue them for reuse.
				int buffersProcessed;
				AL10.alGetSourcei(Source, AL10.AL_BUFFERS_PROCESSED, out buffersProcessed);
				for (var i = 0; i < buffersProcessed; i++)
				{
					// Dequeue a processed buffer, so we can reuse it.
					var dequeued = 0U;
					AL10.alSourceUnqueueBuffers(Source, new IntPtr(1), ref dequeued);
					freeBuffers.Push(dequeued);

					// When we remove a buffer, we need to account for this to calculate the overall seek position.
					// The final buffer in the track may be shorter then BufferSizeInSecs, so we'll need to check how
					// many bytes are actually in each buffer to avoid adding too much at the end.
					int byteSize;
					AL10.alGetBufferi(dequeued, AL10.AL_SIZE, out byteSize);

					int bitRate;
					AL10.alGetBufferi(dequeued, AL10.AL_BITS, out bitRate);

					totalSamplesPlayed += byteSize / (bitRate / 8);
				}
			}
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
				streamTask.Wait();
			}
			catch (AggregateException)
			{
			}
		}

		public override float SeekPosition
		{
			get
			{
				// Stop buffers being dequeued whilst we calculate the seek position.
				lock (bufferDequeueLock)
				{
					int sampleOffset;
					AL10.alGetSourcei(Source, AL11.AL_SAMPLE_OFFSET, out sampleOffset);

					int state;
					AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out state);

					// If the source is not stopped, add the current offset to the total offset and return that.
					if (state != AL10.AL_STOPPED)
						return (sampleOffset + totalSamplesPlayed) / SampleRate;

					// If the source stopped, the current offset will have been reset to 0.
					// We'll need to dequeue any buffers played first and then return the total.
					DequeueBuffers();
					return totalSamplesPlayed / SampleRate;
				}
			}
		}

		public override bool Complete
		{
			get { return streamTask.IsCompleted; }
		}
	}
}
