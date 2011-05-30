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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CargoInfo : ITraitInfo
	{
		public readonly int MaxWeight = 0;
		public readonly int PipCount = 0;
		public readonly string[] Types = { };
		public readonly int UnloadFacing = 0;

		public object Create( ActorInitializer init ) { return new Cargo( init.self, this ); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyKilled
	{
		readonly Actor self;
		readonly CargoInfo info;

		int totalWeight = 0;
		List<Actor> cargo = new List<Actor>();
		public IEnumerable<Actor> Passengers { get { return cargo; } }

		public Cargo( Actor self, CargoInfo info ) { this.self = self; this.info = info; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter( "Unload", 10, () => CanUnload( self ) ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Unload" )
				return new Order( order.OrderID, self, queued );

			return null;
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

		public bool HasSpace(int weight) { return totalWeight + weight <= info.MaxWeight; }
		public bool IsEmpty(Actor self) { return cargo.Count == 0; }

		public Actor Peek(Actor self) {	return cargo[0]; }
		
		public Actor Unload(Actor self)
		{
			var a = cargo[0];
			cargo.RemoveAt(0);

			var pi = a.Info.Traits.GetOrDefault<PassengerInfo>();
			totalWeight -= pi != null ? pi.Weight : 1;

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
			var pi = a.Info.Traits.GetOrDefault<PassengerInfo>();
			totalWeight += pi != null ? pi.Weight : 1;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach( var c in cargo )
				c.Destroy();
			cargo.Clear();
		}
	}
}
