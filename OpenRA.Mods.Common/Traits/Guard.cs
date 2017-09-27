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

using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The player can give this unit the order to follow and protect friendly units with the Guardable trait.")]
	public class GuardInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Guard(this); }
	}

	public class Guard : IResolveOrder, IOrderVoice, INotifyCreated
	{
		readonly GuardInfo info;
		IMove move;

		public Guard(GuardInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			move = self.Trait<IMove>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Guard")
				GuardTarget(self, Target.FromActor(order.TargetActor), order.Queued);
		}

		public void GuardTarget(Actor self, Target target, bool queued = false)
		{
			if (!queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);

			var range = target.Actor.Info.TraitInfo<GuardableInfo>().Range;
			self.QueueActivity(new AttackMoveActivity(self, move.MoveFollow(self, target, WDist.Zero, range)));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Guard" ? info.Voice : null;
		}
	}
}
