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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class UndeployForGrantedCondition : Activity
	{
		readonly GrantConditionOnDeploy deploy;

		public UndeployForGrantedCondition(Actor self, GrantConditionOnDeploy deploy)
		{
			this.deploy = deploy;
		}

		protected override void OnFirstRun(Actor self)
		{
			var tInfo = self.Info.TraitInfoOrDefault<TurretedInfo>();
			if (tInfo != null)
				QueueChild(new WaitForTurretAlignment(self, tInfo.InitialFacing));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceled)
				return NextActivity;

			IsInterruptible = false; // must DEPLOY from now.
			deploy.Undeploy();

			// Wait for deployment
			if (deploy.DeployState == DeployState.Undeploying)
				return this;

			return NextActivity;
		}
	}
}
