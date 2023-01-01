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

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor when it is owned by an AI bot.")]
	public class GrantConditionOnBotOwnerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Bot types that trigger the condition.")]
		public readonly string[] Bots = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new GrantConditionOnBotOwner(this); }
	}

	public class GrantConditionOnBotOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnBotOwnerInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnBotOwner(GrantConditionOnBotOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (self.Owner.IsBot && info.Bots.Contains(self.Owner.BotType))
				conditionToken = self.GrantCondition(info.Condition);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (info.Bots.Contains(newOwner.BotType))
				conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
