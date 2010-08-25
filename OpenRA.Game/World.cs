#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Collections;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Support;
using OpenRA.Traits;

using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public class World
	{
		Set<Actor> actors = new Set<Actor>();
		List<IEffect> effects = new List<IEffect>();
		Queue<Action<World>> frameEndActions = new Queue<Action<World>>();

		public XRandom SharedRandom = new XRandom(0);

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		public void AddPlayer(Player p) { players[p.Index] = p; }

		public bool GameHasStarted { get { return Game.orderManager.GameStarted; } }

		int localPlayerIndex;
		public Player LocalPlayer
		{
			get { return players.ContainsKey(localPlayerIndex) ? players[localPlayerIndex] : null; }
		}

		public void SetLocalPlayer(int index)
		{			
			localPlayerIndex = index;
		}

		public readonly Actor WorldActor;		
		public readonly PathFinder PathFinder;

		public readonly Map Map;
		public readonly TileSet TileSet;

		public readonly WorldRenderer WorldRenderer;
		
		public IOrderGenerator OrderGenerator = new UnitOrderGenerator();
		public Selection Selection = new Selection();

		public void CancelInputMode() { OrderGenerator = new UnitOrderGenerator(); }

		public bool ToggleInputMode<T>() where T : IOrderGenerator, new()
		{
			if (OrderGenerator is T)
			{
				CancelInputMode();
				return false;
			}
			else
			{
				OrderGenerator = new T();
				return true;
			}
		}
		
		public World(Manifest manifest, Map map)
		{
			Timer.Time( "----World.ctor" );
			Map = map;
			
			TileSet = Rules.TileSets[Map.Tileset];
			TileSet.LoadTiles();
			Timer.Time( "Tileset: {0}" );

			WorldRenderer = new WorldRenderer(this);
			Timer.Time("renderer: {0}");

			WorldActor = CreateActor( "World", new TypeDictionary() );
			Queries = new AllQueries(this);
			
			// Add players
			foreach (var cmp in WorldActor.TraitsImplementing<ICreatePlayers>())
				cmp.CreatePlayers(this);		
			
			// Set defaults for any unset stances
			foreach (var p in players.Values)
				foreach (var q in players.Values)
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = Stance.Neutral;		
			
			Timer.Time( "worldActor, players: {0}" );

			PathFinder = new PathFinder(this);

			foreach (var wlh in WorldActor.TraitsImplementing<IWorldLoaded>())
				wlh.WorldLoaded(this);
			
			Timer.Time( "hooks, pathing: {0}" );

			Timer.Time( "----end World.ctor" );
		}

		public Actor CreateActor( string name, TypeDictionary initDict )
		{
			return CreateActor( true, name, initDict );
		}

		public Actor CreateActor( bool addToWorld, string name, TypeDictionary initDict )
		{
			var a = new Actor( this, name, initDict );
			if( addToWorld )
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

		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Enqueue( a ); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = _ => { };

		// Will do bad things in multiplayer games
		public bool DisableTick = false;
		public void Tick()
		{
			if (DisableTick)
				return;
			
			Timer.Time("----World Tick");

			actors.DoTimed( x => x.Tick(), "expensive actor tick: {0} ({1:0.000})", 0.001 );
			Timer.Time("		actors: {0:0.000}");

			Queries.WithTraitMultiple<ITick>().DoTimed( x =>
			{
				x.Trait.Tick( x.Actor );
				Timer.Time( "trait tick \"{0}\": {{0}}".F( x.Trait.GetType().Name ) );
			}, "expensive trait tick: {0} ({1:0.000})", 0.001 );
			Timer.Time("		traits: {0:0.000}");

			effects.DoTimed( e => e.Tick( this ), "expensive effect tick: {0} ({1:0.000})", 0.001 );
			Timer.Time("		effects: {0:0.000}");

			Game.viewport.Tick();
			Timer.Time("		viewport: {0:0.000}");

			while( frameEndActions.Count != 0 )
				frameEndActions.Dequeue()( this );
			Timer.Time("		frameEndActions: {0:0.000}");

			WorldRenderer.Tick();
			Timer.Time("		worldrenderer: {0:0.000}");
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
			//using (new PerfSample("synchash"))
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

			public readonly Cache<Player, OwnedByCachedView> OwnedBy;
			readonly TypeDictionary hasTrait = new TypeDictionary();

			public AllQueries( World world )
			{
				this.world = world;
				OwnedBy = new Cache<Player, OwnedByCachedView>(p => new OwnedByCachedView(world, world.actors, x => x.Owner == p));
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
					x => x.HasTrait<T>(),
					x => new TraitPair<T> { Actor = x, Trait = x.Trait<T>() } );
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
					x => x.HasTrait<T>(),
					x => x.TraitsImplementing<T>().Select( t => new TraitPair<T> { Actor = x, Trait = t } ) );
				hasTrait.Add( ret );
				return ret;
			}

			public struct TraitPair<T>
			{
				public Actor Actor;
				public T Trait;

				public override string ToString()
				{
					return "{0}->{1}".F( Actor.Info.Name, Trait.GetType().Name );
				}
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
