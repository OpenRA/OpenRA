#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Traits
{
	public class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int[] SelectableCash = { 2500, 5000, 10000, 20000 };
		public readonly int DefaultCash = 5000;
		public readonly int AdviceInterval = 250;

		public object Create(ActorInitializer init) { return new PlayerResources(init.self, this); }
	}

	public class PlayerResources : ITick, ISync
	{
		readonly Player Owner;
		int AdviceInterval;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			Owner = self.Owner;

			Cash = self.World.LobbyInfo.GlobalSettings.StartingCash;
			AdviceInterval = info.AdviceInterval;
		}

		[Sync] public int Cash;

		[Sync] public int Ore;
		[Sync] public int OreCapacity;

		public int DisplayCash;
		public int DisplayOre;
		public bool AlertSilo;

		public int Earned;
		public int Spent;

		public bool CanGiveOre(int amount)
		{
			return Ore + amount <= OreCapacity;
		}

		public void GiveOre(int num)
		{
			Ore += num;
			Earned += num;

			if (Ore > OreCapacity)
			{
				nextSiloAdviceTime = 0;

				Earned -= Ore - OreCapacity;
				Ore = OreCapacity;
			}
		}

		public bool TakeOre(int num)
		{
			if (Ore < num) return false;
			Ore -= num;
			Spent += num;

			return true;
		}

		public void GiveCash(int num)
		{
			Cash += num;
			Earned += num;
		}

		public bool TakeCash(int num)
		{
			if (Cash + Ore < num) return false;

			// Spend ore before cash
			Ore -= num;
			Spent += num;
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
		int nextCashTickTime = 0;

		public void Tick(Actor self)
		{
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			OreCapacity = self.World.ActorsWithTrait<IStoreOre>()
				.Where(a => a.Actor.Owner == Owner)
				.Sum(a => a.Trait.Capacity);

			if (Ore > OreCapacity)
				Ore = OreCapacity;

			if (--nextSiloAdviceTime <= 0)
			{
				if (Ore > 0.8 * OreCapacity)
				{
					Sound.PlayNotification(self.World.Map.Rules, Owner, "Speech", "SilosNeeded", Owner.Country.Race);
					AlertSilo = true;
				}
				else
					AlertSilo = false;

				nextSiloAdviceTime = AdviceInterval;
			}

			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);


			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				playCashTickUp(self);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				playCashTickDown(self);
			}

			diff = Math.Abs(Ore - DisplayOre);
			move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			if (DisplayOre < Ore)
			{
				DisplayOre += move;
				playCashTickUp(self);
			}
			else if (DisplayOre > Ore)
			{
				DisplayOre -= move;
				playCashTickDown(self);
			}
		}
		
		
		public void playCashTickUp(Actor self)
		{
			if (Game.Settings.Sound.CashTicks)
				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickUp", self.Owner.Country.Race);
		}
		
		public void playCashTickDown(Actor self)
		{
			if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
			{
				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickDown", self.Owner.Country.Race);
				nextCashTickTime = 2;
			}
		}
	}
}
