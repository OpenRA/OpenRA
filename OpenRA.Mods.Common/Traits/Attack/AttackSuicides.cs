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
using OpenRA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Does a suicide attack where it moves next to the target when used in combination with `Explodes`.")]
	class AttackSuicidesInfo : ConditionalTraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new AttackSuicides(init.Self, this); }
	}

	class AttackSuicides : ConditionalTrait<AttackSuicidesInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly IMove move;

		public AttackSuicides(Actor self, AttackSuicidesInfo info)
			: base(info)
		{
			move = self.Trait<IMove>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new TargetTypeOrderTargeter(new HashSet<string> { "DetonateAttack" }, "DetonateAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self)
		{
			return new Order("Detonate", self, false);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return true; }

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return Info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DetonateAttack")
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (target.Type != TargetType.Actor)
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.QueueActivity(move.MoveToTarget(self, target));
				var act = new CallFunc(() => self.Kill(self));
				act.SetTargetLineNode(target, Color.Red, true);
				self.QueueActivity(act);

				self.ShowTargetLines();
			}
			else if (order.OrderString == "Detonate")
				self.Kill(self);
		}
	}
}
