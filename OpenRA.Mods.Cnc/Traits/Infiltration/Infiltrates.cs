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
using System.Linq;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class InfiltratesInfo : ConditionalTraitInfo
	{
		public readonly BitSet<TargetableType> Types;

		[VoiceReference] public readonly string Voice = "Action";

		[Desc("What diplomatic stances can be infiltrated by this actor.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("Notification to play when a building is infiltrated.")]
		public readonly string Notification = "BuildingInfiltrated";

		[Desc("Experience to grant to the infiltrating player.")]
		public readonly int PlayerExperience = 0;

		public override object Create(ActorInitializer init) { return new Infiltrates(this); }
	}

	public class Infiltrates : ConditionalTrait<InfiltratesInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public Infiltrates(InfiltratesInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new InfiltrationOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "Infiltrate")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		bool IsValidOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return false;

			var targetTypes = new BitSet<TargetableType>();
			if (order.Target.Type == TargetType.FrozenActor)
				targetTypes = order.Target.FrozenActor.TargetTypes;

			if (order.Target.Type == TargetType.Actor)
				targetTypes = order.Target.Actor.GetEnabledTargetTypes();

			return Info.Types.Overlaps(targetTypes);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Infiltrate" && IsValidOrder(self, order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Infiltrate" || !IsValidOrder(self, order) || IsTraitDisabled)
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor
				|| !Info.Types.Overlaps(target.Actor.GetAllTargetTypes()))
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Infiltrate(self, target.Actor, this));
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

			return info.Types.Overlaps(target.Info.GetAllTargetTypes());
		}
	}
}
