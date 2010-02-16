#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

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
