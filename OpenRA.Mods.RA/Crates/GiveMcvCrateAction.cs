#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Traits;
using System.Linq;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Spawns units when collected.", "Adjust selection shares when player has no base.")]
	class GiveMcvCrateActionInfo : GiveUnitCrateActionInfo
	{
		[Desc("The selection shares to use if the collector has no base.")]
		public int NoBaseSelectionShares = 1000;

		public override object Create(ActorInitializer init) { return new GiveMcvCrateAction(init.self, this); }
	}

	class GiveMcvCrateAction : GiveUnitCrateAction
	{
		readonly GiveMcvCrateActionInfo info;
		public GiveMcvCrateAction(Actor self, GiveMcvCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			// There's some other really good reason why we shouldn't give this.
			if (!CanGiveTo(collector))
				return 0;

			var hasBase = collector.World.ActorsWithTrait<BaseBuilding>()
				.Any(a => a.Actor.Owner == collector.Owner);

			return hasBase ? info.SelectionShares : info.NoBaseSelectionShares;
		}
	}
}
