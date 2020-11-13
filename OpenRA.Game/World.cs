#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
		readonly List<IEffect> unpartitionedEffects = new List<IEffect>();
		readonly List<ISync> syncedEffects = new List<ISync>();

		readonly Queue<Action<World>> frameEndActions = new Queue<Action<World>>();

		public int Timestep;

		internal readonly OrderManager OrderManager;
		public Session LobbyInfo { get { return OrderManager.LobbyInfo; } }

		public readonly MersenneTwister SharedRandom;
		public readonly MersenneTwister LocalRandom;
		public readonly IModelCache ModelCache;
		public LongBitSet<PlayerBitMask> AllPlayersMask = default(LongBitSet<PlayerBitMask>);
		public readonly LongBitSet<PlayerBitMask> NoPlayersMask = default(LongBitSet<PlayerBitMask>);

		public Player[] Players = new Player[0];

		public event Action<Player> RenderPlayerChanged;

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
				gameInfo.FinalGameTick = WorldTick;
				GameOver();
			}
		}

		Player renderPlayer;
		public Player RenderPlayer
		{
			get
			{
				return renderPlayer;
			}

			set
			{
				if (LocalPlayer == null || LocalPlayer.UnlockedRenderPlayer)
				{
					renderPlayer = value;

					RenderPlayerChanged?.Invoke(value);
				}
			}
		}

		public bool FogObscures(Actor a) { return RenderPlayer != null && !a.CanBeViewedByPlayer(RenderPlayer); }
		public bool FogObscures(CPos p) { return RenderPlayer != null && !RenderPlayer.Shroud.IsVisible(p); }
		public bool FogObscures(WPos pos) { return RenderPlayer != null && !RenderPlayer.Shroud.IsVisible(pos); }
		public bool ShroudObscures(CPos p) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(p); }
		public bool ShroudObscures(MPos uv) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(uv); }
		public bool ShroudObscures(WPos pos) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(pos); }
		public bool ShroudObscures(PPos uv) { return RenderPlayer != null && !RenderPlayer.Shroud.IsExplored(uv); }

		public bool IsReplay
		{
			get { return OrderManager.Connection is ReplayConnection; }
		}

		public bool IsLoadingGameSave
		{
			get { return OrderManager.NetFrameNumber <= OrderManager.GameSaveLastFrame; }
		}

		public int GameSaveLoadingPercentage
		{
			get { return OrderManager.NetFrameNumber * 100 / OrderManager.GameSaveLastFrame; }
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

			// Set the property backing field directly
			renderPlayer = LocalPlayer;
		}

		public readonly Actor WorldActor;

		public readonly Map Map;

		public readonly IActorMap ActorMap;
		public readonly ScreenMap ScreenMap;
		public readonly WorldType Type;

		public readonly IValidateOrder[] OrderValidators;

		readonly GameInformation gameInfo;

		// Hide the OrderManager from mod code
		public void IssueOrder(Order o) { OrderManager.IssueOrder(o); }

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
				orderGenerator?.Deactivate();

				orderGenerator = value;
			}
		}

		public readonly ISelection Selection;

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

		public bool RulesContainTemporaryBlocker { get; private set; }

		bool wasLoadingGameSave;

		internal World(ModData modData, Map map, OrderManager orderManager, WorldType type)
		{
			Type = type;
			OrderManager = orderManager;
			orderGenerator = new UnitOrderGenerator();
			Map = map;
			Timestep = orderManager.LobbyInfo.GlobalSettings.Timestep;
			SharedRandom = new MersenneTwister(orderManager.LobbyInfo.GlobalSettings.RandomSeed);
			LocalRandom = new MersenneTwister();

			ModelCache = modData.ModelSequenceLoader.CacheModels(map, modData, map.Rules.ModelSequences);

			var worldActorType = type == WorldType.Editor ? "EditorWorld" : "World";
			WorldActor = CreateActor(worldActorType, new TypeDictionary());
			ActorMap = WorldActor.Trait<IActorMap>();
			ScreenMap = WorldActor.Trait<ScreenMap>();
			Selection = WorldActor.Trait<ISelection>();
			OrderValidators = WorldActor.TraitsImplementing<IValidateOrder>().ToArray();

			LongBitSet<PlayerBitMask>.Reset();

			// Create an isolated RNG to simplify synchronization between client and server player faction/spawn assignments
			var playerRandom = new MersenneTwister(orderManager.LobbyInfo.GlobalSettings.RandomSeed);
			foreach (var cmp in WorldActor.TraitsImplementing<ICreatePlayers>())
				cmp.CreatePlayers(this, playerRandom);

			Game.Sound.SoundVolumeModifier = 1.0f;

			gameInfo = new GameInformation
			{
				Mod = Game.ModData.Manifest.Id,
				Version = Game.ModData.Manifest.Metadata.Version,

				MapUid = Map.Uid,
				MapTitle = Map.Title
			};

			RulesContainTemporaryBlocker = map.Rules.Actors.Any(a => a.Value.HasTraitInfo<ITemporaryBlockerInfo>());
		}

		public void AddToMaps(Actor self, IOccupySpace ios)
		{
			ActorMap.AddInfluence(self, ios);
			ActorMap.AddPosition(self, ios);
			ScreenMap.AddOrUpdate(self);
		}

		public void UpdateMaps(Actor self, IOccupySpace ios)
		{
			if (!self.IsInWorld)
				return;

			ScreenMap.AddOrUpdate(self);
			ActorMap.UpdatePosition(self, ios);
		}

		public void RemoveFromMaps(Actor self, IOccupySpace ios)
		{
			ActorMap.RemoveInfluence(self, ios);
			ActorMap.RemovePosition(self, ios);
			ScreenMap.Remove(self);
		}

		public void LoadComplete(WorldRenderer wr)
		{
			if (IsLoadingGameSave)
			{
				wasLoadingGameSave = true;
				Game.Sound.DisableAllSounds = true;
				foreach (var nsr in WorldActor.TraitsImplementing<INotifyGameLoading>())
					nsr.GameLoading(this);
			}

			// ScreenMap must be initialized before anything else
			using (new PerfTimer("ScreenMap.WorldLoaded"))
				ScreenMap.WorldLoaded(this, wr);

			foreach (var iwl in WorldActor.TraitsImplementing<IWorldLoaded>())
			{
				// These have already been initialized
				if (iwl == ScreenMap)
					continue;

				using (new PerfTimer(iwl.GetType().Name + ".WorldLoaded"))
					iwl.WorldLoaded(this, wr);
			}

			foreach (var p in Players)
				foreach (var iwl in p.PlayerActor.TraitsImplementing<IWorldLoaded>())
					using (new PerfTimer(iwl.GetType().Name + ".WorldLoaded"))
						iwl.WorldLoaded(this, wr);

			gameInfo.StartTimeUtc = DateTime.UtcNow;
			foreach (var player in Players)
				gameInfo.AddPlayer(player, OrderManager.LobbyInfo);

			gameInfo.DisabledSpawnPoints = OrderManager.LobbyInfo.DisabledSpawnPoints;

			var echo = OrderManager.Connection as EchoConnection;
			var rc = echo != null ? echo.Recorder : null;

			if (rc != null)
				rc.Metadata = new ReplayMetadata(gameInfo);
		}

		public void SetWorldOwner(Player p)
		{
			WorldActor.Owner = p;
		}

		public Actor CreateActor(string name, TypeDictionary initDict)
		{
			return CreateActor(true, name, initDict);
		}

		public Actor CreateActor(bool addToWorld, ActorReference reference)
		{
			return CreateActor(addToWorld, reference.Type, reference.InitDict);
		}

		public Actor CreateActor(bool addToWorld, string name, TypeDictionary initDict)
		{
			var a = new Actor(this, name, initDict);
			a.Initialize(addToWorld);
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

		public void Add(IEffect e)
		{
			effects.Add(e);

			var sp = e as ISpatiallyPartitionable;
			if (sp == null)
				unpartitionedEffects.Add(e);

			var se = e as ISync;
			if (se != null)
				syncedEffects.Add(se);
		}

		public void Remove(IEffect e)
		{
			effects.Remove(e);

			var sp = e as ISpatiallyPartitionable;
			if (sp == null)
				unpartitionedEffects.Remove(e);

			var se = e as ISync;
			if (se != null)
				syncedEffects.Remove(se);
		}

		public void RemoveAll(Predicate<IEffect> predicate)
		{
			effects.RemoveAll(predicate);
			unpartitionedEffects.RemoveAll(e => predicate((IEffect)e));
			syncedEffects.RemoveAll(e => predicate((IEffect)e));
		}

		public void AddFrameEndTask(Action<World> a) { frameEndActions.Enqueue(a); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = _ => { };

		public bool Paused { get; internal set; }
		public bool PredictedPaused { get; internal set; }
		public bool PauseStateLocked { get; set; }

		public int WorldTick { get; private set; }

		Dictionary<int, MiniYaml> gameSaveTraitData = new Dictionary<int, MiniYaml>();
		internal void AddGameSaveTraitData(int traitIndex, MiniYaml yaml)
		{
			gameSaveTraitData[traitIndex] = yaml;
		}

		public void SetPauseState(bool paused)
		{
			if (PauseStateLocked)
				return;

			IssueOrder(Order.FromTargetString("PauseGame", paused ? "Pause" : "UnPause", false));
			PredictedPaused = paused;
		}

		public void SetLocalPauseState(bool paused)
		{
			Paused = PredictedPaused = paused;
		}

		public void Tick()
		{
			if (wasLoadingGameSave && !IsLoadingGameSave)
			{
				foreach (var kv in gameSaveTraitData)
				{
					var tp = TraitDict.ActorsWithTrait<IGameSaveTraitData>()
						.Skip(kv.Key)
						.FirstOrDefault();

					if (tp.Actor == null)
						break;

					tp.Trait.ResolveTraitData(tp.Actor, kv.Value.Nodes);
				}

				gameSaveTraitData.Clear();

				Game.Sound.DisableAllSounds = false;
				foreach (var nsr in WorldActor.TraitsImplementing<INotifyGameLoaded>())
					nsr.GameLoaded(this);

				wasLoadingGameSave = false;
			}

			if (!Paused)
			{
				WorldTick++;

				using (new PerfSample("tick_actors"))
					foreach (var a in actors.Values)
						a.Tick();

				ApplyToActorsWithTraitTimed<ITick>((Actor actor, ITick trait) => trait.Tick(actor), "Trait");

				effects.DoTimed(e => e.Tick(this), "Effect");
			}

			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);
		}

		// For things that want to update their render state once per tick, ignoring pause state
		public void TickRender(WorldRenderer wr)
		{
			ApplyToActorsWithTraitTimed<ITickRender>((Actor actor, ITickRender trait) => trait.TickRender(wr, actor), "Render");
			ScreenMap.TickRender();
		}

		public IEnumerable<Actor> Actors { get { return actors.Values; } }
		public IEnumerable<IEffect> Effects { get { return effects; } }
		public IEnumerable<IEffect> UnpartitionedEffects { get { return unpartitionedEffects; } }
		public IEnumerable<ISync> SyncedEffects { get { return syncedEffects; } }

		public Actor GetActorById(uint actorId)
		{
			if (actors.TryGetValue(actorId, out var a))
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

				// Hash all the actors.
				foreach (var a in Actors)
					ret += n++ * (int)(1 + a.ActorID) * Sync.HashActor(a);

				// Hash fields marked with the ISync interface.
				foreach (var actor in ActorsHavingTrait<ISync>())
					foreach (var syncHash in actor.SyncHashes)
						ret += n++ * (int)(1 + actor.ActorID) * syncHash.Hash();

				// Hash game state relevant effects such as projectiles.
				foreach (var sync in SyncedEffects)
					ret += n++ * Sync.Hash(sync);

				// Hash the shared random number generator.
				ret += SharedRandom.Last;

				// Hash player RenderPlayer status
				foreach (var p in Players)
					if (p.UnlockedRenderPlayer)
						ret += Sync.HashPlayer(p);

				return ret;
			}
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return TraitDict.ActorsWithTrait<T>();
		}

		public void ApplyToActorsWithTraitTimed<T>(Action<Actor, T> action, string text)
		{
			TraitDict.ApplyToActorsWithTraitTimed<T>(action, text);
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>()
		{
			return TraitDict.ActorsHavingTrait<T>();
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>(Func<T, bool> predicate)
		{
			return TraitDict.ActorsHavingTrait(predicate);
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

		public void OnPlayerDisconnected(Player player)
		{
			var pi = gameInfo.GetPlayer(player);
			if (pi == null)
				return;

			pi.DisconnectFrame = OrderManager.NetFrameNumber;
		}

		public void RequestGameSave(string filename)
		{
			// Allow traits to save arbitrary data that will be passed back via IGameSaveTraitData.ResolveTraitData
			// at the end of the save restoration
			// TODO: This will need to be generalized to a request / response pair for multiplayer game saves
			var i = 0;
			foreach (var tp in TraitDict.ActorsWithTrait<IGameSaveTraitData>())
			{
				var data = tp.Trait.IssueTraitData(tp.Actor);
				if (data != null)
				{
					var yaml = new List<MiniYamlNode>() { new MiniYamlNode(i.ToString(), new MiniYaml("", data)) };
					IssueOrder(Order.FromTargetString("GameSaveTraitData", yaml.WriteToString(), true));
				}

				i++;
			}

			IssueOrder(Order.FromTargetString("CreateGameSave", filename, true));
		}

		public bool Disposing;

		public void Dispose()
		{
			Disposing = true;

			OrderGenerator?.Deactivate();

			frameEndActions.Clear();

			Game.Sound.StopAudio();
			Game.Sound.StopVideo();
			if (IsLoadingGameSave)
				Game.Sound.DisableAllSounds = false;

			ModelCache.Dispose();

			// Dispose newer actors first, and the world actor last
			foreach (var a in actors.Values.Reverse())
				a.Dispose();

			// Actor disposals are done in a FrameEndTask
			while (frameEndActions.Count != 0)
				frameEndActions.Dequeue()(this);

			Game.FinishBenchmark();
		}
	}

	public struct TraitPair<T> : IEquatable<TraitPair<T>>
	{
		public readonly Actor Actor;
		public readonly T Trait;

		public TraitPair(Actor actor, T trait) { Actor = actor; Trait = trait; }

		public static bool operator ==(TraitPair<T> me, TraitPair<T> other) { return me.Actor == other.Actor && Equals(me.Trait, other.Trait); }
		public static bool operator !=(TraitPair<T> me, TraitPair<T> other) { return !(me == other); }

		public override int GetHashCode() { return Actor.GetHashCode() ^ Trait.GetHashCode(); }

		public bool Equals(TraitPair<T> other) { return this == other; }
		public override bool Equals(object obj) { return obj is TraitPair<T> && Equals((TraitPair<T>)obj); }

		public override string ToString() { return Actor.Info.Name + "->" + Trait.GetType().Name; }
	}
}
