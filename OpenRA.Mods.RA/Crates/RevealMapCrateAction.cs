#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Reveals the entire map.")]
	class RevealMapCrateActionInfo : CrateActionInfo
	{
		[Desc("Should the map also be revealed for the allies of the collector's owner.")]
		public readonly bool IncludeAllies = false;

		public override object Create(ActorInitializer init) { return new RevealMapCrateAction(init.self, this); }
	}

	class RevealMapCrateAction : CrateAction
	{
		public RevealMapCrateAction(Actor self, RevealMapCrateActionInfo info)
			: base(self, info) {}

		bool ShouldReveal(Player collectingPlayer)
		{
			if (((RevealMapCrateActionInfo)info).IncludeAllies)
				return collectingPlayer.World.LocalPlayer != null &&
					collectingPlayer.Stances[collectingPlayer.World.LocalPlayer] == Stance.Ally;

			return collectingPlayer == collectingPlayer.World.LocalPlayer;
		}

		public override void Activate(Actor collector)
		{
			base.Activate(collector);

			if (ShouldReveal( collector.Owner ))
				collector.Owner.Shroud.ExploreAll(collector.World);
		}
	}
}
