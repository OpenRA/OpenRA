#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns units when collected.", "Adjust selection shares when player has no base.")]
	sealed class GiveBaseBuilderCrateActionInfo : GiveUnitCrateActionInfo
	{
		[Desc("The selection shares to use if the collector has no actor with `" + nameof(BaseBuilding) + ".")]
		public readonly int NoBaseSelectionShares = 1000;

		public override object Create(ActorInitializer init) { return new GiveBaseBuilderCrateAction(init.Self, this); }
	}

	sealed class GiveBaseBuilderCrateAction : GiveUnitCrateAction
	{
		readonly GiveBaseBuilderCrateActionInfo info;
		public GiveBaseBuilderCrateAction(Actor self, GiveBaseBuilderCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			// There's some other really good reason why we shouldn't give this.
			if (!CanGiveTo(collector))
				return 0;

			var hasBase = collector.World.ActorsHavingTrait<BaseBuilding>()
				.Any(a => a.Owner == collector.Owner);

			return hasBase ? info.SelectionShares : info.NoBaseSelectionShares;
		}
	}
}
