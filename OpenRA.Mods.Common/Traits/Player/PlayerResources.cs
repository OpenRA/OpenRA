#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	[TraitLocation(SystemActors.Player | SystemActors.EditorPlayer)]
	public class PlayerResourcesInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the starting cash option in the lobby.")]
		public readonly string DefaultCashDropdownLabel = "Starting Cash";

		[Desc("Tooltip description for the starting cash option in the lobby.")]
		public readonly string DefaultCashDropdownDescription = "The amount of cash that players start with";

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

		[Desc("Text notification to display when the player does not have any funds.")]
		public readonly string InsufficientFundsTextNotification = null;

		[Desc("Delay (in milliseconds) during which warnings will be muted.")]
		public readonly int InsufficientFundsNotificationInterval = 30000;

		[NotificationReference("Sounds")]
		public readonly string CashTickUpNotification = null;

		[NotificationReference("Sounds")]
		public readonly string CashTickDownNotification = null;

		[Desc("Monetary value of each resource type.", "Dictionary of [resource type]: [value per unit].")]
		public readonly Dictionary<string, int> ResourceValues = new();

		[Desc("Special value of each resource type.", "Dictionary of [resource type]: [usage1 type : value type], [usage2 type : value type]...  .")]
		public readonly Dictionary<string, Dictionary<string, int>> SpecialResourceValues = new Dictionary<string, Dictionary<string, int>>();

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			var startingCash = SelectableCash.ToDictionary(c => c.ToString(), c => "$" + c.ToString());

			if (startingCash.Count > 0)
				yield return new LobbyOption(map, "startingcash", DefaultCashDropdownLabel, DefaultCashDropdownDescription, DefaultCashDropdownVisible, DefaultCashDropdownDisplayOrder,
					startingCash, DefaultCash.ToString(), DefaultCashDropdownLocked);
		}

		public override object Create(ActorInitializer init) { return new PlayerResources(init.Self, this); }
	}

	public class PlayerResources : ISync
	{
		public readonly PlayerResourcesInfo Info;
		readonly Player owner;

		public readonly string[] SpecialResourcesTypes;
		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			Info = info;
			owner = self.Owner;

			var startingCash = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("startingcash", info.DefaultCash.ToString());

			if (!int.TryParse(startingCash, out Cash))
				Cash = info.DefaultCash;

			lastNotificationTime = -Info.InsufficientFundsNotificationInterval;

			foreach (var resUsages in info.SpecialResourceValues)
			{
				foreach (var usage in resUsages.Value)
				{
					if (!specialResources.ContainsKey(usage.Key))
					{
						specialResources.Add(usage.Key, 0);
						SpecialResourcesCapacity.Add(usage.Key, 0);
					}
				}
			}

			SpecialResourcesTypes = new string[specialResources.Count];
			var i = 0;
			foreach (var kv in specialResources)
			{
				SpecialResourcesTypes[i] = kv.Key;
				i++;
			}
		}

		[Sync]
		public int Cash;

		[Sync]
		public int Resources;

		[Sync]
		public int ResourceCapacity;

		readonly Dictionary<string, int> specialResources = new Dictionary<string, int>();
		public readonly Dictionary<string, int> SpecialResourcesCapacity = new Dictionary<string, int>();

		public int Earned;
		public int Spent;

		long lastNotificationTime;

		public void GiveSpecialRawResources(int count, string resourceValue)
		{
			Dictionary<string, int> allUsage;
			if (Info.SpecialResourceValues.TryGetValue(resourceValue, out allUsage))
			{
				foreach (var u in allUsage)
				{
					GiveSpecialResources(u.Value * count, u.Key);
				}
			}
		}

		public bool CanAcceptSpecialRawResources(string resourceValue)
		{
			return Info.SpecialResourceValues.ContainsKey(resourceValue);
		}

		public void GiveSpecialResources(int num, string type)
		{
			if (specialResources.ContainsKey(type))
			{
				specialResources[type] += num;
			}
		}

		public int HasSpecialResources(string type)
		{
			if (specialResources.ContainsKey(type))
			{
				return specialResources[type];
			}

			return 0;
		}

		public bool HasSpecialResourcesType(string type)
		{
			if (specialResources.ContainsKey(type))
			{
				return true;
			}

			return false;
		}

		public bool TakeSpecialResources(int num, string type)
		{
			if (specialResources.ContainsKey(type))
			{
				if (specialResources[type] < num)
					return false;
				specialResources[type] -= num;
			}
			else
				return false;

			return true;
		}

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
				if (notifyLowFunds && Game.RunTime > lastNotificationTime + Info.InsufficientFundsNotificationInterval)
				{
					lastNotificationTime = Game.RunTime;
					Game.Sound.PlayNotification(owner.World.Map.Rules, owner, "Speech", Info.InsufficientFundsNotification, owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(Info.InsufficientFundsTextNotification, owner);
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

		public void AddSpecialStorage(int capacity, string type)
		{
			if (SpecialResourcesCapacity.ContainsKey(type))
				SpecialResourcesCapacity[type] += capacity;
		}

		public void RemoveSpecialStorage(int capacity, string type)
		{
			if (SpecialResourcesCapacity.ContainsKey(type))
				SpecialResourcesCapacity[type] -= capacity;

			if (specialResources[type] > SpecialResourcesCapacity[type])
				specialResources[type] = SpecialResourcesCapacity[type];
		}
	}
}
