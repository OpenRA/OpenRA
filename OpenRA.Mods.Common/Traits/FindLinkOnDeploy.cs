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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Finds the closest link host on deploy.")]
	public class FindLinkOnDeployInfo : ConditionalTraitInfo, Requires<LinkClientManagerInfo>
	{
		[FieldLoader.Require]
		[Desc("Linking type.")]
		public readonly BitSet<LinkType> LinkType;

		[CursorReference]
		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		public override object Create(ActorInitializer init) { return new FindLinkOnDeploy(init.Self, this); }
	}

	public class FindLinkOnDeploy : ConditionalTrait<FindLinkOnDeployInfo>, IIssueDeployOrder, IResolveOrder, IOrderVoice, IIssueOrder
	{
		readonly LinkClientManager manager;
		public FindLinkOnDeploy(Actor self, FindLinkOnDeployInfo info)
			: base(info)
		{
			manager = self.Trait<LinkClientManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("FindLink", 10,
					() => !IsTraitDisabled && manager.LinkingPossible(Info.LinkType)
						? Info.DeployCursor
						: Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "FindLink")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
			=> new("FindLink", self, queued);

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued)
			=> !IsTraitDisabled && manager.LinkingPossible(Info.LinkType, queued);

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "FindLink")
			{
				self.QueueActivity(order.Queued, new FindLink(this, manager));
				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "FindLink")
				return manager.Info.Voice;

			return null;
		}
	}

	public class FindLink : Activity
	{
		readonly FindLinkOnDeploy linkOnDeploy;
		readonly LinkClientManager manager;
		public FindLink(FindLinkOnDeploy linkOnDeploy, LinkClientManager manager)
		{
			this.linkOnDeploy = linkOnDeploy;
			this.manager = manager;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (linkOnDeploy.IsTraitDisabled || !manager.LinkingPossible(linkOnDeploy.Info.LinkType))
				return;

			var linkHost = manager.ClosestLinkHost(null, linkOnDeploy.Info.LinkType)
				?? manager.ClosestLinkHost(null, linkOnDeploy.Info.LinkType, ignoreOccupancy: true);

			if (!linkHost.HasValue)
				return;

			QueueChild(new MoveToDock(self, linkHost.Value.Actor, linkHost.Value.Trait));
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (ChildActivity != null)
				foreach (var target in ChildActivity.GetTargets(self))
					yield return target;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var node in ChildActivity.TargetLineNodes(self))
					yield return node;
		}
	}
}
