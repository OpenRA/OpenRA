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
	[Desc("Grant a condition via player orders for a specified amount of time.")]
	public class GrantChargedConditionOnToggleInfo : PausableConditionalTraitInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant when enabled.")]
		public readonly string ActivatedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when charge is above " + nameof(ChargeThreshhold) + ".")]
		public readonly string ChargedCondition = null;

		[Desc("Charge to start with. If set to -1 the unit will start with full charge.")]
		public readonly int InitialCharge = -1;

		[Desc("Cooldown (in ticks) to reach full charge.")]
		public readonly int ChargeDuration = 500;

		[Desc("The amount of charge that needs to be present to turn on the condition. " +
			"If set to -1, threshold is set to full charge. " +
			"If activated without full charge " + nameof(ConditionDuration) + " is proportionally smaller.")]
		public readonly int ChargeThreshhold = -1;

		[Desc("How long (in ticks) should the condition stay active?")]
		public readonly int ConditionDuration = 1;

		[Desc("Can " + nameof(ActivatedCondition) + " be turned off manually?")]
		public readonly bool CanCancelCondition = false;

		[Desc("Should we interrupt the current activity")]
		public readonly bool CancelsCurrentActivity = false;

		[CursorReference]
		[Desc("Cursor to display when able to trigger a state change.")]
		public readonly string Cursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to trigger a state change.")]
		public readonly string BlockedCursor = "deploy-blocked";

		[Desc("Play a randomly selected sound from this list when turning on.")]
		public readonly string[] ActivationSounds = null;

		[Desc("Play a randomly selected sound from this list when turning off.")]
		public readonly string[] DeactivattionSounds = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color of the charge bar when deactivated.")]
		public readonly Color DeactivatedColor = Color.Magenta;

		[Desc("Color of the charge bar  when activated.")]
		public readonly Color ActivatedColor = Color.DarkMagenta;

		[Desc("Should the charge bar be displayed when not charged or the trait is disabled?")]
		public readonly bool DisplayBarWhenEmpty = true;

		public override object Create(ActorInitializer init) { return new GrantChargedConditionOnToggle(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (ChargeDuration < 1)
				throw new YamlException($"{nameof(ChargeDuration)} cannot be lower than 1.");

			if (ConditionDuration < 1)
				throw new YamlException($"{nameof(ConditionDuration)} cannot be lower than 1.");
		}
	}

	public class GrantChargedConditionOnToggle : PausableConditionalTrait<GrantChargedConditionOnToggleInfo>,
		IIssueOrder, IResolveOrder, ITick, ISelectionBar, IOrderVoice, ISync, IIssueDeployOrder
	{
		[Sync]
		int chargeTick = 0;

		bool isActive = false;

		int activatedToken = Actor.InvalidConditionToken;

		int chargedToken = Actor.InvalidConditionToken;

		readonly int chargeThreshold;
		readonly int activatedChargeThreshold;

		public GrantChargedConditionOnToggle(GrantChargedConditionOnToggleInfo info)
			: base(info)
		{
			chargeTick = info.InitialCharge < 0 || info.InitialCharge >= info.ChargeDuration ? Info.ChargeDuration : info.InitialCharge;

			// PERF: Cache the conversions.
			chargeThreshold = Info.ChargeThreshhold < 0 || Info.ChargeThreshhold > Info.ChargeDuration ? Info.ChargeDuration : Info.ChargeThreshhold;
			activatedChargeThreshold = chargeThreshold * Info.ConditionDuration / Info.ChargeDuration;
		}

		protected override void TraitDisabled(Actor self)
		{
			base.TraitDisabled(self);

			if (isActive)
				Deactivate(self);

			// Reset charge.
			chargeTick = Info.InitialCharge < 0 || Info.InitialCharge > Info.ChargeDuration ? Info.ChargeDuration : Info.InitialCharge;
			if (chargedToken != Actor.InvalidConditionToken)
				chargedToken = self.RevokeCondition(chargedToken);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "ActivateCondition" ? Info.Voice : null;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("ActivateCondition", 5,
						() => CanToggle() ? Info.Cursor : Info.BlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "ActivateCondition")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("ActivateCondition", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return queued || CanToggle(); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "ActivateCondition")
				return;

			if (order.Queued || Info.CancelsCurrentActivity)
				self.QueueActivity(order.Queued, new ToggleChargedCondition(self, this));
			else if (CanToggle())
				ToggleState(self);
		}

		public bool CanToggle() => !IsTraitDisabled && !IsTraitPaused && ((!isActive && chargeTick >= chargeThreshold) || (isActive && Info.CanCancelCondition));

		public void ToggleState(Actor self)
		{
			if (isActive)
			{
				// Keep the percentage of the unused charge.
				chargeTick = chargeTick * Info.ChargeDuration / Info.ConditionDuration;
				Deactivate(self);
			}
			else
			{
				// If activated without full charge, subtract from the activated duration.
				chargeTick = chargeTick * Info.ConditionDuration / Info.ChargeDuration;
				Activate(self);
			}
		}

		void Activate(Actor self)
		{
			if (Info.ActivationSounds != null && Info.ActivationSounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.ActivationSounds, self.World, self.CenterPosition);

			if (activatedToken == Actor.InvalidConditionToken)
				activatedToken = self.GrantCondition(Info.ActivatedCondition);

			isActive = true;
		}

		void Deactivate(Actor self)
		{
			if (Info.DeactivattionSounds != null && Info.DeactivattionSounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.DeactivattionSounds, self.World, self.CenterPosition);

			if (activatedToken != Actor.InvalidConditionToken)
				activatedToken = self.RevokeCondition(activatedToken);

			isActive = false;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (isActive)
			{
				if (chargeTick > 0)
					chargeTick--;
				else
					Deactivate(self);
			}
			else
			{
				if (chargeTick < Info.ChargeDuration)
					chargeTick++;
			}

			if (Info.ChargedCondition != null)
			{
				if (chargeTick < (isActive ? activatedChargeThreshold : chargeThreshold))
				{
					if (chargedToken != Actor.InvalidConditionToken)
						chargedToken = self.RevokeCondition(chargedToken);
				}
				else
				{
					if (chargedToken == Actor.InvalidConditionToken)
						chargedToken = self.GrantCondition(Info.ChargedCondition);
				}
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0f;

			return isActive
				? (float)chargeTick / Info.ConditionDuration
				: (float)chargeTick / Info.ChargeDuration;
		}

		Color ISelectionBar.GetColor() { return isActive ? Info.ActivatedColor : Info.DeactivatedColor; }
		bool ISelectionBar.DisplayWhenEmpty => Info.DisplayBarWhenEmpty;
	}

	public class ToggleChargedCondition : Activity
	{
		readonly GrantChargedConditionOnToggle toggle;

		public ToggleChargedCondition(Actor self, GrantChargedConditionOnToggle toggle)
		{
			this.toggle = toggle;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (toggle.CanToggle())
				toggle.ToggleState(self);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (NextActivity != null)
				foreach (var n in NextActivity.TargetLineNodes(self))
					yield return n;
		}
	}
}
