using System.Collections.Generic;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class CargoInfo : ITraitInfo
	{
		public readonly int Passengers = 0;
		public readonly UnitMovementType[] PassengerTypes = { };
		public readonly int UnloadFacing = 0;

		public object Create(Actor self) { return new Cargo(self); }
	}

	class Cargo : IPips, IIssueOrder, IResolveOrder
	{
		List<Actor> cargo = new List<Actor>();

		public Cargo(Actor self) {}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			// todo: check if there is an unoccupied `land` tile adjacent
			if (mi.Button == MouseButton.Right && underCursor == self && cargo.Count > 0)
			{
				var unit = underCursor.traits.GetOrDefault<Unit>();
				if (unit != null && unit.Altitude > 0) return null;

				return new Order("Deploy", self);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				// todo: eject the units
				self.CancelActivity();
				self.QueueActivity(new UnloadCargo());
			}
		}

		public bool IsFull(Actor self)
		{
			return cargo.Count == self.Info.Traits.Get<CargoInfo>().Passengers;
		}

		public bool IsEmpty(Actor self)
		{
			return cargo.Count == 0;
		}

		public Actor Unload(Actor self)
		{
			var a = cargo[0];
			cargo.RemoveAt(0);
			return a;
		}

		public IEnumerable<PipType> GetPips( Actor self )
		{
			var numPips = self.Info.Traits.Get<CargoInfo>().Passengers;
			for (var i = 0; i < numPips; i++)
				if (i >= cargo.Count)
					yield return PipType.Transparent;
				else
					yield return GetPipForPassenger(cargo[i]);
		}

		static PipType GetPipForPassenger(Actor a)
		{
			// probably not actually right yet; fix to match real-ra

			if (a.traits.Contains<AutoHeal>())
				return PipType.Yellow;
			if (!a.traits.Contains<AttackBase>())
				return PipType.Yellow;	// noncombat [E6,SPY,THF]

			// todo: fix E7 color again.

			return PipType.Green;
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
		}
	}
}
