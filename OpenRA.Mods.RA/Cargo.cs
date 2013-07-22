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
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CargoInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		public readonly int MaxWeight = 0;
		public readonly int PipCount = 0;
		public readonly string[] Types = { };
		public readonly int UnloadFacing = 0;
		public readonly string[] InitialUnits = { };
		public readonly WRange MaximumUnloadAltitude = WRange.Zero;

		public object Create( ActorInitializer init ) { return new Cargo( init, this ); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyKilled, INotifyCapture
	{
		readonly Actor self;
		readonly CargoInfo info;

		int totalWeight = 0;
		List<Actor> cargo = new List<Actor>();
		public IEnumerable<Actor> Passengers { get { return cargo; } }

		public Cargo(ActorInitializer init, CargoInfo info)
		{
			this.self = init.self;
			this.info = info;

			if (init.Contains<CargoInit>())
			{
				cargo = init.Get<CargoInit,Actor[]>().ToList();
				totalWeight = cargo.Sum(c => GetWeight(c));
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					if (CanLoad(self, unit))
						Load(self, unit);
				}
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("Unload", 10, () => CanUnload(self)); }
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
				if (!CanUnload(self))
					return;

				self.CancelActivity();
				self.QueueActivity(new UnloadCargo(true));
			}
		}

		bool CanUnload(Actor self)
		{
			if (IsEmpty(self))
				return false;

			// Cannot unload mid-air
			var ios = self.TraitOrDefault<IOccupySpace>();
			if (ios != null && ios.CenterPosition.Z > info.MaximumUnloadAltitude.Range)
				return false;

			// TODO: Check if there is a free tile to unload to
			return true;
		}

		public bool CanLoad(Actor self, Actor a)
		{
			if (!HasSpace(GetWeight(a)))
				return false;

			// Cannot load mid-air
			return self.CenterPosition.Z <= info.MaximumUnloadAltitude.Range;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload") return null;
			return CanUnload(self) ? "deploy" : "deploy-blocked";
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self)) return null;
			return self.HasVoice("Unload") ? "Unload" : "Move";
		}

		public bool HasSpace(int weight) { return totalWeight + weight <= info.MaxWeight; }
		public bool IsEmpty(Actor self) { return cargo.Count == 0; }

		public Actor Peek(Actor self) {	return cargo[0]; }

		static int GetWeight(Actor a) { return a.Info.Traits.Get<PassengerInfo>().Weight; }

		public Actor Unload(Actor self)
		{
			var a = cargo[0];
			cargo.RemoveAt(0);
			totalWeight -= GetWeight(a);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.PassengerExited(self, a);

			return a;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			int numPips = info.PipCount;

			for (int i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			var n = i * info.MaxWeight / info.PipCount;

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
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var c in cargo)
				c.Kill(e.Attacker);
			cargo.Clear();
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (cargo == null)
				return;

			self.World.AddFrameEndTask(w =>
			{
				foreach (var p in Passengers)
					p.Owner = newOwner;
			});
		}
	}

	public interface INotifyPassengerEntered { void PassengerEntered(Actor self, Actor passenger); }
	public interface INotifyPassengerExited { void PassengerExited(Actor self, Actor passenger); }

	public class CargoInit : IActorInit<Actor[]>
	{
		[FieldFromYamlKey] public readonly Actor[] value = {};
		public CargoInit() { }
		public CargoInit(Actor[] init) { value = init; }
		public Actor[] Value(World world) { return value; }
	}
}
