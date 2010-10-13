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
using OpenRA.Collections;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Traits;

using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public class World
	{
		internal TraitDictionary traitDict = new TraitDictionary();
		Set<Actor> actors = new Set<Actor>();
		List<IEffect> effects = new List<IEffect>();
		Queue<Action<World>> frameEndActions = new Queue<Action<World>>();

		internal readonly OrderManager orderManager;
		public Session LobbyInfo { get { return orderManager.LobbyInfo; } }

		public XRandom SharedRandom;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		public void AddPlayer(Player p) { players[p.Index] = p; }

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

		public void IssueOrder( Order o ) { orderManager.IssueOrder( o ); }	/* avoid exposing the OM to mod code */

		IOrderGenerator orderGenerator_;
		public IOrderGenerator OrderGenerator
		{
			get { return orderGenerator_; }
			set
			{
				Sync.AssertUnsynced( "The current order generator may not be changed from synced code" );
				orderGenerator_ = value;
			}
		}

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
		
		internal World(Manifest manifest, Map map, OrderManager orderManager)
		{
			this.orderManager = orderManager;
			orderGenerator_ = new UnitOrderGenerator();
			Map = map;
			
			TileSet = Rules.TileSets[Map.Tileset];
			TileSet.LoadTiles();

			SharedRandom = new XRandom(orderManager.LobbyInfo.GlobalSettings.RandomSeed);

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
			
			PathFinder = new PathFinder(this);

			foreach (var wlh in WorldActor.TraitsImplementing<IWorldLoaded>())
				wlh.WorldLoaded(this);
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
			
			actors.Do( x => x.Tick() );
			Queries.WithTraitMultiple<ITick>().DoTimed( x =>
			{
				x.Trait.Tick( x.Actor );
			}, "[{2}] Trait: {0} ({1:0.000} ms)", Game.Settings.Debug.LongTickThreshold );

			effects.DoTimed( e => e.Tick( this ), "[{2}] Effect: {0} ({1:0.000} ms)", Game.Settings.Debug.LongTickThreshold );
			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);
			Game.viewport.Tick();
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
				int n = 0;
				int ret = 0;

				// hash all the actors
				foreach (var a in Actors)
					ret += n++ * (int)a.ActorID * Sync.CalculateSyncHash(a);

				// hash all the traits that tick
				foreach (var x in traitDict.ActorsWithTraitMultiple<object>(this))
					ret += n++ * (int)x.Actor.ActorID * Sync.CalculateSyncHash(x.Trait);

				// Hash the shared rng
				ret += SharedRandom.Last;
				
				return ret;
			}
		}

		public class AllQueries
		{
			readonly World world;

			public readonly Cache<Player, OwnedByCachedView> OwnedBy;

			public AllQueries( World world )
			{
				this.world = world;
				OwnedBy = new Cache<Player, OwnedByCachedView>(p => new OwnedByCachedView(world, world.actors, x => x.Owner == p));
			}

			public IEnumerable<TraitPair<T>> WithTrait<T>()
			{
				return world.traitDict.ActorsWithTraitMultiple<T>( world );
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

			public IEnumerable<TraitPair<T>> WithTraitMultiple<T>()
			{
				return world.traitDict.ActorsWithTraitMultiple<T>( world );
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

	public struct TraitPair<T>
	{
		public Actor Actor;
		public T Trait;

		public override string ToString()
		{
			return "{0}->{1}".F( Actor.Info.Name, Trait.GetType().Name );
		}
	}
}
