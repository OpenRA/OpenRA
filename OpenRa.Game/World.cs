using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Effects;
using OpenRa.Support;
using OpenRa.FileFormats;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa
{
	public class World
	{
		List<Actor> actors = new List<Actor>();
		List<IEffect> effects = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		int localPlayerIndex;
		public Player LocalPlayer
		{
			get { return players[localPlayerIndex]; }
			set
			{
				localPlayerIndex = value.Index;
				Game.viewport.GoToStartLocation();
			}
		}

		public readonly BuildingInfluenceMap BuildingInfluence;
		public readonly UnitInfluenceMap UnitInfluence;

		public readonly PathFinder PathFinder;

		public readonly Map Map;
		public readonly TileSet TileSet;

		// for tricky things like bridges.
		public readonly ICustomTerrain[,] customTerrain = new ICustomTerrain[128, 128];

		public readonly WorldRenderer WorldRenderer;
		internal readonly Minimap Minimap;

		readonly int oreFrequency;
		int oreTicks;

		public World()
		{
			Map = new Map( Rules.AllRules );
			FileSystem.MountTemporary( new Package( Map.Theater + ".mix" ) );
			TileSet = new TileSet( Map.TileSuffix );

			BuildingInfluence = new BuildingInfluenceMap( this );
			UnitInfluence = new UnitInfluenceMap( this );

			oreFrequency = (int)(Rules.General.GrowthRate * 60 * 25);
			oreTicks = oreFrequency;
			Map.InitOreDensity();

			CreateActor("World", new int2(int.MaxValue, int.MaxValue), null);

			for (int i = 0; i < 8; i++)
				players[i] = new Player(this, i, Game.LobbyInfo.Clients.FirstOrDefault(a => a.Index == i));

			Bridges.MakeBridges(this);
			PathFinder = new PathFinder(this);

			WorldRenderer = new WorldRenderer(this, Game.renderer);
			Minimap = new Minimap(this, Game.renderer);
		}

		public Actor CreateActor( string name, int2 location, Player owner )
		{
			var a = new Actor( this, name, location, owner );
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
					this.GrowOre( Game.SharedRandom );
					Minimap.InvalidateOre();
					oreTicks = oreFrequency;
				}

			foreach (var a in actors) a.Tick();
			foreach (var e in effects) e.Tick( this );

			Game.viewport.Tick();

			var acts = frameEndActions;
			frameEndActions = new List<Action<World>>();
			foreach (var a in acts) a(this);

			UnitInfluence.Tick();

			Minimap.Update();
			foreach (var player in players.Values)
				player.Tick();
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
