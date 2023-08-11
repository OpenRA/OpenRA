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
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allow deploying on specified charge to grant a condition for a specified duration.")]
	public class GrantConditionOnDeployWithChargeInfo : PausableConditionalTraitInfo, Requires<IMoveInfo>, IRulesetLoaded
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant after deploying.")]
		public readonly string DeployedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when charge is above " + nameof(ChargeThreshhold) + ".")]
		public readonly string ChargedCondition = null;

		[Desc("Charge to start with. If set to -1 the unit will start with full charge.")]
		public readonly int InitialCharge = -1;

		[Desc("Cooldown (in ticks) to reach full charge.")]
		public readonly int ChargeDuration = 500;

		[Desc("The ammount of charge that needs to be present for deploy to be issued. If set to -1, threshold is set to full charge. If activated without full charge " + nameof(ConditionDuration) + " is percentally smaller.")]
		public readonly int ChargeThreshhold = -1;

		[Desc("How long (in ticks) should the condition stay active?")]
		public readonly int ConditionDuration = 1;

		[Desc("Can " + nameof(DeployedCondition) + " be canceled by followup deploy order?")]
		public readonly bool CanCancelCondition = false;

		[CursorReference]
		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[Desc("Play a randomly selected sound from this list when deploying.")]
		public readonly string[] DeploySounds = null;

		[Desc("Play a randomly selected sound from this list when undeploying.")]
		public readonly string[] UndeploySounds = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly Color ChargingColor = Color.Magenta;
		public readonly Color DeployedColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantConditionOnDeployWithCharge(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (ChargeDuration < 1)
				throw new YamlException($"{nameof(ChargeDuration)} cannot be lower than 1.");

			if (ConditionDuration < 1)
				throw new YamlException($"{nameof(ConditionDuration)} cannot be lower than 1.");
		}
	}

	public class GrantConditionOnDeployWithCharge : PausableConditionalTrait<GrantConditionOnDeployWithChargeInfo>, IIssueOrder, IResolveOrder, ITick, ISelectionBar, IOrderVoice, ISync, IIssueDeployOrder
	{
		[Sync]
		int chargeTick = 0;

		bool deployed = false;

		int deployedToken = Actor.InvalidConditionToken;

		int chargedToken = Actor.InvalidConditionToken;

		readonly int chargeThreshold;
		readonly int deployedChargeThreshold;

		public GrantConditionOnDeployWithCharge(GrantConditionOnDeployWithChargeInfo info)
			: base(info)
		{
			chargeTick = info.InitialCharge < 0 || info.InitialCharge >= info.ChargeDuration ? Info.ChargeDuration : info.InitialCharge;

			// PERF: Cache the conversions.
			chargeThreshold = Info.ChargeThreshhold < 0 || Info.ChargeThreshhold > Info.ChargeDuration ? Info.ChargeDuration : Info.ChargeThreshhold;
			deployedChargeThreshold = chargeThreshold * Info.ConditionDuration / Info.ChargeDuration;
		}

		protected override void TraitDisabled(Actor self)
		{
			base.TraitDisabled(self);

			if (deployed)
				Undeploy(self);

			// Reset charge.
			chargeTick = Info.InitialCharge < 0 || Info.InitialCharge > Info.ChargeDuration ? Info.ChargeDuration : Info.InitialCharge;
			if (chargedToken != Actor.InvalidConditionToken)
				chargedToken = self.RevokeCondition(chargedToken);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "DeployWithCharge" ? Info.Voice : null;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("DeployWithCharge", 5,
						() => CanDeploy() ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "DeployWithCharge")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("DeployWithCharge", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return queued || CanDeploy(); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployWithCharge")
				self.QueueActivity(order.Queued, new DeployForGrantedConditionWithCharge(self, this));
		}

		public bool CanDeploy() => !IsTraitDisabled && !IsTraitPaused && ((!deployed && chargeTick >= chargeThreshold) || (deployed && Info.CanCancelCondition));

		public void TriggerDeploy(Actor self)
		{
			if (deployed)
			{
				// Keep the percentage of the unused charge.
				chargeTick = chargeTick * Info.ChargeDuration / Info.ConditionDuration;
				Undeploy(self);
			}
			else
			{
				// If deployed without full charge, reduce the deploy duration.
				chargeTick = chargeTick * Info.ConditionDuration / Info.ChargeDuration;
				Deploy(self);
			}
		}

		void Deploy(Actor self)
		{
			if (Info.DeploySounds != null && Info.DeploySounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.DeploySounds, self.World, self.CenterPosition);

			if (deployedToken == Actor.InvalidConditionToken)
				deployedToken = self.GrantCondition(Info.DeployedCondition);

			deployed = true;
		}

		void Undeploy(Actor self)
		{
			if (Info.UndeploySounds != null && Info.UndeploySounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.UndeploySounds, self.World, self.CenterPosition);

			if (deployedToken != Actor.InvalidConditionToken)
				deployedToken = self.RevokeCondition(deployedToken);

			deployed = false;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (deployed)
			{
				if (chargeTick > 0)
					chargeTick--;
				else
					Undeploy(self);
			}
			else
			{
				if (chargeTick < Info.ChargeDuration)
					chargeTick++;
			}

			if (Info.ChargedCondition != null)
			{
				if (chargeTick < (deployed ? deployedChargeThreshold : chargeThreshold))
				{
					if (chargedToken != Actor.InvalidConditionToken)
						chargedToken = self.RevokeCondition(chargedToken);
				}
				else
					if (chargedToken == Actor.InvalidConditionToken)
						chargedToken = self.GrantCondition(Info.ChargedCondition);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0f;

			return deployed
				? (float)chargeTick / Info.ConditionDuration
				: (float)chargeTick / Info.ChargeDuration;
		}

		Color ISelectionBar.GetColor() { return deployed ? Info.DeployedColor : Info.ChargingColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}

	public class DeployForGrantedConditionWithCharge : Activity
	{
		readonly GrantConditionOnDeployWithCharge deploy;

		public DeployForGrantedConditionWithCharge(Actor self, GrantConditionOnDeployWithCharge deploy)
		{
			this.deploy = deploy;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (deploy.CanDeploy())
				deploy.TriggerDeploy(self);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (NextActivity != null)
				foreach (var n in NextActivity.TargetLineNodes(self))
					yield return n;

			yield break;
		}
	}
}
