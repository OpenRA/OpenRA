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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reveals the entire map.")]
	class RevealMapCrateActionInfo : CrateActionInfo
	{
		[Desc("Should the map also be revealed for the allies of the collector's owner?")]
		public readonly bool IncludeAllies = false;

		public override object Create(ActorInitializer init) { return new RevealMapCrateAction(init.Self, this); }
	}

	class RevealMapCrateAction : CrateAction
	{
		readonly RevealMapCrateActionInfo info;

		public RevealMapCrateAction(Actor self, RevealMapCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor collector)
		{
			if (info.IncludeAllies)
			{
				foreach (var player in collector.World.Players)
					if (collector.Owner.IsAlliedWith(player))
						player.Shroud.ExploreAll();
			}
			else
				collector.Owner.Shroud.ExploreAll();

			base.Activate(collector);
		}
	}
}
