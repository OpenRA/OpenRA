#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The player can give this unit the order to follow and protect friendly units with the Guardable trait.")]
	public class GuardInfo : TraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.OrangeRed;

		public override object Create(ActorInitializer init) { return new Guard(this); }
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
				GuardTarget(self, order.Target, order.Queued);
		}

		public void GuardTarget(Actor self, Target target, bool queued = false)
		{
			if (target.Type != TargetType.Actor)
				return;

			var range = target.Actor.Info.TraitInfo<GuardableInfo>().Range;
			self.QueueActivity(queued, new AttackMoveActivity(self, () => move.MoveFollow(self, target, WDist.Zero, range, targetLineColor: info.TargetLineColor)));
			self.ShowTargetLines();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Guard" ? info.Voice : null;
		}
	}
}
