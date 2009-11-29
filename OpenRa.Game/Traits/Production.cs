using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

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
			return newUnit.unitInfo.InitialFacing;
		}

		public bool Produce( Actor self, UnitInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || Game.UnitInfluence.GetUnitAt( location.Value ) != null )
				return false;

			var newUnit = new Actor( producee.Name, location.Value, self.Owner );
			newUnit.traits.Get<Unit>().Facing = CreationFacing( self, newUnit ); ;

			var rp = self.traits.GetOrDefault<RallyPoint>();
			if( rp != null )
			{
				var mobile = newUnit.traits.GetOrDefault<Mobile>();
				if( mobile != null )
					newUnit.QueueActivity( new Traits.Activities.Move( rp.rallyPoint, 1 ) );

				var heli = newUnit.traits.GetOrDefault<Helicopter>();
				if( heli != null )
					heli.targetLocation = rp.rallyPoint; // TODO: make Activity.Move work for helis.
			}

			Game.world.Add( newUnit );

			if( self.traits.Contains<RenderWarFactory>() )
				self.traits.Get<RenderWarFactory>().EjectUnit();

			return true;
		}
	}

	class ProductionSurround : Production
	{
		public ProductionSurround( Actor self ) : base( self ) { }

		public override int2? CreationLocation( Actor self, UnitInfo producee )
		{
			return Game.FindAdjacentTile( self, producee.WaterBound ?
			        UnitMovementType.Float : UnitMovementType.Wheel);	/* hackety hack */
		}

		public override int CreationFacing( Actor self, Actor newUnit )
		{
			return Util.GetFacing( newUnit.CenterLocation - self.CenterLocation, 128 );
		}
	}
}
