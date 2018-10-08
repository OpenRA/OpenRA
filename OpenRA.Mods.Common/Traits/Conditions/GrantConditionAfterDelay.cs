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

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives a condition to the actor after a delay.")]
	public class GrantConditionAfterDelayInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before applying the condition.")]
		public readonly int Delay = 50;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.Magenta;

		public override object Create(ActorInitializer init) { return new GrantConditionAfterDelay(this); }
	}

	public class GrantConditionAfterDelay : PausableConditionalTrait<GrantConditionAfterDelayInfo>, ITick, ISync, INotifyCreated, ISelectionBar
	{
		readonly GrantConditionAfterDelayInfo info;
		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;
		[Sync] public int Ticks { get; private set; }

		public GrantConditionAfterDelay(GrantConditionAfterDelayInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.Delay;
		}

		protected override void Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();

			base.Created(self);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			if (token == ConditionManager.InvalidConditionToken)
				token = manager.GrantCondition(self, cond);
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
			if (IsTraitDisabled)
				Ticks = info.Delay;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (token != ConditionManager.InvalidConditionToken)
				return;

			if (--Ticks < 0)
				GrantCondition(self, info.Condition);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar)
				return 0f;

			return 1f - (float)Ticks / Info.Delay;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return !IsTraitDisabled && Info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition(self);
		}
	}
}
