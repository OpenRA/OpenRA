#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Spawns units when collected.","Adjust selection shares when player has no base.")]
	class GiveMcvCrateActionInfo : GiveUnitCrateActionInfo
	{
		[Desc("The selection shares to use if the collector has no base.")]
		public int NoBaseSelectionShares = 1000;

		public override object Create(ActorInitializer init) { return new GiveMcvCrateAction(init.self, this); }
	}

	class GiveMcvCrateAction : GiveUnitCrateAction
	{
		public GiveMcvCrateAction(Actor self, GiveMcvCrateActionInfo info)
			: base(self, info) { }

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector))
				return 0;	// there's some other really good reason why we shouldn't give this.

			var hasBase = self.World.ActorsWithTrait<BaseBuilding>()
				.Any(a => a.Actor.Owner == collector.Owner);

			return hasBase ? info.SelectionShares :
				((GiveMcvCrateActionInfo)info).NoBaseSelectionShares;
		}
	}
}
