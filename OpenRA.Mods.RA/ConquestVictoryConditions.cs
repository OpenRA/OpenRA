#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
	public class ConquestVictoryConditionsInfo : ITraitInfo
	{
		public int NotificationDelay = 1500; // Milliseconds

		public object Create(ActorInitializer init) { return new ConquestVictoryConditions(this); }
	}

	public class ConquestVictoryConditions : ITick, IResolveOrder
	{
		ConquestVictoryConditionsInfo Info;
		public ConquestVictoryConditions(ConquestVictoryConditionsInfo info) { Info = info; }

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;

			var hasAnything = self.World.ActorsWithTrait<MustBeDestroyed>()
				.Any( a => a.Actor.Owner == self.Owner );

			if (!hasAnything && !self.Owner.NonCombatant)
				Lose(self);

			var others = self.World.Players.Where( p => !p.NonCombatant
				&& p != self.Owner && p.Stances[self.Owner] != Stance.Ally );

			if (others.Count() == 0) return;

			if(others.All(p => p.WinState == WinState.Lost))
				Win(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				Lose(self);
		}

		public void Lose(Actor self)
		{
			if (self.Owner.WinState == WinState.Lost) return;
			self.Owner.WinState = WinState.Lost;

			Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));

			foreach (var a in self.World.Actors.Where(a => a.Owner == self.Owner))
				a.Kill(a);

			self.Owner.Shroud.Disabled = true;
			if (self.Owner == self.World.LocalPlayer)
			{
				Game.RunAfterDelay(Info.NotificationDelay, () =>
				{
					if (Game.IsCurrentWorld(self.World))
						Sound.PlayNotification(self.Owner, "Speech", "Lose", self.Owner.Country.Race);
				});
			}
		}

		public void Win(Actor self)
		{
			if (self.Owner.WinState == WinState.Won) return;
			self.Owner.WinState = WinState.Won;

			Game.Debug("{0} is victorious.".F(self.Owner.PlayerName));
			self.Owner.Shroud.Disabled = true;
			if (self.Owner == self.World.LocalPlayer)
			{
				Game.RunAfterDelay(Info.NotificationDelay, () => Sound.PlayNotification(self.Owner, "Speech", "Win", self.Owner.Country.Race));
			}
		}
	}

	/* tag trait for things that must be destroyed for a short game to end */

	public class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	public class MustBeDestroyed { }

	// Provides game mode information for players/observers
	// Goes on WorldActor - observers don't have a player it can live on
	public class ConquestObjectivesPanelInfo : ITraitInfo
	{
		public string ObjectivesPanel = null;
		public object Create(ActorInitializer init) { return new ConquestObjectivesPanel(this); }
	}

	public class ConquestObjectivesPanel : IObjectivesPanel
	{
		ConquestObjectivesPanelInfo info;
		public ConquestObjectivesPanel(ConquestObjectivesPanelInfo info) { this.info = info; }
		public string ObjectivesPanel { get { return info.ObjectivesPanel; } }
	}
}
