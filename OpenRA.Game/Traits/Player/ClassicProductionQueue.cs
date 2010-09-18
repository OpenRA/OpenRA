#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class ClassicProductionQueueInfo : ProductionQueueInfo, ITraitPrerequisite<TechTreeInfo>, ITraitPrerequisite<PowerManagerInfo>, ITraitPrerequisite<PlayerResourcesInfo>
	{
		public override object Create(ActorInitializer init) { return new ClassicProductionQueue(init.self, this); }
	}

	public class ClassicProductionQueue : ProductionQueue
	{
		public ClassicProductionQueue( Actor self, ClassicProductionQueueInfo info )
			: base(self, self, info as ProductionQueueInfo) {}
				
		[Sync] bool QueueActive = true;
		public override void Tick( Actor self )
		{
			QueueActive = self.World.Queries.OwnedBy[self.Owner].WithTrait<Production>()
				.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
				.Any();
			
			base.Tick(self);
		}
		
		ActorInfo[] None = new ActorInfo[]{};
		public override IEnumerable<ActorInfo> AllItems()
		{
			return QueueActive ? base.AllItems() : None;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return QueueActive ? base.BuildableItems() : None;
		}
		
		protected override void BuildUnit( string name )
		{			
			// Find a production structure to build this actor
			var producers = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<Production>()
				.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding() ? 1 : 0 ); // prioritize the primary.

			if (producers.Count() == 0)
			{
				CancelProduction(name);
				return;
			}
			
			foreach (var p in producers)
			{
				if (IsDisabledBuilding(p.Actor)) continue;

				if (p.Trait.Produce(p.Actor, Rules.Info[ name ]))
				{
					FinishProduction();
					break;
				}
			}
		}
	}
}
