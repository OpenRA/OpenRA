#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Support;
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

		public int FrameNumber { get { return orderManager.LocalFrameNumber; } }

		internal readonly OrderManager orderManager;
		public Session LobbyInfo { get { return orderManager.LobbyInfo; } }

		public XRandom SharedRandom;

		public readonly List<Player> Players = new List<Player>();

		public void AddPlayer(Player p) { Players.Add(p); }
		public Player LocalPlayer { get; private set; }
		public readonly Shroud LocalShroud;
		public bool Observer { get { return LocalPlayer == null; } }
		public Player RenderedPlayer;
		public Shroud RenderedShroud { get { return RenderedPlayer != null ? RenderedPlayer.Shroud : LocalShroud; } }
		

		public void SetLocalPlayer(string pr)
		{
			if (orderManager.Connection is ReplayConnection)
				return;

	 		LocalPlayer = Players.FirstOrDefault(p => p.InternalName == pr);
			RenderedPlayer = LocalPlayer;
		}

		public readonly Actor WorldActor;
		public readonly Map Map;
		public readonly TileSet TileSet;
		public readonly ActorMap ActorMap;

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

		internal World(Manifest manifest, Map map, OrderManager orderManager, bool isShellmap)
		{
			IsShellmap = isShellmap;
			this.orderManager = orderManager;
			orderGenerator_ = new UnitOrderGenerator();
			Map = map;

			TileSet = Rules.TileSets[Map.Tileset];
			TileSet.LoadTiles();

			SharedRandom = new XRandom(orderManager.LobbyInfo.GlobalSettings.RandomSeed);

			WorldActor = CreateActor( "World", new TypeDictionary() );
			LocalShroud = WorldActor.Trait<Shroud>();
			ActorMap = new ActorMap(this);

			// Add players
			foreach (var cmp in WorldActor.TraitsImplementing<ICreatePlayers>())
				cmp.CreatePlayers(this);

			// Set defaults for any unset stances
			foreach (var p in Players)
				foreach (var q in Players)
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = Stance.Neutral;

			Sound.SoundVolumeModifier = 1.0f;
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
		public bool EnableTick = true;
		public bool IsShellmap = false;

		bool ShouldTick()
		{
			if (!EnableTick) return false;
			return !IsShellmap || Game.Settings.Game.ShowShellmap;
		}

		public void Tick()
		{
			// TODO: Expose this as an order so it can be synced
			if (ShouldTick())
			{
				using( new PerfSample("tick_idle") )
					foreach( var ni in ActorsWithTrait<INotifyIdle>() )
						if (ni.Actor.IsIdle)
							ni.Trait.TickIdle(ni.Actor);

				using( new PerfSample("tick_activities") )
					foreach( var a in actors )
						a.Tick();

				ActorsWithTrait<ITick>().DoTimed( x =>
				{
					x.Trait.Tick( x.Actor );
				}, "[{2}] Trait: {0} ({1:0.000} ms)", Game.Settings.Debug.LongTickThreshold );

				effects.DoTimed( e => e.Tick( this ), "[{2}] Effect: {0} ({1:0.000} ms)",
					Game.Settings.Debug.LongTickThreshold );
			}

			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);
		}

		// For things that want to update their render state once per tick, ignoring pause state
		public void TickRender(WorldRenderer wr)
		{
			ActorsWithTrait<ITickRender>().Do(x => x.Trait.TickRender(wr, x.Actor));
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
					ret += n++ * (int)(1+a.ActorID) * Sync.CalculateSyncHash(a);

				// hash all the traits that tick
				foreach (var x in traitDict.ActorsWithTraitMultiple<ISync>(this))
					ret += n++ * (int)(1+x.Actor.ActorID) * Sync.CalculateSyncHash(x.Trait);

				// Hash the shared rng
				ret += SharedRandom.Last;

				return ret;
			}
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return traitDict.ActorsWithTraitMultiple<T>(this);
		}
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
