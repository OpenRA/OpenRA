#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ClassicProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PowerManagerInfo>, Requires<PlayerResourcesInfo>
	{
		public override object Create(ActorInitializer init) { return new ClassicProductionQueue(init.self, this); }
	}

	public class ClassicProductionQueue : ProductionQueue, ISync
	{
		public ClassicProductionQueue( Actor self, ClassicProductionQueueInfo info )
			: base(self, self, info) {}

		[Sync] bool isActive = false;

		public override void Tick( Actor self )
		{
			isActive = self.World.ActorsWithTrait<Production>()
				.Any(x => x.Actor.Owner == self.Owner
					 && x.Trait.Info.Produces.Contains(Info.Type));

			base.Tick(self);
		}

		static ActorInfo[] None = { };
		public override IEnumerable<ActorInfo> AllItems()
		{
			return isActive ? base.AllItems() : None;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return isActive ? base.BuildableItems() : None;
		}

		protected override bool BuildUnit( string name )
		{
			// Find a production structure to build this actor
			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					   && x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding() ? 1 : 0 ); // prioritize the primary.

			if (!producers.Any())
			{
				CancelProduction(name,1);
				return true;
			}

			foreach (var p in producers.Where(p => !p.Actor.IsDisabled()))
			{
				if (p.Trait.Produce(p.Actor, Rules.Info[ name ]))
				{
					FinishProduction();
					return true;
				}
			}
			return false;
		}
	}
}
