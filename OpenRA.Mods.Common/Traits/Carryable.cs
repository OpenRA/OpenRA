#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be carried by actors with the `Carryall` trait.")]
	public class CarryableInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self while a carryall has been reserved.")]
		public readonly string ReservedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while being carried.")]
		public readonly string CarriedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while being locked for carry.")]
		public readonly string LockedCondition = null;

		[Desc("Carryall attachment point relative to body.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new Carryable(init.Self, this); }
	}

	public enum LockResponse { Success, Pending, Failed }

	public interface IDelayCarryallPickup
	{
		bool TryLockForPickup(Actor self, Actor carrier);
	}

	public class Carryable : ConditionalTrait<CarryableInfo>
	{
		ConditionManager conditionManager;
		int reservedToken = ConditionManager.InvalidConditionToken;
		int carriedToken = ConditionManager.InvalidConditionToken;
		int lockedToken = ConditionManager.InvalidConditionToken;

		Mobile mobile;
		IDelayCarryallPickup[] delayPickups;

		public Actor Carrier { get; private set; }
		public bool Reserved { get { return state != State.Free; } }
		public CPos? Destination { get; protected set; }
		public bool WantsTransport { get { return Destination != null && !IsTraitDisabled; } }

		protected enum State { Free, Reserved, Locked }
		protected State state = State.Free;
		protected bool attached;

		public Carryable(Actor self, CarryableInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
			mobile = self.TraitOrDefault<Mobile>();
			delayPickups = self.TraitsImplementing<IDelayCarryallPickup>().ToArray();

			base.Created(self);
		}

		public virtual void Attached(Actor self)
		{
			if (attached)
				return;

			attached = true;

			if (carriedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.CarriedCondition))
				carriedToken = conditionManager.GrantCondition(self, Info.CarriedCondition);
		}

		// This gets called by carrier after we touched down
		public virtual void Detached(Actor self)
		{
			if (!attached)
				return;

			attached = false;

			if (carriedToken != ConditionManager.InvalidConditionToken)
				carriedToken = conditionManager.RevokeCondition(self, carriedToken);
		}

		public virtual bool Reserve(Actor self, Actor carrier)
		{
			if (Reserved || IsTraitDisabled)
				return false;

			state = State.Reserved;
			Carrier = carrier;

			if (reservedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.ReservedCondition))
				reservedToken = conditionManager.GrantCondition(self, Info.ReservedCondition);

			return true;
		}

		public virtual void UnReserve(Actor self)
		{
			state = State.Free;
			Carrier = null;

			if (reservedToken != ConditionManager.InvalidConditionToken)
				reservedToken = conditionManager.RevokeCondition(self, reservedToken);

			if (lockedToken != ConditionManager.InvalidConditionToken)
				lockedToken = conditionManager.RevokeCondition(self, lockedToken);
		}

		// Prepare for transport pickup
		public virtual LockResponse LockForPickup(Actor self, Actor carrier)
		{
			if (state == State.Locked && Carrier != carrier)
				return LockResponse.Failed;

			if (delayPickups.Any(d => d.IsTraitEnabled() && !d.TryLockForPickup(self, carrier)))
				return LockResponse.Pending;

			if (state != State.Locked)
			{
				state = State.Locked;
				Carrier = carrier;

				if (lockedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.LockedCondition))
					lockedToken = conditionManager.GrantCondition(self, Info.LockedCondition);
			}

			// Make sure we are not moving and at our normal position with respect to the cell grid
			if (mobile != null && mobile.IsMovingBetweenCells)
				return LockResponse.Pending;

			return LockResponse.Success;
		}
	}
}
