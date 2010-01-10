using IjwFramework.Collections;
using IrrKlang;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;

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
		static bool paused;

		static ISoundSource LoadSound(string filename)
		{
			var data = AudLoader.LoadSound(FileSystem.Open(filename));
			return soundEngine.AddSoundSourceFromPCMData(data, filename,
				new AudioFormat()
				{
					ChannelCount = 1,
					FrameCount = data.Length / 2,
					Format = SampleFormat.Signed16Bit,
					SampleRate = 22050
				});
		}

		public static void Initialize()
		{
			soundEngine = new ISoundEngine();
			sounds = new Cache<string, ISoundSource>(LoadSound);
			music = null;
			soundVolume = soundEngine.SoundVolume;
			musicVolume = soundEngine.SoundVolume;
		}

		public static void Play(string name)
		{
			var sound = sounds[name];
			// todo: positioning
			soundEngine.Play2D(sound, false /* loop */, false, false);
		}
		
		public static void PlayMusic(string name)
		{
			var sound = sounds[name];
			music = soundEngine.Play2D(sound, true /* loop */, false, false);
			music.Volume = musicVolume;
		}

		public static bool Paused
		{
			get { return paused; }
			set { paused = value; soundEngine.SetAllSoundsPaused(paused); }
		}

		public static float Volume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				soundEngine.SoundVolume = value;
			}
		}
		
		public static float MusicVolume
		{
			get { return musicVolume; }
			set {
				musicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}
					
		public static void SeekMusic(uint delta)
		{
			if (music != null)
			{
				music.PlayPosition += delta;
				if (music.PlayPosition < 0 || music.PlayPosition > music.PlayLength) 
					music.PlayPosition = 0;
			}
		}
		
		public static void PlayVoice(string phrase, Actor voicedUnit)
		{
			if (voicedUnit == null) return;

			var mi = voicedUnit.LegacyInfo as LegacyMobileInfo;
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
}
