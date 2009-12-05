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
		}

		public static void Play(string name)
		{
			var sound = sounds[name];
			// todo: positioning
			soundEngine.Play2D(sound, false /* loop */, false, false);
		}

		public static void PlayVoice(string phrase, Actor voicedUnit)
		{
			if (voicedUnit == null) return;

			var mi = voicedUnit.Info as MobileInfo;
			if (mi == null) return;

			var vi = Rules.VoiceInfo[mi.Voice];

			var clip = vi.Pools.Value[phrase].GetNext();
			if (clip == null)
				return;

			var variants = (voicedUnit.Owner.Race == Race.Soviet)
							? vi.SovietVariants : vi.AlliedVariants;

			var variant = variants[voicedUnit.ActorID % variants.Length];

			Play(clip + variant);
		}
	}
}
