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

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnJumpjetLayerInfo : GrantConditionOnLayerInfo
	{
		public override object Create(ActorInitializer init) { return new GrantConditionOnJumpjetLayer(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var mobileInfo = ai.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo == null || !(mobileInfo.LocomotorInfo is JumpjetLocomotorInfo))
				throw new YamlException("GrantConditionOnJumpjetLayer requires Mobile to be linked to a JumpjetLocomotor!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class GrantConditionOnJumpjetLayer : GrantConditionOnLayer<GrantConditionOnJumpjetLayerInfo>, INotifyFinishedMoving
	{
		bool jumpjetInAir;

		public GrantConditionOnJumpjetLayer(Actor self, GrantConditionOnJumpjetLayerInfo info)
			: base(self, info, CustomMovementLayerType.Jumpjet) { }

		void INotifyFinishedMoving.FinishedMoving(Actor self, byte oldLayer, byte newLayer)
		{
			if (jumpjetInAir && oldLayer != ValidLayerType && newLayer != ValidLayerType)
				UpdateConditions(self, oldLayer, newLayer);
		}

		protected override void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			if (!jumpjetInAir && newLayer == ValidLayerType && oldLayer != ValidLayerType && conditionToken == ConditionManager.InvalidConditionToken)
			{
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
				jumpjetInAir = true;
			}

			// By the time the condition is meant to be revoked, the 'oldLayer' is already no longer the Jumpjet layer, either
			if (jumpjetInAir && newLayer != ValidLayerType && oldLayer != ValidLayerType && conditionToken != ConditionManager.InvalidConditionToken)
			{
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
				jumpjetInAir = false;
			}
		}
	}
}
