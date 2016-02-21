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

using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	class AttackMoveInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new AttackMove(init.Self, this); }
	}

	class AttackMove : IResolveOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public CPos _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : CPos.Zero; } }
		public CPos? TargetLocation = null;

		readonly IMove move;
		readonly AttackMoveInfo info;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			this.info = info;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
				return info.Voice;

			return null;
		}

		void Activate(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(TargetLocation.Value, 1)));
		}

		public void TickIdle(Actor self)
		{
			// This might cause the actor to be stuck if the target location is unreachable
			if (TargetLocation.HasValue && self.Location != TargetLocation.Value)
				Activate(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			TargetLocation = null;

			if (order.OrderString == "AttackMove")
			{
				TargetLocation = move.NearestMoveableCell(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(self.World, TargetLocation.Value), Color.Red);
				Activate(self);
			}
		}
	}
}
