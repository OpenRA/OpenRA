using System.Collections.Generic;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class ATMineInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ATMine(self); }
	}

	class ATMine : ICrushable, IOccupySpace
	{
		readonly Actor self;
		public ATMine(Actor self)
		{
			this.self = self;
			Game.UnitInfluence.Add(self, this);
		}

		public void OnCrush(Actor crusher)
		{
			if (crusher.traits.Contains<MineImmune>() && crusher.Owner == self.Owner)
				return;

			Game.world.AddFrameEndTask(_ =>
			{
				Game.world.Remove(self);
				Game.world.Add(new Explosion(self.CenterLocation.ToInt2(), 3, false));
				crusher.InflictDamage(crusher, Rules.General.AVMineDamage, Rules.WarheadInfo["ATMine"]);
			});
		}

		public bool IsPathableCrush(UnitMovementType umt, Player player)
		{
			return (player != Game.LocalPlayer); // Units should avoid friendly mines
		}

		public bool IsCrushableBy(UnitMovementType umt, Player player)
		{
			// Mines should explode indiscriminantly of player
			switch (umt)
			{
				case UnitMovementType.Wheel:
				case UnitMovementType.Track: return true;
				default: return false;
			}
		}

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
	}
}
