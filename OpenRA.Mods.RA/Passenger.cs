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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class PassengerInfo : ITraitInfo
	{
		public readonly string CargoType = null;
		public readonly PipType PipType = PipType.Green;

		public object Create( ActorInitializer init ) { return new Passenger( init.self ); }
	}

	class Passenger : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		public Passenger( Actor self ) { this.self = self; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Cargo>( "EnterTransport", 6, false, true,
					target => IsCorrectCargoType( target ), target => CanEnter( target ) );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "EnterTransport" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
		}

		bool IsCorrectCargoType( Actor target )
		{
			var pi = self.Info.Traits.Get<PassengerInfo>();
			var ci = target.Info.Traits.Get<CargoInfo>();
			return ci.Types.Contains( pi.CargoType );
		}

		bool CanEnter( Actor target )
		{
			var cargo = target.TraitOrDefault<Cargo>();
			return (cargo != null && !cargo.IsFull(target));
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTransport" ||
				!CanEnter(order.TargetActor)) return null;
			return "Move";
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport")
			{
				if (order.TargetActor == null) return;
				if (!CanEnter(order.TargetActor)) return;
				if (!IsCorrectCargoType(order.TargetActor)) return;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, 1));
				self.QueueActivity(new EnterTransport(self, order.TargetActor));
			}
		}

		public PipType ColorOfCargoPip( Actor self )
		{
			return self.Info.Traits.Get<PassengerInfo>().PipType;
		}
	}
}
