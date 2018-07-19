#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can transport Passenger actors.")]
	public class SharedCargoInfo : PausableConditionalTraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Number of pips to display when this actor is selected.")]
		public readonly int PipCount = 0;

		[Desc("`Passenger.CargoType`s that can be loaded into this actor.")]
		public readonly HashSet<string> Types = new HashSet<string>();

		[Desc("`SharedCargoManager.Type` thar this actor shares its passengers.")]
		public readonly string ShareType = "tunnel";

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

		public override object Create(ActorInitializer init) { return new SharedCargo(init, this); }
	}

	public class SharedCargo : PausableConditionalTrait<SharedCargoInfo>, IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated,
		INotifyAddedToWorld, ITick, IIssueDeployOrder, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;
		public readonly SharedCargoManager Manager;
		readonly Dictionary<string, Stack<int>> passengerTokens = new Dictionary<string, Stack<int>>();
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;

		Aircraft aircraft;
		ConditionManager conditionManager;
		int loadingToken = ConditionManager.InvalidConditionToken;
		Stack<int> loadedTokens = new Stack<int>();

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }
		public bool Unloading { get; internal set; }

		public SharedCargo(ActorInitializer init, SharedCargoInfo info)
			: base(info)
		{
			self = init.Self;
			Manager = self.Owner.PlayerActor.TraitsImplementing<SharedCargoManager>().Where(m => m.Info.Type == Info.ShareType).First();
			Unloading = false;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;
			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		void INotifyCreated.Created(Actor self)
		{
			aircraft = self.TraitOrDefault<Aircraft>();
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		static int GetWeight(Actor a) { return a.Info.TraitInfo<SharedPassengerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DeployOrderTargeter("UnloadShared", 10, () => CanUnload()? Info.UnloadCursor : Info.UnloadBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "UnloadShared")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self)
		{
			return new Order("UnloadShared", self, false);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return true; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "UnloadShared")
			{
				if (!CanUnload())
					return;

				Unloading = true;
				self.CancelActivity();
				if (aircraft != null)
					self.QueueActivity(new HeliLand(self, true));
				self.QueueActivity(new UnloadSharedCargo(self, true));
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

			return !Manager.IsEmpty() && (aircraft == null || aircraft.CanLand(self.Location)) && !IsTraitPaused
				&& CurrentAdjacentCells != null && CurrentAdjacentCells.Any(c => Manager.Passengers.Any(p => p.Trait<IPositionable>().CanEnterCell(c)));
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return (Manager.Reserves.Contains(a) || Manager.HasSpace(GetWeight(a))) && self.IsAtGroundLevel() && !IsTraitPaused;
		}

		internal bool ReserveSpace(Actor a)
		{
			if (Manager.Reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!Manager.HasSpace(w))
				return false;

			if (conditionManager != null && loadingToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.LoadingCondition))
				loadingToken = conditionManager.GrantCondition(self, Info.LoadingCondition);

			Manager.Reserves.Add(a);
			Manager.ReservedWeight += w;

			return true;
		}

		internal void UnreserveSpace(Actor a)
		{
			if (!Manager.Reserves.Contains(a))
				return;

			Manager.ReservedWeight -= GetWeight(a);
			Manager.Reserves.Remove(a);

			if (loadingToken != ConditionManager.InvalidConditionToken)
				loadingToken = conditionManager.RevokeCondition(self, loadingToken);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "UnloadShared")
				return null;

			return CanUnload() ? Info.UnloadCursor : Info.UnloadBlockedCursor;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "UnloadShared" || Manager.IsEmpty() || !self.HasVoice(Info.UnloadVoice))
				return null;

			return Info.UnloadVoice;
		}

		public Actor Peek(Actor self) { return Manager.Cargo.Peek(); }

		public Actor Unload(Actor self)
		{
			var a = Manager.Cargo.Pop();

			Manager.TotalWeight -= GetWeight(a);

			SetPassengerFacing(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.OnPassengerExited(self, a);

			foreach (var nec in a.TraitsImplementing<INotifyExitedSharedCargo>())
				nec.OnExitedSharedCargo(a, self);

			var p = a.Trait<SharedPassenger>();
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
			if (IsTraitDisabled)
				yield break;

			var numPips = Info.PipCount;

			for (var i = 0; i<numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			var n = i * Manager.Info.MaxWeight / Info.PipCount;

			foreach (var c in Manager.Cargo)
			{
				var pi = c.Info.TraitInfo<SharedPassengerInfo>();
				if (n<pi.Weight)
					return pi.PipType;
				else
					n -= pi.Weight;
			}

			return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			Manager.Cargo.Push(a);
			var w = GetWeight(a);
			Manager.TotalWeight += w;
			if (Manager.Reserves.Contains(a))
			{
				Manager.ReservedWeight -= w;
				Manager.Reserves.Remove(a);

				if (loadingToken != ConditionManager.InvalidConditionToken)
					loadingToken = conditionManager.RevokeCondition(self, loadingToken);
			}

			// If not initialized then this will be notified in the first tick
			if (initialized)
			{
				foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
					npe.OnPassengerEntered(self, a);

				foreach (var nec in a.TraitsImplementing<INotifyEnteredSharedCargo>())
					nec.OnEnteredSharedCargo(a, self);
			}

			var p = a.Trait<SharedPassenger>();
			p.Transport = self;

			string passengerCondition;
			if (conditionManager != null && Info.PassengerConditions.TryGetValue(a.Info.Name, out passengerCondition))
				passengerTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, passengerCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Force location update to avoid issues when initial spawn is outside map
			currentCell = self.Location;
			CurrentAdjacentCells = GetAdjacentCells();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!self.World.ActorsWithTrait<SharedCargo>().Where(a => a.Trait.Info.ShareType == Info.ShareType && a.Actor.Owner == self.Owner && a.Actor != self).Any())
				Manager.Clear(e);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (!self.World.ActorsWithTrait<SharedCargo>().Where(a => a.Trait.Info.ShareType == Info.ShareType && a.Actor.Owner == self.Owner && a.Actor != self).Any())
				Manager.Clear();
		}

		bool initialized;
		void ITick.Tick(Actor self)
		{
			// Notify initial cargo load
			if (!initialized)
			{
				foreach (var c in Manager.Cargo)
				{
					c.Trait<SharedPassenger>().Transport = self;

					foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
						npe.OnPassengerEntered(self, c);

					foreach (var nec in c.TraitsImplementing<INotifyEnteredSharedCargo>())
						nec.OnEnteredSharedCargo(c, self);
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
}
