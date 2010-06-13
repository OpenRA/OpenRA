#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class OreRefineryInfo : ITraitInfo
	{
		public readonly int Pips = 0;
		public readonly int Capacity = 0;
		public readonly int ProcessTick = 25;
		public readonly int ProcessAmount = 50;
		public object Create(Actor self) { return new OreRefinery(self, this); }
	}

	class OreRefinery : ITick, IAcceptOre, IPips
	{
		Actor self;
		OreRefineryInfo Info;

		[Sync]
		int nextProcessTime = 0;
		[Sync]
		public int Ore = 0;
		public OreRefinery(Actor self, OreRefineryInfo info)
		{
			this.self = self;
			Info = info;
		}
		
		public void GiveOre(int amount)
		{
			Ore += amount;
			if (Ore > Info.Capacity)
				Ore = Info.Capacity;
		}
		
		public void Tick(Actor self)
		{
			if (--nextProcessTime <= 0)
			{
				// Convert resources to cash
				var pr = self.Owner.PlayerActor.traits.Get<PlayerResources>();
				int amount = Math.Min(Ore, Info.ProcessAmount);
					amount = Math.Min(amount, pr.OreCapacity - pr.Ore);
				if (amount > 0)
				{
					Ore -=amount;
					pr.GiveOre(amount);
				}
				nextProcessTime = Info.ProcessTick;
			}
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray( Info.Pips, 
				i => (Ore * 1.0f / Info.Capacity > i * 1.0f / Info.Pips) 
					? PipType.Red : PipType.Transparent );
		}
		
		public int2 DeliverOffset {	get { return new int2(1, 2); } }
		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			var unit = harv.traits.Get<Unit>();
			if (unit.Facing != 64)
				harv.QueueActivity(new Turn(64));
				
			harv.QueueActivity( new CallFunc( () => {
				var renderUnit = harv.traits.Get<RenderUnit>();
				if (renderUnit.anim.CurrentSequence.Name != "empty")
					renderUnit.PlayCustomAnimation(harv, "empty", () =>
					{
						harv.traits.Get<Harvester>().Deliver(harv, self);
						harv.QueueActivity(new Harvest());
					});
				}
			));
		}
	}
}
