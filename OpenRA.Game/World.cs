#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public enum WorldType { Regular, Shellmap, Editor }

	public sealed class World : IDisposable
	{
		internal readonly TraitDictionary TraitDict = new TraitDictionary();
		readonly SortedDictionary<uint, Actor> actors = new SortedDictionary<uint, Actor>();
		readonly List<IEffect> effects = new List<IEffect>();
		readonly Queue<Action<World>> frameEndActions = new Queue<Action<World>>();

		public int Timestep;

		internal readonly OrderManager OrderManager;
		public Session LobbyInfo { get { return OrderManager.LobbyInfo; } }

		public readonly MersenneTwister SharedRandom;

		public Player[] Players = new Player[0];

		public void SetPlayers(IEnumerable<Player> players, Player localPlayer)
		{
			if (Players.Length > 0)
				throw new InvalidOperationException("Players are fixed once they have been set.");
			Players = players.ToArray();
			SetLocalPlayer(localPlayer);
		}

		public Player LocalPlayer { get; private set; }

		public event Action GameOver = () => { };
		public bool IsGameOver { get; private set; }
		public void EndGame()
		{
			if (!IsGameOver)
			{
				IsGameOver = true;

				foreach (var t in WorldActor.TraitsImplementing<IGameOver>())
					t.GameOver(this);

				GameOver();
			}
		}

		Player renderPlayer;
		public Player RenderPlayer
		{
			get { return renderPlayer == null || (renderPlayer.WinState != WinState.Undefined && !Map.Visibility.HasFlag(MapVisibility.MissionSelector)) ? null : renderPlayer; }
			set { renderPlayer = value; }
		}

		public bool FogObscures(Actor a) { return RenderPlayer != null && !RenderPlayer.CanViewActor(a); }
		public bool FogObscures(CPos p) { return RenderPlayer != null && !RenderPlayer.Shroud.IsVisible(p); }
		public bool FogObscures(WPos pos) { return RenderPlayer != null && !RenderPlayer.Shroud.IsVisible(pos); }
		public bool ShroudObscures(CPos p) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(p); }
		public bool ShroudObscures(WPos pos) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(pos); }
		public bool ShroudObscures(PPos uv) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(uv); }

		public bool IsReplay
		{
			get { return OrderManager.Connection is ReplayConnection; }
		}

		public bool AllowDevCommands
		{
			get { return LobbyInfo.GlobalSettings.AllowCheats || LobbyInfo.IsSinglePlayer; }
		}

		void SetLocalPlayer(Player localPlayer)
		{
			if (localPlayer == null)
				return;

			if (!Players.Contains(localPlayer))
				throw new ArgumentException("The local player must be one of the players in the world.", "localPlayer");

			if (IsReplay)
				return;

			LocalPlayer = localPlayer;
			RenderPlayer = LocalPlayer;
		}

		public readonly Actor WorldActor;

		public readonly Map Map;

		public readonly TileSet TileSet;

		public readonly ActorMap ActorMap;
		public readonly ScreenMap ScreenMap;
		public readonly WorldType Type;

		readonly GameInformation gameInfo;

		public void IssueOrder(Order o) { OrderManager.IssueOrder(o); } /* avoid exposing the OM to mod code */

		IOrderGenerator orderGenerator;
		public IOrderGenerator OrderGenerator
		{
			get
			{
				return orderGenerator;
			}

			set
			{
				Sync.AssertUnsynced("The current order generator may not be changed from synced code");
				orderGenerator = value;
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

		internal World(Map map, OrderManager orderManager, WorldType type)
		{
			Type = type;
			OrderManager = orderManager;
			orderGenerator = new UnitOrderGenerator();
			Map = map;
			Timestep = orderManager.LobbyInfo.GlobalSettings.Timestep;

			TileSet = map.Rules.TileSets[Map.Tileset];
			SharedRandom = new MersenneTwister(orderManager.LobbyInfo.GlobalSettings.RandomSeed);

			var worldActorType = type == WorldType.Editor ? "EditorWorld" : "World";
			WorldActor = CreateActor(worldActorType, new TypeDictionary());
			ActorMap = WorldActor.Trait<ActorMap>();
			ScreenMap = WorldActor.Trait<ScreenMap>();

			// Add players
			foreach (var cmp in WorldActor.TraitsImplementing<ICreatePlayers>())
				cmp.CreatePlayers(this);

			// Set defaults for any unset stances
			foreach (var p in Players)
				foreach (var q in Players)
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = Stance.Neutral;

			Game.Sound.SoundVolumeModifier = 1.0f;

			gameInfo = new GameInformation
			{
				Mod = Game.ModData.Manifest.Mod.Id,
				Version = Game.ModData.Manifest.Mod.Version,

				MapUid = Map.Uid,
				MapTitle = Map.Title
			};

			if (!LobbyInfo.GlobalSettings.Shroud)
				foreach (var player in Players)
					player.Shroud.ExploreAll(this);
		}

		public void LoadComplete(WorldRenderer wr)
		{
			// ScreenMap must be initialized before anything else
			using (new Support.PerfTimer("ScreenMap.WorldLoaded"))
				ScreenMap.WorldLoaded(this, wr);

			foreach (var wlh in WorldActor.TraitsImplementing<IWorldLoaded>())
			{
				// These have already been initialized
				if (wlh == ScreenMap)
					continue;

				using (new Support.PerfTimer(wlh.GetType().Name + ".WorldLoaded"))
					wlh.WorldLoaded(this, wr);
			}

			gameInfo.StartTimeUtc = DateTime.UtcNow;
			foreach (var player in Players)
				gameInfo.AddPlayer(player, OrderManager.LobbyInfo);

			var echo = OrderManager.Connection as EchoConnection;
			var rc = echo != null ? echo.Recorder : null;

			if (rc != null)
				rc.Metadata = new ReplayMetadata(gameInfo);
		}

		public Actor CreateActor(string name, TypeDictionary initDict)
		{
			return CreateActor(true, name, initDict);
		}

		public Actor CreateActor(bool addToWorld, string name, TypeDictionary initDict)
		{
			var a = new Actor(this, name, initDict);
			foreach (var t in a.TraitsImplementing<INotifyCreated>())
				t.Created(a);
			if (addToWorld)
				Add(a);
			return a;
		}

		public void Add(Actor a)
		{
			a.IsInWorld = true;
			actors.Add(a.ActorID, a);
			ActorAdded(a);

			foreach (var t in a.TraitsImplementing<INotifyAddedToWorld>())
				t.AddedToWorld(a);
		}

		public void Remove(Actor a)
		{
			a.IsInWorld = false;
			actors.Remove(a.ActorID);
			ActorRemoved(a);

			foreach (var t in a.TraitsImplementing<INotifyRemovedFromWorld>())
				t.RemovedFromWorld(a);
		}

		public void Add(IEffect b) { effects.Add(b); }
		public void Remove(IEffect b) { effects.Remove(b); }
		public void RemoveAll(Predicate<IEffect> predicate) { effects.RemoveAll(predicate); }

		public void AddFrameEndTask(Action<World> a) { frameEndActions.Enqueue(a); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = _ => { };

		public bool ShouldTick { get { return Type != WorldType.Shellmap || Game.Settings.Game.ShowShellmap; } }
		public bool Paused { get; internal set; }
		public bool PredictedPaused { get; internal set; }
		public bool PauseStateLocked { get; set; }

		public int WorldTick { get; private set; }

		public void SetPauseState(bool paused)
		{
			if (PauseStateLocked)
				return;

			IssueOrder(Order.PauseGame(paused));
			PredictedPaused = paused;
		}

		public void SetLocalPauseState(bool paused)
		{
			Paused = PredictedPaused = paused;
		}

		public void Tick()
		{
			if (!Paused)
			{
				WorldTick++;

				using (new PerfSample("tick_idle"))
					foreach (var ni in ActorsWithTrait<INotifyIdle>())
						if (ni.Actor.IsIdle)
							ni.Trait.TickIdle(ni.Actor);

				using (new PerfSample("tick_activities"))
					foreach (var a in actors.Values)
						a.Tick();

				ActorsWithTrait<ITick>().DoTimed(x => x.Trait.Tick(x.Actor), "Trait");

				effects.DoTimed(e => e.Tick(this), "Effect");
			}

			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);
		}

		// For things that want to update their render state once per tick, ignoring pause state
		public void TickRender(WorldRenderer wr)
		{
			ActorsWithTrait<ITickRender>().DoTimed(x => x.Trait.TickRender(wr, x.Actor), "Render");
		}

		public IEnumerable<Actor> Actors { get { return actors.Values; } }
		public IEnumerable<IEffect> Effects { get { return effects; } }

		public Actor GetActorById(uint actorId)
		{
			Actor a;
			if (actors.TryGetValue(actorId, out a))
				return a;
			return null;
		}

		uint nextAID = 0;
		internal uint NextAID()
		{
			return nextAID++;
		}

		public int SyncHash()
		{
			// using (new PerfSample("synchash"))
			{
				var n = 0;
				var ret = 0;

				// hash all the actors
				foreach (var a in Actors)
					ret += n++ * (int)(1 + a.ActorID) * Sync.CalculateSyncHash(a);

				// hash all the traits that tick
				foreach (var x in ActorsWithTrait<ISync>())
					ret += n++ * (int)(1 + x.Actor.ActorID) * Sync.CalculateSyncHash(x.Trait);

				// TODO: don't go over all effects
				foreach (var e in Effects)
				{
					var sync = e as ISync;
					if (sync != null)
						ret += n++ * Sync.CalculateSyncHash(sync);
				}

				// Hash the shared rng
				ret += SharedRandom.Last;

				return ret;
			}
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return TraitDict.ActorsWithTrait<T>();
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>()
		{
			return TraitDict.ActorsHavingTrait<T>();
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>(Func<T, bool> predicate)
		{
			return TraitDict.ActorsHavingTrait<T>(predicate);
		}

		public void OnPlayerWinStateChanged(Player player)
		{
			var pi = gameInfo.GetPlayer(player);
			if (pi != null)
			{
				pi.Outcome = player.WinState;
				pi.OutcomeTimestampUtc = DateTime.UtcNow;
			}
		}

		public bool Disposing;

		public void Dispose()
		{
			Disposing = true;

			frameEndActions.Clear();

			Game.Sound.StopAudio();
			Game.Sound.StopVideo();

			// Dispose newer actors first, and the world actor last
			foreach (var a in actors.Values.Reverse())
				a.Dispose();

			// Actor disposals are done in a FrameEndTask
			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);
		}
	}

	public struct TraitPair<T>
	{
		public Actor Actor;
		public T Trait;

		public override string ToString()
		{
			return "{0}->{1}".F(Actor.Info.Name, Trait.GetType().Name);
		}
	}
}
