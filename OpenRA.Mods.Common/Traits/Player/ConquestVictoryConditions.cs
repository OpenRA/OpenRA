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

using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ConquestVictoryConditionsInfo : TraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Delay for the end game notification in milliseconds.")]
		public readonly int NotificationDelay = 1500;

		[Desc("Description of the objective.")]
		public readonly string Objective = "Destroy all opposition!";

		[Desc("Disable the win/loss messages and audio notifications?")]
		public readonly bool SuppressNotifications = false;

		public override object Create(ActorInitializer init) { return new ConquestVictoryConditions(init.Self, this); }
	}

	public class ConquestVictoryConditions : ITick, INotifyWinStateChanged, INotifyTimeLimit
	{
		readonly ConquestVictoryConditionsInfo info;
		readonly MissionObjectives mo;
		readonly bool shortGame;
		Player[] otherPlayers;
		int objectiveID = -1;

		public ConquestVictoryConditions(Actor self, ConquestVictoryConditionsInfo cvcInfo)
		{
			info = cvcInfo;
			mo = self.Trait<MissionObjectives>();
			shortGame = self.Owner.World.WorldActor.Trait<MapOptions>().ShortGame;
		}

		void ITick.Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant)
				return;

			if (objectiveID < 0)
				objectiveID = mo.Add(self.Owner, info.Objective, "Primary", inhibitAnnouncement: true);

			if (!self.Owner.NonCombatant && self.Owner.HasNoRequiredUnits(shortGame))
				mo.MarkFailed(self.Owner, objectiveID);

			// Players, NonCombatants, and IsAlliedWith are all fixed once the game starts, so we can cache the result.
			if (otherPlayers == null)
				otherPlayers = self.World.Players.Where(p => !p.NonCombatant && !p.IsAlliedWith(self.Owner)).ToArray();

			if (otherPlayers.Length == 0) return;

			// PERF: Avoid LINQ.
			foreach (var otherPlayer in otherPlayers)
				if (otherPlayer.WinState != WinState.Lost)
					return;

			mo.MarkCompleted(self.Owner, objectiveID);
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (objectiveID < 0)
				return;

			var myTeam = self.World.LobbyInfo.ClientWithIndex(self.Owner.ClientIndex).Team;
			var teams = self.World.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (self.World.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0));

			if (teams.First().Key == myTeam && (myTeam != 0 || teams.First().First().Player == self.Owner))
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

			TextNotificationsManager.AddSystemLine(player.PlayerName + " is defeated.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.LoseNotification, player.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(mo.Info.LoseTextNotification, player);
				}
			});
		}

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (info.SuppressNotifications)
				return;

			TextNotificationsManager.AddSystemLine(player.PlayerName + " is victorious.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.WinNotification, player.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(mo.Info.WinTextNotification, player);
				}
			});
		}
	}
}
