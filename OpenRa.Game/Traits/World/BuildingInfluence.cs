using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	public class BuildingInfluenceInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new BuildingInfluence( self ); }
	}

	public class BuildingInfluence
	{
		bool[,] blocked = new bool[128, 128];
		Actor[,] influence = new Actor[128, 128];

		public BuildingInfluence( Actor self )
		{
			self.World.ActorAdded +=
				a => { if (a.traits.Contains<Building>()) 
					ChangeInfluence(a, a.traits.Get<Building>(), true); };
			self.World.ActorRemoved +=
				a => { if (a.traits.Contains<Building>()) 
					ChangeInfluence(a, a.traits.Get<Building>(), false); };
		}

		void ChangeInfluence( Actor a, Building building, bool isAdd )
		{
			foreach( var u in Footprint.UnpathableTiles( a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location ) )
				if( IsValid( u ) )
					blocked[ u.X, u.Y ] = isAdd;

			foreach( var u in Footprint.Tiles( a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location ) )
				if( IsValid( u ) )
					influence[ u.X, u.Y ] = isAdd ? a : null;
		}

		bool IsValid(int2 t)
		{
			return !(t.X < 0 || t.Y < 0 || t.X >= 128 || t.Y >= 128);
		}

		public Actor GetBuildingAt(int2 cell)
		{
			if (!IsValid(cell)) return null;
			return influence[cell.X, cell.Y];
		}

		public bool CanMoveHere(int2 cell)
		{
			return IsValid(cell) && !blocked[cell.X, cell.Y];
		}
	}}
