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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class HazardLayerInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new HazardLayer( init.world ); }
	}

	public class HazardLayer : ICustomTerrain
	{
		List<Pair<Actor, Hazard>>[,] hazards;
		Map map;

		public HazardLayer( World world )
		{
			//System.Console.WriteLine("Created HazardLayer");
			map = world.Map;
			hazards = new List<Pair<Actor, Hazard>>[world.Map.MapSize.X, world.Map.MapSize.Y];
			for (int i = 0; i < world.Map.MapSize.X; i++)
				for (int j = 0; j < world.Map.MapSize.Y; j++)
					hazards[ i, j ] = new List<Pair<Actor, Hazard>>();

			world.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IProvideHazard>() );
		}

		
		public float GetSpeedModifier(int2 p, Actor forActor)
		{
			return 1f;
		}
		
		public float GetCost(int2 p, Actor forActor)
		{
			//System.Console.WriteLine("GetCost for {0}", forActor.Info.Name);

			var avoid = forActor.traits.WithInterface<IAvoidHazard>().Select(h => h.Type).ToList();
			var intensity = hazards[p.X,p.Y].Aggregate(1f,(a,b) => a + (avoid.Contains(b.Second.type) ? b.Second.intensity : 0f));			
			//System.Console.WriteLine("Avoid {0} cost {1}", avoid.Aggregate("",(a,b) => a+","+b), intensity);

			return intensity;
		}
		
		public void Add( Actor self, IProvideHazard hazard )
		{
			//System.Console.WriteLine("Adding hazard {0}", self.Info.Name);

			foreach( var h in hazard.HazardCells(self) )
			{
			//	System.Console.WriteLine("\t{0} {1} {2}", h.location, h.type, h.intensity);
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
