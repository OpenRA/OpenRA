#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CargoInfo : TraitInfo<Cargo>
	{
		public readonly int Passengers = 0;
		public readonly string[] PassengerTypes = { };
		public readonly int UnloadFacing = 0;
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IProvideCursor
	{
		List<Actor> cargo = new List<Actor>();

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
		
		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Deploy") ? "deploy" : null;
		}

		public bool IsFull(Actor self)
		{
			return cargo.Count == self.Info.Traits.Get<CargoInfo>().Passengers;
		}

		public bool IsEmpty(Actor self)
		{
			return cargo.Count == 0;
		}

		public Actor Peek(Actor self)
		{
			return cargo[0];
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
