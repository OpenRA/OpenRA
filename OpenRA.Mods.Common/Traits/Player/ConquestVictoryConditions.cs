#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ConquestVictoryConditionsInfo : ITraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Delay for the end game notification in milliseconds.")]
		public readonly int NotificationDelay = 1500;

		[Desc("Description of the objective.")]
		[Translate] public readonly string Objective = "Destroy all opposition!";

		[Desc("Disable the win/loss messages and audio notifications?")]
		public readonly bool SuppressNotifications = false;

		public object Create(ActorInitializer init) { return new ConquestVictoryConditions(init.Self, this); }
	}

	public class ConquestVictoryConditions : ITick, INotifyObjectivesUpdated
	{
		readonly ConquestVictoryConditionsInfo info;
		readonly MissionObjectives mo;
		int objectiveID = -1;

		public ConquestVictoryConditions(Actor self, ConquestVictoryConditionsInfo cvcInfo)
		{
			info = cvcInfo;
			mo = self.Trait<MissionObjectives>();
		}

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;

			if (objectiveID < 0)
				objectiveID = mo.Add(self.Owner, info.Objective, ObjectiveType.Primary, true);

			if (!self.Owner.NonCombatant && self.Owner.HasNoRequiredUnits())
				mo.MarkFailed(self.Owner, objectiveID);

			var others = self.World.Players.Where(p => !p.NonCombatant
				&& !p.IsAlliedWith(self.Owner));

			if (!others.Any()) return;

			if (others.All(p => p.WinState == WinState.Lost))
				mo.MarkCompleted(self.Owner, objectiveID);
		}

		public void OnPlayerLost(Player player)
		{
			foreach (var a in player.World.Actors.Where(a => a.Owner == player))
				a.Kill(a);

			if (info.SuppressNotifications)
				return;

			Game.AddChatLine(Color.White, "Battlefield Control", player.PlayerName + " is defeated.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", "Lose", player.Faction.InternalName);
			});
		}

		public void OnPlayerWon(Player player)
		{
			if (info.SuppressNotifications)
				return;

			Game.AddChatLine(Color.White, "Battlefield Control", player.PlayerName + " is victorious.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", "Win", player.Faction.InternalName);
			});
		}

		public void OnObjectiveAdded(Player player, int id) { }
		public void OnObjectiveCompleted(Player player, int id) { }
		public void OnObjectiveFailed(Player player, int id) { }
	}
}
