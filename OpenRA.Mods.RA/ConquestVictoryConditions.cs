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
	public class ConquestVictoryConditionsInfo : TraitInfo<ConquestVictoryConditions> { }

	public class ConquestVictoryConditions : ITick, IResolveOrder
	{
		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;
			
			var hasAnything = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<MustBeDestroyed>().Any();

			if (!hasAnything && !self.Owner.NonCombatant)
				Surrender(self);
			
			var others = self.World.players.Where( p => !p.Value.NonCombatant && p.Value != self.Owner && p.Value.Stances[self.Owner] != Stance.Ally );
			if (others.Count() == 0) return;	
			
			if(others.All(p => p.Value.WinState == WinState.Lost))
				Win(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				Surrender(self);
		}

		public void Surrender(Actor self)
		{
			if (self.Owner.WinState == WinState.Lost) return;
			self.Owner.WinState = WinState.Lost;
			
			Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));
			foreach (var a in self.World.Queries.OwnedBy[self.Owner])
				a.Kill(a);

			if (self.Owner == self.World.LocalPlayer)
				self.World.LocalShroud.Disabled = true;
		}
		
		public void Win(Actor self)	
		{
			if (self.Owner.WinState == WinState.Won) return;
			self.Owner.WinState = WinState.Won;
			
			Game.Debug("{0} is victorious.".F(self.Owner.PlayerName));
			if (self.Owner == self.World.LocalPlayer)
				self.World.LocalShroud.Disabled = true;
		}
	}

	/* tag trait for things that must be destroyed for a short game to end */

	class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	class MustBeDestroyed { }
}
