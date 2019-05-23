#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
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

		[Desc("Range from self for looking for an alternate transport (default: 5.5 cells).")]
		public readonly WDist AlternateTransportScanRange = WDist.FromCells(11) / 2;

		[GrantedConditionReference]
		[Desc("The condition to grant to when this actor is loaded inside any transport.")]
		public readonly string CargoCondition = null;

		[Desc("Conditions to grant when this actor is loaded inside specified transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> CargoConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterCargoConditions { get { return CargoConditions.Values; } }

		[VoiceReference]
		public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Passenger(this); }
	}

	public class Passenger : INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld, INotifyEnteredCargo, INotifyExitedCargo, INotifyKilled
	{
		public readonly PassengerInfo Info;
		public Actor Transport;

		ConditionManager conditionManager;
		int anyCargoToken = ConditionManager.InvalidConditionToken;
		int specificCargoToken = ConditionManager.InvalidConditionToken;

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

		public Cargo ReservedCargo { get; private set; }

		public IEnumerable<IOrderTargeter> Orders { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterTransport" || order.OrderID == "EnterTransports")
				return new Order(order.OrderID, self, target, queued);

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
			if (order.OrderString != "EnterTransport" && order.OrderString != "EnterTransports")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
				return null;

			return Info.Voice;
		}

		void INotifyEnteredCargo.OnEnteredCargo(Actor self, Actor cargo)
		{
			string specificCargoCondition;
			if (conditionManager != null)
			{
				if (anyCargoToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.CargoCondition))
					anyCargoToken = conditionManager.GrantCondition(self, Info.CargoCondition);

				if (specificCargoToken == ConditionManager.InvalidConditionToken && Info.CargoConditions.TryGetValue(cargo.Info.Name, out specificCargoCondition))
					specificCargoToken = conditionManager.GrantCondition(self, specificCargoCondition);
			}
		}

		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo)
		{
			if (anyCargoToken != ConditionManager.InvalidConditionToken)
				anyCargoToken = conditionManager.RevokeCondition(self, anyCargoToken);

			if (specificCargoToken != ConditionManager.InvalidConditionToken)
				specificCargoToken = conditionManager.RevokeCondition(self, specificCargoToken);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTransport" && order.OrderString != "EnterTransports")
				return;

			// Enter orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (!CanEnter(targetActor))
				return;

			if (!IsCorrectCargoType(targetActor))
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(order.Target, Color.Green);
			if (order.OrderString == "EnterTransports")
				self.QueueActivity(new EnterTransports(self, order.Target));
			else
				self.QueueActivity(new EnterTransport(self, order.Target));
		}

		public bool Reserve(Actor self, Cargo cargo)
		{
			Unreserve(self);
			if (!cargo.ReserveSpace(self))
				return false;
			ReservedCargo = cargo;
			return true;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { Unreserve(self); }

		public void Unreserve(Actor self)
		{
			if (ReservedCargo == null)
				return;
			ReservedCargo.UnreserveSpace(self);
			ReservedCargo = null;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Transport == null)
				return;

			// Something killed us, but it wasn't our transport blowing up. Remove us from the cargo.
			if (!Transport.IsDead)
				Transport.Trait<Cargo>().Unload(Transport, self);
		}
	}
}
