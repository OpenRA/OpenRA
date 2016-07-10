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
	[Desc("This actor can capture other actors which have the ExternalCapturable: trait.")]
	class ExternalCapturesInfo : ITraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the ExternalCapturable Type: trait.")]
		public readonly HashSet<string> CaptureTypes = new HashSet<string> { "building" };

		[Desc("Destroy the unit after capturing.")]
		public readonly bool ConsumeActor = false;

		[Desc("Experience granted to the capturing player.")]
		public readonly int PlayerExperience = 0;

		[Desc("Stance that the structure's previous owner needs to have for the capturing player to receive Experience.")]
		public readonly Stance PlayerExperienceStances = Stance.Enemy;

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new ExternalCaptures(init.Self, this); }
	}

	class ExternalCaptures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly ExternalCapturesInfo Info;

		public ExternalCaptures(Actor self, ExternalCapturesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new ExternalCaptureOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "ExternalCaptureActor")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting an actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				var ci = frozen.Info.TraitInfoOrDefault<ExternalCapturableInfo>();
				return ci != null && ci.CanBeTargetedBy(self, frozen.Owner);
			}

			var c = order.TargetActor.TraitOrDefault<ExternalCapturable>();
			return c != null && !c.CaptureInProgress && c.Info.CanBeTargetedBy(self, order.TargetActor.Owner);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "ExternalCaptureActor" && IsValidOrder(self, order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "ExternalCaptureActor" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new ExternalCaptureActor(self, target));
		}
	}

	class ExternalCaptureOrderTargeter : UnitOrderTargeter
	{
		public ExternalCaptureOrderTargeter() : base("ExternalCaptureActor", 6, "enter", true, true) { }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			var c = target.TraitOrDefault<ExternalCapturable>();

			var canTargetActor = c != null && !c.CaptureInProgress && c.Info.CanBeTargetedBy(self, target.Owner);
			cursor = canTargetActor ? "ability" : "move-blocked";
			return canTargetActor;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			var c = target.Info.TraitInfoOrDefault<ExternalCapturableInfo>();

			var canTargetActor = c != null && c.CanBeTargetedBy(self, target.Owner);
			cursor = canTargetActor ? "ability" : "move-blocked";
			return canTargetActor;
		}
	}
}
