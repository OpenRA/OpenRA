#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor to collect observer stats.")]
	public class PlayerStatisticsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlayerStatistics(init.Self); }
	}

	public class PlayerStatistics : ITick, IResolveOrder, INotifyCreated
	{
		PlayerResources resources;
		PlayerExperience experience;

		public int OrderCount;

		public int EarnedThisMinute
		{
			get
			{
				return resources != null ? resources.Earned - earnedAtBeginningOfMinute : 0;
			}
		}

		public int Experience
		{
			get
			{
				return experience != null ? experience.Experience : 0;
			}
		}

		public Queue<int> EarnedSamples = new Queue<int>(100);
		int earnedAtBeginningOfMinute;

		public int KillsCost;
		public int DeathsCost;

		public int UnitsKilled;
		public int UnitsDead;

		public int BuildingsKilled;
		public int BuildingsDead;

		public PlayerStatistics(Actor self) { }

		void INotifyCreated.Created(Actor self)
		{
			resources = self.TraitOrDefault<PlayerResources>();
			experience = self.TraitOrDefault<PlayerExperience>();
		}

		void UpdateEarnedThisMinute()
		{
			EarnedSamples.Enqueue(EarnedThisMinute);
			earnedAtBeginningOfMinute = resources != null ? resources.Earned : 0;
			if (EarnedSamples.Count > 100)
				EarnedSamples.Dequeue();
		}

		void ITick.Tick(Actor self)
		{
			if (self.World.WorldTick % 1500 == 1)
				UpdateEarnedThisMinute();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Chat":
				case "TeamChat":
				case "HandshakeResponse":
				case "PauseGame":
				case "StartGame":
				case "Disconnected":
				case "ServerError":
				case "AuthenticationError":
				case "SyncLobbyInfo":
				case "SyncClientInfo":
				case "SyncLobbySlots":
				case "SyncLobbyGlobalSettings":
				case "SyncClientPing":
				case "Ping":
				case "Pong":
					return;
			}

			if (order.OrderString.StartsWith("Dev"))
				return;
			OrderCount++;
		}
	}

	[Desc("Attach this to a unit to update observer stats.")]
	public class UpdatesPlayerStatisticsInfo : TraitInfo<UpdatesPlayerStatistics> { }

	public class UpdatesPlayerStatistics : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			if (self.Owner.WinState != WinState.Undefined)
				return;

			var attackerStats = e.Attacker.Owner.PlayerActor.Trait<PlayerStatistics>();
			var defenderStats = self.Owner.PlayerActor.Trait<PlayerStatistics>();
			if (self.Info.HasTraitInfo<BuildingInfo>())
			{
				attackerStats.BuildingsKilled++;
				defenderStats.BuildingsDead++;
			}
			else if (self.Info.HasTraitInfo<IPositionableInfo>())
			{
				attackerStats.UnitsKilled++;
				defenderStats.UnitsDead++;
			}

			if (self.Info.HasTraitInfo<ValuedInfo>())
			{
				var cost = self.Info.TraitInfo<ValuedInfo>().Cost;
				attackerStats.KillsCost += cost;
				defenderStats.DeathsCost += cost;
			}
		}
	}
}
