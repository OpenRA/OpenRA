#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Disables the actor when a power outage is triggered (see `InfiltrateForPowerOutage` for more information).")]
	public class AffectedByPowerOutageInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant while there is a power outage.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new AffectedByPowerOutage(init.Self, this); }
	}

	public class AffectedByPowerOutage : ConditionalTrait<AffectedByPowerOutageInfo>, INotifyOwnerChanged, ISelectionBar, INotifyCreated, INotifyAddedToWorld
	{
		PowerManager playerPower;
		int token = Actor.InvalidConditionToken;

		public AffectedByPowerOutage(Actor self, AffectedByPowerOutageInfo info)
			: base(info)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self) { UpdateStatus(self); }
		protected override void TraitEnabled(Actor self) { UpdateStatus(self); }
		protected override void TraitDisabled(Actor self) { Revoke(self); }

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || playerPower.PowerOutageRemainingTicks <= 0)
				return 0;

			return (float)playerPower.PowerOutageRemainingTicks / playerPower.PowerOutageTotalTicks;
		}

		Color ISelectionBar.GetColor()
		{
			return Color.Yellow;
		}

		bool ISelectionBar.DisplayWhenEmpty => false;

		public void UpdateStatus(Actor self)
		{
			if (!IsTraitDisabled && playerPower.PowerOutageRemainingTicks > 0)
				Grant(self);
			else
				Revoke(self);
		}

		void Grant(Actor self)
		{
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void Revoke(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
			UpdateStatus(self);
		}
	}
}
