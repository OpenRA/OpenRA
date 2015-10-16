#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class InfiltratesInfo : ITraitInfo
	{
		public readonly HashSet<string> Types = new HashSet<string>();

		[VoiceReference] public readonly string Voice = "Action";

		[Desc("What diplomatic stances can be infiltrated by this actor.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		public object Create(ActorInitializer init) { return new Infiltrates(this); }
	}

	class Infiltrates : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly InfiltratesInfo Info;

		public Infiltrates(InfiltratesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new InfiltrationOrderTargeter(Info); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "Infiltrate")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting an actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			IEnumerable<string> targetTypes;

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				targetTypes = frozen.TargetTypes;
			}
			else
				targetTypes = order.TargetActor.GetEnabledTargetTypes();

			return Info.Types.Overlaps(targetTypes);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Infiltrate" && IsValidOrder(self, order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Infiltrate" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor
				|| !Info.Types.Overlaps(target.Actor.GetAllTargetTypes()))
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Infiltrate(self, target.Actor));
		}
	}

	class InfiltrationOrderTargeter : UnitOrderTargeter
	{
		readonly InfiltratesInfo info;

		public InfiltrationOrderTargeter(InfiltratesInfo info)
			: base("Infiltrate", 7, "enter", true, false)
		{
			this.info = info;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			var stance = self.Owner.Stances[target.Owner];

			if (!info.ValidStances.HasStance(stance))
				return false;

			return info.Types.Overlaps(target.GetAllTargetTypes());
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			var stance = self.Owner.Stances[target.Owner];

			if (!info.ValidStances.HasStance(stance))
				return false;

			return info.Types.Overlaps(target.Info.TraitInfos<ITargetableInfo>().SelectMany(ti => ti.GetTargetTypes()));
		}
	}
}
