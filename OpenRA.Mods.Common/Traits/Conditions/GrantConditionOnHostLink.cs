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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class GrantConditionOnHostLinkInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant to self")]
		public readonly string Condition = null;

		[Desc("How long condition is applied after unlinking. Use -1 for infinite.")]
		public readonly int AfterLinkDuration = 0;

		[Desc("Client actor type(s) leading to the condition being granted. Leave empty for allowing all clients by default.")]
		public readonly HashSet<string> LinkClientNames = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnHostLink(this); }
	}

	public sealed class GrantConditionOnHostLink : INotifyLinkHost, ITick, ISync
	{
		readonly GrantConditionOnHostLinkInfo info;
		int token;
		int delayedtoken;

		[Sync]
		public int Duration { get; private set; }

		public GrantConditionOnHostLink(GrantConditionOnHostLinkInfo info)
		{
			this.info = info;
			token = Actor.InvalidConditionToken;
			delayedtoken = Actor.InvalidConditionToken;
		}

		void INotifyLinkHost.Linked(Actor self, Actor client)
		{
			if (info.Condition != null &&
				(info.LinkClientNames == null || info.LinkClientNames.Contains(client.Info.Name)) &&
				token == Actor.InvalidConditionToken)
			{
				if (delayedtoken == Actor.InvalidConditionToken)
					token = self.GrantCondition(info.Condition);
				else
				{
					token = delayedtoken;
					delayedtoken = Actor.InvalidConditionToken;
				}
			}
		}

		void INotifyLinkHost.Unlinked(Actor self, Actor client)
		{
			if (token == Actor.InvalidConditionToken || info.AfterLinkDuration < 0)
				return;
			if (info.AfterLinkDuration == 0)
				token = self.RevokeCondition(token);
			else
			{
				delayedtoken = token;
				token = Actor.InvalidConditionToken;
				Duration = info.AfterLinkDuration;
			}
		}

		void ITick.Tick(Actor self)
		{
			if (delayedtoken != Actor.InvalidConditionToken && --Duration <= 0)
				delayedtoken = self.RevokeCondition(delayedtoken);
		}
	}
}
