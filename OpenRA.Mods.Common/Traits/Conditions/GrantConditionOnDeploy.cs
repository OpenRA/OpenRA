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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnDeployInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant while the actor is undeployed.")]
		public readonly string UndeployedCondition = null;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant after deploying and revoke before undeploying.")]
		public readonly string DeployedCondition = null;

		[Desc("The terrain types that this actor can deploy on. Leave empty to allow any.")]
		public readonly HashSet<string> AllowedTerrainTypes = new HashSet<string>();

		[Desc("Can this actor deploy on slopes?")]
		public readonly bool CanDeployOnRamps = false;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[Desc("Facing that the actor must face before deploying. Set to -1 to deploy regardless of facing.")]
		public readonly int Facing = -1;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		[Desc("Sound to play when undeploying.")]
		public readonly string UndeploySound = null;

		[Desc("Can this actor undeploy?")]
		public readonly bool CanUndeploy = true;

		public object Create(ActorInitializer init) { return new GrantConditionOnDeploy(init, this); }
	}

	public enum DeployState { Undeployed, Deploying, Deployed, Undeploying }

	public class GrantConditionOnDeploy : IResolveOrder, IIssueOrder, INotifyCreated, INotifyDeployComplete, IIssueDeployOrder
	{
		readonly Actor self;
		readonly GrantConditionOnDeployInfo info;
		readonly bool checkTerrainType;
		readonly bool canTurn;

		DeployState deployState;
		ConditionManager conditionManager;
		INotifyDeployTriggered[] notify;
		int deployedToken = ConditionManager.InvalidConditionToken;
		int undeployedToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnDeploy(ActorInitializer init, GrantConditionOnDeployInfo info)
		{
			self = init.Self;
			this.info = info;
			checkTerrainType = info.AllowedTerrainTypes.Count > 0;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			if (init.Contains<DeployStateInit>())
				deployState = init.Get<DeployStateInit, DeployState>();
		}

		public void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			notify = self.TraitsImplementing<INotifyDeployTriggered>().ToArray();

			switch (deployState)
			{
				case DeployState.Undeployed:
					OnUndeployCompleted();
					break;
				case DeployState.Deploying:
					if (canTurn)
						self.Trait<IFacing>().Facing = info.Facing;

					Deploy(true);
					break;
				case DeployState.Deployed:
					if (canTurn)
						self.Trait<IFacing>().Facing = info.Facing;

					OnDeployCompleted();
					break;
				case DeployState.Undeploying:
					if (canTurn)
						self.Trait<IFacing>().Facing = info.Facing;

					Undeploy(true);
					break;
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("GrantConditionOnDeploy", 5,
				() => IsCursorBlocked() ? info.DeployBlockedCursor : info.DeployCursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "GrantConditionOnDeploy")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self)
		{
			return new Order("GrantConditionOnDeploy", self, false);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GrantConditionOnDeploy" || deployState == DeployState.Deploying || deployState == DeployState.Undeploying)
				return;

			if (!order.Queued)
				self.CancelActivity();

			if (deployState == DeployState.Deployed && info.CanUndeploy)
			{
				self.QueueActivity(new CallFunc(Undeploy));
			}
			else if (deployState == DeployState.Undeployed)
			{
				// Turn to the required facing.
				if (info.Facing != -1 && canTurn)
					self.QueueActivity(new Turn(self, info.Facing));

				self.QueueActivity(new CallFunc(Deploy));
			}
		}

		bool IsCursorBlocked()
		{
			return ((deployState == DeployState.Deployed) && !info.CanUndeploy) || (!IsOnValidTerrain() && (deployState != DeployState.Deployed));
		}

		bool IsOnValidTerrain()
		{
			return IsOnValidTerrainType() && IsOnValidRampType();
		}

		bool IsOnValidTerrainType()
		{
			if (!self.World.Map.Contains(self.Location))
				return false;

			if (!checkTerrainType)
				return true;

			var terrainType = self.World.Map.GetTerrainInfo(self.Location).Type;

			return info.AllowedTerrainTypes.Contains(terrainType);
		}

		bool IsOnValidRampType()
		{
			if (info.CanDeployOnRamps)
				return true;

			var ramp = 0;
			if (self.World.Map.Contains(self.Location))
			{
				var tile = self.World.Map.Tiles[self.Location];
				var ti = self.World.Map.Rules.TileSet.GetTileInfo(tile);
				if (ti != null)
					ramp = ti.RampType;
			}

			return ramp == 0;
		}

		void INotifyDeployComplete.FinishedDeploy(Actor self)
		{
			OnDeployCompleted();
		}

		void INotifyDeployComplete.FinishedUndeploy(Actor self)
		{
			OnUndeployCompleted();
		}

		/// <summary>Play deploy sound and animation.</summary>
		void Deploy() { Deploy(false); }
		void Deploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Undeployed)
				return;

			if (!IsOnValidTerrain())
				return;

			if (!string.IsNullOrEmpty(info.DeploySound))
				Game.Sound.Play(SoundType.World, info.DeploySound, self.CenterPosition);

			// Revoke condition that is applied while undeployed.
			if (!init)
				OnDeployStarted();

			// If there is no animation to play just grant the condition that is used while deployed.
			// Alternatively, play the deploy animation and then grant the condition.
			if (!notify.Any())
				OnDeployCompleted();
			else
				foreach (var n in notify)
					n.Deploy(self);
		}

		/// <summary>Play undeploy sound and animation and after that revoke the condition.</summary>
		void Undeploy() { Undeploy(false); }
		void Undeploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Deployed)
				return;

			if (!string.IsNullOrEmpty(info.UndeploySound))
				Game.Sound.Play(SoundType.World, info.UndeploySound, self.CenterPosition);

			if (!init)
				OnUndeployStarted();

			// If there is no animation to play just grant the condition that is used while undeployed.
			// Alternatively, play the undeploy animation and then grant the condition.
			if (!notify.Any())
				OnUndeployCompleted();
			else
				foreach (var n in notify)
					n.Undeploy(self);
		}

		void OnDeployStarted()
		{
			if (undeployedToken != ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.RevokeCondition(self, undeployedToken);

			deployState = DeployState.Deploying;
		}

		void OnDeployCompleted()
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.DeployedCondition) && deployedToken == ConditionManager.InvalidConditionToken)
				deployedToken = conditionManager.GrantCondition(self, info.DeployedCondition);

			deployState = DeployState.Deployed;
		}

		void OnUndeployStarted()
		{
			if (deployedToken != ConditionManager.InvalidConditionToken)
				deployedToken = conditionManager.RevokeCondition(self, deployedToken);

			deployState = DeployState.Deploying;
		}

		void OnUndeployCompleted()
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.UndeployedCondition) && undeployedToken == ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.GrantCondition(self, info.UndeployedCondition);

			deployState = DeployState.Undeployed;
		}
	}

	public class DeployStateInit : IActorInit<DeployState>
	{
		[FieldFromYamlKey]
		readonly DeployState value = DeployState.Deployed;
		public DeployStateInit() { }
		public DeployStateInit(DeployState init) { value = init; }
		public DeployState Value(World world) { return value; }
	}
}
