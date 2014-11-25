#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Mods.Common.Orders;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can transport Passenger actors.")]
	public class CargoInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		public readonly int MaxWeight = 0;
		public readonly int PipCount = 0;
		public readonly string[] Types = { };
		public readonly string[] InitialUnits = { };
		public readonly bool EjectOnSell = true;

		[Desc("Which direction the passenger will face (relative to the transport) when unloading.")]
		public readonly int PassengerFacing = 128;

		public object Create(ActorInitializer init) { return new Cargo(init, this); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated, INotifyKilled, INotifyOwnerChanged, INotifyAddedToWorld, ITick, INotifySold, IDisableMove
	{
		public readonly CargoInfo Info;
		readonly Actor self;
		readonly List<Actor> cargo = new List<Actor>();
		readonly HashSet<Actor> reserves = new HashSet<Actor>();
		readonly Lazy<IFacing> facing;

		int totalWeight = 0;
		int reservedWeight = 0;
		Helicopter helicopter;

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }
		public bool Unloading { get; internal set; }
		public IEnumerable<Actor> Passengers { get { return cargo; } }
		public int PassengerCount { get { return cargo.Count; } }

		public Cargo(ActorInitializer init, CargoInfo info)
		{
			self = init.self;
			Info = info;
			Unloading = false;

			if (init.Contains<RuntimeCargoInit>())
			{
				cargo = init.Get<RuntimeCargoInit, Actor[]>().ToList();
				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			else if (init.Contains<CargoInit>())
			{
				foreach (var u in init.Get<CargoInit, string[]>())
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					cargo.Add(unit);
				}

				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					cargo.Add(unit);
				}

				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		public void Created(Actor self)
		{
			helicopter = self.TraitOrDefault<Helicopter>();
		}

		static int GetWeight(Actor a) { return a.Info.Traits.Get<PassengerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter(OrderCode.Unload, 10, CanUnload); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == OrderCode.Unload)
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.ID == OrderCode.Unload)
			{
				if (!CanUnload())
					return;

				Unloading = true;
				self.CancelActivity();
				if (helicopter != null)
					self.QueueActivity(new HeliLand(true));
				self.QueueActivity(new UnloadCargo(self, true));
			}
		}

		IEnumerable<CPos> GetAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		bool CanUnload()
		{
			return !IsEmpty(self) && (helicopter == null || helicopter.CanLand(self.Location))
				&& CurrentAdjacentCells != null && CurrentAdjacentCells.Any(c => Passengers.Any(p => p.Trait<IPositionable>().CanEnterCell(c)));
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return (reserves.Contains(a) || HasSpace(GetWeight(a))) && self.CenterPosition.Z == 0;
		}

		internal bool ReserveSpace(Actor a)
		{
			if (reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!HasSpace(w))
				return false;

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
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.ID != OrderCode.Unload)
				return null;

			return CanUnload() ? "deploy" : "deploy-blocked";
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.ID != OrderCode.Unload || IsEmpty(self))
				return null;

			return self.HasVoice("Unload") ? "Unload" : "Move";
		}

		public bool MoveDisabled(Actor self) { return reserves.Any(); }
		public bool HasSpace(int weight) { return totalWeight + reservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty(Actor self) { return cargo.Count == 0; }

		public Actor Peek(Actor self) { return cargo[0]; }

		public Actor Unload(Actor self)
		{
			var a = cargo[0];

			cargo.RemoveAt(0);
			totalWeight -= GetWeight(a);

			SetPassengerFacing(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.PassengerExited(self, a);

			var p = a.Trait<Passenger>();
			p.Transport = null;

			foreach (var u in p.Info.GrantUpgrades)
				self.Trait<UpgradeManager>().RevokeUpgrade(self, u, p);

			return a;
		}

		void SetPassengerFacing(Actor passenger)
		{
			if (facing.Value == null)
				return;

			var passengerFacing = passenger.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacing.Facing = facing.Value.Facing + Info.PassengerFacing;

			var passengerTurreted = passenger.TraitOrDefault<Turreted>();
			if (passengerTurreted != null)
				passengerTurreted.TurretFacing = facing.Value.Facing + Info.PassengerFacing;
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
				var pi = c.Info.Traits.Get<PassengerInfo>();
				if (n < pi.Weight)
					return pi.PipType;
				else
					n -= pi.Weight;
			}

			return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
			var w = GetWeight(a);
			totalWeight += w;
			if (reserves.Contains(a))
			{
				reservedWeight -= w;
				reserves.Remove(a);
			}

			foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
				npe.PassengerEntered(self, a);

			var p = a.Trait<Passenger>();
			p.Transport = self;
			foreach (var u in p.Info.GrantUpgrades)
				self.Trait<UpgradeManager>().GrantUpgrade(self, u, p);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var c in cargo)
				c.Kill(e.Attacker);

			cargo.Clear();
		}

		public void Selling(Actor self) { }
		public void Sold(Actor self)
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

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (cargo == null)
				return;

			self.World.AddFrameEndTask(w =>
			{
				foreach (var p in Passengers)
					p.Owner = newOwner;
			});
		}

		public void AddedToWorld(Actor self)
		{
			// Force location update to avoid issues when initial spawn is outside map
			currentCell = self.Location;
			CurrentAdjacentCells = GetAdjacentCells();
		}

		bool initialized;
		public void Tick(Actor self)
		{
			// Notify initial cargo load
			if (!initialized)
			{
				foreach (var c in cargo)
				{
					c.Trait<Passenger>().Transport = self;

					foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
						npe.PassengerEntered(self, c);
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

	public interface INotifyPassengerEntered { void PassengerEntered(Actor self, Actor passenger); }
	public interface INotifyPassengerExited { void PassengerExited(Actor self, Actor passenger); }

	public class RuntimeCargoInit : IActorInit<Actor[]>
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
