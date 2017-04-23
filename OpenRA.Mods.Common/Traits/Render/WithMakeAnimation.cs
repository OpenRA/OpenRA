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

using System;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the sprite during construction.")]
	public class WithMakeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "make";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation : INotifyCreated
	{
		readonly WithMakeAnimationInfo info;
		readonly WithSpriteBody wsb;

		ConditionManager conditionManager;
		int token = ConditionManager.InvalidConditionToken;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsb = self.Trait<WithSpriteBody>();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			var building = self.TraitOrDefault<Building>();
			if (building != null && !building.SkipMakeAnimation)
				Forward(self, () => building.NotifyBuildingComplete(self));
		}

		void Forward(Actor self, Action onComplete)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			wsb.PlayCustomAnimation(self, info.Sequence, () =>
			{
				if (token != ConditionManager.InvalidConditionToken)
					token = conditionManager.RevokeCondition(self, token);

				// TODO: Rewrite this to use a trait notification for save game support
				onComplete();
			});
		}

		void Reverse(Actor self, Action onComplete)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			wsb.PlayCustomAnimationBackwards(self, info.Sequence, () =>
			{
				if (token != ConditionManager.InvalidConditionToken)
					token = conditionManager.RevokeCondition(self, token);

				// TODO: Rewrite this to use a trait notification for save game support
				onComplete();
			});
		}

		public void Reverse(Actor self, Activity activity, bool queued = true)
		{
			Reverse(self, () =>
			{
				// HACK: The actor remains alive and active for one tick before the followup activity
				// (sell/transform/etc) runs. This causes visual glitches that we attempt to minimize
				// by forcing the animation to frame 0 and regranting the make condition.
				// These workarounds will break the actor if the followup activity doesn't dispose it!
				wsb.DefaultAnimation.PlayFetchIndex(info.Sequence, () => 0);

				if (conditionManager != null && !string.IsNullOrEmpty(info.Condition))
					token = conditionManager.GrantCondition(self, info.Condition);

				self.QueueActivity(queued, activity);
			});
		}
	}
}
