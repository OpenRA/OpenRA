#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public enum AlternateTransportsMode { None, Force, Default, Always }

	public class EnterTransportTargeter : EnterAlliedActorTargeter<Cargo>
	{
		readonly AlternateTransportsMode mode;

		public EnterTransportTargeter(string order, int priority,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor,
			AlternateTransportsMode mode)
			: base (order, priority, canTarget, useEnterCursor) { this.mode = mode; }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			switch (mode)
			{
				case AlternateTransportsMode.None:
					break;
				case AlternateTransportsMode.Force:
					if (modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Default:
					if (!modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Always:
					return false;
			}

			return base.CanTargetActor(self, target, modifiers, ref cursor);
		}
	}

	public class EnterTransportsTargeter : EnterAlliedActorTargeter<Cargo>
	{
		readonly AlternateTransportsMode mode;

		public EnterTransportsTargeter(string order, int priority,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor,
			AlternateTransportsMode mode)
			: base (order, priority, canTarget, useEnterCursor) { this.mode = mode; }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			switch (mode)
			{
				case AlternateTransportsMode.None:
					return false;
				case AlternateTransportsMode.Force:
					if (!modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Default:
					if (modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Always:
					break;
			}
			return base.CanTargetActor(self, target, modifiers, ref cursor);
		}
	}

	[Desc("This actor can enter Cargo actors.")]
	public class PassengerInfo : ITraitInfo
	{
		public readonly string CargoType = null;
		public readonly PipType PipType = PipType.Green;
		public readonly int Weight = 1;

		[Desc("Use to set when to use alternate transports (Never, Force, Default, Always).",
			"Force - use force move modifier (Alt) to enable.",
			"Default - use force move modifier (Alt) to disable.")]
		public readonly AlternateTransportsMode AlternateTransportsMode = AlternateTransportsMode.Force;

		[Desc("Number of retries using alternate transports.")]
		public readonly int MaxAlternateTransportAttempts = 1;

		[Desc("Range from self for looking for an alternate transport (default: 5.5 cells).")]
		public readonly WRange AlternateTransportScanRange = WRange.FromCells(11) / 2;

		public object Create(ActorInitializer init) { return new Passenger(this); }
	}

	public class Passenger : IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld
	{
		public readonly PassengerInfo Info;
		public Passenger(PassengerInfo info) { Info = info; }
		public Actor Transport;
		public Cargo ReservedCargo { get; private set; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterTransportTargeter("EnterTransport", 6,
					target => IsCorrectCargoType(target), target => CanEnter(target),
					Info.AlternateTransportsMode);
				yield return new EnterTransportsTargeter("EnterTransports", 6,
					target => IsCorrectCargoType(target), target => CanEnter(target),
					Info.AlternateTransportsMode);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterTransport" || order.OrderID == "EnterTransports")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool IsCorrectCargoType(Actor target)
		{
			var ci = target.Info.Traits.Get<CargoInfo>();
			return ci.Types.Contains(Info.CargoType);
		}

		bool CanEnter(Cargo cargo)
		{
			return cargo != null && cargo.HasSpace(Info.Weight);
		}

		bool CanEnter(Actor target)
		{
			return CanEnter(target.TraitOrDefault<Cargo>());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if ((order.OrderString != "EnterTransport" && order.OrderString != "EnterTransports") ||
				!CanEnter(order.TargetActor)) return null;
			return "Move";
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport" || order.OrderString == "EnterTransports")
			{
				if (order.TargetActor == null) return;
				if (!CanEnter(order.TargetActor)) return;
				if (!IsCorrectCargoType(order.TargetActor)) return;

				var target = Target.FromOrder(self.World, order);
				self.SetTargetLine(target, Color.Green);

				self.CancelActivity();
				var transports = order.OrderString == "EnterTransports";
				self.QueueActivity(new EnterTransport(self, order.TargetActor, transports ? Info.MaxAlternateTransportAttempts : 0, transports));
			}
		}

		public bool Reserve(Actor self, Cargo cargo)
		{
			Unreserve(self);
			if (!cargo.ReserveSpace(self))
				return false;
			ReservedCargo = cargo;
			return true;
		}

		public void RemovedFromWorld(Actor self) { Unreserve(self); }
		public void Unreserve(Actor self)
		{
			if (ReservedCargo == null)
				return;
			ReservedCargo.UnreserveSpace(self);
			ReservedCargo = null;
		}
	}
}
