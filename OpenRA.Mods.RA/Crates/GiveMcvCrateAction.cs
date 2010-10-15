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

namespace OpenRA.Mods.RA.Crates
{
	class GiveMcvCrateActionInfo : GiveUnitCrateActionInfo
	{
		public int NoBaseSelectionShares = 1000;
		public override object Create(ActorInitializer init) { return new GiveMcvCrateAction(init.self, this); }
	}

	class GiveMcvCrateAction : GiveUnitCrateAction
	{
		public GiveMcvCrateAction(Actor self, GiveMcvCrateActionInfo info)
			: base(self, info) { }

		public override int GetSelectionShares(Actor collector)
		{
			if (base.GetSelectionShares(collector) == 0)
				return 0;	// there's some other really good reason why we shouldn't give this.

			var hasBase = self.World.Queries.OwnedBy[collector.Owner].WithTrait<BaseBuilding>().Any();
			return hasBase ? info.SelectionShares : (info as GiveMcvCrateActionInfo).NoBaseSelectionShares;
		}
	}
}
