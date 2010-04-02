#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits.Activities;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class CargoInfo : ITraitInfo
	{
		public readonly int Passengers = 0;
		public readonly UnitMovementType[] PassengerTypes = { };
		public readonly int UnloadFacing = 0;

		public object Create(Actor self) { return new Cargo(self); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder
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
			return a.traits.Get<Passenger>().ColorOfCargoPip( a );
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
		}
	}
}
