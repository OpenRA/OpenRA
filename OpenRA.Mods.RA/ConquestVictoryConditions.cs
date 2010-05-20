#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ConquestVictoryConditionsInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ConquestVictoryConditions( self ); }
	}

	class ConquestVictoryConditions : ITick, IVictoryConditions, IResolveOrder
	{
		public bool HasLost { get; private set; }
		public bool HasWon { get; private set; }

		public ConquestVictoryConditions(Actor self) { }

		public void Tick(Actor self)
		{
			var hasAnything = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<MustBeDestroyed>().Any();

			var hasLost = !hasAnything && self.Owner != self.World.NeutralPlayer;

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
