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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to mark a place that needs to be in possession for StrategicVictoryConditions.")]
	public class StrategicPointInfo : TraitInfo<StrategicPoint> { }
	public class StrategicPoint { }

	[Desc("Allows King of the Hill (KotH) style gameplay.")]
	public class StrategicVictoryConditionsInfo : TraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Amount of time (in game ticks) that the player has to hold all the strategic points.", "Defaults to 7500 ticks (5 minutes at default speed).")]
		public readonly int HoldDuration = 7500;

		[Desc("Should the timer reset when the player loses hold of a strategic point.")]
		public readonly bool ResetOnHoldLost = true;

		[Desc("Percentage of all strategic points the player has to hold to win.")]
		public readonly int RatioRequired = 50;

		[Desc("Delay for the end game notification in milliseconds.")]
		public readonly int NotificationDelay = 1500;

		[Translate]
		[Desc("Description of the objective")]
		public readonly string Objective = "Hold all the strategic positions!";

		[Desc("Disable the win/loss messages and audio notifications?")]
		public readonly bool SuppressNotifications = false;

		public override object Create(ActorInitializer init) { return new StrategicVictoryConditions(init.Self, this); }
	}

	public class StrategicVictoryConditions : ITick, ISync, INotifyWinStateChanged, INotifyTimeLimit
	{
		readonly StrategicVictoryConditionsInfo info;

		[Sync]
		public int TicksLeft;

		readonly Player player;
		readonly MissionObjectives mo;
		readonly bool shortGame;
		int objectiveID = -1;

		public StrategicVictoryConditions(Actor self, StrategicVictoryConditionsInfo svcInfo)
		{
			info = svcInfo;
			TicksLeft = info.HoldDuration;
			player = self.Owner;
			mo = self.Trait<MissionObjectives>();
			shortGame = player.World.WorldActor.Trait<MapOptions>().ShortGame;
		}

		public IEnumerable<Actor> AllPoints
		{
			get { return player.World.ActorsHavingTrait<StrategicPoint>(); }
		}

		public int Total { get { return AllPoints.Count(); } }
		int Owned { get { return AllPoints.Count(a => WorldUtils.AreMutualAllies(player, a.Owner)); } }

		public bool Holding { get { return Owned >= info.RatioRequired * Total / 100; } }

		void ITick.Tick(Actor self)
		{
			if (player.WinState != WinState.Undefined || player.NonCombatant)
				return;

			if (objectiveID < 0)
				objectiveID = mo.Add(player, info.Objective, "Primary", inhibitAnnouncement: true);

			if (!self.Owner.NonCombatant && self.Owner.HasNoRequiredUnits(shortGame))
				mo.MarkFailed(self.Owner, objectiveID);

			var others = self.World.Players.Where(p => !p.NonCombatant
				&& !p.IsAlliedWith(self.Owner));

			if (others.All(p => p.WinState == WinState.Lost))
				mo.MarkCompleted(player, objectiveID);

			if (others.Any(p => p.WinState == WinState.Won))
				mo.MarkFailed(player, objectiveID);

			// See if any of the conditions are met to increase the count
			if (Total > 0)
			{
				if (Holding)
				{
					// Hah! We met this critical owned condition
					if (--TicksLeft == 0)
						mo.MarkCompleted(player, objectiveID);
				}
				else if (TicksLeft != 0)
					if (info.ResetOnHoldLost)
						TicksLeft = info.HoldDuration; // Reset the time hold
			}
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (objectiveID < 0)
				return;

			var myTeam = self.World.LobbyInfo.ClientWithIndex(self.Owner.ClientIndex).Team;
			var teams = self.World.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => new Pair<Player, PlayerStatistics>(p, p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.Second != null ? p.Second.Experience : 0)
				.GroupBy(p => (self.World.LobbyInfo.ClientWithIndex(p.First.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.Second != null ? gg.Second.Experience : 0));

			if (teams.First().Key == myTeam && (myTeam != 0 || teams.First().First().First == self.Owner))
			{
				mo.MarkCompleted(self.Owner, objectiveID);
				return;
			}

			mo.MarkFailed(self.Owner, objectiveID);
		}

		void INotifyWinStateChanged.OnPlayerLost(Player player)
		{
			foreach (var a in player.World.ActorsWithTrait<INotifyOwnerLost>().Where(a => a.Actor.Owner == player))
				a.Trait.OnOwnerLost(a.Actor);

			if (info.SuppressNotifications)
				return;

			Game.AddSystemLine(player.PlayerName + " is defeated.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.LoseNotification, player.Faction.InternalName);
			});
		}

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (info.SuppressNotifications)
				return;

			Game.AddSystemLine(player.PlayerName + " is victorious.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.WinNotification, player.Faction.InternalName);
			});
		}
	}
}
