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
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface ISoundLoader
	{
		bool TryParseSound(Stream stream, out ISoundFormat sound);
	}

	public interface ISoundFormat : IDisposable
	{
		int Channels { get; }
		int SampleBits { get; }
		int SampleRate { get; }
		float LengthInSeconds { get; }
		Stream GetPCMInputStream();
	}

	public enum SoundType { World, UI }

	public sealed class Sound : IDisposable
	{
		readonly ISoundEngine soundEngine;
		ISoundLoader[] loaders;
		IReadOnlyFileSystem fileSystem;
		Cache<string, ISoundSource> sounds;
		ISoundSource videoSource;
		ISound music;
		ISound video;
		MusicInfo currentMusic;
		readonly Dictionary<uint, ISound> currentSounds = new Dictionary<uint, ISound>();
		readonly Dictionary<string, ISound> currentNotifications = new Dictionary<string, ISound>();
		public bool DummyEngine { get; }

		public Sound(IPlatform platform, SoundSettings soundSettings)
		{
			soundEngine = platform.CreateSound(soundSettings.Device);
			DummyEngine = soundEngine.Dummy;

			if (soundSettings.Mute)
				MuteAudio();
		}

		T LoadSound<T>(string filename, Func<ISoundFormat, T> loadFormat)
		{
			if (!fileSystem.Exists(filename))
			{
				Log.Write("sound", "LoadSound, file does not exist: {0}", filename);
				return default;
			}

			using (var stream = fileSystem.Open(filename))
			{
				foreach (var loader in loaders)
				{
					stream.Position = 0;
					if (loader.TryParseSound(stream, out var soundFormat))
					{
						var source = loadFormat(soundFormat);
						soundFormat.Dispose();
						return source;
					}
				}
			}

			throw new InvalidDataException(filename + " is not a valid sound file!");
		}

		public void Initialize(ISoundLoader[] loaders, IReadOnlyFileSystem fileSystem)
		{
			StopMusic();
			soundEngine.StopAllSounds();

			if (sounds != null)
				foreach (var soundSource in sounds.Values)
					soundSource?.Dispose();

			this.loaders = loaders;
			this.fileSystem = fileSystem;
			Func<ISoundFormat, ISoundSource> loadIntoMemory = soundFormat => soundEngine.AddSoundSourceFromMemory(
				soundFormat.GetPCMInputStream().ReadAllBytes(), soundFormat.Channels, soundFormat.SampleBits, soundFormat.SampleRate);
			sounds = new Cache<string, ISoundSource>(filename => LoadSound(filename, loadIntoMemory));
			currentSounds.Clear();
			currentNotifications.Clear();
			video = null;
		}

		public SoundDevice[] AvailableDevices()
		{
			return soundEngine.AvailableDevices();
		}

		public void SetListenerPosition(WPos position)
		{
			soundEngine.SetListenerPosition(position);
		}

		ISound Play(SoundType type, Player player, string name, bool headRelative, WPos pos, float volumeModifier = 1f, bool loop = false)
		{
			if (string.IsNullOrEmpty(name) || DisableAllSounds || (DisableWorldSounds && type == SoundType.World))
				return null;

			if (player != null && player != player.World.LocalPlayer)
				return null;

			return soundEngine.Play2D(sounds[name],
				loop, headRelative, pos,
				InternalSoundVolume * volumeModifier, true);
		}

		public void StopAudio()
		{
			soundEngine.StopAllSounds();
		}

		public void EndLoop(ISound sound)
		{
			soundEngine.SetSoundLooping(false, sound);
		}

		public void MuteAudio()
		{
			soundEngine.Volume = 0f;
		}

		public void UnmuteAudio()
		{
			soundEngine.Volume = 1f;
		}

		public void SetMusicLooped(bool loop)
		{
			Game.Settings.Sound.Repeat = loop;
			soundEngine.SetSoundLooping(loop, music);
		}

		public bool DisableAllSounds { get; set; }
		public bool DisableWorldSounds { get; set; }
		public ISound Play(SoundType type, string name) { return Play(type, null, name, true, WPos.Zero, 1f); }
		public ISound Play(SoundType type, string name, WPos pos) { return Play(type, null, name, false, pos, 1f); }
		public ISound Play(SoundType type, string name, float volumeModifier) { return Play(type, null, name, true, WPos.Zero, volumeModifier); }
		public ISound Play(SoundType type, string name, WPos pos, float volumeModifier) { return Play(type, null, name, false, pos, volumeModifier); }
		public ISound PlayToPlayer(SoundType type, Player player, string name) { return Play(type, player, name, true, WPos.Zero, 1f); }
		public ISound PlayToPlayer(SoundType type, Player player, string name, WPos pos) { return Play(type, player, name, false, pos, 1f); }
		public ISound PlayLooped(SoundType type, string name) { return Play(type, null, name, true, WPos.Zero, 1f, true); }
		public ISound PlayLooped(SoundType type, string name, WPos pos) { return Play(type, null, name, false, pos, 1f, true); }

		public ISound Play(SoundType type, string[] names, World world, Player player = null, float volumeModifier = 1f)
		{
			return Play(type, player, names.Random(world.LocalRandom), true, WPos.Zero, volumeModifier);
		}

		public ISound Play(SoundType type, string[] names, World world, WPos pos, Player player = null, float volumeModifier = 1f)
		{
			return Play(type, player, names.Random(world.LocalRandom), false, pos, volumeModifier);
		}

		public ISound Play(ISoundFormat soundFormat) => Play(soundFormat, MusicVolume);

		public ISound Play(ISoundFormat soundFormat, float volume)
		{
			return soundEngine.Play2DStream(soundFormat.GetPCMInputStream(), soundFormat.Channels, soundFormat.SampleBits, soundFormat.SampleRate,
				false, true, WPos.Zero, volume);
		}

		public void PlayVideo(byte[] raw, int channels, int sampleBits, int sampleRate)
		{
			StopVideo();
			videoSource = soundEngine.AddSoundSourceFromMemory(raw, channels, sampleBits, sampleRate);
			video = soundEngine.Play2D(videoSource, false, true, WPos.Zero, InternalSoundVolume, false);
		}

		public void PlayVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, false);
		}

		public void PauseVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, true);
		}

		public void StopVideo()
		{
			if (video != null)
			{
				soundEngine.StopSound(video);
				videoSource.Dispose();
				videoSource = null;
				video = null;
			}
		}

		public void Tick()
		{
			// Song finished
			if (MusicPlaying && music.Complete)
			{
				StopMusic();
				onMusicComplete();
			}
		}

		Action onMusicComplete;
		public bool MusicPlaying { get; private set; }
		public MusicInfo CurrentMusic => currentMusic;

		public void PlayMusicThen(MusicInfo m, Action then)
		{
			if (m == null || !m.Exists)
				return;

			onMusicComplete = then;

			if (m == currentMusic && music != null)
			{
				soundEngine.PauseSound(music, false);
				MusicPlaying = true;
				return;
			}

			PlayMusic(m, Game.Settings.Sound.Repeat);
		}

		public void PlayMusic(MusicInfo m, bool looped = false)
		{
			if (m == null || !m.Exists)
				return;

			StopMusic();

			Func<ISoundFormat, ISound> stream = soundFormat => soundEngine.Play2DStream(
				soundFormat.GetPCMInputStream(), soundFormat.Channels, soundFormat.SampleBits, soundFormat.SampleRate,
				looped, true, WPos.Zero, MusicVolume * m.VolumeModifier);

			music = LoadSound(m.Filename, stream);
			if (music == null)
			{
				onMusicComplete = null;
				return;
			}

			currentMusic = m;
			MusicPlaying = true;
		}

		public void PlayMusic()
		{
			if (music == null)
				return;

			MusicPlaying = true;
			soundEngine.PauseSound(music, false);
		}

		public void StopSound(ISound sound)
		{
			if (sound != null)
				soundEngine.StopSound(sound);
		}

		public void StopMusic()
		{
			if (music != null)
			{
				soundEngine.StopSound(music);
				music = null;
			}

			currentMusic = null;
			MusicPlaying = false;
		}

		public void PauseMusic()
		{
			if (music == null)
				return;

			MusicPlaying = false;
			soundEngine.PauseSound(music, true);
		}

		float soundVolumeModifier = 1.0f;
		public float SoundVolumeModifier
		{
			get => soundVolumeModifier;

			set
			{
				soundVolumeModifier = value;
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		float InternalSoundVolume => SoundVolume * soundVolumeModifier;

		public float SoundVolume
		{
			get => Game.Settings.Sound.SoundVolume;

			set
			{
				Game.Settings.Sound.SoundVolume = value;
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		public float MusicVolume
		{
			get => Game.Settings.Sound.MusicVolume;

			set
			{
				Game.Settings.Sound.MusicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}

		public float VideoVolume
		{
			get => Game.Settings.Sound.VideoVolume;

			set
			{
				Game.Settings.Sound.VideoVolume = value;
				if (video != null)
					video.Volume = value;
			}
		}

		public float MusicSeekPosition => music?.SeekPosition ?? 0;

		public float VideoSeekPosition => video?.SeekPosition ?? 0;

		// Returns true if played successfully
		public bool PlayPredefined(SoundType soundType, Ruleset ruleset, Player p, Actor voicedActor, string type, string definition, string variant,
			bool relative, WPos pos, float volumeModifier, bool attenuateVolume)
		{
			if (ruleset == null)
				throw new ArgumentNullException(nameof(ruleset));

			if (definition == null || DisableAllSounds || (DisableWorldSounds && soundType == SoundType.World))
				return false;

			if (ruleset.Voices == null || ruleset.Notifications == null)
				return false;

			var rules = voicedActor != null ? ruleset.Voices[type] : ruleset.Notifications[type];
			if (rules == null)
				return false;

			var id = voicedActor?.ActorID ?? 0;

			SoundPool pool;
			var suffix = rules.DefaultVariant;
			var prefix = rules.DefaultPrefix;

			if (voicedActor != null)
			{
				if (!rules.VoicePools.Value.ContainsKey(definition))
					throw new InvalidOperationException($"Can't find {definition} in voice pool.");

				pool = rules.VoicePools.Value[definition];
			}
			else
			{
				if (!rules.NotificationsPools.Value.ContainsKey(definition))
					throw new InvalidOperationException($"Can't find {definition} in notification pool.");

				pool = rules.NotificationsPools.Value[definition];
			}

			var clip = pool.GetNext();
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
			var actorId = voicedActor != null && voicedActor.World.Selection.Contains(voicedActor) ? 0 : id;

			if (!string.IsNullOrEmpty(name) && (p == null || p == p.World.LocalPlayer))
			{
				ISound PlaySound()
				{
					var volume = InternalSoundVolume * volumeModifier * pool.VolumeModifier;
					return soundEngine.Play2D(sounds[name], false, relative, pos, volume, attenuateVolume);
				}

				if (pool.Type == SoundPool.InterruptType.Overlap)
				{
					if (PlaySound() == null)
						return false;
				}
				else if (voicedActor == null)
				{
					if (currentNotifications.TryGetValue(name, out var currentNotification) && !currentNotification.Complete)
					{
						if (pool.Type == SoundPool.InterruptType.Interrupt)
							soundEngine.StopSound(currentNotification);
						else if (pool.Type == SoundPool.InterruptType.DoNotPlay)
							return false;
					}

					var sound = PlaySound();
					if (sound == null)
						return false;
					else
						currentNotifications[name] = sound;
				}
				else
				{
					if (currentSounds.TryGetValue(actorId, out var currentSound) && !currentSound.Complete)
					{
						if (pool.Type == SoundPool.InterruptType.Interrupt)
							soundEngine.StopSound(currentSound);
						else if (pool.Type == SoundPool.InterruptType.DoNotPlay)
							return false;
					}

					var sound = PlaySound();
					if (sound == null)
						return false;
					else
						currentSounds[actorId] = sound;
				}
			}

			return true;
		}

		public bool PlayNotification(Ruleset rules, Player player, string type, string notification, string variant)
		{
			if (rules == null)
				throw new ArgumentNullException(nameof(rules));

			if (type == null || notification == null)
				return false;

			return PlayPredefined(SoundType.UI, rules, player, null, type.ToLowerInvariant(), notification, variant, true, WPos.Zero, 1f, false);
		}

		public void Dispose()
		{
			StopAudio();
			if (sounds != null)
				foreach (var soundSource in sounds.Values)
					soundSource?.Dispose();

			soundEngine.Dispose();
		}
	}
}
