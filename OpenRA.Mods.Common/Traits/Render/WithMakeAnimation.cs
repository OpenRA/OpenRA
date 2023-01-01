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
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the sprite during construction/deploy/undeploy.")]
	public class WithMakeAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use.")]
		public readonly string Sequence = "make";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;

		[Desc("Apply to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		public override object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation : INotifyCreated, INotifyDeployTriggered
	{
		readonly WithMakeAnimationInfo info;
		readonly WithSpriteBody[] wsbs;
		readonly bool skipMakeAnimation;
		WithMakeOverlay[] overlays;

		int token = Actor.InvalidConditionToken;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsbs = self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name)).ToArray();
			skipMakeAnimation = init.Contains<SkipMakeAnimsInit>(info);
		}

		void INotifyCreated.Created(Actor self)
		{
			overlays = self.TraitsImplementing<WithMakeOverlay>().ToArray();
			if (!skipMakeAnimation)
				Forward(self, () => { });
		}

		public void Forward(Actor self, Action onComplete)
		{
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

			var wsb = wsbs.FirstEnabledConditionalTraitOrDefault();

			if (wsb == null)
				return;

			wsb.PlayCustomAnimation(self, info.Sequence, () =>
			{
				self.World.AddFrameEndTask(w =>
				{
					if (token != Actor.InvalidConditionToken)
						token = self.RevokeCondition(token);

					// TODO: Rewrite this to use a trait notification for save game support
					onComplete();
				});
			});

			foreach (var overlay in overlays)
				overlay.Forward();
		}

		public void Reverse(Actor self, Action onComplete)
		{
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

			var wsb = wsbs.FirstEnabledConditionalTraitOrDefault();

			if (wsb == null)
				return;

			wsb.PlayCustomAnimationBackwards(self, info.Sequence, () =>
			{
				self.World.AddFrameEndTask(w =>
				{
					if (token != Actor.InvalidConditionToken)
						token = self.RevokeCondition(token);

					// TODO: Rewrite this to use a trait notification for save game support
					onComplete();
				});
			});

			foreach (var overlay in overlays)
				overlay.Reverse();
		}

		public void Reverse(Actor self, Activity activity, bool queued = true)
		{
			Reverse(self, () =>
			{
				// HACK: The actor remains alive and active for one tick before the followup activity
				// (sell/transform/etc) runs. This causes visual glitches that we attempt to minimize
				// by forcing the animation to frame 0 and regranting the make condition.
				// These workarounds will break the actor if the followup activity doesn't dispose it!
				wsbs.FirstEnabledConditionalTraitOrDefault()?.DefaultAnimation.PlayFetchIndex(info.Sequence, () => 0);

				token = self.GrantCondition(info.Condition);

				self.QueueActivity(queued, activity);
			});

			foreach (var overlay in overlays)
				overlay.Reverse();
		}

		// TODO: Make this use Forward instead
		void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		{
			var notified = false;
			var notify = self.TraitsImplementing<INotifyDeployComplete>();

			if (skipMakeAnim)
			{
				foreach (var n in notify)
					n.FinishedDeploy(self);

				return;
			}

			foreach (var wsb in wsbs)
			{
				if (wsb.IsTraitDisabled)
					continue;

				wsb.PlayCustomAnimation(self, info.Sequence, () =>
				{
					if (notified)
						return;

					foreach (var n in notify)
					{
						n.FinishedDeploy(self);
						notified = true;
					}
				});
			}

			foreach (var overlay in overlays)
				overlay.Forward();
		}

		// TODO: Make this use Reverse instead
		void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim)
		{
			var notified = false;
			var notify = self.TraitsImplementing<INotifyDeployComplete>();

			if (skipMakeAnim)
			{
				foreach (var n in notify)
					n.FinishedUndeploy(self);

				return;
			}

			foreach (var wsb in wsbs)
			{
				if (wsb.IsTraitDisabled)
					continue;

				wsb.PlayCustomAnimationBackwards(self, info.Sequence, () =>
				{
					if (notified)
						return;

					foreach (var n in notify)
					{
						n.FinishedUndeploy(self);
						notified = true;
					}
				});
			}

			foreach (var overlay in overlays)
				overlay.Reverse();
		}
	}
}
