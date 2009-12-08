using OpenRa.Game.GameRules;
using System.Linq;

namespace OpenRa.Game.Traits
{
	class Production : IProducer
	{
		public Production( Actor self ) { }

		public virtual int2? CreationLocation( Actor self, UnitInfo producee )
		{
			return ( 1 / 24f * self.CenterLocation ).ToInt2();
		}

		public virtual int CreationFacing( Actor self, Actor newUnit )
		{
			return newUnit.Info.InitialFacing;
		}

		public bool Produce( Actor self, UnitInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || Game.UnitInfluence.GetUnitAt( location.Value ) != null )
				return false;

			var newUnit = new Actor( producee, location.Value, self.Owner );
			newUnit.traits.Get<Unit>().Facing = CreationFacing( self, newUnit ); ;

			var rp = self.traits.GetOrDefault<RallyPoint>();
			if( rp != null )
			{
				var mobile = newUnit.traits.GetOrDefault<Mobile>();
				if( mobile != null )
					newUnit.QueueActivity( new Activities.Move( rp.rallyPoint, 1 ) );

				var heli = newUnit.traits.GetOrDefault<Helicopter>();
				if (heli != null)
					heli.targetLocation = rp.rallyPoint; // TODO: make Activity.Move work for helis.
			}

			var bi = self.Info as BuildingInfo;
			if (bi != null && bi.SpawnOffset != null)
				newUnit.CenterLocation = self.CenterLocation 
					+ new float2(bi.SpawnOffset[0], bi.SpawnOffset[1]);

			Game.world.Add( newUnit );

			if( self.traits.Contains<RenderWarFactory>() )
				self.traits.Get<RenderWarFactory>().EjectUnit();

			return true;
		}
	}

	class ProductionSurround : Production
	{
		public ProductionSurround( Actor self ) : base( self ) { }

		static int2? FindAdjacentTile(Actor a, UnitMovementType umt)
		{
			var tiles = Footprint.Tiles(a, a.traits.Get<Traits.Building>());
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (Game.IsCellBuildable(new int2(i, j), umt))
						return new int2(i, j);

			return null;
		}

		public override int2? CreationLocation( Actor self, UnitInfo producee )
		{
			return FindAdjacentTile( self, producee.WaterBound ?
			        UnitMovementType.Float : UnitMovementType.Wheel);	/* hackety hack */
		}

		public override int CreationFacing( Actor self, Actor newUnit )
		{
			return Util.GetFacing( newUnit.CenterLocation - self.CenterLocation, 128 );
		}
	}
}
