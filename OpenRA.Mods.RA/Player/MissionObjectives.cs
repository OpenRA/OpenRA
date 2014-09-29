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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public enum ObjectiveType { Primary, Secondary };
	public enum ObjectiveState { Incomplete, Completed, Failed };

	public class MissionObjective
	{
		public readonly ObjectiveType Type;
		public readonly string Description;
		public ObjectiveState State;

		public MissionObjective(ObjectiveType type, string description)
		{
			Type = type;
			Description = description;
			State = ObjectiveState.Incomplete;
		}
	}

	public class MissionObjectivesInfo : ITraitInfo
	{
		[Desc("Set this to true if multiple cooperative players have a distinct set of " +
		      "objectives that each of them has to complete to win the game. This is mainly " +
		      "useful for multiplayer coop missions. Do not use this for skirmish team games.")]
		public readonly bool Cooperative = false;

		[Desc("If set to true, this setting causes the game to end immediately once the first " +
		      "player (or team of cooperative players) fails or completes his objectives.  If " +
		      "set to false, players that fail their objectives will stick around and become observers.")]
		public readonly bool EarlyGameOver = false;

		[Desc("Delay between the game over condition being met, and the game actually ending, in milliseconds.")]
		public readonly int GameOverDelay = 1500;

		public object Create(ActorInitializer init) { return new MissionObjectives(init.world, this); }
	}

	public class MissionObjectives : INotifyObjectivesUpdated, ISync, IResolveOrder
	{
		readonly MissionObjectivesInfo info;
		readonly List<MissionObjective> objectives = new List<MissionObjective>();
		public ReadOnlyList<MissionObjective> Objectives;

		[Sync]
		public int ObjectivesHash { get { return Objectives.Aggregate(0, (code, objective) => code ^ Sync.hash(objective.State)); } }

		// This property is used as a flag in 'Cooperative' games to mark that the player has completed all his objectives.
		// The player's WinState is only updated when his allies have all completed their objective as well.
		public WinState WinStateCooperative { get; private set; }

		public MissionObjectives(World world, MissionObjectivesInfo moInfo)
		{
			info = moInfo;
			Objectives = new ReadOnlyList<MissionObjective>(objectives);

			world.ObserveAfterWinOrLose = !info.EarlyGameOver;
		}

		public int Add(Player player, string description, ObjectiveType type = ObjectiveType.Primary)
		{
			var newID = objectives.Count;

			objectives.Insert(newID, new MissionObjective(type, description));

			ObjectiveAdded(player);
			foreach (var inou in player.PlayerActor.TraitsImplementing<INotifyObjectivesUpdated>())
				inou.OnObjectiveAdded(player, newID);

			return newID;
		}

		public void MarkCompleted(Player player, int objectiveID)
		{
			if (objectiveID >= objectives.Count || objectives[objectiveID].State != ObjectiveState.Incomplete)
				return;

			var inous = player.PlayerActor.TraitsImplementing<INotifyObjectivesUpdated>();

			objectives[objectiveID].State = ObjectiveState.Completed;
			foreach (var inou in inous)
				inou.OnObjectiveCompleted(player, objectiveID);

			if (objectives[objectiveID].Type == ObjectiveType.Primary)
			{
				var playerWon = objectives.Where(o => o.Type == ObjectiveType.Primary).All(o => o.State == ObjectiveState.Completed);

				if (playerWon)
				{
					foreach (var inou in inous)
						inou.OnPlayerWon(player);

					CheckIfGameIsOver(player);
				}
			}
		}

		public void MarkFailed(Player player, int objectiveID)
		{
			if (objectiveID >= objectives.Count || objectives[objectiveID].State == ObjectiveState.Failed)
				return;

			var inous = player.PlayerActor.TraitsImplementing<INotifyObjectivesUpdated>();

			objectives[objectiveID].State = ObjectiveState.Failed;
			foreach (var inou in inous)
				inou.OnObjectiveFailed(player, objectiveID);

			if (objectives[objectiveID].Type == ObjectiveType.Primary)
			{
				var playerLost = objectives.Where(o => o.Type == ObjectiveType.Primary).Any(o => o.State == ObjectiveState.Failed);

				if (playerLost)
				{
					foreach (var inou in inous)
						inou.OnPlayerLost(player);
	
					CheckIfGameIsOver(player);
				}
			}
		}

		void CheckIfGameIsOver(Player player)
		{
			var players = player.World.Players.Where(p => !p.NonCombatant);

			var gameOver = players.All(p => p.WinState != WinState.Undefined);
			if (gameOver)
				Game.RunAfterDelay(info.GameOverDelay, () =>
				{
					player.World.EndGame();
					player.World.SetPauseState(true);
					player.World.PauseStateLocked = true;
				});
		}

		public void OnPlayerWon(Player player)
		{
			var players = player.World.Players.Where(p => !p.NonCombatant);
			var enemies = players.Where(p => !p.IsAlliedWith(player));

			if (info.Cooperative)
			{
				WinStateCooperative = WinState.Won;
				var allies = players.Where(p => p.IsAlliedWith(player));

				if (allies.All(p => p.PlayerActor.Trait<MissionObjectives>().WinStateCooperative == WinState.Won))
				{
					foreach (var p in allies)
					{
						p.WinState = WinState.Won;
						p.World.OnPlayerWinStateChanged(p);
					}

					if (info.EarlyGameOver)
						foreach (var p in enemies)
							p.PlayerActor.Trait<MissionObjectives>().ForceDefeat(p);
				}
			}
			else
			{
				player.WinState = WinState.Won;
				player.World.OnPlayerWinStateChanged(player);

				if (info.EarlyGameOver)
					foreach (var p in enemies)
						p.PlayerActor.Trait<MissionObjectives>().ForceDefeat(p);
			}
		}

		public void OnPlayerLost(Player player)
		{
			var players = player.World.Players.Where(p => !p.NonCombatant);
			var enemies = players.Where(p => !p.IsAlliedWith(player));

			if (info.Cooperative)
			{
				WinStateCooperative = WinState.Lost;
				var allies = players.Where(p => p.IsAlliedWith(player));

				if (allies.Any(p => p.PlayerActor.Trait<MissionObjectives>().WinStateCooperative == WinState.Lost))
				{
					foreach (var p in allies)
					{
						p.WinState = WinState.Lost;
						p.World.OnPlayerWinStateChanged(p);
					}

					if (info.EarlyGameOver)
						foreach (var p in enemies)
							p.PlayerActor.Trait<MissionObjectives>().ForceDefeat(p);
				}
			}
			else
			{
				player.WinState = WinState.Lost;
				player.World.OnPlayerWinStateChanged(player);

				if (info.EarlyGameOver)
					foreach (var p in enemies)
					{
						p.WinState = WinState.Won;
						p.World.OnPlayerWinStateChanged(p);
					}
			}
		}

		public void ForceDefeat(Player player)
		{
			for (var id = 0; id < Objectives.Count; id++)
				if (Objectives[id].State == ObjectiveState.Incomplete)
					MarkFailed(player, id);
		}

		public event Action<Player> ObjectiveAdded = player => { };

		public void OnObjectiveAdded(Player player, int id) {}
		public void OnObjectiveCompleted(Player player, int id) {}
		public void OnObjectiveFailed(Player player, int id) {}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				ForceDefeat(self.Owner);
		}
	}

	[Desc("Provides game mode progress information for players.",
	      "Goes on WorldActor - observers don't have a player it can live on.",
	      "Current options for PanelName are 'SKIRMISH_STATS' and 'MISSION_OBJECTIVES'.")]
	public class ObjectivesPanelInfo : ITraitInfo
	{
		public string PanelName = null;
		public object Create(ActorInitializer init) { return new ObjectivesPanel(this); }
	}

	public class ObjectivesPanel : IObjectivesPanel
	{
		ObjectivesPanelInfo info;
		public ObjectivesPanel(ObjectivesPanelInfo info) { this.info = info; }
		public string PanelName { get { return info.PanelName; } }
	}
}
