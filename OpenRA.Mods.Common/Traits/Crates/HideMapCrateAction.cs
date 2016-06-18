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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Hides the entire map in shroud.")]
	class HideMapCrateActionInfo : CrateActionInfo
	{
		[Desc("Should the map also be hidden for the allies of the collector's owner?")]
		public readonly bool IncludeAllies = false;

		public override object Create(ActorInitializer init) { return new HideMapCrateAction(init.Self, this); }
	}

	class HideMapCrateAction : CrateAction
	{
		readonly HideMapCrateActionInfo info;

		public HideMapCrateAction(Actor self, HideMapCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			// Don't hide the map if the shroud is force-revealed
			if (collector.Owner.HasFogVisibility || collector.Owner.Shroud.ExploreMapEnabled)
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			if (info.IncludeAllies)
			{
				foreach (var player in collector.World.Players)
					if (player.IsAlliedWith(collector.Owner))
						player.Shroud.ResetExploration();
            }
			else
				collector.Owner.Shroud.ResetExploration();

			base.Activate(collector);
		}
	}
}
