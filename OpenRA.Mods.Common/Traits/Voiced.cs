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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor has a voice.")]
	public class VoicedInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("A collection of voice sets that can be used. One will be selected at random.")]
		[VoiceSetReference] public readonly string[] VoiceSets = new string[0];

		[Desc("Multiply volume with this factor.")]
		public readonly float Volume = 1f;

		public object Create(ActorInitializer init) { return new Voiced(init.Self, this); }
	}

	public class Voiced : IVoiced
	{
		public readonly VoicedInfo Info;

		public string VoiceSet { get; }

		public Voiced(Actor self, VoicedInfo info)
		{
			Info = info;
			VoiceSet = Info.VoiceSets.RandomOrDefault(Game.CosmeticRandom);
		}

		public bool PlayVoice(Actor self, string phrase, string variant)
		{
			if (phrase == null)
				return false;

			if (string.IsNullOrEmpty(VoiceSet))
				return false;

			var type = VoiceSet.ToLowerInvariant();
			var volume = Info.Volume;
			return Game.Sound.PlayPredefined(self.World.Map.Rules, null, self, type, phrase, variant, true, WPos.Zero, volume, true);
		}

		public bool PlayVoiceLocal(Actor self, string phrase, string variant, float volume)
		{
			if (phrase == null)
				return false;

			if (string.IsNullOrEmpty(VoiceSet))
				return false;

			var type = VoiceSet.ToLowerInvariant();
			return Game.Sound.PlayPredefined(self.World.Map.Rules, null, self, type, phrase, variant, false, self.CenterPosition, volume, true);
		}

		public bool HasVoice(Actor self, string voice)
		{
			if (string.IsNullOrEmpty(VoiceSet))
				return false;

			var voices = self.World.Map.Rules.Voices[VoiceSet.ToLowerInvariant()];
			return voices != null && voices.Voices.ContainsKey(voice);
		}
	}
}
