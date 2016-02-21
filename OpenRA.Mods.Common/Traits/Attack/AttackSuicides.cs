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
using OpenRA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Does a suicide attack where it moves next to the target when used in combination with `Explodes`.")]
	class AttackSuicidesInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new AttackSuicides(init.Self, this); }
	}

	class AttackSuicides : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly AttackSuicidesInfo info;
		readonly IMove move;

		public AttackSuicides(Actor self, AttackSuicidesInfo info)
		{
			this.info = info;
			move = self.Trait<IMove>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new HashSet<string> { "DetonateAttack" }, "DetonateAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return info.Voice;
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

				self.SetTargetLine(target, Color.Red);

				self.QueueActivity(move.MoveToTarget(self, target));

				self.QueueActivity(new CallFunc(() => self.Kill(self)));
			}
			else if (order.OrderString == "Detonate")
				self.Kill(self);
		}
	}
}
