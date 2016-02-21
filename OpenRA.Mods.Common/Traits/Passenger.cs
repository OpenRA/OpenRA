#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum AlternateTransportsMode { None, Force, Default, Always }

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
		public readonly WDist AlternateTransportScanRange = WDist.FromCells(11) / 2;

		[Desc("Upgrade types to grant to transport.")]
		[UpgradeGrantedReference] public readonly string[] GrantUpgrades = { };

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Passenger(this); }
	}

	public class Passenger : IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld
	{
		public readonly PassengerInfo Info;
		public Passenger(PassengerInfo info)
		{
			Info = info;
			Func<Actor, bool> canTarget = IsCorrectCargoType;
			Func<Actor, bool> useEnterCursor = CanEnter;
			Orders = new EnterAlliedActorTargeter<CargoInfo>[]
			{
				new EnterTransportTargeter("EnterTransport", 5, canTarget, useEnterCursor, Info.AlternateTransportsMode),
				new EnterTransportsTargeter("EnterTransports", 5, canTarget, useEnterCursor, Info.AlternateTransportsMode)
			};
		}

		public Actor Transport;
		public Cargo ReservedCargo { get; private set; }

		public IEnumerable<IOrderTargeter> Orders { get; private set; }

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterTransport" || order.OrderID == "EnterTransports")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool IsCorrectCargoType(Actor target)
		{
			var ci = target.Info.TraitInfo<CargoInfo>();
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
			return Info.Voice;
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
