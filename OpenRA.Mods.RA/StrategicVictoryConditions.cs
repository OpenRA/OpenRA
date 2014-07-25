#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class StrategicPointInfo : TraitInfo<StrategicPoint> {}
	public class StrategicPoint {}

	public class StrategicVictoryConditionsInfo : ITraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Amount of time (in game ticks) that the player has to hold all the strategic points.")]
		public readonly int TicksToHold = 25 * 60 * 5; // ~5 minutes

		[Desc("Should the timer reset when the player loses hold of a strategic point.")]
		public readonly bool ResetOnHoldLost = true;

		[Desc("Percentage of strategic points the player has to hold to win.")]
		public readonly float RatioRequired = 0.5f; // 50% required of all koth locations

		[Desc("Delay for the end game notification in milliseconds.")]
		public int NotificationDelay = 1500;

		public object Create(ActorInitializer init) { return new StrategicVictoryConditions(init.self, this); }
	}

	public class StrategicVictoryConditions : ITick, ISync, INotifyObjectivesUpdated
	{
		readonly StrategicVictoryConditionsInfo info;

		[Sync] public int TicksLeft;
		readonly Player player;
		readonly MissionObjectives mo;
		int objectiveID = -1;

		public StrategicVictoryConditions(Actor self, StrategicVictoryConditionsInfo svcInfo)
		{
			info = svcInfo;
			TicksLeft = info.TicksToHold;
			player = self.Owner;
			mo = self.Trait<MissionObjectives>();
		}

		public IEnumerable<TraitPair<StrategicPoint>> AllPoints
		{
			get { return player.World.ActorsWithTrait<StrategicPoint>(); }
		}

		public int Total { get { return AllPoints.Count(); } }
		int Owned { get { return AllPoints.Count(a => WorldUtils.AreMutualAllies(player, a.Actor.Owner)); } }

		public bool Holding { get { return Owned >= info.RatioRequired * Total; } }

		public void Tick(Actor self)
		{
			if (player.WinState != WinState.Undefined || player.NonCombatant) return;

			if (objectiveID < 0)
				objectiveID = mo.Add(player, "Hold all the strategic positions for a specified time!");

			if (!self.Owner.NonCombatant && self.Owner.HasNoRequiredUnits())
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
					// Hah! We met ths critical owned condition
					if (--TicksLeft == 0)
						mo.MarkCompleted(player, objectiveID);
				}
				else if (TicksLeft != 0)
					if (info.ResetOnHoldLost)
						TicksLeft = info.TicksToHold; // Reset the time hold
			}
		}

		public void OnPlayerLost(Player player)
		{
			Game.Debug("{0} is defeated.".F(player.PlayerName));

			foreach (var a in player.World.Actors.Where(a => a.Owner == player))
				a.Kill(a);

			if (player == player.World.LocalPlayer)
			{
				Game.RunAfterDelay(info.NotificationDelay, () =>
				{
					if (Game.IsCurrentWorld(player.World))
						Sound.PlayNotification(player.World.Map.Rules, player, "Speech", "Lose", player.Country.Race);
				});
			}
		}

		public void OnPlayerWon(Player player)
		{
			Game.Debug("{0} is victorious.".F(player.PlayerName));

			if (player == player.World.LocalPlayer)
				Game.RunAfterDelay(info.NotificationDelay, () => Sound.PlayNotification(player.World.Map.Rules, player, "Speech", "Win", player.Country.Race));
		}

		public void OnObjectiveAdded(Player player, int id) {}
		public void OnObjectiveCompleted(Player player, int id) {}
		public void OnObjectiveFailed(Player player, int id) {}
	}
}
