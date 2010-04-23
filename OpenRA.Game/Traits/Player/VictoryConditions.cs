using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class VictoryConditionsInfo : ITraitInfo
	{
		public readonly string[] ShortGameUnits = { "mcv" };
		public object Create(Actor self) { return new VictoryConditions( self ); }
	}

	interface IVictoryConditions { bool HasLost { get; } bool HasWon { get; } }

	class VictoryConditions : ITick, IVictoryConditions
	{
		public bool HasLost { get; private set; }
		public bool HasWon { get; private set; }

		public VictoryConditions(Actor self) { }

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<VictoryConditionsInfo>();
			var hasAnything = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<MustBeDestroyed>().Any();

			if (!hasAnything && !HasLost)
				Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));

			HasLost = !hasAnything;
		}
	}

	/* tag trait for things that must be destroyed for a short game to end */

	class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	class MustBeDestroyed { }
}
