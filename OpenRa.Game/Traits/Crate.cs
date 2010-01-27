using System.Collections.Generic;
using System.Linq;
using OpenRa.Effects;
using OpenRa.Traits;

namespace OpenRa.Traits
{
	class CrateInfo : ITraitInfo
	{
		public readonly int Lifetime = 5; // Seconds
		public object Create(Actor self) { return new Crate(self); }
	}

	class Crate : ICrushable, IOccupySpace, ITick
	{
		readonly Actor self;
		int ticks;
		public Crate(Actor self)
		{
			this.self = self;
			self.World.UnitInfluence.Add(self, this);
		}

		public void OnCrush(Actor crusher)
		{
			// TODO: Do Stuff
			
			
			self.World.AddFrameEndTask(w =>	w.Remove(self));
		}

		public bool IsPathableCrush(UnitMovementType umt, Player player)
		{
			return true;
		}

		public bool IsCrushableBy(UnitMovementType umt, Player player)
		{
			return true;
		}

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
		
		public void Tick(Actor self)
		{
			if (++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25)
			{
				self.World.AddFrameEndTask(w =>	w.Remove(self));
			}
		}
	}
}
