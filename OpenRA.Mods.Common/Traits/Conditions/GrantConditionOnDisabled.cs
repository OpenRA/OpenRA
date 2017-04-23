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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition to the actor when it is disabled.",
		"This is a temporary shim to help migration away from the legacy IDisable code")]
	public class GrantConditionOnDisabledInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionOnDisabled(this); }
	}

	public class GrantConditionOnDisabled : INotifyCreated, ITick
	{
		readonly GrantConditionOnDisabledInfo info;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnDisabled(GrantConditionOnDisabledInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			// Set initial disabled state
			Tick(self);
		}

		public void Tick(Actor self)
		{
			if (conditionManager == null)
				return;

			var disabled = self.IsDisabled();
			if (disabled && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
			else if (!disabled && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
