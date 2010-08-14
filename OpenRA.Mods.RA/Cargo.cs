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
		public readonly string[] Types = { };
		public readonly int UnloadFacing = 0;
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		List<Actor> cargo = new List<Actor>();

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right || underCursor != self)
				return null;
			
			return new Order("Unload", self);				
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Unload")
			{
				if (!CanUnload(self))
					return;
				
				self.CancelActivity();
				self.QueueActivity(new UnloadCargo());
			}
		}
		
		bool CanUnload(Actor self)
		{
			if (IsEmpty(self))
				return false;
			
			// Cannot unload mid-air
			var move = self.TraitOrDefault<IMove>();
			if (move != null && move.Altitude > 0)
				return false;
			
			// Todo: Check if there is a free tile to unload to
			return true;
		}
		
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload") return null;
			return CanUnload(self) ? "deploy" : "deploy-blocked";
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self)) return null;			
			return "Move";
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
			return a.Trait<Passenger>().ColorOfCargoPip( a );
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
		}
	}
}
