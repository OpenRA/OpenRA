#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to a building to allow queued transform orders while undeploying.")]
	public class TransformsIntoTransformsInfo : ConditionalTraitInfo, Requires<TransformsInfo>, Requires<IHealthInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new TransformsIntoTransforms(init.Self, this); }
	}

	public class TransformsIntoTransforms : ConditionalTrait<TransformsIntoTransformsInfo>, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		public TransformsIntoTransforms(Actor self, TransformsIntoTransformsInfo info)
			: base(info) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || order.OrderString != "AfterDeployTransform")
				return;

			// The DeployTransform order does not have a position associated with it,
			// so we can only queue a new transformation if something else has
			// already triggered the undeploy.
			var currentTransform = self.CurrentActivity as Transform;
			if (!order.Queued || currentTransform == null)
				return;

			if (!order.Queued && currentTransform.NextActivity != null)
				currentTransform.NextActivity.Cancel(self);

			currentTransform.Queue(new IssueOrderAfterTransform("DeployTransform", order.Target, Color.Green));

			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "DeployTransform" && !IsTraitDisabled ? Info.Voice : null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("AfterDeployTransform", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued)
		{
			// The DeployTransform order does not have a position associated with it,
			// so we can only queue a new transformation if something else has
			// already triggered the undeploy.
			return queued && self.CurrentActivity is Transform;
		}
	}
}
