using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa
{
	public class UnitInfluenceMap
	{
		List<Actor>[,] influence = new List<Actor>[128, 128];
		readonly int2 searchDistance = new int2(2,2);

		public UnitInfluenceMap( World world )
		{
			for (int i = 0; i < 128; i++)
				for (int j = 0; j < 128; j++)
					influence[ i, j ] = new List<Actor>();

			world.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IOccupySpace>() );
		}

		public void Tick()
		{
			// Does this belong here? NO, but it's your mess.
			
			// Get the crushable actors
			foreach (var a in Game.world.Actors.Where(b => b.traits.Contains<ICrushable>()))
			{
				// Are there any units in the same cell that can crush this?
				foreach( var ios in a.traits.WithInterface<IOccupySpace>() )
					foreach( var cell in ios.OccupiedCells() )
					{
						// There should only be one (counterexample: An infantry and a tank try to pick up a crate at the same time.)
						// If there is more than one, do action on the first crusher
						var crusher = GetUnitsAt(cell).Where(b => a != b && Game.IsActorCrushableByActor(a, b)).FirstOrDefault();
						if (crusher != null)
						{
							Log.Write("{0} crushes {1}", crusher.Info.Name, a.Info.Name);
							// Apply the crush action
							foreach (var crush in a.traits.WithInterface<ICrushable>())
								crush.OnCrush(crusher);
						}
					}
			}
			SanityCheck();
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheck()
		{
			for( int y = 0 ; y < 128 ; y++ )
				for( int x = 0 ; x < 128 ; x++ )
					if( influence[ x, y ] != null )
						foreach (var a in influence[ x, y ])
							if (!a.traits.Get<IOccupySpace>().OccupiedCells().Contains( new int2( x, y ) ) )
								throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( Actor a in Game.world.Actors )
				foreach( var ios in a.traits.WithInterface<IOccupySpace>() )
					foreach( var cell in ios.OccupiedCells() )
						if (!influence[cell.X, cell.Y].Contains(a))
							throw new InvalidOperationException( "UIM: Sanity check failed B" );
		}

		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			return influence[ a.X, a.Y ];
		}

		public void Add( Actor self, IOccupySpace unit )
		{
			foreach( var c in unit.OccupiedCells() )
				influence[c.X, c.Y].Add(self);
		}

		public void Remove( Actor self, IOccupySpace unit )
		{
			if (unit != null)
				foreach (var c in unit.OccupiedCells())
					influence[c.X, c.Y].Remove(self);
		}

		public void Update(Actor self, IOccupySpace unit)
		{
			Remove(self, unit);
			if (!self.IsDead) Add(self, unit);
		}
	}
}
