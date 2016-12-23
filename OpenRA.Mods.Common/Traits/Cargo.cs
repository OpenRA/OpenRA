#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can transport Passenger actors.")]
	public class CargoInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("The maximum sum of Passenger.Weight that this actor can support.")]
		public readonly int MaxWeight = 0;

		[Desc("Number of pips to display when this actor is selected.")]
		public readonly int PipCount = 0;

		[Desc("`Passenger.CargoType`s that can be loaded into this actor.")]
		public readonly HashSet<string> Types = new HashSet<string>();

		[Desc("A list of actor types that are initially spawned into this actor.")]
		public readonly string[] InitialUnits = { };

		[Desc("When this actor is sold should all of its passengers be unloaded?")]
		public readonly bool EjectOnSell = true;

		[Desc("When this actor dies should all of its passengers be unloaded?")]
		public readonly bool EjectOnDeath = false;

		[Desc("Terrain types that this actor is allowed to eject actors onto. Leave empty for all terrain types.")]
		public readonly HashSet<string> UnloadTerrainTypes = new HashSet<string>();

		[Desc("Voice to play when ordered to unload the passengers.")]
		[VoiceReference] public readonly string UnloadVoice = "Action";

		[Desc("Which direction the passenger will face (relative to the transport) when unloading.")]
		public readonly int PassengerFacing = 128;

		[Desc("Cursor to display when able to unload the passengers.")]
		public readonly string UnloadCursor = "deploy";

		[Desc("Cursor to display when unable to unload the passengers.")]
		public readonly string UnloadBlockedCursor = "deploy-blocked";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while waiting for cargo to load.")]
		public readonly string LoadingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while passengers are loaded.",
			"Condition can stack with multiple passengers.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are loaded inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> PassengerConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterPassengerConditions { get { return PassengerConditions.Values; } }

		public object Create(ActorInitializer init) { return new Cargo(init, this); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated, INotifyKilled,
		INotifyOwnerChanged, INotifyAddedToWorld, ITick, INotifySold, INotifyActorDisposing
	{
		public readonly CargoInfo Info;
		readonly Actor self;
		readonly Stack<Actor> cargo = new Stack<Actor>();
		readonly HashSet<Actor> reserves = new HashSet<Actor>();
		readonly Dictionary<string, Stack<int>> passengerTokens = new Dictionary<string, Stack<int>>();
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;

		int totalWeight = 0;
		int reservedWeight = 0;
		Aircraft aircraft;
		ConditionManager conditionManager;
		int loadingToken = ConditionManager.InvalidConditionToken;
		Stack<int> loadedTokens = new Stack<int>();

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }
		public bool Unloading { get; internal set; }
		public IEnumerable<Actor> Passengers { get { return cargo; } }
		public int PassengerCount { get { return cargo.Count; } }

		public Cargo(ActorInitializer init, CargoInfo info)
		{
			self = init.Self;
			Info = info;
			Unloading = false;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;

			if (init.Contains<RuntimeCargoInit>())
			{
				cargo = new Stack<Actor>(init.Get<RuntimeCargoInit, Actor[]>());
				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			else if (init.Contains<CargoInit>())
			{
				foreach (var u in init.Get<CargoInit, string[]>())
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					cargo.Push(unit);
				}

				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					cargo.Push(unit);
				}

				totalWeight = cargo.Sum(c => GetWeight(c));
			}

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		void INotifyCreated.Created(Actor self)
		{
			aircraft = self.TraitOrDefault<Aircraft>();
			conditionManager = self.Trait<ConditionManager>();
		}

		static int GetWeight(Actor a) { return a.Info.TraitInfo<PassengerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("Unload", 10,
				() => CanUnload() ? Info.UnloadCursor : Info.UnloadBlockedCursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Unload")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Unload")
			{
				if (!CanUnload())
					return;

				Unloading = true;
				self.CancelActivity();
				if (aircraft != null)
					self.QueueActivity(new HeliLand(self, true));
				self.QueueActivity(new UnloadCargo(self, true));
			}
		}

		IEnumerable<CPos> GetAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		bool CanUnload()
		{
			if (checkTerrainType)
			{
				var terrainType = self.World.Map.GetTerrainInfo(self.Location).Type;

				if (!Info.UnloadTerrainTypes.Contains(terrainType))
					return false;
			}

			return !IsEmpty(self) && (aircraft == null || aircraft.CanLand(self.Location))
				&& CurrentAdjacentCells != null && CurrentAdjacentCells.Any(c => Passengers.Any(p => p.Trait<IPositionable>().CanEnterCell(c)));
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return (reserves.Contains(a) || HasSpace(GetWeight(a))) && self.IsAtGroundLevel();
		}

		internal bool ReserveSpace(Actor a)
		{
			if (reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!HasSpace(w))
				return false;

			if (conditionManager != null && loadingToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.LoadingCondition))
				loadingToken = conditionManager.GrantCondition(self, Info.LoadingCondition);

			reserves.Add(a);
			reservedWeight += w;

			return true;
		}

		internal void UnreserveSpace(Actor a)
		{
			if (!reserves.Contains(a))
				return;

			reservedWeight -= GetWeight(a);
			reserves.Remove(a);

			if (loadingToken != ConditionManager.InvalidConditionToken)
				loadingToken = conditionManager.RevokeCondition(self, loadingToken);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload")
				return null;

			return CanUnload() ? Info.UnloadCursor : Info.UnloadBlockedCursor;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self) || !self.HasVoice(Info.UnloadVoice))
				return null;

			return Info.UnloadVoice;
		}

		public bool HasSpace(int weight) { return totalWeight + reservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty(Actor self) { return cargo.Count == 0; }

		public Actor Peek(Actor self) { return cargo.Peek(); }

		public Actor Unload(Actor self)
		{
			var a = cargo.Pop();

			totalWeight -= GetWeight(a);

			SetPassengerFacing(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.OnPassengerExited(self, a);

			var p = a.Trait<Passenger>();
			p.Transport = null;

			Stack<int> passengerToken;
			if (passengerTokens.TryGetValue(a.Info.Name, out passengerToken) && passengerToken.Any())
				conditionManager.RevokeCondition(self, passengerToken.Pop());

			if (loadedTokens.Any())
				conditionManager.RevokeCondition(self, loadedTokens.Pop());

			return a;
		}

		void SetPassengerFacing(Actor passenger)
		{
			if (facing.Value == null)
				return;

			var passengerFacing = passenger.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacing.Facing = facing.Value.Facing + Info.PassengerFacing;

			foreach (var t in passenger.TraitsImplementing<Turreted>())
				t.TurretFacing = facing.Value.Facing + Info.PassengerFacing;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = Info.PipCount;

			for (var i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			var n = i * Info.MaxWeight / Info.PipCount;

			foreach (var c in cargo)
			{
				var pi = c.Info.TraitInfo<PassengerInfo>();
				if (n < pi.Weight)
					return pi.PipType;
				else
					n -= pi.Weight;
			}

			return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Push(a);
			var w = GetWeight(a);
			totalWeight += w;
			if (reserves.Contains(a))
			{
				reservedWeight -= w;
				reserves.Remove(a);

				if (loadingToken != ConditionManager.InvalidConditionToken)
					loadingToken = conditionManager.RevokeCondition(self, loadingToken);
			}

			// If not initialized then this will be notified in the first tick
			if (initialized)
				foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
					npe.OnPassengerEntered(self, a);

			var p = a.Trait<Passenger>();
			p.Transport = self;

			string passengerCondition;
			if (conditionManager != null && Info.PassengerConditions.TryGetValue(a.Info.Name, out passengerCondition))
				passengerTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, passengerCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Info.EjectOnDeath)
				while (!IsEmpty(self) && CanUnload())
				{
					var passenger = Unload(self);
					var cp = self.CenterPosition;
					var inAir = self.World.Map.DistanceAboveTerrain(cp).Length != 0;
					var positionable = passenger.Trait<IPositionable>();
					positionable.SetPosition(passenger, self.Location);

					if (!inAir && positionable.CanEnterCell(self.Location, self, false))
					{
						self.World.AddFrameEndTask(w => w.Add(passenger));
						var nbm = passenger.TraitOrDefault<INotifyBlockingMove>();
						if (nbm != null)
							nbm.OnNotifyBlockingMove(passenger, passenger);
					}
					else
						passenger.Kill(e.Attacker);
				}

			foreach (var c in cargo)
				c.Kill(e.Attacker);

			cargo.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in cargo)
				c.Dispose();

			cargo.Clear();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			if (!Info.EjectOnSell || cargo == null)
				return;

			while (!IsEmpty(self))
				SpawnPassenger(Unload(self));
		}

		void SpawnPassenger(Actor passenger)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Add(passenger);
				passenger.Trait<IPositionable>().SetPosition(passenger, self.Location);

				// TODO: this won't work well for >1 actor as they should move towards the next enterable (sub) cell instead
			});
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (cargo == null)
				return;

			foreach (var p in Passengers)
				p.ChangeOwner(newOwner);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Force location update to avoid issues when initial spawn is outside map
			currentCell = self.Location;
			CurrentAdjacentCells = GetAdjacentCells();
		}

		bool initialized;
		void ITick.Tick(Actor self)
		{
			// Notify initial cargo load
			if (!initialized)
			{
				foreach (var c in cargo)
				{
					c.Trait<Passenger>().Transport = self;

					foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
						npe.OnPassengerEntered(self, c);
				}

				initialized = true;
			}

			var cell = self.World.Map.CellContaining(self.CenterPosition);
			if (currentCell != cell)
			{
				currentCell = cell;
				CurrentAdjacentCells = GetAdjacentCells();
			}
		}
	}

	public class RuntimeCargoInit : IActorInit<Actor[]>, ISuppressInitExport
	{
		[FieldFromYamlKey]
		readonly Actor[] value = { };
		public RuntimeCargoInit() { }
		public RuntimeCargoInit(Actor[] init) { value = init; }
		public Actor[] Value(World world) { return value; }
	}

	public class CargoInit : IActorInit<string[]>
	{
		[FieldFromYamlKey]
		readonly string[] value = { };
		public CargoInit() { }
		public CargoInit(string[] init) { value = init; }
		public string[] Value(World world) { return value; }
	}
}
