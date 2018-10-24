#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	[Desc("Play the Build voice of this actor when trained.")]
	public class AnnounceOnBuildInfo : ITraitInfo
	{
		[Desc("Voice to use when built/trained.")]
		[VoiceReference] public readonly string Voice = "Build";

		[Desc("Player stances who can hear this voice.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Play the voice to the owning player even if Stance.Ally is not included in ValidStances")]
		public readonly bool PlayToOwner = true;

		public object Create(ActorInitializer init) { return new AnnounceOnBuild(init.Self, this); }
	}

	public class AnnounceOnBuild : INotifyBuildComplete
	{
		readonly AnnounceOnBuildInfo info;

		public AnnounceOnBuild(Actor self, AnnounceOnBuildInfo info)
		{
			this.info = info;
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			var player = self.World.LocalPlayer ?? self.World.RenderPlayer;
			if (player == null)
				return;

			if (info.ValidStances.HasStance(self.Owner.Stances[player]))
				self.PlayVoice(info.Voice);
			else if (info.PlayToOwner && self.Owner == player)
				self.PlayVoice(info.Voice);
		}
	}
}
