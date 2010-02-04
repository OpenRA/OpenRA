using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Effects;
using OpenRa.Support;
using OpenRa.FileFormats;
using OpenRa.Graphics;
using OpenRa.Traits;
using OpenRa.Collections;

namespace OpenRa
{
	public class World
	{
		Set<Actor> actors = new Set<Actor>();
		List<IEffect> effects = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		int localPlayerIndex;
		public Player LocalPlayer
		{
			get { return players[localPlayerIndex]; }
		}

		public void SetLocalPlayer(int index)
		{
			if (index != localPlayerIndex)
			{
				localPlayerIndex = index;
				Game.viewport.GoToStartLocation(LocalPlayer);
				Game.chat.AddLine(LocalPlayer, "is now YOU");
			}
			if (!string.IsNullOrEmpty(Game.Settings.PlayerName) && LocalPlayer.PlayerName != Game.Settings.PlayerName)
				Game.IssueOrder(Order.Chat("/name " + Game.Settings.PlayerName));

		}

		public readonly Actor WorldActor;

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
			Timer.Time( "----World.ctor" );

			Map = new Map( Rules.AllRules );
			Timer.Time( "new Map: {0}" );
			TileSet = new TileSet( Map.TileSuffix );
			SpriteSheetBuilder.Initialize( Map );
			Timer.Time( "Tileset: {0}" );

			oreFrequency = (int)(Rules.General.GrowthRate * 60 * 25);
			oreTicks = oreFrequency;
			Map.InitOreDensity();
			Timer.Time( "Ore: {0}" );

			WorldActor = CreateActor("World", new int2(int.MaxValue, int.MaxValue), null);

			for (int i = 0; i < 8; i++)
			{
				players[i] = new Player(this, i, Game.LobbyInfo.Clients.FirstOrDefault(a => a.Index == i));
			}
			Timer.Time( "worldActor, players: {0}" );

			Queries = new AllQueries( this );
			Timer.Time( "queries: {0}" );

			Bridges.MakeBridges(this);
			PathFinder = new PathFinder(this);
			Timer.Time( "bridge, pathing: {0}" );

			WorldRenderer = new WorldRenderer(this, Game.renderer);
			Minimap = new Minimap(this, Game.renderer);
			Timer.Time( "renderer, minimap: {0}" );

			Timer.Time( "----end World.ctor" );
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
			Queries.WithTraitMultiple<ITick>().Do( x => x.Trait.Tick( x.Actor ) );

			foreach (var e in effects) e.Tick( this );

			Game.viewport.Tick();

			var acts = frameEndActions;
			frameEndActions = new List<Action<World>>();
			foreach (var a in acts) a(this);

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

		public class AllQueries
		{
			readonly World world;

			public readonly Dictionary<Player, OwnedByCachedView> OwnedBy = new Dictionary<Player, OwnedByCachedView>();
			readonly TypeDictionary hasTrait = new TypeDictionary();

			public AllQueries( World world )
			{
				this.world = world;
				foreach( var p in world.players.Values )
				{
					var player = p;
					OwnedBy.Add( player, new OwnedByCachedView( world, world.actors, x => x.Owner == player ) );
				}
			}

			public CachedView<Actor, TraitPair<T>> WithTrait<T>()
			{
				return WithTraitInner<T>( world, hasTrait );
			}

			static CachedView<Actor, TraitPair<T>> WithTraitInner<T>( World world, TypeDictionary hasTrait )
			{
				var ret = hasTrait.GetOrDefault<CachedView<Actor, TraitPair<T>>>();
				if( ret != null )
					return ret;
				ret = new CachedView<Actor, TraitPair<T>>(
					world.actors,
					x => x.traits.Contains<T>(),
					x => new TraitPair<T> { Actor = x, Trait = x.traits.Get<T>() } );
				hasTrait.Add( ret );
				return ret;
			}

			public CachedView<Actor, TraitPair<T>> WithTraitMultiple<T>()
			{
				var ret = hasTrait.GetOrDefault<CachedView<Actor, TraitPair<T>>>();
				if( ret != null )
					return ret;
				ret = new CachedView<Actor, TraitPair<T>>(
					world.actors,
					x => x.traits.Contains<T>(),
					x => x.traits.WithInterface<T>().Select( t => new TraitPair<T> { Actor = x, Trait = t } ) );
				hasTrait.Add( ret );
				return ret;
			}

			public struct TraitPair<T>
			{
				public Actor Actor;
				public T Trait;
			}

			public class OwnedByCachedView : CachedView<Actor, Actor>
			{
				readonly World world;
				readonly TypeDictionary hasTrait = new TypeDictionary();

				public OwnedByCachedView( World world, Set<Actor> set, Func<Actor, bool> include )
					: base( set, include, a => a )
				{
					this.world = world;
				}

				public CachedView<Actor, TraitPair<T>> WithTrait<T>()
				{
					return WithTraitInner<T>( world, hasTrait );
				}
			}
		}

		public readonly AllQueries Queries;
	}
}
