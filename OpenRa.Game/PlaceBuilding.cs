using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class PlaceBuilding : IOrderGenerator
	{
		public readonly Player Owner;
		public readonly string Name;

		public PlaceBuilding(Player owner, string name)
		{
			Owner = owner;
			Name = name;
		}

		public IEnumerable<Order> Order(int2 xy, bool lmb)
		{
			if( lmb )
			{
				// todo: check that space is free
				var bi = (UnitInfo.BuildingInfo)Rules.UnitInfo[ Name ];
				if( Footprint.Tiles( bi, xy ).Any(
					t => !Game.IsCellBuildable( t,
						bi.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel ) ) )
					yield break;

				var maxDistance = bi.Adjacent + 2;	/* real-ra is weird. this is 1 GAP. */
				if( !Footprint.Tiles( bi, xy ).Any(
					t => Game.GetDistanceToBase( t, Owner ) < maxDistance ) )
					yield break;

				yield return OpenRa.Game.Order.PlaceBuilding( Owner, xy, Name );
			}
			else // rmb
			{
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
			}
		}

		public void Tick() { }
	}
}
