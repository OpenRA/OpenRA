#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using Tao.OpenAl;

namespace OpenRA
{
	public static class Sound
	{
		static ISoundEngine soundEngine;
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
				return LoadWave(new WavLoader(GlobalFileSystem.Open(filename)));

			return LoadSoundRaw(AudLoader.LoadSound(GlobalFileSystem.Open(filename)));
		}

		static ISoundSource LoadWave(WavLoader wave)
		{
			return soundEngine.AddSoundSourceFromMemory(wave.RawOutput, wave.Channels, wave.BitsPerSample, wave.SampleRate);
		}

		static ISoundSource LoadSoundRaw(byte[] rawData)
		{
			return soundEngine.AddSoundSourceFromMemory(rawData, 1, 16, 22050);
		}

		static ISoundEngine CreateEngine(string engine)
		{
			engine = Game.Settings.Server.Dedicated ? "Null" : engine;
			switch (engine)
			{
			case "AL": return new OpenAlSoundEngine();
			case "Null": return new NullSoundEngine();

			default:
				throw new InvalidOperationException("Unsupported sound engine: {0}".F(engine));
			}
		}

		public static void Create(string engine)
		{
			soundEngine = CreateEngine(engine);
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
				new SoundDevice("AL", null, "Default Output"),
				new SoundDevice("Null", null, "Output Disabled")
			};

			var devices = OpenAlSoundEngine.AvailableDevices()
				.Select(d => new SoundDevice("AL", d, d));

