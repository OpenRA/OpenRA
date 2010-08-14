#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
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
		static string currentMusic;

		static ISoundSource LoadSound(string filename)
		{
			var data = AudLoader.LoadSound(FileSystem.Open(filename));
			return soundEngine.AddSoundSourceFromMemory(data, 1, 16, 22050);
		}

		static ISoundSource LoadSoundRaw(byte[] rawData)
		{
			return soundEngine.AddSoundSourceFromMemory(rawData, 1, 16, 22050);
		}

		public static void Initialize()
		{
			soundEngine = new OpenAlSoundEngine();
			sounds = new Cache<string, ISoundSource>(LoadSound);
			music = null;
			currentMusic = null;
			video = null;
		}

		public static void SetListenerPosition(float2 position) { soundEngine.SetListenerPosition(position); }

		public static void Play(string name)
		{
			if (name == "" || name == null)
				return;

			var sound = sounds[name];
			soundEngine.Play2D(sound, false, true, float2.Zero, SoundVolume);
		}

		public static void Play(string name, float2 pos)
		{
			if (name == "" || name == null)
				return;
			
			var sound = sounds[name];
			soundEngine.Play2D(sound, false, false, pos, SoundVolume);
		}

		public static void PlayToPlayer(Player player, string name)
		{
			if( player == player.World.LocalPlayer )
				Play( name );
		}

		public static void PlayToPlayer(Player player, string name, float2 pos)
		{
			if (player == player.World.LocalPlayer)
				Play(name, pos);
		}

		public static void PlayVideo(byte[] raw)
		{
			rawSource = LoadSoundRaw(raw);
			video = soundEngine.Play2D(rawSource, false, true, float2.Zero, SoundVolume);
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
		
		public static void PlayMusic(string name)
		{
			if (name == "" || name == null)
				return;

			if (name == currentMusic && music != null)
			{
				soundEngine.PauseSound(music, false);
				return;
			}
			StopMusic();
			
			currentMusic = name;
			var sound = sounds[name];
			music = soundEngine.Play2D(sound, true, true, float2.Zero, MusicVolume);
		}

		public static void StopMusic()
		{
			if (music != null)
				soundEngine.StopSound(music);
			
			currentMusic = null;
		}
		
		public static void PauseMusic()
		{
			if (music != null)
				soundEngine.PauseSound(music, true);
		}

		public static float GlobalVolume
		{
			get { return soundEngine.Volume; }
			set { soundEngine.Volume = value;}
		}
		
		public static float SoundVolume
		{
			get { return Game.Settings.SoundVolume; }
			set
			{
				Game.Settings.SoundVolume = value;
				soundEngine.SetSoundVolume(value, music, video);
			}
		}

		public static float MusicVolume
		{
			get { return Game.Settings.MusicVolume; }
			set
			{
				Game.Settings.MusicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}
		
		public static float VideoVolume
		{
			get { return Game.Settings.VideoVolume; }
			set
			{
				Game.Settings.VideoVolume = value;
				if (video != null)
					video.Volume = value;
			}
		}
		
		public static float MusicSeekPosition
		{
			get { return (music != null)? music.SeekPosition : 0; }	
		}
		
		public static float VideoSeekPosition
		{
			get { return (video != null)? video.SeekPosition : 0; }	
		}
		
		// Returns true if it played a phrase
		public static bool PlayVoice(string phrase, Actor voicedUnit, string variant)
		{
			if (voicedUnit == null) return false;
			if (phrase == null) return false;

			var mi = voicedUnit.Info.Traits.GetOrDefault<SelectableInfo>();
			if (mi == null) return false;
			if (mi.Voice == null) return false;

			var vi = Rules.Voices[mi.Voice.ToLowerInvariant()];

			var clip = vi.Pools.Value[phrase].GetNext();
			if (clip == null)
				return false;

			if (clip.Contains("."))		/* no variants! */
			{
				Play(clip);
				return true;
			}
			
			if (vi.Variants.Count == 0)
			{
				Play(clip + vi.DefaultVariant);
				return true;
			}

			var variantext = vi.Variants.ContainsKey(variant)? 
				  vi.Variants[variant][voicedUnit.ActorID % vi.Variants.Count] : vi.DefaultVariant;
			Play(clip + variantext);
			return true;
		}
	}

	interface ISoundEngine
	{
		ISoundSource AddSoundSourceFromMemory(byte[] data, int channels, int sampleBits, int sampleRate);
		ISound Play2D(ISoundSource sound, bool loop, bool relative, float2 pos, float volume);
		float Volume { get; set; }
		void PauseSound(ISound sound, bool paused);
		void StopSound(ISound sound);
		void SetAllSoundsPaused(bool paused);
		void StopAllSounds();
		void SetListenerPosition(float2 position);
		void SetSoundVolume(float volume, ISound music, ISound video);
	}

	interface ISoundSource {}
	interface ISound
	{
		float Volume { get; set; }
		float SeekPosition { get; }
	}

	class OpenAlSoundEngine : ISoundEngine
	{
		float volume = 1f;
		Dictionary<int, bool> sourcePool = new Dictionary<int, bool>();
		const int POOL_SIZE = 32;

		public OpenAlSoundEngine()
		{
			//var str = Alc.alcGetString(IntPtr.Zero, Alc.ALC_DEFAULT_DEVICE_SPECIFIER);
			var dev = Alc.alcOpenDevice(null);
			if (dev == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL device");
			var ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
			if (ctx == IntPtr.Zero)
				throw new InvalidOperationException("Can't create OpenAL context");
			Alc.alcMakeContextCurrent(ctx);

			for (var i = 0; i < POOL_SIZE; i++)
			{
				var source = 0;
				Al.alGenSources(1, out source);
				if (0 != Al.alGetError())
				{
					Log.Write("debug", "Failed generating OpenAL source {0}", i);
					return;
				}
					
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
				Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
				if (state != Al.AL_PLAYING && state != Al.AL_PAUSED)
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

		public ISound Play2D(ISoundSource sound, bool loop, bool relative, float2 pos, float volume)
		{
			int source = GetSourceFromPool();
			return new OpenAlSound(source, (sound as OpenAlSoundSource).buffer, loop, relative, pos, volume);
		}

		public float Volume
		{
			get { return volume; }
			set { Al.alListenerf(Al.AL_GAIN, volume = value); }
		}
		
		public void PauseSound(ISound sound, bool paused)
		{
			int key = ((OpenAlSound) sound).source;
			int state;
			Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
			if (state == Al.AL_PLAYING && paused)
				Al.alSourcePause(key);
			else if (state == Al.AL_PAUSED && !paused)
				Al.alSourcePlay(key);
		}
		
		public void SetAllSoundsPaused(bool paused)
		{	
			foreach (int key in sourcePool.Keys)
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
			var sounds = sourcePool.Select(s => s.Key).Where( b => 
			{ 
				int state;
				Al.alGetSourcei(b, Al.AL_SOURCE_STATE, out state);
				return ((state == Al.AL_PLAYING || state == Al.AL_PAUSED) && 
					   ((music != null)? b != ((OpenAlSound) music).source : true) &&
					   ((video != null)? b != ((OpenAlSound) video).source : true));
			}).ToList();
			foreach (var s in sounds)
			{
				Al.alSourcef(s, Al.AL_GAIN, volume);
			}
		}
		
		public void StopSound(ISound sound)
		{
			int key = ((OpenAlSound) sound).source;
			int state;
			Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
			if (state == Al.AL_PLAYING || state == Al.AL_PAUSED)
				Al.alSourceStop(key);
		}
		
		public void StopAllSounds()
		{
			foreach (int key in sourcePool.Keys)
			{
				int state;
				Al.alGetSourcei(key, Al.AL_SOURCE_STATE, out state);
				if (state == Al.AL_PLAYING || state == Al.AL_PAUSED)
					Al.alSourceStop(key);
			}
		}

		public void SetListenerPosition(float2 position)
		{
			var orientation = new [] { 0f, 0f, 1f, 0f, -1f, 0f };

			Al.alListener3f(Al.AL_POSITION, position.X, position.Y, 50);
			Al.alListenerfv(Al.AL_ORIENTATION, ref orientation[0]);
			Al.alListenerf(Al.AL_METERS_PER_UNIT, .01f);
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
		public readonly int source = -1;
		float volume = 1f;

		public OpenAlSound(int source, int buffer, bool looping, bool relative, float2 pos, float volume)
		{
			if (source == -1) return;
			this.source = source;
			Al.alSourcef(source, Al.AL_PITCH, 1f);
			Al.alSourcef(source, Al.AL_GAIN, 1f);
			Al.alSource3f(source, Al.AL_POSITION, pos.X, pos.Y, 0f);
			Al.alSource3f(source, Al.AL_VELOCITY, 0f, 0f, 0f);
			Al.alSourcei(source, Al.AL_BUFFER, buffer);
			Al.alSourcei(source, Al.AL_LOOPING, looping ? Al.AL_TRUE : Al.AL_FALSE);
			Al.alSourcei(source, Al.AL_SOURCE_RELATIVE, relative ? 1 : 0);
			Al.alSourcef(source, Al.AL_REFERENCE_DISTANCE, 200);
			Al.alSourcef(source, Al.AL_MAX_DISTANCE, 1500);
			Volume = volume;
			Al.alSourcePlay(source);
		}

		public float Volume
		{
			get { return volume; }
			set 
			{
				if (source != -1)
					Al.alSourcef(source, Al.AL_GAIN, volume = value); 
			}
		}
		
		public float SeekPosition
		{
			get
			{
				float pos;
				Al.alGetSourcef(source, Al.AL_SAMPLE_OFFSET, out pos);
				return pos/22050f; 
			}
		}
	}
}
