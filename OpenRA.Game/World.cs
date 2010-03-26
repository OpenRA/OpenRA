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
using System.Linq;
using OpenRA.Collections;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public class World
	{
		Set<Actor> actors = new Set<Actor>();
		List<IEffect> effects = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();

		public XRandom SharedRandom = new XRandom(0);	// synced
		public XRandom CosmeticRandom = new XRandom();	// not synced

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		public void AddPlayer(Player p) { players[p.Index] = p; }

		int localPlayerIndex;
		public Player LocalPlayer
		{
			get { return players[localPlayerIndex]; }
		}

		public Player NeutralPlayer { get; private set; }

		public void SetLocalPlayer(int index)
		{
			localPlayerIndex = index;
			if (!string.IsNullOrEmpty(Game.Settings.PlayerName) 
				&& Game.LobbyInfo.Clients[index].Name != Game.Settings.PlayerName)
				Game.IssueOrder(Order.Chat("/name " + Game.Settings.PlayerName));
		}

		public readonly Actor WorldActor;		
		public readonly PathFinder PathFinder;

		public readonly Map Map;
		public readonly TileSet TileSet;

		// for tricky things like bridges.
		public readonly ICustomTerrain[,] customTerrain;

		public readonly WorldRenderer WorldRenderer;
		internal readonly Minimap Minimap;

		public World()
		{
			Timer.Time( "----World.ctor" );
			
			Map = new Map( Game.LobbyInfo.GlobalSettings.Map );
			customTerrain = new ICustomTerrain[Map.MapSize, Map.MapSize];
			Timer.Time( "new Map: {0}" );
			
			var theaterInfo = Rules.Info["world"].Traits.WithInterface<TheaterInfo>()
				.FirstOrDefault(t => t.Theater == Map.Theater);
			TileSet = new TileSet(theaterInfo.Tileset, theaterInfo.Templates, theaterInfo.Suffix);
			
			SpriteSheetBuilder.Initialize( Map );
			Timer.Time( "Tileset: {0}" );

			WorldRenderer = new WorldRenderer(this, Game.renderer);
			Timer.Time("renderer: {0}");
			
			WorldActor = CreateActor("World", new int2(int.MaxValue, int.MaxValue), null);
			AddPlayer(NeutralPlayer = new Player(this, null));		// add the neutral player

			Timer.Time( "worldActor: {0}" );

			foreach (var wlh in WorldActor.traits.WithInterface<ILoadWorldHook>())
				wlh.WorldLoaded(this);

			PathFinder = new PathFinder(this);
			Timer.Time( "hooks, pathing: {0}" );

			Minimap = new Minimap(this, Game.renderer);
			Timer.Time( "minimap: {0}" );

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
				return WithTraitInner<T>( world.actors, hasTrait );
			}

			static CachedView<Actor, TraitPair<T>> WithTraitInner<T>( Set<Actor> set, TypeDictionary hasTrait )
			{
				var ret = hasTrait.GetOrDefault<CachedView<Actor, TraitPair<T>>>();
				if( ret != null )
					return ret;
				ret = new CachedView<Actor, TraitPair<T>>(
					set,
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
				readonly TypeDictionary hasTrait = new TypeDictionary();

				public OwnedByCachedView( World world, Set<Actor> set, Func<Actor, bool> include )
					: base( set, include, a => a )
				{
				}

				public CachedView<Actor, TraitPair<T>> WithTrait<T>()
				{
					return WithTraitInner<T>( this, hasTrait );
				}
			}
		}

		public AllQueries Queries;
	}
}
