#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 10000;
		public readonly int InitialResources = 0;
		public readonly int AdviceInterval = 250;

		public object Create(ActorInitializer init) { return new PlayerResources(init.self, this); }
	}

	public class PlayerResources : ITick, ISync
	{
		readonly Player Owner;
		int AdviceInterval;
		
		int cashtickallowed = 0;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			Owner = self.Owner;

			Cash = info.InitialCash;
			Resources = info.InitialResources;
			AdviceInterval = info.AdviceInterval;
		}

		[Sync] public int Cash;

		[Sync] public int Resources;
		[Sync] public int Capacity;

		public int DisplayCash;
		public int DisplayResources;
		public bool AlertSilo;

		public int Earned;
		public int Spent;

		public bool CanGiveResources(int amount)
		{
			return Resources + amount <= Capacity;
		}

		public void GiveResources(int num)
		{
			Resources += num;
			Earned += num;

			if (Resources > Capacity)
			{
				nextSiloAdviceTime = 0;

				Earned -= Resources - Capacity;
				Resources = Capacity;
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

		public void Tick(Actor self)
		{
			if(cashtickallowed > 0) {
				cashtickallowed = cashtickallowed - 1;
			}

			Capacity = self.World.ActorsWithTrait<IStoreResources>()
				.Where(a => a.Actor.Owner == Owner)
				.Sum(a => a.Trait.Capacity);

			if (Resources > Capacity)
				Resources = Capacity;

			if (--nextSiloAdviceTime <= 0)
			{
				if (Resources > 0.8 * Capacity)
				{
					Sound.PlayNotification(Owner, "Speech", "SilosNeeded", Owner.Country.Race);
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
			if (Game.Settings.Sound.SoundCashTickType != SoundCashTicks.Disabled)
			{
				Sound.PlayNotification(self.Owner, "Sounds", "CashTickUp", self.Owner.Country.Race);
			}
		}
		
		public void playCashTickDown(Actor self)
		{
			if (
				Game.Settings.Sound.SoundCashTickType == SoundCashTicks.Extreme ||
				(Game.Settings.Sound.SoundCashTickType == SoundCashTicks.Normal && cashtickallowed == 0)
			) {
				Sound.PlayNotification(self.Owner, "Sounds", "CashTickDown", self.Owner.Country.Race);
				cashtickallowed = 3;
			}
			
		}
	}
}
