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
		protected ConditionManager conditionManager;
		protected int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnLayer(Actor self, InfoType info, byte validLayer)
			: base(info)
		{
			ValidLayerType = validLayer;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		void INotifyCustomLayerChanged.CustomLayerChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (conditionManager == null)
				return;

			UpdateConditions(self, oldLayer, newLayer);
		}

		protected virtual void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			if (newLayer == ValidLayerType && oldLayer != ValidLayerType && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
			else if (newLayer != ValidLayerType && oldLayer == ValidLayerType && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (self.Location.Layer == ValidLayerType && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken)
				return;

			conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
