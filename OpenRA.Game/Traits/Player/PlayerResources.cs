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
using System.Linq;

namespace OpenRA.Traits
{
	class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 10000;
		public readonly int InitialOre = 0;
		public readonly int AdviceInterval = 250;

		public object Create(ActorInitializer init) { return new PlayerResources(init.self); }
	}

	public class PlayerResources : ITick
	{
		Player Owner;
		int AdviceInterval;
		public PlayerResources(Actor self)
		{
			Owner = self.Owner;
			Cash = self.Info.Traits.Get<PlayerResourcesInfo>().InitialCash;
			Ore = self.Info.Traits.Get<PlayerResourcesInfo>().InitialOre;
			AdviceInterval = self.Info.Traits.Get<PlayerResourcesInfo>().AdviceInterval;
		}

		[Sync]
		public int Cash;
		[Sync]
		public int DisplayCash;
		
		[Sync]
		public int Ore;
		[Sync]
		public int OreCapacity;
		[Sync]
		public int DisplayOre;

		public float GetSiloFullness() { return (float)Ore / OreCapacity; }

		public void GiveOre(int num)
		{
			Ore += num;
			
			if (Ore > OreCapacity)
			{
				nextSiloAdviceTime = 0;
				Ore = OreCapacity;
			}
		}
		
		public bool TakeOre(int num)
		{
			if (Ore < num) return false;
			Ore -= num;
			
			return true;
		}
		
		public void GiveCash(int num)
		{
			Cash += num;
		}
		
		public bool TakeCash(int num)
		{			
			if (Cash + Ore < num) return false;
			
			// Spend ore before cash
			Ore -= num;
			if (Ore < 0)
			{
				Cash += Ore;
				Ore = 0;	
			}
			
			return true;
		}

		const float displayCashFracPerFrame = .07f;
		const int displayCashDeltaPerFrame = 37;
		int nextSiloAdviceTime = 0;

		public void Tick(Actor self)
		{
			OreCapacity = self.World.Queries.OwnedBy[Owner].WithTrait<IStoreOre>()
				.Sum(a => a.Trait.Capacity);
			
			if (Ore > OreCapacity)
				Ore = OreCapacity;
			
			if (--nextSiloAdviceTime <= 0)
			{
				if (Ore > 0.8*OreCapacity)
					Owner.GiveAdvice(Owner.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().SilosNeeded);
				
				nextSiloAdviceTime = AdviceInterval;
			}
			
			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
			
			diff = Math.Abs(Ore - DisplayOre);
			move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			if (DisplayOre < Ore)
			{
				DisplayOre += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayOre > Ore)
			{
				DisplayOre -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
		}
	}
}
