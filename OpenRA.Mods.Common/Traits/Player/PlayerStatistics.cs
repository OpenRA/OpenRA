#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor to collect observer stats.")]
	public class PlayerStatisticsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlayerStatistics(init.Self); }
	}

	public class PlayerStatistics : ITick, IResolveOrder, INotifyCreated, IWorldLoaded
	{
		PlayerResources resources;
		PlayerExperience experience;

		public int OrderCount;

		public int Experience
		{
			get
			{
				return experience != null ? experience.Experience : 0;
			}
		}

		// Low resolution (every 30 seconds) record of earnings, covering the entire game
		public List<int> IncomeSamples = new List<int>(100);
		public int Income;
		public int DisplayIncome;

		public List<int> ArmySamples = new List<int>(100);

		public int KillsCost;
		public int DeathsCost;

		public int UnitsKilled;
		public int UnitsDead;

		public int BuildingsKilled;
		public int BuildingsDead;

		public int ArmyValue;

		// High resolution (every second) record of earnings, limited to the last minute
		readonly Queue<int> earnedSeconds = new Queue<int>(60);

		int lastIncome;
		int lastIncomeTick;
		int ticks;
		int replayTimestep;

		public PlayerStatistics(Actor self) { }

		void INotifyCreated.Created(Actor self)
		{
			resources = self.TraitOrDefault<PlayerResources>();
			experience = self.TraitOrDefault<PlayerExperience>();
		}

		void ITick.Tick(Actor self)
		{
			ticks++;

			var timestep = self.World.IsReplay ? replayTimestep : self.World.Timestep;
			if (ticks * timestep >= 30000)
			{
				ticks = 0;

				if (ArmyValue != 0 || self.Owner.WinState == WinState.Undefined)
					ArmySamples.Add(ArmyValue);

				if (resources != null && (Income != 0 || self.Owner.WinState == WinState.Undefined))
					IncomeSamples.Add(Income);
			}

			if (resources == null)
				return;

			var tickDelta = self.World.WorldTick - lastIncomeTick;
			if (tickDelta * timestep >= 1000)
			{
				lastIncomeTick = self.World.WorldTick;

				var lastEarned = earnedSeconds.Count > 59 ? earnedSeconds.Dequeue() : 0;
				lastIncome = DisplayIncome = Income;
				Income = resources.Earned - lastEarned;
				earnedSeconds.Enqueue(resources.Earned);
			}
			else
				DisplayIncome = int2.Lerp(lastIncome, Income, tickDelta * timestep, 1000);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Chat":
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

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.IsReplay)
				replayTimestep = w.WorldActor.Trait<MapOptions>().GameSpeed.Timestep;

			ArmySamples.Add(ArmyValue);
			IncomeSamples.Add(Income);
		}
	}

	[Desc("Attach this to a unit to update observer stats.")]
	public class UpdatesPlayerStatisticsInfo : ITraitInfo
	{
		[Desc("Add to army value in statistics")]
		public bool AddToArmyValue = false;

		public object Create(ActorInitializer init) { return new UpdatesPlayerStatistics(this, init.Self); }
	}

	public class UpdatesPlayerStatistics : INotifyKilled, INotifyCreated, INotifyOwnerChanged, INotifyActorDisposing
	{
		UpdatesPlayerStatisticsInfo info;
		PlayerStatistics playerStats;
		int cost = 0;
		bool includedInArmyValue = false;

		public UpdatesPlayerStatistics(UpdatesPlayerStatisticsInfo info, Actor self)
		{
			this.info = info;
			if (self.Info.HasTraitInfo<ValuedInfo>())
				cost = self.Info.TraitInfo<ValuedInfo>().Cost;
			playerStats = self.Owner.PlayerActor.Trait<PlayerStatistics>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
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

			attackerStats.KillsCost += cost;
			defenderStats.DeathsCost += cost;
			if (includedInArmyValue)
			{
				defenderStats.ArmyValue -= cost;
				includedInArmyValue = false;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			includedInArmyValue = info.AddToArmyValue;
			if (includedInArmyValue)
				playerStats.ArmyValue += cost;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			var newOwnerStats = newOwner.PlayerActor.Trait<PlayerStatistics>();
			if (includedInArmyValue)
			{
				playerStats.ArmyValue -= cost;
				newOwnerStats.ArmyValue += cost;
			}

			playerStats = newOwnerStats;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (includedInArmyValue)
			{
				playerStats.ArmyValue -= cost;
				includedInArmyValue = false;
			}
		}
	}
}
