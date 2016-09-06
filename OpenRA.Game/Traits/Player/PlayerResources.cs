#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public class PlayerResourcesInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Starting cash options that are available in the lobby options.")]
		public readonly int[] SelectableCash = { 2500, 5000, 10000, 20000 };

		[Desc("Default starting cash option: should be one of the SelectableCash options.")]
		public readonly int DefaultCash = 5000;

		[Desc("Force the DefaultCash option by disabling changes in the lobby.")]
		public readonly bool DefaultCashLocked = false;

		[Desc("Speech notification to play when the player does not have any funds.")]
		public readonly string InsufficientFundsNotification = null;

		[Desc("Delay (in ticks) during which warnings will be muted.")]
		public readonly int InsufficientFundsNotificationDelay = 750;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var startingCash = SelectableCash.ToDictionary(c => c.ToString(), c => "$" + c.ToString());

			if (startingCash.Any())
				yield return new LobbyOption("startingcash", "Starting Cash", new ReadOnlyDictionary<string, string>(startingCash), DefaultCash.ToString(), DefaultCashLocked);
		}

		public object Create(ActorInitializer init) { return new PlayerResources(init.Self, this); }
	}

	public class PlayerResources : ITick, ISync
	{
		const float DisplayCashFracPerFrame = .07f;
		const int DisplayCashDeltaPerFrame = 37;
		readonly PlayerResourcesInfo info;
		readonly Player owner;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			this.info = info;
			owner = self.Owner;

			var startingCash = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("startingcash", info.DefaultCash.ToString());

			if (!int.TryParse(startingCash, out Cash))
				Cash = info.DefaultCash;
		}

		[Sync] public int Cash;

		[Sync] public int Resources;
		[Sync] public int ResourceCapacity;

		public int DisplayCash;
		public int DisplayResources;

		public int Earned;
		public int Spent;

		int lastNotificationTick;

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
			if (Cash < int.MaxValue)
			{
				try
				{
					checked
					{
						Cash += num;
					}
				}
				catch (OverflowException)
				{
					Cash = int.MaxValue;
				}
			}

			if (Earned < int.MaxValue)
			{
				try
				{
					checked
					{
						Earned += num;
					}
				}
				catch (OverflowException)
				{
					Earned = int.MaxValue;
				}
			}
		}

		public bool TakeCash(int num, bool notifyLowFunds = false)
		{
			if (Cash + Resources < num)
			{
				if (notifyLowFunds && !string.IsNullOrEmpty(info.InsufficientFundsNotification) &&
					owner.World.WorldTick - lastNotificationTick >= info.InsufficientFundsNotificationDelay)
				{
					lastNotificationTick = owner.World.WorldTick;
					Game.Sound.PlayNotification(owner.World.Map.Rules, owner, "Speech", info.InsufficientFundsNotification, owner.Faction.InternalName);
				}

				return false;
			}

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

		int nextCashTickTime = 0;

		public void Tick(Actor self)
		{
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			ResourceCapacity = self.World.ActorsWithTrait<IStoreResources>()
				.Where(a => a.Actor.Owner == owner)
				.Sum(a => a.Trait.Capacity);

			if (Resources > ResourceCapacity)
				Resources = ResourceCapacity;

			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * DisplayCashFracPerFrame), DisplayCashDeltaPerFrame), diff);

			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				PlayCashTickUp(self);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				PlayCashTickDown(self);
			}

			diff = Math.Abs(Resources - DisplayResources);
			move = Math.Min(Math.Max((int)(diff * DisplayCashFracPerFrame),
					DisplayCashDeltaPerFrame), diff);

			if (DisplayResources < Resources)
			{
				DisplayResources += move;
				PlayCashTickUp(self);
			}
			else if (DisplayResources > Resources)
			{
				DisplayResources -= move;
				PlayCashTickDown(self);
			}
		}

		public void PlayCashTickUp(Actor self)
		{
			if (Game.Settings.Sound.CashTicks)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickUp", self.Owner.Faction.InternalName);
		}

		public void PlayCashTickDown(Actor self)
		{
			if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickDown", self.Owner.Faction.InternalName);
				nextCashTickTime = 2;
			}
		}
	}
}
