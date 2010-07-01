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
		public readonly bool LocalStorage = false;
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Red;
		public readonly int2 DockOffset = new int2 (1, 2);
		public readonly int Capacity = 0;
		public readonly int ProcessTick = 25;
		public readonly int ProcessAmount = 50;
		[WeaponReference]
		public readonly string DeathWeapon = null;

		public object Create(ActorInitializer init) { return new OreRefinery(init.self, this); }
	}

	class OreRefinery : ITick, IAcceptOre, INotifyDamage, INotifySold, INotifyCapture, IPips
	{
		readonly Actor self;
		readonly OreRefineryInfo Info;
		readonly PlayerResources Player;
		List<Actor> LinkedHarv;

		[Sync]
		int nextProcessTime = 0;
		[Sync]
		public int Ore = 0;

		public OreRefinery (Actor self, OreRefineryInfo info)
		{
			this.self = self;
			Info = info;
			Player = self.Owner.PlayerActor.traits.Get<PlayerResources> ();
			LinkedHarv = new List<Actor> ();
		}

		public void LinkHarvester (Actor self, Actor harv)
		{
			LinkedHarv.Add (harv);
		}

		public void UnlinkHarvester (Actor self, Actor harv)
		{
			if (LinkedHarv.Contains (harv))
				LinkedHarv.Remove (harv);
		}

		public void GiveOre (int amount)
		{
			if (!Info.LocalStorage)
				Player.GiveOre(amount);
			else
			{
				Ore += amount;
				if (Ore > Info.Capacity)
					Ore = Info.Capacity;
			}
		}

		public void Tick (Actor self)
		{
			if (!Info.LocalStorage)
				return;
			
			if (--nextProcessTime <= 0) {
				// Convert resources to cash
				int amount = Math.Min (Ore, Info.ProcessAmount);
				amount = Math.Min (amount, Player.OreCapacity - Player.Ore);
				
				if (amount > 0)
				{
					Ore -= amount;
					Player.GiveOre (amount);
				}
				nextProcessTime = Info.ProcessTick;
			}
		}

		public void Damaged (Actor self, AttackInfo e)
		{
			if (self.IsDead) {
				if (Info.DeathWeapon != null && Ore > 0) {
					Combat.DoExplosion (e.Attacker, Info.DeathWeapon, self.CenterLocation.ToInt2 (), 0);
				}
				
				foreach (var harv in LinkedHarv)
					harv.traits.Get<Harvester> ().UnlinkProc(harv, self);
			}
		}

		public int2 DeliverOffset {get{ return Info.DockOffset; }}
		public void OnDock (Actor harv, DeliverResources dockOrder)
		{
			self.traits.Get<IAcceptOreDockAction>().OnDock(self, harv, dockOrder);
		}
		
		public void OnCapture (Actor self, Actor captor)
		{		
			// Unlink any non-docked harvs
			foreach (var harv in LinkedHarv)
			{
				if (harv.Owner == self.Owner)
					harv.traits.Get<Harvester>().UnlinkProc (harv, self);
			}
		}

		public void Selling (Actor self) {}
		public void Sold (Actor self)
		{
			foreach (var harv in LinkedHarv)
				harv.traits.Get<Harvester>().UnlinkProc (harv, self);
		}

		public IEnumerable<PipType> GetPips (Actor self)
		{
			if (!Info.LocalStorage)
				return new PipType[] { };
			
			return Graphics.Util.MakeArray (Info.PipCount, i => (Ore * 1f / Info.Capacity > i * 1f / Info.PipCount) ? Info.PipColor : PipType.Transparent);
		}
	}
}
