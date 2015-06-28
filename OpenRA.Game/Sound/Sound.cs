#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA
{
	public static class Sound
	{
		public static ISoundEngine Engine;

		static Cache<string, ISoundSource> sounds;
		static ISoundSource rawSource;
		static ISound music;
		static ISound video;
		static MusicInfo currentMusic;

		static ISoundSource LoadSound(string filename)
		{
			if (!GlobalFileSystem.Exists(filename))
			{
				Log.Write("sound", "LoadSound, file does not exist: {0}", filename);
				return null;
			}

			if (filename.ToLowerInvariant().EndsWith("wav"))
				using (var s = GlobalFileSystem.Open(filename))
					return LoadWave(new WavLoader(s));

			using (var s = GlobalFileSystem.Open(filename))
				return LoadSoundRaw(AudLoader.LoadSound(s), 1, 16, 22050);
		}

		static ISoundSource LoadWave(WavLoader wave)
		{
			return Engine.AddSoundSourceFromMemory(wave.RawOutput, wave.Channels, wave.BitsPerSample, wave.SampleRate);
		}

		static ISoundSource LoadSoundRaw(byte[] rawData, int channels, int sampleBits, int sampleRate)
		{
			return Engine.AddSoundSourceFromMemory(rawData, channels, sampleBits, sampleRate);
		}

		public static void Initialize()
		{
			sounds = new Cache<string, ISoundSource>(LoadSound);
			music = null;
			currentMusic = null;
			video = null;
		}

		public static SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice("Default", null, "Default Output"),
				new SoundDevice("Null", null, "Output Disabled")
			};

			return defaultDevices;
		}

		public static void SetListenerPosition(WPos position)
		{
			Engine.SetListenerPosition(position);
		}

		static ISound Play(Player player, string name, bool headRelative, WPos pos, float volumeModifier = 1f, bool loop = false)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			if (player != null && player != player.World.LocalPlayer)
				return null;

			return Engine.Play2D(sounds[name],
				loop, headRelative, pos,
				InternalSoundVolume * volumeModifier, true);
		}

		public static ISound Play(string name) { return Play(null, name, true, WPos.Zero, 1f); }
		public static ISound Play(string name, WPos pos) { return Play(null, name, false, pos, 1f); }
		public static ISound Play(string name, float volumeModifier) { return Play(null, name, true, WPos.Zero, volumeModifier); }
		public static ISound Play(string name, WPos pos, float volumeModifier) { return Play(null, name, false, pos, volumeModifier); }
		public static ISound PlayToPlayer(Player player, string name) { return Play(player, name, true, WPos.Zero, 1f); }
		public static ISound PlayToPlayer(Player player, string name, WPos pos) { return Play(player, name, false, pos, 1f); }
		public static ISound PlayLooped(string name) { return PlayLooped(name, WPos.Zero); }
		public static ISound PlayLooped(string name, WPos pos) { return Play(null, name, true, pos, 1f, true); }

		public static void PlayVideo(byte[] raw, int channels, int sampleBits, int sampleRate)
		{
			rawSource = LoadSoundRaw(raw, channels, sampleBits, sampleRate);
			video = Engine.Play2D(rawSource, false, true, WPos.Zero, InternalSoundVolume, false);
		}

		public static void PlayVideo()
		{
			if (video != null)
				Engine.PauseSound(video, false);
		}

		public static void PauseVideo()
		{
			if (video != null)
				Engine.PauseSound(video, true);
		}

		public static void StopVideo()
		{
			if (video != null)
				Engine.StopSound(video);
		}

		public static void Tick()
		{
			// Song finished
			if (MusicPlaying && !music.Playing)
			{
				StopMusic();
				onMusicComplete();
			}
		}

		static Action onMusicComplete;
		public static bool MusicPlaying { get; private set; }
		public static MusicInfo CurrentMusic { get { return currentMusic; } }

		public static void PlayMusic(MusicInfo m)
		{
			PlayMusicThen(m, () => { });
		}

		public static void PlayMusicThen(MusicInfo m, Action then)
		{
			if (m == null || !m.Exists)
				return;

			onMusicComplete = then;

			if (m == currentMusic && music != null)
			{
				Engine.PauseSound(music, false);
				MusicPlaying = true;
				return;
			}

			StopMusic();

			var sound = sounds[m.Filename];
			if (sound == null)
				return;

			music = Engine.Play2D(sound, false, true, WPos.Zero, MusicVolume, false);
			currentMusic = m;
			MusicPlaying = true;
		}

		public static void PlayMusic()
		{
			if (music == null)
				return;

			MusicPlaying = true;
			Engine.PauseSound(music, false);
		}

		public static void StopSound(ISound sound)
		{
			if (sound != null)
				Engine.StopSound(sound);
		}

		public static void StopMusic()
		{
			if (music != null)
				Engine.StopSound(music);

			MusicPlaying = false;
			currentMusic = null;
		}

		public static void PauseMusic()
		{
			if (music == null)
				return;

			MusicPlaying = false;
			Engine.PauseSound(music, true);
		}

		public static float GlobalVolume
		{
			get { return Engine.Volume; }
			set { Engine.Volume = value; }
		}

		static float soundVolumeModifier = 1.0f;
		public static float SoundVolumeModifier
		{
			get
			{
				return soundVolumeModifier;
			}

			set
			{
				soundVolumeModifier = value;
				Engine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		static float InternalSoundVolume { get { return SoundVolume * soundVolumeModifier; } }
		public static float SoundVolume
		{
			get
			{
				return Game.Settings.Sound.SoundVolume;
			}

			set
			{
				Game.Settings.Sound.SoundVolume = value;
				Engine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		public static float MusicVolume
		{
			get
			{
				return Game.Settings.Sound.MusicVolume;
			}

			set
			{
				Game.Settings.Sound.MusicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}

		public static float VideoVolume
		{
			get
			{
				return Game.Settings.Sound.VideoVolume;
			}

			set
			{
				Game.Settings.Sound.VideoVolume = value;
				if (video != null)
					video.Volume = value;
			}
		}

		public static float MusicSeekPosition
		{
			get { return music != null ? music.SeekPosition : 0; }
		}

		public static float VideoSeekPosition
		{
			get { return video != null ? video.SeekPosition : 0; }
		}

		// Returns true if played successfully
		public static bool PlayPredefined(Ruleset ruleset, Player p, Actor voicedActor, string type, string definition, string variant,
			bool relative, WPos pos, float volumeModifier, bool attenuateVolume)
		{
			if (ruleset == null)
				throw new ArgumentNullException("ruleset");

			if (definition == null)
				return false;

			if (ruleset.Voices == null || ruleset.Notifications == null)
				return false;

			var rules = (voicedActor != null) ? ruleset.Voices[type] : ruleset.Notifications[type];
			if (rules == null)
				return false;

			var id = voicedActor != null ? voicedActor.ActorID : 0;

			string clip;
			var suffix = rules.DefaultVariant;
			var prefix = rules.DefaultPrefix;

			if (voicedActor != null)
			{
				if (!rules.VoicePools.Value.ContainsKey(definition))
					throw new InvalidOperationException("Can't find {0} in voice pool.".F(definition));

				clip = rules.VoicePools.Value[definition].GetNext();
			}
			else
			{
				if (!rules.NotificationsPools.Value.ContainsKey(definition))
					throw new InvalidOperationException("Can't find {0} in notification pool.".F(definition));

				clip = rules.NotificationsPools.Value[definition].GetNext();
			}

			if (string.IsNullOrEmpty(clip))
				return false;

			if (variant != null)
			{
				if (rules.Variants.ContainsKey(variant) && !rules.DisableVariants.Contains(definition))
					suffix = rules.Variants[variant][id % rules.Variants[variant].Length];
				if (rules.Prefixes.ContainsKey(variant) && !rules.DisablePrefixes.Contains(definition))
					prefix = rules.Prefixes[variant][id % rules.Prefixes[variant].Length];
			}

			var name = prefix + clip + suffix;

			if (!string.IsNullOrEmpty(name) && (p == null || p == p.World.LocalPlayer))
				Engine.Play2D(sounds[name],
					false, relative, pos,
					InternalSoundVolume * volumeModifier, attenuateVolume);

			return true;
		}

		public static bool PlayNotification(Ruleset rules, Player player, string type, string notification, string variant)
		{
			if (rules == null)
				throw new ArgumentNullException("rules");

			if (type == null || notification == null)
				return false;

			return PlayPredefined(rules, player, null, type.ToLowerInvariant(), notification, variant, true, WPos.Zero, 1f, false);
		}
	}
}
