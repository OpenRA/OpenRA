using System.Linq;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class ProductionSurroundInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ProductionSurround(self); }
	}

	class ProductionSurround : Production
	{
		public ProductionSurround(Actor self) : base(self) { }

		static int2? FindAdjacentTile(Actor a, UnitMovementType umt)
		{
			var tiles = Footprint.Tiles(a, a.traits.Get<Traits.Building>());
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (Game.world.IsCellBuildable(new int2(i, j), umt))
						return new int2(i, j);

			return null;
		}

		public override int2? CreationLocation(Actor self, ActorInfo producee)
		{
			return FindAdjacentTile(self, producee.Traits.Get<OwnedActorInfo>().WaterBound ?
					UnitMovementType.Float : UnitMovementType.Wheel);	/* hackety hack */
		}

		public override int CreationFacing(Actor self, Actor newUnit)
		{
			return Util.GetFacing(newUnit.CenterLocation - self.CenterLocation, 128);
		}
	}
}
