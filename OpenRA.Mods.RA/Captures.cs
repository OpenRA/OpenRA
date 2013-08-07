#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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
	[Desc("This actor can capture other actors which have the Capturable: trait.")]
	class CapturesInfo : ITraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly string[] CaptureTypes = { "building" };
		[Desc("Destroy the unit after capturing.")]
		public readonly bool ConsumeActor = false;

		public object Create(ActorInitializer init) { return new Captures(init.self, this); }
	}

	class Captures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CapturesInfo Info;
		readonly Actor self;

		public Captures(Actor self, CapturesInfo info)
		{
			this.self = self;
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new CaptureOrderTargeter(CanCapture);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "CaptureActor")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "CaptureActor" && CanCapture(order.TargetActor)) ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CaptureActor")
			{
				if (!CanCapture(order.TargetActor))
					return;

				self.SetTargetLine(Target.FromOrder(order), Color.Red);

				self.CancelActivity();
				self.QueueActivity(new CaptureActor(Target.FromOrder(order)));
			}
		}

		bool CanCapture(Actor target)
		{
			var c = target.TraitOrDefault<Capturable>();
			return c != null && c.CanBeTargetedBy(self);
		}
	}

	class CaptureOrderTargeter : UnitOrderTargeter
	{
		readonly Func<Actor, bool> useCaptureCursor;

		public CaptureOrderTargeter(Func<Actor, bool> useCaptureCursor)
			: base("CaptureActor", 6, "enter", true, true)
		{
			this.useCaptureCursor = useCaptureCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			var canTargetActor = useCaptureCursor(target);
			cursor = canTargetActor ? "ability" : "move-blocked";

			if (canTargetActor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				return true;
			}

			return false;
		}
	}
}
