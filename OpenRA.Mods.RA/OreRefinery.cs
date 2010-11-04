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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

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
		public readonly int LowPowerProcessTick = 50;

		public object Create(ActorInitializer init) { return new OreRefinery(init.self, this); }
	}

	class OreRefinery : ITick, IAcceptOre, INotifyDamage, INotifySold, INotifyCapture, IPips, IExplodeModifier
	{
		readonly Actor self;
		readonly OreRefineryInfo Info;
		readonly PlayerResources PlayerResources;
		readonly PowerManager PlayerPower;
		List<Actor> LinkedHarv;

		[Sync]
		int nextProcessTime = 0;
		[Sync]
		public int Ore = 0;

		public OreRefinery (Actor self, OreRefineryInfo info)
		{
			this.self = self;
			Info = info;
			PlayerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			PlayerPower = self.Owner.PlayerActor.Trait<PowerManager>();

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
				PlayerResources.GiveOre(amount);
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
				amount = Math.Min (amount, PlayerResources.OreCapacity - PlayerResources.Ore);
				
				if (amount > 0)
				{
					Ore -= amount;
					PlayerResources.GiveOre (amount);
				}
				nextProcessTime = (PlayerPower.PowerState == PowerState.Normal)? 
					Info.ProcessTick : Info.LowPowerProcessTick ;
			}
		}

		public void Damaged (Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				foreach (var harv in LinkedHarv)
					harv.Trait<Harvester> ().UnlinkProc(harv, self);
		}

		public int2 DeliverOffset {get{ return Info.DockOffset; }}
		public void OnDock (Actor harv, DeliverResources dockOrder)
		{
			self.Trait<IAcceptOreDockAction>().OnDock(self, harv, dockOrder);
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{		
			// Unlink any non-docked harvs
			foreach (var harv in LinkedHarv)
			{
				if (harv.Owner == self.Owner)
					harv.Trait<Harvester>().UnlinkProc (harv, self);
			}
		}

		public void Selling (Actor self) {}
		public void Sold (Actor self)
		{
			foreach (var harv in LinkedHarv)
				harv.Trait<Harvester>().UnlinkProc (harv, self);
		}

		public IEnumerable<PipType> GetPips (Actor self)
		{
			if (!Info.LocalStorage)
				return null;
			
			return Graphics.Util.MakeArray (Info.PipCount, i => (Ore * 1f / Info.Capacity > i * 1f / Info.PipCount) ? Info.PipColor : PipType.Transparent);
		}

		public bool ShouldExplode(Actor self) { return Ore > 0; }
	}
}
