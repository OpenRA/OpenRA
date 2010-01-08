using IjwFramework.Collections;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using Tao.OpenAl;
using System;

namespace OpenRa.Game
{
	static class Sound
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

			var mi = voicedUnit.Info as MobileInfo;
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

			var variants = (voicedUnit.Owner.Race == Race.Soviet)
							? vi.SovietVariants : vi.AlliedVariants;

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

		public OpenAlSoundEngine()
		{
			var str = Alc.alcGetString(IntPtr.Zero, Alc.ALC_DEFAULT_DEVICE_SPECIFIER);
			var dev = Alc.alcOpenDevice(str);
			if (dev == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL device");
			var ctx = Alc.alcCreateContext(dev, (int[]) null);
			Alc.alcMakeContextCurrent(ctx);
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
		}

		public ISound Play2D(ISoundSource sound, bool loop)
		{
			return new OpenAlSound((sound as OpenAlSoundSource).buffer, loop);
		}

		public float Volume
		{
			get { return volume; }
			set { Al.alListenerf(Al.AL_GAIN, volume = value); }
		}
	}

	class OpenAlSoundSource : ISoundSource
	{
		public readonly int buffer;

		static int MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? Al.AL_FORMAT_MONO16 : Al.AL_FORMAT_MONO8;
			else
				return bits == 16 ? Al.AL_FORMAT_STEREO16 : Al.AL_FORMAT_STEREO8;
		}

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			Al.alGenBuffers(1, out buffer);
			Al.alBufferData(buffer, MakeALFormat(channels, sampleBits), data, data.Length, sampleRate);
		}
	}

	class OpenAlSound : ISound
	{
		public readonly int source;
		float volume = 1f;

		public OpenAlSound(int buffer, bool looping)
		{
			Al.alGenSources(1, out source);
			Al.alSourcef(source, Al.AL_PITCH, 1f);
			Al.alSourcef(source, Al.AL_GAIN, 1f);
			Al.alSource3f(source, Al.AL_POSITION, 0f, 0f, 0f);
			Al.alSource3f(source, Al.AL_VELOCITY, 0f, 0f, 0f);
			Al.alSourcei(source, Al.AL_BUFFER, buffer);
			Al.alSourcei(source, Al.AL_LOOPING, looping ? Al.AL_TRUE : Al.AL_FALSE);
			Al.alSourcePlay(source);
		}

		public float Volume
		{
			get { return volume; }
			set { Al.alSourcef(source, Al.AL_GAIN, volume = value); }
		}
	}
}
