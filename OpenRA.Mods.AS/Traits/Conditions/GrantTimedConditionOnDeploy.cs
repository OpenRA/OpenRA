#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class GrantTimedConditionOnDeployInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition granted during deploying.")]
		public readonly string DeployingCondition = null;

		[GrantedConditionReference, FieldLoader.Require]
		[Desc("The condition granted after deploying.")]
		public readonly string DeployedCondition = null;

		[Desc("Cooldown in ticks until the unit can deploy.")]
		public readonly int CooldownTicks;

		[Desc("The deployed state's length in ticks.")]
		public readonly int DeployedTicks;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[SequenceReference, Desc("Animation to play for deploying.")]
		public readonly string DeployAnimation = null;

		[Desc("Facing that the actor must face before deploying. Set to -1 to deploy regardless of facing.")]
		public readonly int Facing = -1;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		[Desc("Sound to play when undeploying.")]
		public readonly string UndeploySound = null;

		public readonly bool StartsFullyCharged = false;

		[VoiceReference] public readonly string Voice = "Action";

		public readonly bool ShowSelectionBar = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public object Create(ActorInitializer init) { return new GrantTimedConditionOnDeploy(init, this); }
	}

	public enum TimedDeployState { Charging, Ready, Active, Deploying, Undeploying }

	public class GrantTimedConditionOnDeploy : IResolveOrder, IIssueOrder, INotifyCreated, ISelectionBar, IOrderVoice, ISync, ITick
	{
		readonly Actor self;
		readonly GrantTimedConditionOnDeployInfo info;
		readonly bool canTurn;
		readonly Lazy<WithSpriteBody> body;
		int deployedToken = ConditionManager.InvalidConditionToken;
		int deployingToken = ConditionManager.InvalidConditionToken;

		ConditionManager manager;
		[Sync] int ticks;
		TimedDeployState deployState;

		public GrantTimedConditionOnDeploy(ActorInitializer init, GrantTimedConditionOnDeployInfo info)
		{
			self = init.Self;
			this.info = info;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			body = Exts.Lazy(self.TraitOrDefault<WithSpriteBody>);
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();

			if (info.StartsFullyCharged)
			{
				ticks = info.DeployedTicks;
				deployState = TimedDeployState.Ready;
			}
			else
			{
				ticks = info.CooldownTicks;
				deployState = TimedDeployState.Charging;
			}
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new DeployOrderTargeter("GrantConditionOnDeploy", 5,
				() => IsCursorBlocked() ? info.DeployBlockedCursor : info.DeployCursor); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "GrantConditionOnDeploy")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GrantConditionOnDeploy" || deployState != TimedDeployState.Ready)
				return;

			if (!order.Queued)
				self.CancelActivity();

			// Turn to the required facing.
			if (info.Facing != -1 && canTurn)
				self.QueueActivity(new Turn(self, info.Facing));

			self.QueueActivity(new CallFunc(Deploy));
		}

		bool IsCursorBlocked()
		{
			return deployState != TimedDeployState.Ready;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "GrantConditionOnDeploy" && deployState == TimedDeployState.Ready ? info.Voice : null;
		}

		void Deploy()
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (deployState != TimedDeployState.Ready)
				return;

			deployState = TimedDeployState.Deploying;

			if (!string.IsNullOrEmpty(info.DeploySound))
				Game.Sound.Play(SoundType.World, info.DeploySound, self.CenterPosition);

			// If there is no animation to play just grant the upgrades that are used while deployed.
			// Alternatively, play the deploy animation and then grant the upgrades.
			if (string.IsNullOrEmpty(info.DeployAnimation) || body.Value == null)
				OnDeployCompleted();
			else
			{
				if (manager != null && !string.IsNullOrEmpty(info.DeployingCondition) && deployingToken == ConditionManager.InvalidConditionToken)
					deployingToken = manager.GrantCondition(self, info.DeployingCondition);
				body.Value.PlayCustomAnimation(self, info.DeployAnimation, OnDeployCompleted);
			}
		}

		void OnDeployCompleted()
		{
			if (manager != null && !string.IsNullOrEmpty(info.DeployedCondition) && deployedToken == ConditionManager.InvalidConditionToken)
				deployedToken = manager.GrantCondition(self, info.DeployedCondition);

			if (deployingToken != ConditionManager.InvalidConditionToken)
				deployingToken = manager.RevokeCondition(self, deployingToken);

			deployState = TimedDeployState.Active;
		}

		void RevokeDeploy()
		{
			deployState = TimedDeployState.Undeploying;

			if (!string.IsNullOrEmpty(info.UndeploySound))
				Game.Sound.Play(SoundType.World, info.UndeploySound, self.CenterPosition);

			if (string.IsNullOrEmpty(info.DeployAnimation) || body.Value == null)
				OnUndeployCompleted();
			else
			{
				if (manager != null && !string.IsNullOrEmpty(info.DeployingCondition) && deployingToken == ConditionManager.InvalidConditionToken)
					deployingToken = manager.GrantCondition(self, info.DeployingCondition);
				body.Value.PlayCustomAnimationBackwards(self, info.DeployAnimation, OnUndeployCompleted);
			}
		}

		void OnUndeployCompleted()
		{
			if (deployedToken != ConditionManager.InvalidConditionToken)
				deployedToken = manager.RevokeCondition(self, deployedToken);

			if (deployingToken != ConditionManager.InvalidConditionToken)
				deployingToken = manager.RevokeCondition(self, deployingToken);

			deployState = TimedDeployState.Charging;
			ticks = info.CooldownTicks;
		}

		void ITick.Tick(Actor self)
		{
			if (deployState == TimedDeployState.Ready || deployState == TimedDeployState.Deploying)
				return;

			if (--ticks < 0)
			{
				if (deployState == TimedDeployState.Charging)
				{
					ticks = info.DeployedTicks;
					deployState = TimedDeployState.Ready;
				}
				else
				{
					RevokeDeploy();
				}
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || deployState == TimedDeployState.Undeploying)
				return 0f;

			if (deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Ready)
				return 1f;

			return deployState == TimedDeployState.Charging
				? (float)(info.CooldownTicks - ticks) / info.CooldownTicks
				: (float)ticks / info.DeployedTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return deployState == TimedDeployState.Charging ? info.ChargingColor : info.DischargingColor; }
	}
}
