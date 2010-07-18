#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ConquestVictoryConditionsInfo : TraitInfo<ConquestVictoryConditions> { }

	class ConquestVictoryConditions : ITick, IVictoryConditions, IResolveOrder
	{
		public bool HasLost { get; private set; }
		public bool HasWon { get; private set; }

		public void Tick(Actor self)
		{
			var hasAnything = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<MustBeDestroyed>().Any();

			var hasLost = !hasAnything && !self.Owner.NonCombatant;

			if (hasLost && !HasLost)
				Surrender(self);

			HasLost = hasLost;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				Surrender(self);
		}

		void Surrender(Actor self)
		{
			Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));
			foreach (var a in self.World.Queries.OwnedBy[self.Owner])
				a.InflictDamage(a, a.Health, null);

			self.Owner.Shroud.Disabled = true;
			HasLost = true;
		}
	}

	/* tag trait for things that must be destroyed for a short game to end */

	class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	class MustBeDestroyed { }
}
