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

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can capture other actors which have the Capturable: trait.")]
	public class CapturesInfo : ConditionalTraitInfo, Requires<CaptureManagerInfo>
	{
		[FieldLoader.Require]
		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly BitSet<CaptureType> CaptureTypes = default(BitSet<CaptureType>);

		[Desc("Targets with health above this percentage will be sabotaged instead of captured.",
			"Set to 0 to disable sabotaging.")]
		public readonly int SabotageThreshold = 0;

		[Desc("Sabotage damage expressed as a percentage of maximum target health.")]
		public readonly int SabotageHPRemoval = 50;

		[Desc("Damage types that applied with the sabotage damage.")]
		public readonly BitSet<DamageType> SabotageDamageTypes = default(BitSet<DamageType>);

		[Desc("Delay (in ticks) that to wait next to the target before initiating the capture.")]
		public readonly int CaptureDelay = 0;

		[Desc("Enter the target actor and be consumed by the capture.")]
		public readonly bool ConsumedByCapture = true;

		[Desc("Experience granted to the capturing player.")]
		public readonly int PlayerExperience = 0;

		[Desc("Stance that the structure's previous owner needs to have for the capturing player to receive Experience.")]
		public readonly Stance PlayerExperienceStances = Stance.Enemy;

		public readonly string SabotageCursor = "capture";
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new Captures(init.Self, this); }
	}

	public class Captures : ConditionalTrait<CapturesInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly CaptureManager captureManager;

		public Captures(Actor self, CapturesInfo info)
			: base(info)
		{
			captureManager = self.Trait<CaptureManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new CaptureOrderTargeter(this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "CaptureActor")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "CaptureActor" ? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "CaptureActor" || IsTraitDisabled)
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new CaptureActor(self, target.Actor));
		}

		protected override void TraitEnabled(Actor self) { captureManager.RefreshCaptures(self); }
		protected override void TraitDisabled(Actor self) { captureManager.RefreshCaptures(self); }

		class CaptureOrderTargeter : UnitOrderTargeter
		{
			readonly Captures captures;

			public CaptureOrderTargeter(Captures captures)
				: base("CaptureActor", 6, captures.Info.EnterCursor, true, true)
			{
				this.captures = captures;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var captureManager = target.TraitOrDefault<CaptureManager>();
				if (captureManager == null || !captureManager.CanBeTargetedBy(target, self, captures))
				{
					cursor = captures.Info.EnterBlockedCursor;
					return false;
				}

				cursor = captures.Info.EnterCursor;
				if (captures.Info.SabotageThreshold > 0 && !target.Owner.NonCombatant)
				{
					var health = target.Trait<IHealth>();

					// Sabotage instead of capture
					if ((long)health.HP * 100 > captures.Info.SabotageThreshold * (long)health.MaxHP)
						cursor = captures.Info.SabotageCursor;
				}

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var captureManagerInfo = target.Info.TraitInfoOrDefault<CaptureManagerInfo>();
				if (captureManagerInfo == null || !captureManagerInfo.CanBeTargetedBy(target, self, captures))
				{
					cursor = captures.Info.EnterBlockedCursor;
					return false;
				}

				cursor = captures.Info.EnterCursor;
				if (captures.Info.SabotageThreshold > 0 && !target.Owner.NonCombatant)
				{
					var healthInfo = target.Info.TraitInfoOrDefault<IHealthInfo>();

					// Sabotage instead of capture
					if ((long)target.HP * 100 > captures.Info.SabotageThreshold * (long)healthInfo.MaxHP)
						cursor = captures.Info.SabotageCursor;
				}

				return true;
			}
		}
	}
}
