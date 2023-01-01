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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class GrantConditionOnLayerInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant to self when changing to specific custom layer.")]
		public readonly string Condition = null;
	}

	public abstract class GrantConditionOnLayer<InfoType> : ConditionalTrait<InfoType>, INotifyCustomLayerChanged where InfoType : GrantConditionOnLayerInfo
	{
		protected readonly byte ValidLayerType;
		protected int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnLayer(InfoType info, byte validLayer)
			: base(info)
		{
			ValidLayerType = validLayer;
		}

		void INotifyCustomLayerChanged.CustomLayerChanged(Actor self, byte oldLayer, byte newLayer)
		{
			UpdateConditions(self, oldLayer, newLayer);
		}

		protected virtual void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			if (newLayer == ValidLayerType && oldLayer != ValidLayerType && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
			else if (newLayer != ValidLayerType && oldLayer == ValidLayerType && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (self.Location.Layer == ValidLayerType && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
