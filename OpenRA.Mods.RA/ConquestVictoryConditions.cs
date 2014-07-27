#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ConquestVictoryConditionsInfo : ITraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Delay for the end game notification in milliseconds.")]
		public int NotificationDelay = 1500;

		public object Create(ActorInitializer init) { return new ConquestVictoryConditions(init.self, this); }
	}

	public class ConquestVictoryConditions : ITick, IResolveOrder, INotifyObjectivesUpdated
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
				objectiveID = mo.Add(self.Owner, "Destroy all opposition!");

			if (!self.Owner.NonCombatant && self.Owner.HasNoRequiredUnits())
				mo.MarkFailed(self.Owner, objectiveID);

			var others = self.World.Players.Where(p => !p.NonCombatant
				&& !p.IsAlliedWith(self.Owner));

			if (!others.Any()) return;

			if (others.All(p => p.WinState == WinState.Lost))
				mo.MarkCompleted(self.Owner, objectiveID);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				mo.MarkFailed(self.Owner, objectiveID);
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

	[Desc("Tag trait for things that must be destroyed for a short game to end.")]
	public class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	public class MustBeDestroyed { }
}
