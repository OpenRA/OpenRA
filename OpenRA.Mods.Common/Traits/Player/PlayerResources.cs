#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PlayerResourcesInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the starting cash option in the lobby.")]
		public readonly string DefaultCashDropdownLabel = "Starting Cash";

		[Desc("Tooltip description for the starting cash option in the lobby.")]
		public readonly string DefaultCashDropdownDescription = "Change the amount of cash that players start with";

		[Desc("Starting cash options that are available in the lobby options.")]
		public readonly int[] SelectableCash = { 2500, 5000, 10000, 20000 };

		[Desc("Default starting cash option: should be one of the SelectableCash options.")]
		public readonly int DefaultCash = 5000;

		[Desc("Force the DefaultCash option by disabling changes in the lobby.")]
		public readonly bool DefaultCashDropdownLocked = false;

		[Desc("Whether to display the DefaultCash option in the lobby.")]
		public readonly bool DefaultCashDropdownVisible = true;

		[Desc("Display order for the DefaultCash option.")]
		public readonly int DefaultCashDropdownDisplayOrder = 0;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when the player does not have any funds.")]
		public readonly string InsufficientFundsNotification = null;

		[Desc("Delay (in ticks) during which warnings will be muted.")]
		public readonly int InsufficientFundsNotificationDelay = 750;

		[NotificationReference("Sounds")]
		public readonly string CashTickUpNotification = null;

		[NotificationReference("Sounds")]
		public readonly string CashTickDownNotification = null;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var startingCash = SelectableCash.ToDictionary(c => c.ToString(), c => "$" + c.ToString());

			if (startingCash.Any())
				yield return new LobbyOption("startingcash", DefaultCashDropdownLabel, DefaultCashDropdownDescription, DefaultCashDropdownVisible, DefaultCashDropdownDisplayOrder,
					new ReadOnlyDictionary<string, string>(startingCash), DefaultCash.ToString(), DefaultCashDropdownLocked);
		}

		public override object Create(ActorInitializer init) { return new PlayerResources(init.Self, this); }
	}

	public class PlayerResources : ISync
	{
		public readonly PlayerResourcesInfo Info;
		readonly Player owner;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			Info = info;
			owner = self.Owner;

			var startingCash = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("startingcash", info.DefaultCash.ToString());

			if (!int.TryParse(startingCash, out Cash))
				Cash = info.DefaultCash;
		}

		[Sync]
		public int Cash;

		[Sync]
		public int Resources;

		[Sync]
		public int ResourceCapacity;

		public int Earned;
		public int Spent;

		int lastNotificationTick;

		public int ChangeCash(int amount)
		{
			if (amount >= 0)
				GiveCash(amount);
			else
			{
				// Don't put the player into negative funds
				amount = Math.Max(-(Cash + Resources), amount);

				TakeCash(-amount);
			}

			return amount;
		}

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
				if (notifyLowFunds && !string.IsNullOrEmpty(Info.InsufficientFundsNotification) &&
					owner.World.WorldTick - lastNotificationTick >= Info.InsufficientFundsNotificationDelay)
				{
					lastNotificationTick = owner.World.WorldTick;
					Game.Sound.PlayNotification(owner.World.Map.Rules, owner, "Speech", Info.InsufficientFundsNotification, owner.Faction.InternalName);
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

		public void AddStorage(int capacity)
		{
			ResourceCapacity += capacity;
		}

		public void RemoveStorage(int capacity)
		{
			ResourceCapacity -= capacity;

			if (Resources > ResourceCapacity)
				Resources = ResourceCapacity;
		}
	}
}
