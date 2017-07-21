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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor when it is owned by an AI bot.")]
	public class GrantConditionOnBotOwnerInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Bot types that trigger the condition.")]
		public readonly string[] Bots = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnBotOwner(init, this); }
	}

	public class GrantConditionOnBotOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnBotOwnerInfo info;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnBotOwner(ActorInitializer init, GrantConditionOnBotOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			if (conditionManager != null && self.Owner.IsBot && info.Bots.Contains(self.Owner.BotType))
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionManager == null)
				return;

			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			if (info.Bots.Contains(newOwner.BotType))
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
		}
	}
}
