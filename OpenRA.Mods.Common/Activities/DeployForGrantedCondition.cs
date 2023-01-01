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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeployForGrantedCondition : Activity
	{
		readonly GrantConditionOnDeploy deploy;
		readonly bool canTurn;
		readonly bool moving;

		public DeployForGrantedCondition(Actor self, GrantConditionOnDeploy deploy, bool moving = false)
		{
			this.deploy = deploy;
			this.moving = moving;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			// Turn to the required facing.
			if (deploy.DeployState == DeployState.Undeployed && deploy.Info.Facing.HasValue && canTurn && !moving)
				QueueChild(new Turn(self, deploy.Info.Facing.Value));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || (deploy.DeployState != DeployState.Deployed && moving))
				return true;

			QueueChild(new DeployInner(deploy));
			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (NextActivity != null)
				foreach (var n in NextActivity.TargetLineNodes(self))
					yield return n;

			yield break;
		}
	}

	public class DeployInner : Activity
	{
		readonly GrantConditionOnDeploy deployment;
		bool initiated;

		public DeployInner(GrantConditionOnDeploy deployment)
		{
			this.deployment = deployment;

			// Once deployment animation starts, the animation must finish.
			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			// Wait for deployment
			if (deployment.DeployState == DeployState.Deploying || deployment.DeployState == DeployState.Undeploying)
				return false;

			if (initiated)
				return true;

			if (deployment.DeployState == DeployState.Undeployed)
				deployment.Deploy();
			else
				deployment.Undeploy();

			initiated = true;
			return false;
		}
	}
}
