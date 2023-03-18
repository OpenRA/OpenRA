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
	public sealed class GrantConditionOnHostDockInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant to self")]
		public readonly string Condition = null;

		[Desc("How long condition is applied even after undock. Use -1 for infinite.")]
		public readonly int AfterDockDuration = 0;

		[Desc("Client actor type(s) leading to the condition being granted. Leave empty for allowing all clients by default.")]
		public readonly HashSet<string> DockClientNames = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnHostDock(this); }
	}

	public sealed class GrantConditionOnHostDock : INotifyDockHost, ITick, ISync
	{
		readonly GrantConditionOnHostDockInfo info;
		int token;
		int delayedtoken;

		[Sync]
		public int Duration { get; private set; }

		public GrantConditionOnHostDock(GrantConditionOnHostDockInfo info)
		{
			this.info = info;
			token = Actor.InvalidConditionToken;
			delayedtoken = Actor.InvalidConditionToken;
		}

		void INotifyDockHost.Docked(Actor self, Actor client)
		{
			if (info.Condition != null &&
				(info.DockClientNames == null || info.DockClientNames.Contains(client.Info.Name)) &&
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

		void INotifyDockHost.Undocked(Actor self, Actor client)
		{
			if (token == Actor.InvalidConditionToken || info.AfterDockDuration < 0)
				return;
			if (info.AfterDockDuration == 0)
				token = self.RevokeCondition(token);
			else
			{
				delayedtoken = token;
				token = Actor.InvalidConditionToken;
				Duration = info.AfterDockDuration;
			}
		}

		void ITick.Tick(Actor self)
		{
			if (delayedtoken != Actor.InvalidConditionToken && --Duration <= 0)
				delayedtoken = self.RevokeCondition(delayedtoken);
		}
	}
}
