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
	public sealed class GrantConditionOnClientDockInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant to self")]
		public readonly string Condition = null;

		[Desc("How long condition is applied even after undock. Use -1 for infinite.")]
		public readonly int AfterDockDuration = 0;

		[Desc("Host actor type(s) leading to the condition being granted. Leave empty for allowing all hosts by default.")]
		public readonly HashSet<string> DockHostNames = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnClientDock(this); }
	}

	public sealed class GrantConditionOnClientDock : INotifyDockClient, ITick, ISync
	{
		readonly GrantConditionOnClientDockInfo info;
		int token;
		int delayedtoken;

		[Sync]
		public int Duration { get; private set; }

		public GrantConditionOnClientDock(GrantConditionOnClientDockInfo info)
		{
			this.info = info;
			token = Actor.InvalidConditionToken;
			delayedtoken = Actor.InvalidConditionToken;
		}

		void INotifyDockClient.Docked(Actor self, Actor host)
		{
			if (info.Condition != null &&
				(info.DockHostNames == null || info.DockHostNames.Contains(host.Info.Name)) &&
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

		void INotifyDockClient.Undocked(Actor self, Actor host)
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
