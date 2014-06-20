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

		[Sync] public int Resources;
		[Sync] public int ResourceCapacity;

		public int DisplayCash;
		public int DisplayResources;
		public bool AlertSilo;

		public int Earned;
		public int Spent;

		public bool CanGiveResources(int amount)
		{
			return Resources + amount <= ResourceCapacity;
		}

		public void GiveResources(int num)
		{
			Resources += num;
			Earned += num;

			if (Resources > ResourceCapacity)
			{
				nextSiloAdviceTime = 0;

				Earned -= Resources - ResourceCapacity;
				Resources = ResourceCapacity;
			}
		}

		public bool TakeResources(int num)
		{
			if (Resources < num) return false;
			Resources -= num;
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
			if (Cash + Resources < num) return false;

			// Spend ore before cash
			Resources -= num;
			Spent += num;
			if (Resources < 0)
			{
				Cash += Resources;
				Resources = 0;
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

			ResourceCapacity = self.World.ActorsWithTrait<IStoreResources>()
				.Where(a => a.Actor.Owner == Owner)
				.Sum(a => a.Trait.Capacity);

			if (Resources > ResourceCapacity)
				Resources = ResourceCapacity;

			if (--nextSiloAdviceTime <= 0)
			{
				if (Resources > 0.8 * ResourceCapacity)
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

			diff = Math.Abs(Resources - DisplayResources);
			move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			if (DisplayResources < Resources)
			{
				DisplayResources += move;
				playCashTickUp(self);
			}
			else if (DisplayResources > Resources)
			{
				DisplayResources -= move;
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
