using System;
using System.Collections.Generic;
using OpenRa.FileFormats;
using OpenRa.Support;
using OpenRa.Traits;

namespace OpenRa
{
	public static class Sound
	{
		static ISoundEngine soundEngine;
		static Cache<string, ISoundSource> sounds;
		static ISound music;

		//TODO: read these from somewhere?
		static float soundVolume;
		static float musicVolume;
	//	static bool paused;

		static ISoundSource LoadSound(string filename)
		{
			var data = AudLoader.LoadSound(FileSystem.Open(filename));
			return soundEngine.AddSoundSourceFromMemory(data, 1, 16, 22050);
		}

		public static void Initialize()
		{
			soundEngine = new OpenAlSoundEngine();
			sounds = new Cache<string, ISoundSource>(LoadSound);
			music = null;
			soundVolume = soundEngine.Volume;
			musicVolume = soundEngine.Volume;
		}

		public static void Play(string name)
		{
			var sound = sounds[name];
			// todo: positioning
			soundEngine.Play2D(sound, false);
		}

		public static void PlayToPlayer(Player player, string name)
		{
			if( player == player.World.LocalPlayer )
				Play( name );
		}

		public static void PlayMusic(string name)
		{
			var sound = sounds[name];
			music = soundEngine.Play2D(sound, true);
			music.Volume = musicVolume;
		}

		//public static bool Paused
		//{
		//    get { return paused; }
		//    set { paused = value; soundEngine.SetAllSoundsPaused(paused); }
		//}

		public static float Volume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				soundEngine.Volume = value;
			}
		}

		public static float MusicVolume
		{
			get { return musicVolume; }
			set
			{
				musicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}
					
		//public static void SeekMusic(uint delta)
		//{
		//    if (music != null)
		//    {
		//        music.PlayPosition += delta;
		//        if (music.PlayPosition < 0 || music.PlayPosition > music.PlayLength) 
		//            music.PlayPosition = 0;
		//    }
		//}
		
		public static void PlayVoice(string phrase, Actor voicedUnit)
		{
			if (voicedUnit == null) return;

			var mi = voicedUnit.Info.Traits.GetOrDefault<SelectableInfo>();
			if (mi == null) return;

			var vi = Rules.VoiceInfo[mi.Voice];

			var clip = vi.Pools.Value[phrase].GetNext();
			if (clip == null)
				return;

			if (clip.Contains("."))		/* no variants! */
			{
				Play(clip);
				return;
			}

			// todo: fix this
			var variants = (voicedUnit.Owner.Country.Race == "allies")
				? vi.AlliedVariants : vi.SovietVariants;

			var variant = variants[voicedUnit.ActorID % variants.Length];

			Play(clip + variant);
		}
	}

	interface ISoundEngine
	{
		ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate);
		ISound Play2D(ISoundSource sound, bool loop);
		float Volume { get; set; }
	}

	interface ISoundSource {}
	interface ISound
	{
		float Volume { get; set; }
	}

	class OpenAlSoundEngine : ISoundEngine
	{
		float volume = 1f;
		Dictionary<int, bool> sourcePool = new Dictionary<int, bool>();
		const int POOL_SIZE = 32;

		public OpenAlSoundEngine()
		{
			//var str = Alc.alcGetString(IntPtr.Zero, Alc.ALC_DEFAULT_DEVICE_SPECIFIER);
			var dev = OpenAlInterop.alcOpenDevice(IntPtr.Zero);
			if (dev == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL device");
			var ctx = OpenAlInterop.alcCreateContext(dev, IntPtr.Zero);
			if (ctx == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL context");
			OpenAlInterop.alcMakeContextCurrent(ctx);

			for (var i = 0; i < POOL_SIZE; i++)
			{
				var source = 0;
				OpenAlInterop.alGenSources(1, out source);
				if (0 != OpenAlInterop.alGetError())
					throw new InvalidOperationException("failed generating source {0}".F(i));
				sourcePool.Add(source, false);
			}
		}

		int GetSourceFromPool()
		{
			foreach (var kvp in sourcePool)
			{
				if (!kvp.Value)
				{
					sourcePool[kvp.Key] = true;
					return kvp.Key;
				}
			}

			List<int> freeSources = new List<int>();
			foreach (int key in sourcePool.Keys)
			{
				int state;
				OpenAlInterop.alGetSourcei(key, OpenAlInterop.AL_SOURCE_STATE, out state);
				if (state != OpenAlInterop.AL_PLAYING)
					freeSources.Add(key);
			}

			if (freeSources.Count == 0)
				return -1;

			foreach (int i in freeSources)
				sourcePool[i] = false;

			sourcePool[freeSources[0]] = true;

			return freeSources[0];
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
		}

		public ISound Play2D(ISoundSource sound, bool loop)
		{
			int source = GetSourceFromPool();
			return new OpenAlSound(source, (sound as OpenAlSoundSource).buffer, loop);
		}

		public float Volume
		{
			get { return volume; }
			set { OpenAlInterop.alListenerf(OpenAlInterop.AL_GAIN, volume = value); }
		}
	}

	class OpenAlSoundSource : ISoundSource
	{
		public readonly int buffer;

		static int MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? OpenAlInterop.AL_FORMAT_MONO16 : OpenAlInterop.AL_FORMAT_MONO8;
			else
				return bits == 16 ? OpenAlInterop.AL_FORMAT_STEREO16 : OpenAlInterop.AL_FORMAT_STEREO8;
		}

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			OpenAlInterop.alGenBuffers(1, out buffer);
			OpenAlInterop.alBufferData(buffer, MakeALFormat(channels, sampleBits), data, data.Length, sampleRate);
		}
	}

	class OpenAlSound : ISound
	{
		public readonly int source = -1;
		float volume = 1f;

		public OpenAlSound(int source, int buffer, bool looping)
		{
			if (source == -1) return;
			this.source = source;
			OpenAlInterop.alSourcef(source, OpenAlInterop.AL_PITCH, 1f);
			OpenAlInterop.alSourcef(source, OpenAlInterop.AL_GAIN, 1f);
			OpenAlInterop.alSource3f(source, OpenAlInterop.AL_POSITION, 0f, 0f, 0f);
			OpenAlInterop.alSource3f(source, OpenAlInterop.AL_VELOCITY, 0f, 0f, 0f);
			OpenAlInterop.alSourcei(source, OpenAlInterop.AL_BUFFER, buffer);
			OpenAlInterop.alSourcei(source, OpenAlInterop.AL_LOOPING, looping ? OpenAlInterop.AL_TRUE : OpenAlInterop.AL_FALSE);
			OpenAlInterop.alSourcePlay(source);
		}

		public float Volume
		{
			get { return volume; }
			set 
			{
				if (source != -1)
					OpenAlInterop.alSourcef(source, OpenAlInterop.AL_GAIN, volume = value); 
			}
		}
	}
}
