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

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can capture other actors which have the Capturable: trait.")]
	public class CapturesInfo : ConditionalTraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly HashSet<string> CaptureTypes = new HashSet<string> { "building" };

		[Desc("Unit will do damage to the actor instead of capturing it. Unit is destroyed when sabotaging.")]
		public readonly bool Sabotage = true;

		[Desc("Only used if Sabotage=true. Sabotage damage expressed as a percentage of enemy health removed.")]
		public readonly int SabotageHPRemoval = 50;

		[Desc("Experience granted to the capturing player.")]
		public readonly int PlayerExperience = 0;

		[Desc("Stance that the structure's previous owner needs to have for the capturing player to receive Experience.")]
		public readonly Stance PlayerExperienceStances = Stance.Enemy;

		public readonly string SabotageCursor = "capture";
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new Captures(this); }
	}

	public class Captures : ConditionalTrait<CapturesInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public Captures(CapturesInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new CaptureOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "CaptureActor")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
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

		class CaptureOrderTargeter : UnitOrderTargeter
		{
			readonly CapturesInfo capturesInfo;

			public CaptureOrderTargeter(CapturesInfo info)
				: base("CaptureActor", 6, info.EnterCursor, true, true)
			{
				capturesInfo = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.TraitInfoOrDefault<CapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = capturesInfo.EnterBlockedCursor;
					return false;
				}

				var health = target.Trait<Health>();
				var lowEnoughHealth = health.HP <= c.CaptureThreshold * health.MaxHP / 100;

				cursor = !capturesInfo.Sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? capturesInfo.EnterCursor : capturesInfo.SabotageCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.TraitInfoOrDefault<CapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = capturesInfo.EnterCursor;
					return false;
				}

				var health = target.Info.TraitInfoOrDefault<HealthInfo>();
				var lowEnoughHealth = target.HP <= c.CaptureThreshold * health.HP / 100;

				cursor = !capturesInfo.Sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? capturesInfo.EnterCursor : capturesInfo.SabotageCursor;

				return true;
			}
		}
	}
}
