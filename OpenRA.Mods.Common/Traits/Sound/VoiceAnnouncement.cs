#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Plays a voice clip when the trait is enabled.")]
	public class VoiceAnnouncementInfo : ConditionalTraitInfo
	{
		[VoiceReference]
		[FieldLoader.Require]
		[Desc("Voice to play.")]
		public readonly string Voice = null;

		[Desc("Player stances who can hear this voice.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Play the voice to the owning player even if Stance.Ally is not included in ValidStances.")]
		public readonly bool PlayToOwner = true;

		[Desc("Disable the announcement after it has been triggered.")]
		public readonly bool OneShot = false;

		public override object Create(ActorInitializer init) { return new VoiceAnnouncement(this); }
	}

	public class VoiceAnnouncement : ConditionalTrait<VoiceAnnouncementInfo>
	{
		bool triggered;

		public VoiceAnnouncement(VoiceAnnouncementInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Info.OneShot && triggered)
				return;

			triggered = true;
			var player = self.World.LocalPlayer ?? self.World.RenderPlayer;
			if (player == null)
				return;

			if (Info.ValidStances.HasStance(self.Owner.Stances[player]))
				self.PlayVoice(Info.Voice);
			else if (Info.PlayToOwner && self.Owner == player)
				self.PlayVoice(Info.Voice);
		}
	}
}
