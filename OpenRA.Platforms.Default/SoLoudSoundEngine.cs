using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SoLoud;

namespace OpenRA.Platforms.Default
{
	sealed class SoLoudSoundEngine : ISoundEngine
	{
		public float Volume { get { return engine.getGlobalVolume(); } set { engine.setGlobalVolume(value); } }
		Soloud engine;
		WPos listenerPos;

		public SoLoudSoundEngine()
		{
			engine = new Soloud();
			engine.init(aBackend: 4);
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new SoLoudSoundSource(data, channels, sampleBits, sampleRate);
		}

		public SoundDevice[] AvailableDevices()
		{
			return new[] { new SoundDevice(null, "default") };
		}

		public void Dispose()
		{
			engine.deinit();
		}

		public void PauseSound(ISound sound, bool paused)
		{
			var s = (SoLoudSound)sound;
		}

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			var soundSource = (SoLoudSoundSource)sound;

			if (relative)
				soundSource.Wave.set3dListenerRelative(Convert.ToInt32(relative));
			var handle = engine.play3d(soundSource.Wave, pos.X / 1024, pos.Y / 1024, 0);
			engine.update3dAudio();
			return new SoLoudSound(engine, soundSource, handle, loop, relative, pos, volume, attenuateVolume);
		}

		public ISound Play2DStream(Stream stream, int channels, int sampleBits, int sampleRate, bool loop, bool relative, WPos pos, float volume)
		{
			var soundSource = new SoLoudSoundSource(stream.ReadAllBytes(), channels, sampleBits, sampleRate);
			return new SoLoudSound(engine, soundSource, engine.play(soundSource.Wave), loop, relative, pos, volume, false);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			engine.setPauseAll(Convert.ToInt32(paused));
		}

		public void SetListenerPosition(WPos position)
		{
			engine.set3dListenerPosition(position.X / 1024f, position.Y / 1024f, (position.Z + 2133) / 1024);
			engine.set3dListenerAt(0, 0, -1);
			listenerPos = position;
			engine.update3dAudio();
		}

		public void SetSoundVolume(float volume, ISound sound)
		{
			if (sound != null)
				((SoLoudSound)sound).Volume = volume;
		}

		public void StopAllSounds()
		{
			engine.stopAll();
		}

		public void StopSource(ISoundSource sounds)
		{
			engine.stopAudioSource(((SoLoudSoundSource)sounds).Wave);
		}

		public void StopSound(ISound sound)
		{
			engine.stop(((SoLoudSound)sound).Handle);
		}
	}

	sealed class SoLoudSound : ISound
	{
		WPos position;
		float volume;
		public uint Handle { get; private set; }
		Soloud engine;
		SoLoudSoundSource source;

		public float Volume
		{
			get
			{
				return volume;
			}

			set
			{
				volume = value;
				if (engine != null)
					engine.setVolume(Handle, value);
			}
		}

		public SoLoudSound(Soloud engine, SoLoudSoundSource source, uint handle, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			this.Handle = handle;
			this.position = pos;
			this.engine = engine;
			this.source = source;
			Volume = volume;
			engine.update3dAudio();
			engine.setLooping(Handle, Convert.ToInt32(loop));
			engine.set3dSourceMinMaxDistance(Handle, 10f, 1024 * 1024);
			engine.update3dAudio();
			engine.update3dAudio();
			if (attenuateVolume)
				engine.set3dSourceAttenuation(Handle, 1, 1f);

			engine.update3dAudio();
		}

		public float SeekPosition { get { return 0f; } }

		public bool Complete { get { return engine == null || !Convert.ToBoolean(engine.isValidVoiceHandle(Handle)); } }

		public void SetPosition(WPos pos)
		{
			position = pos;

			if (engine != null)
			{
				engine.set3dSourcePosition(Handle, pos.X / 1024, pos.Y / 1024, 0);
				engine.update3dAudio();
			}
		}
	}

	sealed class SoLoudSoundSource : ISoundSource
	{
		public Wav Wave { get; private set; }

		public SoLoudSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			var wav = Encoding.GetEncoding("UTF-8").GetBytes("RIFF    WAVEfmt ")
				.Concat(BitConverter.GetBytes(16))
				.Concat(BitConverter.GetBytes((short)1))
				.Concat(BitConverter.GetBytes((short)channels))
				.Concat(BitConverter.GetBytes(sampleRate))
				.Concat(new byte[6])
				.Concat(BitConverter.GetBytes((short)sampleBits))
				.Concat(Encoding.GetEncoding("UTF-8").GetBytes("data"))
				.Concat(BitConverter.GetBytes(data.Length))
				.Concat(data).ToArray();
			var length = wav.Length;
			var samples = Marshal.AllocHGlobal(length);
			Marshal.Copy(wav, 0, samples, length);

			Wave = new Wav();
			Wave.loadMem(samples, (uint)length, 1, 1);

			Marshal.FreeHGlobal(samples);

			Wave.setInaudibleBehavior(Convert.ToInt32(false), Convert.ToInt32(true));
		}

		public void Dispose()
		{
			Wave.stop();
		}
	}

	/* TODO: Bridge actual streaming
		class SoLoudStream : ISoundSource
		{
			public float Volume
			{
				get
				{
					throw new NotImplementedException();
				}

				set
				{
					throw new NotImplementedException();
				}
			}

			public float SeekPosition => throw new NotImplementedException();

			public bool Complete { get { return sound. } }

			WavStream sound;

			public SoLoudSound()
			{
				sound = new WavStream();
			}

			public void SetPosition(WPos pos)
			{
				throw new NotImplementedException();
			}
		}
		*/
}
