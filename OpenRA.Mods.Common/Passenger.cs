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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class PassengerInfo : ITraitInfo
	{
		public readonly string CargoType = null;
		public readonly PipType PipType = PipType.Green;
		public int Weight = 1;

		public object Create( ActorInitializer init ) { return new Passenger( this ); }
	}

	public class Passenger : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly PassengerInfo info;
		public Passenger( PassengerInfo info ) { this.info = info; }
		public Actor Transport;

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<Cargo>("EnterTransport", 6,
					target => IsCorrectCargoType(target), target => CanEnter(target));
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "EnterTransport" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool IsCorrectCargoType( Actor target )
		{
			var ci = target.Info.Traits.Get<CargoInfo>();
			return ci.Types.Contains( info.CargoType );
		}

		bool CanEnter( Actor target )
		{
			var cargo = target.TraitOrDefault<Cargo>();
			return cargo != null && cargo.HasSpace(info.Weight);
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

				var target = Target.FromOrder(order);
				self.SetTargetLine(target, Color.Green);

				self.CancelActivity();
				self.QueueActivity(new MoveAdjacentTo(self, target));
				self.QueueActivity(new EnterTransport(self, order.TargetActor));
			}
		}
	}
}
