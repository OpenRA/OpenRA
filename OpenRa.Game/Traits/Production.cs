using OpenRa.Game.GameRules;
using System.Linq;
using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class Production : IProducer, ITags
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
			if( location == null || Game.UnitInfluence.GetUnitsAt( location.Value ).Any() )
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

			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			return true;
		}

		public IEnumerable<TagType> GetTags()
		{
			yield return (true) ? TagType.Primary : TagType.None;
		}
	}
}
