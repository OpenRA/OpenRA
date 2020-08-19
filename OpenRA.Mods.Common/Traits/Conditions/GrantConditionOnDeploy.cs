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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition when a deploy order is issued." +
		"Can be paused with the granted condition to disable undeploying.")]
	public class GrantConditionOnDeployInfo : PausableConditionalTraitInfo, IEditorActorOptions
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

		[Desc("Facing that the actor must face before deploying. Leave undefined to deploy regardless of facing.")]
		public readonly WAngle? Facing = null;

		[Desc("Play a randomly selected sound from this list when deploying.")]
		public readonly string[] DeploySounds = null;

		[Desc("Play a randomly selected sound from this list when undeploying.")]
		public readonly string[] UndeploySounds = null;

		[Desc("Skip make/deploy animation?")]
		public readonly bool SkipMakeAnimation = false;

		[Desc("Undeploy before the actor tries to move?")]
		public readonly bool UndeployOnMove = false;

		[Desc("Undeploy before the actor is picked up by a Carryall?")]
		public readonly bool UndeployOnPickup = false;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Display order for the deployed checkbox in the map editor")]
		public readonly int EditorDeployedDisplayOrder = 4;

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorCheckbox("Deployed", EditorDeployedDisplayOrder,
				actor =>
				{
					var init = actor.GetInitOrDefault<DeployStateInit>();
					if (init != null)
						return init.Value == DeployState.Deployed;

					return false;
				},
				(actor, value) =>
				{
					actor.ReplaceInit(new DeployStateInit(value ? DeployState.Deployed : DeployState.Undeployed));
				});
		}

		public override object Create(ActorInitializer init) { return new GrantConditionOnDeploy(init, this); }
	}

	public enum DeployState { Undeployed, Deploying, Deployed, Undeploying }

	public class GrantConditionOnDeploy : PausableConditionalTrait<GrantConditionOnDeployInfo>, IResolveOrder, IIssueOrder,
		INotifyDeployComplete, IIssueDeployOrder, IOrderVoice, IWrapMove, IDelayCarryallPickup
	{
		readonly Actor self;
		readonly bool checkTerrainType;

		DeployState deployState;
		INotifyDeployTriggered[] notify;
		int deployedToken = Actor.InvalidConditionToken;
		int undeployedToken = Actor.InvalidConditionToken;

		public DeployState DeployState { get { return deployState; } }

		public GrantConditionOnDeploy(ActorInitializer init, GrantConditionOnDeployInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.AllowedTerrainTypes.Count > 0;
			deployState = init.GetValue<DeployStateInit, DeployState>(DeployState.Undeployed);
		}

		protected override void Created(Actor self)
		{
			notify = self.TraitsImplementing<INotifyDeployTriggered>().ToArray();
			base.Created(self);

			if (Info.Facing.HasValue && deployState != DeployState.Undeployed)
			{
				var facing = self.TraitOrDefault<IFacing>();
				if (facing != null)
					facing.Facing = Info.Facing.Value;
			}

			switch (deployState)
			{
				case DeployState.Undeployed:
					OnUndeployCompleted();
					break;
				case DeployState.Deploying:
					Deploy(true);
					break;
				case DeployState.Deployed:
					OnDeployCompleted();
					break;
				case DeployState.Undeploying:
					Undeploy(true);
					break;
			}
		}

		Activity IWrapMove.WrapMove(Activity moveInner)
		{
			// Note: We can't assume anything about the current deploy state
			// because WrapMove may be called for a queued order
			if (!Info.UndeployOnMove)
				return moveInner;

			var activity = new DeployForGrantedCondition(self, this, true);
			activity.Queue(moveInner);
			return activity;
		}

		bool IDelayCarryallPickup.TryLockForPickup(Actor self, Actor carrier)
		{
			if (!Info.UndeployOnPickup || deployState == DeployState.Undeployed || IsTraitDisabled)
				return true;

			if (deployState == DeployState.Deployed && !IsTraitPaused)
				Undeploy();

			return false;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("GrantConditionOnDeploy", 5,
						() => CanDeploy() ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "GrantConditionOnDeploy")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("GrantConditionOnDeploy", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (order.OrderString != "GrantConditionOnDeploy")
				return;

			self.QueueActivity(order.Queued, new DeployForGrantedCondition(self, this));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "GrantConditionOnDeploy" ? Info.Voice : null;
		}

		bool CanDeploy()
		{
			if (IsTraitPaused || IsTraitDisabled)
				return false;

			return IsValidTerrain(self.Location) || (deployState == DeployState.Deployed);
		}

		public bool IsValidTerrain(CPos location)
		{
			return IsValidTerrainType(location) && IsValidRampType(location);
		}

		bool IsValidTerrainType(CPos location)
		{
			if (!self.World.Map.Contains(location))
				return false;

			if (!checkTerrainType)
				return true;

			var terrainType = self.World.Map.GetTerrainInfo(location).Type;

			return Info.AllowedTerrainTypes.Contains(terrainType);
		}

		bool IsValidRampType(CPos location)
		{
			if (Info.CanDeployOnRamps)
				return true;

			var map = self.World.Map;
			return !map.Ramp.Contains(location) || map.Ramp[location] == 0;
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
		public void Deploy() { Deploy(false); }
		void Deploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Undeployed)
				return;

			if (!IsValidTerrain(self.Location))
				return;

			if (Info.DeploySounds != null && Info.DeploySounds.Any())
				Game.Sound.Play(SoundType.World, Info.DeploySounds, self.World, self.CenterPosition);

			// Revoke condition that is applied while undeployed.
			if (!init)
				OnDeployStarted();

			// If there is no animation to play just grant the condition that is used while deployed.
			// Alternatively, play the deploy animation and then grant the condition.
			if (!notify.Any())
				OnDeployCompleted();
			else
				foreach (var n in notify)
					n.Deploy(self, Info.SkipMakeAnimation);
		}

		/// <summary>Play undeploy sound and animation and after that revoke the condition.</summary>
		public void Undeploy() { Undeploy(false); }
		void Undeploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Deployed)
				return;

			if (Info.UndeploySounds != null && Info.UndeploySounds.Any())
				Game.Sound.Play(SoundType.World, Info.UndeploySounds, self.World, self.CenterPosition);

			if (!init)
				OnUndeployStarted();

			// If there is no animation to play just grant the condition that is used while undeployed.
			// Alternatively, play the undeploy animation and then grant the condition.
			if (!notify.Any())
				OnUndeployCompleted();
			else
				foreach (var n in notify)
					n.Undeploy(self, Info.SkipMakeAnimation);
		}

		void OnDeployStarted()
		{
			if (undeployedToken != Actor.InvalidConditionToken)
				undeployedToken = self.RevokeCondition(undeployedToken);

			deployState = DeployState.Deploying;
		}

		void OnDeployCompleted()
		{
			if (deployedToken == Actor.InvalidConditionToken)
				deployedToken = self.GrantCondition(Info.DeployedCondition);

			deployState = DeployState.Deployed;
		}

		void OnUndeployStarted()
		{
			if (deployedToken != Actor.InvalidConditionToken)
				deployedToken = self.RevokeCondition(deployedToken);

			deployState = DeployState.Deploying;
		}

		void OnUndeployCompleted()
		{
			if (undeployedToken == Actor.InvalidConditionToken)
				undeployedToken = self.GrantCondition(Info.UndeployedCondition);

			deployState = DeployState.Undeployed;
		}
	}

	public class DeployStateInit : ValueActorInit<DeployState>, ISingleInstanceInit
	{
		public DeployStateInit(DeployState value)
			: base(value) { }
	}
}
