#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Toggles a condition on and off when a specified order type is received.")]
	public class ToggleConditionOnOrderInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Order name that toggles the condition.")]
		public readonly string OrderName = null;

		public readonly string EnabledSound = null;
		public readonly string EnabledSpeech = null;

		public readonly string DisabledSound = null;
		public readonly string DisabledSpeech = null;

		public override object Create(ActorInitializer init) { return new ToggleConditionOnOrder(init.Self, this); }
	}

	public class ToggleConditionOnOrder : PausableConditionalTrait<ToggleConditionOnOrderInfo>, IResolveOrder
	{
		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		// If the trait is paused this may be true with no condition granted
		[Sync] bool enabled = false;

		public ToggleConditionOnOrder(Actor self, ToggleConditionOnOrderInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);

			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void SetCondition(Actor self, bool granted)
		{
			if (conditionManager == null)
				return;

			if (granted && conditionToken == ConditionManager.InvalidConditionToken)
			{
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);

				if (Info.EnabledSound != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.EnabledSound, self.Owner.Faction.InternalName);

				if (Info.EnabledSpeech != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.EnabledSpeech, self.Owner.Faction.InternalName);
			}
			else if (!granted && conditionToken != ConditionManager.InvalidConditionToken)
			{
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

				if (Info.DisabledSound != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.DisabledSound, self.Owner.Faction.InternalName);

				if (Info.DisabledSpeech != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.DisabledSpeech, self.Owner.Faction.InternalName);
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (!IsTraitDisabled && !IsTraitPaused && order.OrderString == Info.OrderName)
			{
				enabled = !enabled;
				SetCondition(self, enabled);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			// Disabling the trait resets the condition
			enabled = false;
			SetCondition(self, false);
		}

		protected override void TraitPaused(Actor self)
		{
			// Pausing the trait removes the condition
			// but does not reset the enabled value
			SetCondition(self, false);
		}

		protected override void TraitResumed(Actor self)
		{
			// Unpausing the trait restores the previous state
			SetCondition(self, enabled);
		}
	}
}
