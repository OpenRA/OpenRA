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
	public class PlayerProductionQueueInfo : ProductionQueueInfo, ITraitPrerequisite<TechTreeCacheInfo>
	{
		public override object Create(ActorInitializer init) { return new PlayerProductionQueue(init.self, this); }
	}

	public class PlayerProductionQueue : ProductionQueue
	{
		public PlayerProductionQueue( Actor self, PlayerProductionQueueInfo info )
			: base(self, self, info as ProductionQueueInfo) {}
				
		[Sync] bool QueueActive = true;
		public override void Tick( Actor self )
		{
			if (self == self.Owner.PlayerActor)
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
			// original ra behavior; queue lives on PlayerActor, need to find a production structure
			var producers = self.World.Queries.OwnedBy[self.Owner]
			.WithTrait<Production>()
			.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
			.OrderByDescending(x => x.Actor.IsPrimaryBuilding() ? 1 : 0 ) // prioritize the primary.
			.ToArray();

			if (producers.Length == 0)
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
