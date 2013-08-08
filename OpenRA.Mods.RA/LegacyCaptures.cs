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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.FileFormats;

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
		readonly Actor self;

		public LegacyCaptures(Actor self, LegacyCapturesInfo info)
		{
			this.self = self;
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new LegacyCaptureOrderTargeter(CanCapture);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "LegacyCaptureActor")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "LegacyCaptureActor") ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "LegacyCaptureActor")
			{
				self.SetTargetLine(Target.FromOrder(order), Color.Red);

				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor, new LegacyCaptureActor(Target.FromOrder(order))));
			}
		}

		bool CanCapture(Actor target)
		{
			var c = target.TraitOrDefault<LegacyCapturable>();
			return c != null && c.CanBeTargetedBy(self);
		}

		class LegacyCaptureOrderTargeter : UnitOrderTargeter
		{
			readonly Func<Actor, bool> useCaptureCursor;

			public LegacyCaptureOrderTargeter(Func<Actor, bool> useCaptureCursor)
				: base("LegacyCaptureActor", 6, "enter", true, true)
			{
				this.useCaptureCursor = useCaptureCursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var canTargetActor = useCaptureCursor(target);

				if (canTargetActor)
				{
					var c = target.Trait<LegacyCapturable>();
					var health = target.Trait<Health>();
					var lowEnoughHealth = health.HP <= c.Info.CaptureThreshold * health.MaxHP;

					cursor = lowEnoughHealth ? "enter" : "capture";

					return true;
				}

				cursor = "enter-blocked";
				return false;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// TODO: Not yet supported
				return false;
			}
		}
	}
}
