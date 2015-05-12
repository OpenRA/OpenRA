#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class VoicedInfo : ITraitInfo
	{
		[VoiceReference] public readonly string VoiceSet = null;

		public object Create(ActorInitializer init) { return new Voiced(init.Self, this); }
	}

	public class Voiced : IVoiced
	{
		public readonly VoicedInfo Info;

		public Voiced(Actor self, VoicedInfo info)
		{
			Info = info;
		}

		public bool PlayVoice(string phrase, Actor voicedActor, string variant)
		{
			if (voicedActor == null || phrase == null)
				return false;

			var mi = voicedActor.TraitOrDefault<IVoiced>();
			if (mi == null || mi.VoiceSet == null)
				return false;

			var type = mi.VoiceSet.ToLowerInvariant();
			return Sound.PlayPredefined(voicedActor.World.Map.Rules, null, voicedActor, type, phrase, variant, true, WPos.Zero, 1f, true);
		}

		public bool PlayVoiceLocal(string phrase, Actor voicedActor, string variant, WPos pos, float volume)
		{
			if (voicedActor == null || phrase == null)
				return false;

			var mi = voicedActor.TraitOrDefault<IVoiced>();
			if (mi == null || mi.VoiceSet == null)
				return false;

			var type = mi.VoiceSet.ToLowerInvariant();
			return Sound.PlayPredefined(voicedActor.World.Map.Rules, null, voicedActor, type, phrase, variant, false, pos, volume, true);
		}

		public bool HasVoices(Actor actor)
		{
			var voice = actor.TraitsImplementing<IVoiced>().FirstOrDefault();
			return voice != null && voice.VoiceSet != null;
		}

		public bool HasVoice(Actor actor, string voice)
		{
			var v = GetVoices(actor);
			return v != null && v.Voices.ContainsKey(voice);
		}

		public SoundInfo GetVoices(Actor actor)
		{
			var voice = actor.TraitsImplementing<IVoiced>().FirstOrDefault();
			if (voice == null)
				return null;

			var v = voice.VoiceSet;
			return (v == null) ? null : actor.World.Map.Rules.Voices[v.ToLowerInvariant()];
		}

		public string VoiceSet { get { return Info.VoiceSet; } }
	}
}
