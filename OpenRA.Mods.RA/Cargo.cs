#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

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

		public object Create(ActorInitializer init) { return new Cargo(init, this); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyKilled, INotifyCapture, ITick, INotifySold
	{
		readonly Actor self;
		public readonly CargoInfo Info;

		int totalWeight = 0;
		List<Actor> cargo = new List<Actor>();
		public IEnumerable<Actor> Passengers { get { return cargo; } }

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }

		public Cargo(ActorInitializer init, CargoInfo info)
		{
			self = init.self;
			Info = info;

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
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("Unload", 10, CanUnload); }
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

				self.CancelActivity();
				self.QueueActivity(new UnloadCargo(self, true));
			}
		}

		IEnumerable<CPos> GetAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		bool CanUnload()
		{
			return !IsEmpty(self) && self.CenterPosition.Z == 0
				&& CurrentAdjacentCells != null && CurrentAdjacentCells.Any(c => Passengers.Any(p => p.Trait<IPositionable>().CanEnterCell(c)));
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return HasSpace(GetWeight(a)) && self.CenterPosition.Z == 0;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload") return null;
			return CanUnload() ? "deploy" : "deploy-blocked";
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self)) return null;
			return self.HasVoice("Unload") ? "Unload" : "Move";
		}

		public bool HasSpace(int weight) { return totalWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty(Actor self) { return cargo.Count == 0; }

		public Actor Peek(Actor self) { return cargo[0]; }

		static int GetWeight(Actor a) { return a.Info.Traits.Get<PassengerInfo>().Weight; }

		public Actor Unload(Actor self)
		{
			var a = cargo[0];
			cargo.RemoveAt(0);
			totalWeight -= GetWeight(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.PassengerExited(self, a);

			a.Trait<Passenger>().Transport = null;
			return a;
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
			totalWeight += GetWeight(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
				npe.PassengerEntered(self, a);

			a.Trait<Passenger>().Transport = self;
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

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (cargo == null)
				return;

			foreach (var p in Passengers)
				p.ChangeOwner(newOwner, captor);
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
		public readonly Actor[] value = { };
		public RuntimeCargoInit() { }
		public RuntimeCargoInit(Actor[] init) { value = init; }
		public Actor[] Value(World world) { return value; }
	}

	public class CargoInit : IActorInit<string[]>
	{
		[FieldFromYamlKey]
		public readonly string[] value = { };
		public CargoInit() { }
		public CargoInit(string[] init) { value = init; }
		public string[] Value(World world) { return value; }
	}
}
