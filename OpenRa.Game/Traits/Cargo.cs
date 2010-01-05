using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Cargo : IPips, IOrder
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

				return new Order("Deploy", self, null, int2.Zero, null);
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
			return cargo.Count == self.Info.Passengers;
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
			for (var i = 0; i < self.Info.Passengers; i++)
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
			if (!a.traits.WithInterface<AttackBase>().Any())
				return PipType.Yellow;	// noncombat [E6,SPY,THF]
			if (a.traits.Contains<C4Demolition>())
				return PipType.Red;		// E7

			return PipType.Green;
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
		}
	}
}
