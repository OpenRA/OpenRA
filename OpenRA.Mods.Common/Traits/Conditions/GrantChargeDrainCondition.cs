#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition for a certain time when deployed. Can be reversed anytime " +
		"by undeploying. Leftover charges are taken into account when recharging.")]
	public class GrantChargeDrainConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Amount of charge points the actor can build up to.")]
		public readonly int MaxChargePoints = 0;

		[FieldLoader.Require]
		[Desc("Time it takes to charge up one charge point.")]
		public readonly int ChargeDuration = 0;

		[FieldLoader.Require]
		[Desc("Time it takes to discharge one charge point.")]
		public readonly int DrainDuration = 0;

		[Desc("How many charge points does the actor spawn with.")]
		public readonly int StartingChargePoints = 0;

		[Desc("Minimum number of charge points required to activate the condition.")]
		public readonly int MinActivationPoints = 0;

		[Desc("Allow the condition to be disabled mid-discharge.")]
		public readonly bool Interruptable = false;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition granted after deploying.")]
		public readonly string DeployedCondition = null;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[Desc("Rate at which the condition discharges compared to charging.")]
		public readonly int DischargeModifier = 100;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly bool ShowSelectionBar = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantChargeDrainCondition(init.Self, this); }
	}

	public class GrantChargeDrainCondition : PausableConditionalTrait<GrantChargeDrainConditionInfo>,
		IResolveOrder, IIssueOrder, ISelectionBar, IOrderVoice, ISync, ITick, IIssueDeployOrder
	{
		enum TimedDeployState { Charging, Ready, Active, Deploying, Undeploying }

		readonly Actor self;

		int deployedToken = Actor.InvalidConditionToken;

		[Sync]
		int currentChargePoints;

		[Sync]
		int ticks;

		TimedDeployState deployState;

		public GrantChargeDrainCondition(Actor self, GrantChargeDrainConditionInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			currentChargePoints = Info.StartingChargePoints;
			deployState = currentChargePoints == Info.MaxChargePoints ? TimedDeployState.Ready : TimedDeployState.Charging;
			ticks = deployState == TimedDeployState.Charging ? Info.ChargeDuration : Info.DrainDuration;

			base.Created(self);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("GrantChargeDrainCondition", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued)
		{
			return CanDeploy();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("GrantChargeDrainCondition", 5,
						() => IsCursorBlocked() ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == "GrantChargeDrainCondition" ? new Order(order.OrderID, self, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GrantChargeDrainCondition" || !CanDeploy())
				return;

			if (!order.Queued)
				self.CancelActivity();

			if (deployState != TimedDeployState.Active)
				self.QueueActivity(new CallFunc(Deploy));
			else
				self.QueueActivity(new CallFunc(RevokeDeploy));
		}

		bool IsCursorBlocked()
		{
			return !CanDeploy();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "GrantChargeDrainCondition" && CanDeploy() ? Info.Voice : null;
		}

		bool CanDeploy()
		{
			if (IsTraitPaused || IsTraitDisabled)
				return false;

			if (deployState == TimedDeployState.Charging && currentChargePoints < Info.MinActivationPoints)
				return false;

			if (deployState == TimedDeployState.Active && !Info.Interruptable)
				return false;

			if (deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Undeploying)
				return false;

			return true;
		}

		void Deploy()
		{
			deployState = TimedDeployState.Deploying;

			OnDeployCompleted();
		}

		void OnDeployCompleted()
		{
			if (deployedToken == Actor.InvalidConditionToken)
				deployedToken = self.GrantCondition(Info.DeployedCondition);

			deployState = TimedDeployState.Active;
		}

		void RevokeDeploy()
		{
			deployState = TimedDeployState.Undeploying;

			OnUndeployCompleted();
		}

		void OnUndeployCompleted()
		{
			if (deployedToken != Actor.InvalidConditionToken)
				deployedToken = self.RevokeCondition(deployedToken);

			deployState = TimedDeployState.Charging;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (deployState == TimedDeployState.Ready || deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Undeploying)
				return;

			if (--ticks != 0)
				return;

			if (deployState == TimedDeployState.Charging)
			{
				currentChargePoints++;
				if (currentChargePoints == Info.MaxChargePoints)
				{
					deployState = TimedDeployState.Ready;
					ticks = Info.DrainDuration;
				}
				else
					ticks = Info.ChargeDuration;
			}
			else
			{
				currentChargePoints = currentChargePoints - (100 / Info.DischargeModifier);
				if (currentChargePoints == 0)
				{
					RevokeDeploy();
					ticks = Info.ChargeDuration;
				}
				else
					ticks = Info.DrainDuration;
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar)
				return 0f;

			return (float)currentChargePoints / Info.MaxChargePoints;
		}

		bool ISelectionBar.DisplayWhenEmpty => !IsTraitDisabled && Info.ShowSelectionBar;

		Color ISelectionBar.GetColor() => deployState == TimedDeployState.Charging ? Info.ChargingColor : Info.DischargingColor;
	}
}
