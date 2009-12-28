using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Effects;
namespace OpenRa.Game.Traits
{
	class APMine : ICrushable, IOccupySpace
	{
		readonly Actor self;
		public APMine(Actor self)
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
				Game.world.Add(new Explosion(self.CenterLocation.ToInt2(), 5, false));
				crusher.InflictDamage(crusher, Rules.General.APMineDamage, Rules.WarheadInfo["APMine"]);
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
				case UnitMovementType.Foot: 
				case UnitMovementType.Wheel:
				case UnitMovementType.Track:
					return true;

				default: return false;
			}
		}

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
	}
}
