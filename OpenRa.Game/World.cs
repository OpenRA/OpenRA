using System;
using System.Collections.Generic;
using OpenRa.Effects;
using OpenRa.Support;
using OpenRa.FileFormats;

namespace OpenRa
{
	public class World
	{
		List<Actor> actors = new List<Actor>();
		List<IEffect> effects = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();

		public readonly BuildingInfluenceMap BuildingInfluence;
		public readonly UnitInfluenceMap UnitInfluence;

		public readonly Map Map;
		public readonly TileSet TileSet;

		readonly int oreFrequency;
		int oreTicks;

		public World()
		{
			Map = new Map( Rules.AllRules );
			FileSystem.MountTemporary( new Package( Map.Theater + ".mix" ) );
			TileSet = new TileSet( Map.TileSuffix );

			BuildingInfluence = new BuildingInfluenceMap();
			UnitInfluence = new UnitInfluenceMap();

			oreFrequency = (int)(Rules.General.GrowthRate * 60 * 25);
			oreTicks = oreFrequency;

			CreateActor("World", new int2(int.MaxValue, int.MaxValue), null);
		}

		public Actor CreateActor( string name, int2 location, Player owner )
		{
			var a = new Actor( name, location, owner );
			Add( a );
			return a;
		}

		public void Add(Actor a)
		{
			a.IsInWorld = true;
			actors.Add(a);
			ActorAdded(a);
		}

		public void Remove(Actor a)
		{
			a.IsInWorld = false;
			actors.Remove(a);
			ActorRemoved(a);
		}

		public void Add(IEffect b) { effects.Add(b); }
		public void Remove(IEffect b) { effects.Remove(b); }

		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = _ => { };
		
		public void Tick()
		{
			if (--oreTicks == 0)
				using( new PerfSample( "ore" ) )
				{
					Map.GrowOre( Game.SharedRandom );
					Game.minimap.InvalidateOre();
					oreTicks = oreFrequency;
				}

			foreach (var a in actors) a.Tick();
			foreach (var e in effects) e.Tick();

			Game.viewport.Tick();

			var acts = frameEndActions;
			frameEndActions = new List<Action<World>>();
			foreach (var a in acts) a(this);

			UnitInfluence.Tick();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public IEnumerable<IEffect> Effects { get { return effects; } }

		uint nextAID = 0;
		internal uint NextAID()
		{
			return nextAID++;
		}

		public int SyncHash()
		{
			using (new PerfSample("synchash"))
			{
				int ret = 0;
				foreach (var a in Actors)
					ret += (int)a.ActorID * Sync.CalculateSyncHash(a);

				return ret;
			}
		}
	}
}
