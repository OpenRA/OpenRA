#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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
		bool initiated;

		public DeployForGrantedCondition(Actor self, GrantConditionOnDeploy deploy, bool moving = false)
		{
			this.deploy = deploy;
			this.moving = moving;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			// Turn to the required facing.
			if (deploy.DeployState == DeployState.Undeployed && deploy.Info.Facing != -1 && canTurn && !moving)
				QueueChild(self, new Turn(self, deploy.Info.Facing));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || initiated || (deploy.DeployState != DeployState.Deployed && moving))
				return NextActivity;

			QueueChild(self, new DeployInner(self, deploy), true);

			initiated = true;
			return this;
		}
	}

	public class DeployInner : Activity
	{
		readonly GrantConditionOnDeploy deployment;
		bool initiated;

		public DeployInner(Actor self, GrantConditionOnDeploy deployment)
		{
			this.deployment = deployment;

			// Once deployment animation starts, the animation must finish.
			IsInterruptible = false;
		}

		public override Activity Tick(Actor self)
		{
			// Wait for deployment
			if (deployment.DeployState == DeployState.Deploying || deployment.DeployState == DeployState.Undeploying)
				return this;

			if (initiated)
				return NextActivity;

			if (deployment.DeployState == DeployState.Undeployed)
				deployment.Deploy();
			else
				deployment.Undeploy();

			initiated = true;
			return this;
		}
	}
}
