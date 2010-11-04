#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class BuildingInfluenceInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new BuildingInfluence( init.world ); }
	}

	public class BuildingInfluence
	{
		Actor[,] influence;
		Map map;

		public BuildingInfluence( World world )
		{
			map = world.Map;

			influence = new Actor[map.MapSize.X, map.MapSize.Y];

			world.ActorAdded +=
				a => { if (a.HasTrait<Building>())
					ChangeInfluence(a, a.Trait<Building>(), true); };
			world.ActorRemoved +=
				a => { if (a.HasTrait<Building>())
					ChangeInfluence(a, a.Trait<Building>(), false); };
		}

		void ChangeInfluence( Actor a, Building building, bool isAdd )
		{
			foreach( var u in FootprintUtils.Tiles( a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location ) )
				if( map.IsInMap( u ) )
					influence[ u.X, u.Y ] = isAdd ? a : null;
		}

		public Actor GetBuildingAt(int2 cell)
		{
			if (!map.IsInMap(cell)) return null;
			return influence[cell.X, cell.Y];
		}
	}
}
