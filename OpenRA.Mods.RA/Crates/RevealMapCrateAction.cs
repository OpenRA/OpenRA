#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RevealMapCrateActionInfo : CrateActionInfo
	{
		public readonly bool IncludeAllies = false;
		public override object Create(ActorInitializer init) { return new RevealMapCrateAction(init.self, this); }
	}

	class RevealMapCrateAction : CrateAction
	{
		public RevealMapCrateAction(Actor self, RevealMapCrateActionInfo info)
			: base(self, info) {}

		bool ShouldReveal(Player collectingPlayer)
		{
			if ((info as RevealMapCrateActionInfo).IncludeAllies)
				return collectingPlayer.World.LocalPlayer != null &&
					collectingPlayer.Stances[collectingPlayer.World.LocalPlayer] == Stance.Ally;

			return collectingPlayer == collectingPlayer.World.LocalPlayer;
		}

		public override void Activate(Actor collector)
		{
			base.Activate(collector);

			if (ShouldReveal( collector.Owner ))
				collector.World.WorldActor.Trait<Shroud>().ExploreAll(collector.World);
		}
	}
}
