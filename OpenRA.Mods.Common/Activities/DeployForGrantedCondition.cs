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
		readonly IMove move;
		readonly bool canTurn;
		readonly bool orderedMove;
		readonly CPos cell;
		bool initiated;

		public DeployForGrantedCondition(Actor self, GrantConditionOnDeploy deploy, Target target)
		{
			this.deploy = deploy;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			orderedMove = target.Type == TargetType.Terrain || (target.Type == TargetType.Actor && target.Actor != self);
			if (orderedMove)
				cell = self.World.Map.Clamp(self.World.Map.CellContaining(target.CenterPosition));

			move = self.TraitOrDefault<IMove>();
		}

		protected override void OnFirstRun(Actor self)
		{
			// Turn to the required facing.
			if (deploy.DeployState == DeployState.Undeployed && deploy.Info.Facing != -1 && canTurn && !orderedMove)
				QueueChild(self, new Turn(self, deploy.Info.Facing));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceling)
				return NextActivity;

			if (!initiated)
			{
				initiated = true;
				if (deploy.DeployState == DeployState.Undeployed)
				{
					if (orderedMove && move != null)
						QueueChild(self, move.MoveTo(cell, 8), true);
					else
						QueueChild(self, new DeployInner(self, deploy, true));
				}
				else if (deploy.DeployState == DeployState.Deployed)
				{
					QueueChild(self, new DeployInner(self, deploy, false));
					if (orderedMove && move != null)
						QueueChild(self, move.MoveTo(cell, 8), true);
				}

				return this;
			}

			// Failed or success, we are going to NextActivity.
			// Deploy() at the first run would have put DeployState == Deploying so
			// if we are back to DeployState.Undeployed, it means deploy failure.
			// Parent activity will see the status and will take appropriate action.
			return NextActivity;
		}
	}

	public class DeployInner : Activity
	{
		readonly GrantConditionOnDeploy deployment;
		readonly bool towardDeploy;
		bool initiated;

		public DeployInner(Actor self, GrantConditionOnDeploy deployment, bool towardDeploy)
		{
			this.deployment = deployment;
			this.towardDeploy = towardDeploy;

			// Once deployment animation starts, the animation must finish.
			IsInterruptible = false;
		}

		public override Activity Tick(Actor self)
		{
			// Wait for deployment
			if (deployment.DeployState == DeployState.Deploying || deployment.DeployState == DeployState.Undeploying)
				return this;

			if (!initiated)
			{
				if (towardDeploy)
					deployment.Deploy();
				else
					deployment.Undeploy();

				initiated = true;
				return this;
			}

			return NextActivity;
		}
	}
}
