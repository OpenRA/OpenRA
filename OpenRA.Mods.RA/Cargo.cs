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
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CargoInfo : ITraitInfo
	{
		public readonly int Passengers = 0;
		public readonly string[] Types = { };
		public readonly int UnloadFacing = 0;

		public object Create( ActorInitializer init ) { return new Cargo( init.self ); }
	}

	public class Cargo : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyDamage
	{
		readonly Actor self;
		List<Actor> cargo = new List<Actor>();
		public IEnumerable<Actor> Passengers { get { return cargo; } }

		public Cargo( Actor self )
		{
			this.self = self;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter( "Unload", 10, () => CanUnload( self ) );
				yield return new UnitTraitOrderTargeter<Passenger>( "ReverseEnterTransport", 6, null, false, true );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Unload" )
				return new Order( order.OrderID, self, queued );

			if( order.OrderID == "ReverseEnterTransport" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

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

			if( order.OrderString == "ReverseEnterTransport" )
			{
				if( order.TargetActor != null && order.Subject.Owner == order.TargetActor.Owner )
				{
					var passenger = order.TargetActor.Trait<Passenger>();
					passenger.ResolveOrder(order.TargetActor,
						new Order("EnterTransport", order.TargetActor, false) { TargetActor = self });
				}
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

		public void Damaged(Actor self, AttackInfo e)
		{
			if( e.DamageStateChanged && e.DamageState == DamageState.Dead )
			{
				foreach( var c in cargo )
					c.Destroy();
				cargo.Clear();
			}
		}
	}
}
