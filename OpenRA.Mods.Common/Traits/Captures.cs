#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public class CapturesInfo : ITraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly HashSet<string> CaptureTypes = new HashSet<string> { "building" };
		[Desc("Unit will do damage to the actor instead of capturing it. Unit is destroyed when sabotaging.")]
		public readonly bool Sabotage = true;
		[Desc("Only used if Sabotage=true. Sabotage damage expressed as a percentage of enemy health removed.")]
		public readonly int SabotageHPRemoval = 50;

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Captures(init.Self, this); }
	}

	public class Captures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CapturesInfo Info;

		public Captures(Actor self, CapturesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new CaptureOrderTargeter(Info.Sabotage);
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
			if (order.OrderString != "CaptureActor")
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
			readonly bool sabotage;

			public CaptureOrderTargeter(bool sabotage)
				: base("CaptureActor", 6, "enter", true, true)
			{
				this.sabotage = sabotage;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.TraitInfoOrDefault<CapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = "enter-blocked";
					return false;
				}

				var health = target.Trait<Health>();
				var lowEnoughHealth = health.HP <= c.CaptureThreshold * health.MaxHP / 100;

				cursor = !sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? "enter" : "capture";
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.TraitInfoOrDefault<CapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = "enter-blocked";
					return false;
				}

				var health = target.Info.TraitInfoOrDefault<HealthInfo>();
				var lowEnoughHealth = target.HP <= c.CaptureThreshold * health.HP / 100;

				cursor = !sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? "enter" : "capture";

				return true;
			}
		}
	}
}
