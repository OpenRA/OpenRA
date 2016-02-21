#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor has a voice.")]
	public class VoicedInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Which voice set to use.")]
		[VoiceSetReference] public readonly string VoiceSet = null;

		[Desc("Multiply volume with this factor.")]
		public readonly float Volume = 1f;

		public object Create(ActorInitializer init) { return new Voiced(init.Self, this); }
	}

	public class Voiced : IVoiced
	{
		public readonly VoicedInfo Info;

		public Voiced(Actor self, VoicedInfo info)
		{
			Info = info;
		}

		public string VoiceSet { get { return Info.VoiceSet; } }

		public bool PlayVoice(Actor self, string phrase, string variant)
		{
			if (phrase == null)
				return false;

			if (string.IsNullOrEmpty(Info.VoiceSet))
				return false;

			var type = Info.VoiceSet.ToLowerInvariant();
			var volume = Info.Volume;
			return Game.Sound.PlayPredefined(self.World.Map.Rules, null, self, type, phrase, variant, true, WPos.Zero, volume, true);
		}

		public bool PlayVoiceLocal(Actor self, string phrase, string variant, float volume)
		{
			if (phrase == null)
				return false;

			if (string.IsNullOrEmpty(Info.VoiceSet))
				return false;

			var type = Info.VoiceSet.ToLowerInvariant();
			return Game.Sound.PlayPredefined(self.World.Map.Rules, null, self, type, phrase, variant, false, self.CenterPosition, volume, true);
		}

		public bool HasVoice(Actor self, string voice)
		{
			if (string.IsNullOrEmpty(Info.VoiceSet))
				return false;

			var voices = self.World.Map.Rules.Voices[Info.VoiceSet.ToLowerInvariant()];
			return voices != null && voices.Voices.ContainsKey(voice);
		}
	}
}
