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

using System.Linq;

namespace OpenRa.Traits
{
	class RenderBuildingWallInfo : RenderBuildingInfo
	{
		public readonly int DamageStates = 2;
		public override object Create(Actor self) { return new RenderBuildingWall(self); }
	}

	class RenderBuildingWall : RenderBuilding
	{
		string seqName;
		int damageStates;
		Actor self;
		
		public RenderBuildingWall(Actor self)
			: base(self)
		{
			seqName = "idle";
			this.self = self;
			this.damageStates = self.Info.Traits.Get<RenderBuildingWallInfo>().DamageStates;
		}
		
		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			switch (e.DamageState)
			{
				case DamageState.Normal:
					seqName = "idle";
					break;
				case DamageState.ThreeQuarter:
					if (damageStates >= 4)
						seqName = "minor-damaged-idle";
					break;
				case DamageState.Half:
					seqName = "damaged-idle";
					Sound.Play("kaboom1.aud");
					break;
				case DamageState.Quarter:
					if (damageStates >= 3)
					{
						seqName = "critical-idle";
						Sound.Play("kaboom1.aud");
					}
					break;
			}
		}
		
		public override void Tick(Actor self)
		{
			base.Tick(self);
			
			// TODO: This only needs updating when a wall is built or destroyed
			int index = NearbyWalls( self.Location );
			
			anim.PlayFetchIndex(seqName, () => index);

		}
		bool IsWall( int x, int y)
		{
			return self.World.Queries.WithTrait<Wall>().Any(a => (a.Actor.Info.Name == self.Info.Name && a.Actor.Location.X == x && a.Actor.Location.Y == y));
		}
		
		int NearbyWalls( int2 xy )
		{
			int ret = 0;
			
			if( IsWall( xy.X, xy.Y - 1 ) )
				ret |= 1;
			if( IsWall( xy.X + 1, xy.Y ) )
				ret |= 2;
			if( IsWall( xy.X, xy.Y + 1 ) )
				ret |= 4;
			if( IsWall( xy.X - 1, xy.Y ) )
				ret |= 8;
			return ret;
		}
		
	}
}
