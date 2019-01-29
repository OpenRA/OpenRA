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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition when a deploy order is issued." +
		"Can be paused with the granted condition to disable undeploying.")]
	public class GrantConditionOnDeployInfo : PausableConditionalTraitInfo
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

		[Desc("Play a randomly selected sound from this list when deploying.")]
		public readonly string[] DeploySounds = null;

		[Desc("Play a randomly selected sound from this list when undeploying.")]
		public readonly string[] UndeploySounds = null;

		[Desc("Skip make/deploy animation?")]
		public readonly bool SkipMakeAnimation = false;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Can this actor be ordered to move when deployed? [Always, ForceOnly, Never]")]
		public readonly UndeployOnMoveType UndeployOnMove = UndeployOnMoveType.Always;

		public override object Create(ActorInitializer init) { return new GrantConditionOnDeploy(init, this); }
	}

	public enum DeployState { Undeployed, Deploying, Deployed, Undeploying }

	public enum UndeployOnMoveType { Always, ForceOnly, Never }

	public class GrantConditionOnDeploy : PausableConditionalTrait<GrantConditionOnDeployInfo>, IResolveOrder, IIssueOrder, INotifyCreated,
		INotifyDeployComplete, IIssueDeployOrder, IOrderVoice
	{
		readonly Actor self;
		readonly bool checkTerrainType;
		readonly bool canTurn;

		DeployState deployState;
		ConditionManager conditionManager;
		INotifyDeployTriggered[] notify;
		int deployedToken = ConditionManager.InvalidConditionToken;
		int undeployedToken = ConditionManager.InvalidConditionToken;

		public DeployState DeployState { get { return deployState; } }

		public GrantConditionOnDeploy(ActorInitializer init, GrantConditionOnDeployInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.AllowedTerrainTypes.Count > 0;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			if (init.Contains<DeployStateInit>())
				deployState = init.Get<DeployStateInit, DeployState>();
		}

		void INotifyCreated.Created(Actor self)
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
						self.Trait<IFacing>().Facing = Info.Facing;

					Deploy(true);
					break;
				case DeployState.Deployed:
					if (canTurn)
						self.Trait<IFacing>().Facing = Info.Facing;

					OnDeployCompleted();
					break;
				case DeployState.Undeploying:
					if (canTurn)
						self.Trait<IFacing>().Facing = Info.Facing;

					Undeploy(true);
					break;
			}
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new MoveDeployOrderTargeter(self, this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveDeployOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("GrantConditionOnDeploy", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return !IsTraitPaused && !IsTraitDisabled; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (order.OrderString != "GrantConditionOnDeploy")
				return;

			self.SetTargetLine(order.Target, Color.Green);
			self.QueueActivity(order.Queued, new DeployForGrantedCondition(self, this, order.Target));
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

			var ramp = 0;
			if (self.World.Map.Contains(location))
			{
				var tile = self.World.Map.Tiles[location];
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
			if (undeployedToken != ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.RevokeCondition(self, undeployedToken);

			deployState = DeployState.Deploying;
		}

		void OnDeployCompleted()
		{
			if (conditionManager != null && !string.IsNullOrEmpty(Info.DeployedCondition) && deployedToken == ConditionManager.InvalidConditionToken)
				deployedToken = conditionManager.GrantCondition(self, Info.DeployedCondition);

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
			if (conditionManager != null && !string.IsNullOrEmpty(Info.UndeployedCondition) && undeployedToken == ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.GrantCondition(self, Info.UndeployedCondition);

			deployState = DeployState.Undeployed;
		}

		class MoveDeployOrderTargeter : IOrderTargeter
		{
			readonly IMoveInfo info;
			readonly bool rejectMove;
			readonly GrantConditionOnDeploy unit;

			public bool TargetOverridesSelection(TargetModifiers modifiers)
			{
				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveDeployOrderTargeter(Actor self, GrantConditionOnDeploy unit)
			{
				this.unit = unit;
				info = self.Info.TraitInfoOrDefault<IMoveInfo>();
				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "GrantConditionOnDeploy"; } }
			public int OrderPriority { get { return 5; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (target.Type == TargetType.Actor)
				{
					cursor = unit.CanDeploy() ? unit.Info.DeployCursor : unit.Info.DeployBlockedCursor;
					return self == target.Actor;
				}

				if (rejectMove || unit.Info.UndeployOnMove == UndeployOnMoveType.Never ||
				    info == null || target.Type != TargetType.Terrain)
					return false;

				if (unit.Info.UndeployOnMove == UndeployOnMoveType.ForceOnly && !modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? "move") : "move-blocked";

				if (unit.IsTraitPaused
					|| (!explored && info != null && !info.CanMoveIntoShroud())
					|| (explored && info != null && !info.CanMoveInCell(self.World, self, location, null, false)))
					cursor = "move-blocked";

				return true;
			}
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
