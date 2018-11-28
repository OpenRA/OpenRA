#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives a condition to the actor for a limited time.")]
	public class GrantTimedConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		public override object Create(ActorInitializer init) { return new GrantTimedCondition(this); }
	}

	public class GrantTimedCondition : PausableConditionalTrait<GrantTimedConditionInfo>, ITick, ISync, INotifyCreated
	{
		readonly GrantTimedConditionInfo info;
		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;
		[Sync] public int Ticks { get; private set; }

		public GrantTimedCondition(GrantTimedConditionInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.Duration;
		}

		protected override void Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			if (token == ConditionManager.InvalidConditionToken)
			{
				Ticks = info.Duration;
				token = manager.GrantCondition(self, cond);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (manager == null)
				return;

			if (token != ConditionManager.InvalidConditionToken)
				token = manager.RevokeCondition(self, token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && token != ConditionManager.InvalidConditionToken)
				RevokeCondition(self);

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (token == ConditionManager.InvalidConditionToken)
				return;

			if (--Ticks < 0)
				RevokeCondition(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			GrantCondition(self, info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
