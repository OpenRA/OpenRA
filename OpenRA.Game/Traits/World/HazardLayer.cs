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

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class HazardLayerInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new HazardLayer( init.world ); }
	}

	public class HazardLayer : ITerrainCost
	{
		List<Pair<Actor, Hazard>>[,] hazards;

		public HazardLayer( World world )
		{
			hazards = new List<Pair<Actor, Hazard>>[world.Map.MapSize.X, world.Map.MapSize.Y];
			for (int i = 0; i < world.Map.MapSize.X; i++)
				for (int j = 0; j < world.Map.MapSize.Y; j++)
					hazards[ i, j ] = new List<Pair<Actor, Hazard>>();

			world.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IProvideHazard>() );
		}

		public float GetTerrainCost(int2 p, Actor forActor)
		{
			var avoid = forActor.traits.WithInterface<IAvoidHazard>().Select(h => h.Type).ToList();
			
			var intensity = hazards[p.X,p.Y].Where(a => avoid.Contains(a.Second.type))
											.Select(b => b.Second.intensity)
											.Sum();			

			return intensity;
		}
		
		public void Add( Actor self, IProvideHazard hazard )
		{
			foreach( var h in hazard.HazardCells(self) )
			{
				hazards[h.location.X, h.location.Y].Add(Pair.New(self, h));
			}
		}

		public void Remove( Actor self, IProvideHazard hazard )
		{
			if (hazard != null)
				foreach (var h in hazard.HazardCells(self))
					hazards[h.location.X, h.location.Y].Remove(Pair.New(self,h));
		}

		public void Update(Actor self, IProvideHazard hazard)
		{
			Remove(self, hazard);
			if (!self.IsDead) Add(self, hazard);
		}
		
		public struct Hazard
		{
			public int2 location;
			public string type;
			public float intensity;
		}
	}
}
