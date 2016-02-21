#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
						player.Shroud.ExploreAll(player.World);
			}
			else
				collector.Owner.Shroud.ExploreAll(collector.World);

			base.Activate(collector);
		}
	}
}
