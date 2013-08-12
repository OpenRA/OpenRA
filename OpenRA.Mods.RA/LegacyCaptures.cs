#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can capture other actors which have the LegacyCapturable: trait.")]
	class LegacyCapturesInfo : ITraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the LegacyCapturable Type: trait.")]
		public readonly string[] CaptureTypes = { "building" };
		[Desc("Unit will do damage to the actor instead of capturing it. Unit is destroyed when sabotaging.")]
		public readonly bool Sabotage = true;
		[Desc("Only used if Sabotage=true. Sabotage damage expressed as a percentage of enemy health removed.")]
		public readonly float SabotageHPRemoval = 0.5f;

		public object Create(ActorInitializer init) { return new LegacyCaptures(init.self, this); }
	}

	class LegacyCaptures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly LegacyCapturesInfo Info;

		public LegacyCaptures(Actor self, LegacyCapturesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new LegacyCaptureOrderTargeter(Info.Sabotage);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "LegacyCaptureActor")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "LegacyCaptureActor" ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "LegacyCaptureActor")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Enter(target.Actor, new LegacyCaptureActor(target)));
		}

		class LegacyCaptureOrderTargeter : UnitOrderTargeter
		{
			readonly bool sabotage;

			public LegacyCaptureOrderTargeter(bool sabotage)
				: base("LegacyCaptureActor", 6, "enter", true, true)
			{
				this.sabotage = sabotage;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.Traits.GetOrDefault<LegacyCapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = "enter-blocked";
					return false;
				}

				var health = target.Trait<Health>();
				var lowEnoughHealth = health.HP <= c.CaptureThreshold * health.MaxHP;

				cursor = !sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? "capture" : "enter";
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var c = target.Info.Traits.GetOrDefault<LegacyCapturableInfo>();
				if (c == null || !c.CanBeTargetedBy(self, target.Owner))
				{
					cursor = "enter-blocked";
					return false;
				}

				var health = target.Info.Traits.GetOrDefault<HealthInfo>();
				var lowEnoughHealth = target.HP <= c.CaptureThreshold * health.HP;

				cursor = !sabotage || lowEnoughHealth || target.Owner.NonCombatant
					? "capture" : "enter";

				return true;
			}
		}
	}
}