			return defaultDevices.Concat(devices).ToArray();
		}

		public static void SetListenerPosition(WPos position)
		{
			soundEngine.SetListenerPosition(position);
		}

		static ISound Play(Player player, string name, bool headRelative, WPos pos, float volumeModifier)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			if (player != null && player != player.World.LocalPlayer)
				return null;

			return soundEngine.Play2D(sounds[name],
				false, headRelative, pos,
				InternalSoundVolume * volumeModifier, true);
		}

		public static ISound Play(string name) { return Play(null, name, true, WPos.Zero, 1); }
		public static ISound Play(string name, WPos pos) { return Play(null, name, false, pos, 1); }
		public static ISound Play(string name, float volumeModifier) { return Play(null, name, true, WPos.Zero, volumeModifier); }
		public static ISound Play(string name, WPos pos, float volumeModifier) { return Play(null, name, false, pos, volumeModifier); }
		public static ISound PlayToPlayer(Player player, string name) { return Play(player, name, true, WPos.Zero, 1); }
		public static ISound PlayToPlayer(Player player, string name, WPos pos) { return Play(player, name, false, pos, 1); }

		public static void PlayVideo(byte[] raw)
		{
			rawSource = LoadSoundRaw(raw);
			video = soundEngine.Play2D(rawSource, false, true, WPos.Zero, InternalSoundVolume, false);
		}

		public static void PlayVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, false);
		}

		public static void PauseVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, true);
		}

		public static void StopVideo()
		{
			if (video != null)
				soundEngine.StopSound(video);
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
				soundEngine.PauseSound(music, false);
				MusicPlaying = true;
				return;
			}

			StopMusic();

			var sound = sounds[m.Filename];
			if (sound == null)
				return;

			music = soundEngine.Play2D(sound, false, true, WPos.Zero, MusicVolume, false);
			currentMusic = m;
			MusicPlaying = true;
		}

		public static void PlayMusic()
		{
			if (music == null)
				return;

			MusicPlaying = true;
			soundEngine.PauseSound(music, false);
		}

		public static void StopSound(ISound sound)
		{
			if (sound != null)
				soundEngine.StopSound(sound);
		}

		public static void StopMusic()
		{
			if (music != null)
				soundEngine.StopSound(music);

			MusicPlaying = false;
			currentMusic = null;
		}

		public static void PauseMusic()
		{
			if (music == null)
				return;

			MusicPlaying = false;
			soundEngine.PauseSound(music, true);
		}

		public static float GlobalVolume
		{
			get { return soundEngine.Volume; }
			set { soundEngine.Volume = value; }
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
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
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
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
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
		public static bool PlayPredefined(Player p, Actor voicedUnit, string type, string definition, string variant, bool attenuateVolume)
		{
			if (definition == null)
				return false;

			if (Rules.Voices == null || Rules.Notifications == null)
				return false;

			var rules = (voicedUnit != null) ? Rules.Voices[type] : Rules.Notifications[type];
			if (rules == null)
				return false;

			var id = voicedUnit != null ? voicedUnit.ActorID : 0;

			string clip;
			var suffix = rules.DefaultVariant;
			var prefix = rules.DefaultPrefix;

			if (voicedUnit != null)
			{
				if (!rules.VoicePools.Value.ContainsKey("Attack"))
					rules.VoicePools.Value.Add("Attack", rules.VoicePools.Value["Move"]);

				if (!rules.VoicePools.Value.ContainsKey("AttackMove"))
					rules.VoicePools.Value.Add("AttackMove", rules.VoicePools.Value["Move"]);

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
				soundEngine.Play2D(sounds[name],
					false, true, WPos.Zero,
					InternalSoundVolume, attenuateVolume);

			return true;
		}

		public static bool PlayVoice(string phrase, Actor voicedUnit, string variant)
		{
			if (voicedUnit == null || phrase == null)
				return false;

			var mi = voicedUnit.Info.Traits.GetOrDefault<SelectableInfo>();
			if (mi == null || mi.Voice == null)
				return false;

			var type = mi.Voice.ToLowerInvariant();
			return PlayPredefined(null, voicedUnit, type, phrase, variant, true);
		}
		
		public static bool PlayVoiceLocal(string phrase, Actor voicedUnit, string variant, WPos pos)
		{
			if (voicedUnit == null || phrase == null)
				return false;

			var mi = voicedUnit.Info.Traits.GetOrDefault<SelectableInfo>();
			if (mi == null || mi.Voice == null)
				return false;

			var type = mi.Voice.ToLowerInvariant();
			return PlayPredefined(null, voicedUnit, type, phrase, variant, true);
		}

		public static bool PlayNotification(Player player, string type, string notification, string variant)
		{
			if (type == null || notification == null)
				return false;

			return PlayPredefined(player, null, type.ToLowerInvariant(), notification, variant, false);
		}
	}

	interface ISoundEngine
	{
		ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate);
		ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume);
		float Volume { get; set; }
		void PauseSound(ISound sound, bool paused);
		void StopSound(ISound sound);
		void SetAllSoundsPaused(bool paused);
		void StopAllSounds();
		void SetListenerPosition(WPos position);
		void SetSoundVolume(float volume, ISound music, ISound video);
	}

	public class SoundDevice
	{
		public readonly string Engine;
		public readonly string Device;
		public readonly string Label;

		public SoundDevice(string engine, string device, string label)
		{
			Engine = engine;
			Device = device;
			Label = label;

			// Limit label to 32 characters
			if (Label.Length > 32)
				Label = "..." + Label.Substring(Label.Length - 32);
		}
	}

	interface ISoundSource { }

	public interface ISound
	{
		float Volume { get; set; }
		float SeekPosition { get; }
		bool Playing { get; }
	}

	class OpenAlSoundEngine : ISoundEngine
	{
		class PoolSlot
		{
			public bool IsActive;
			public int FrameStarted;
			public WPos Pos;
			public bool IsRelative;
			public ISoundSource Sound;
		}

		const int MaxInstancesPerFrame = 3;
		const int GroupDistance = 2730;
		const int GroupDistanceSqr = GroupDistance * GroupDistance;
		const int PoolSize = 32;

		float volume = 1f;
		Dictionary<int, PoolSlot> sourcePool = new Dictionary<int, PoolSlot>();

		static string[] QueryDevices(string label, int type)
		{
			// Clear error bit
			Al.alGetError();

			var devices = Alc.alcGetStringv(IntPtr.Zero, type);
			if (Al.alGetError() != Al.AL_NO_ERROR)
			{
				Log.Write("sound", "Failed to query OpenAL device list using {0}", label);
				return new string[] { };
			}

			return devices;
		}

		public static string[] AvailableDevices()
		{
			// Returns all devices under windows vista and newer
			if (Alc.alcIsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATE_ALL_EXT") == Alc.ALC_TRUE)
				return QueryDevices("ALC_ENUMERATE_ALL_EXT", Alc.ALC_ALL_DEVICES_SPECIFIER);

			if (Alc.alcIsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATION_EXT") == Alc.ALC_TRUE)
				return QueryDevices("ALC_ENUMERATION_EXT", Alc.ALC_DEVICE_SPECIFIER);

			return new string[] { };
		}

		public OpenAlSoundEngine()
		{
			Console.WriteLine("Using OpenAL sound engine");

			if (Game.Settings.Sound.Device != null)
				Console.WriteLine("Using device `{0}`", Game.Settings.Sound.Device);
			else
				Console.WriteLine("Using default device");

			var dev = Alc.alcOpenDevice(Game.Settings.Sound.Device);
			if (dev == IntPtr.Zero)
			{
				Console.WriteLine("Failed to open device. Falling back to default");
				dev = Alc.alcOpenDevice(null);
				if (dev == IntPtr.Zero)
					throw new InvalidOperationException("Can't create OpenAL device");
			}

			var ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
			if (ctx == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL context");
			Alc.alcMakeContextCurrent(ctx);

			for (var i = 0; i < PoolSize; i++)
			{
				var source = 0;
				Al.alGenSources(1, out source);
				if (0 != Al.alGetError())
				{
					Log.Write("sound", "Failed generating OpenAL source {0}", i);
					return;
				}

				sourcePool.Add(source, new PoolSlot() { IsActive = false });
			}
		}

		int GetSourceFromPool()
		{
			foreach (var kvp in sourcePool)
			{
				if (!kvp.Value.IsActive)
				{
					sourcePool[kvp.Key].IsActive = true;
					return kvp.Key;
				}
			}

			List<int> freeSources = new List<int>();
			foreach (int key in sourcePool.Keys)
			{
				int state;
				Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
				if (state != Al.AL_PLAYING && state != Al.AL_PAUSED)
					freeSources.Add(key);
			}

			if (freeSources.Count == 0)
				return -1;

			foreach (int i in freeSources)
				sourcePool[i].IsActive = false;

			sourcePool[freeSources[0]].IsActive = true;

			return freeSources[0];
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
		}

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			if (sound == null)
			{
				Log.Write("sound", "Attempt to Play2D a null `ISoundSource`");
				return null;
			}

			var currFrame = Game.orderManager.LocalFrameNumber;
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
					if (s.Sound != sound)
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

			var source = GetSourceFromPool();
			if (source == -1)
				return null;

			var slot = sourcePool[source];
			slot.Pos = pos;
			slot.FrameStarted = currFrame;
			slot.Sound = sound;
			slot.IsRelative = relative;
			return new OpenAlSound(source, (sound as OpenAlSoundSource).Buffer, loop, relative, pos, volume * atten);
		}

		public float Volume
		{
			get { return volume; }
			set { Al.alListenerf(Al.AL_GAIN, volume = value); }
		}

		public void PauseSound(ISound sound, bool paused)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
			if (state == Al.AL_PLAYING && paused)
				Al.alSourcePause(key);
			else if (state == Al.AL_PAUSED && !paused)
				Al.alSourcePlay(key);
		}

		public void SetAllSoundsPaused(bool paused)
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
				if (state == Al.AL_PLAYING && paused)
					Al.alSourcePause(key);
				else if (state == Al.AL_PAUSED && !paused)
					Al.alSourcePlay(key);
			}
		}

		public void SetSoundVolume(float volume, ISound music, ISound video)
		{
			var sounds = sourcePool.Select(s => s.Key).Where(b =>
			{
				int state;
				Al.alGetSourcei(b, Al.AL_SOURCE_STATE, out state);
				return (state == Al.AL_PLAYING || state == Al.AL_PAUSED) &&
					   (music == null || b != ((OpenAlSound)music).Source) &&
					   (video == null || b != ((OpenAlSound)video).Source);
			});

			foreach (var s in sounds)
				Al.alSourcef(s, Al.AL_GAIN, volume);
		}

		public void StopSound(ISound sound)
		{
			if (sound == null)
				return;

			var key = ((OpenAlSound)sound).Source;
			int state;
			Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
			if (state == Al.AL_PLAYING || state == Al.AL_PAUSED)
				Al.alSourceStop(key);
		}

		public void StopAllSounds()
		{
			foreach (var key in sourcePool.Keys)
			{
				int state;
				Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
				if (state == Al.AL_PLAYING || state == Al.AL_PAUSED)
					Al.alSourceStop(key);
			}
		}

		public void SetListenerPosition(WPos position)
		{
			// Move the listener out of the plane so that sounds near the middle of the screen aren't too positional
			Al.alListener3f(Al.AL_POSITION, position.X, position.Y, position.Z + 2133);

			var orientation = new[] { 0f, 0f, 1f, 0f, -1f, 0f };
			Al.alListenerfv(Al.AL_ORIENTATION, ref orientation[0]);
			Al.alListenerf(Al.AL_METERS_PER_UNIT, .01f);
		}
	}

	class OpenAlSoundSource : ISoundSource
	{
		public readonly int Buffer;

		static int MakeALFormat(int channels, int bits)
		{
			if (channels == 1)
				return bits == 16 ? Al.AL_FORMAT_MONO16 : Al.AL_FORMAT_MONO8;
			else
				return bits == 16 ? Al.AL_FORMAT_STEREO16 : Al.AL_FORMAT_STEREO8;
		}

		public OpenAlSoundSource(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			Al.alGenBuffers(1, out Buffer);
			Al.alBufferData(Buffer, MakeALFormat(channels, sampleBits), data, data.Length, sampleRate);
		}
	}

	class OpenAlSound : ISound
	{
		public readonly int Source = -1;
		float volume = 1f;

		public OpenAlSound(int source, int buffer, bool looping, bool relative, WPos pos, float volume)
		{
			if (source == -1)
				return;

			Source = source;
			Volume = volume;

			Al.alSourcef(source, Al.AL_PITCH, 1f);
			Al.alSource3f(source, Al.AL_POSITION, pos.X, pos.Y, pos.Z);
			Al.alSource3f(source, Al.AL_VELOCITY, 0f, 0f, 0f);
			Al.alSourcei(source, Al.AL_BUFFER, buffer);
			Al.alSourcei(source, Al.AL_LOOPING, looping ? Al.AL_TRUE : Al.AL_FALSE);
			Al.alSourcei(source, Al.AL_SOURCE_RELATIVE, relative ? 1 : 0);

			Al.alSourcef(source, Al.AL_REFERENCE_DISTANCE, 6826);
			Al.alSourcef(source, Al.AL_MAX_DISTANCE, 136533);
			Al.alSourcePlay(source);
		}

		public float Volume
		{
			get
			{
				return volume;
			}

			set
			{
				if (Source != -1)
					Al.alSourcef(Source, Al.AL_GAIN, volume = value);
			}
		}

		public float SeekPosition
		{
			get
			{
				float pos;
				Al.alGetSourcef(Source, Al.AL_SAMPLE_OFFSET, out pos);
				return pos / 22050f;
			}
		}

		public bool Playing
		{
			get
			{
				int state;
				Al.alGetSourcei(Source, Al.AL_SOURCE_STATE, out state);
				return state == Al.AL_PLAYING;
			}
		}
	}

	class NullSoundEngine : ISoundEngine
	{
		public NullSoundEngine()
		{
			Console.WriteLine("Using Null sound engine which disables SFX completely");
		}

		public ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate)
		{
			return new NullSoundSource();
		}

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, WPos pos, float volume, bool attenuateVolume)
		{
			return new NullSound();
		}

		public void PauseSound(ISound sound, bool paused) { }
		public void StopSound(ISound sound) { }
		public void SetAllSoundsPaused(bool paused) { }
		public void StopAllSounds() { }
		public void SetListenerPosition(WPos position) { }
		public void SetSoundVolume(float volume, ISound music, ISound video) { }

		public float Volume { get; set; }
	}

	class NullSoundSource : ISoundSource { }

	class NullSound : ISound
	{
		public float Volume { get; set; }
		public float SeekPosition { get { return 0; } }
		public bool Playing { get { return false; } }
	}
}
