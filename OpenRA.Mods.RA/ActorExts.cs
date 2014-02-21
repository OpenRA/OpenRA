#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public static class ActorExts
	{
		static bool IsDisguisedSpy(this Actor a)
		{
			var spy = a.TraitOrDefault<Spy>();
			return spy != null && spy.Disguised;
		}

		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return true;

			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Ally;

			return stance == Stance.Ally;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */

			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Enemy;

			return stance == Stance.Enemy;
		}

		public static Target ResolveFrozenActorOrder(this Actor self, Order order, Color targetLine)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0)
				return Target.FromOrder(order);

			// Targeted an actor under the fog
			var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
			if (frozenLayer == null)
				return Target.Invalid;

			var frozen = frozenLayer.FromID(order.ExtraData);
			if (frozen == null)
				return Target.Invalid;

			// Flashes the frozen proxy
			self.SetTargetLine(frozen, targetLine, true);

			// Target is still alive - resolve the real order
			if (frozen.Actor != null && frozen.Actor.IsInWorld)
				return Target.FromActor(frozen.Actor);

			if (!order.Queued)
				self.CancelActivity();

			var move = self.TraitOrDefault<IMove>();
			if (move != null)
			{
				// Move within sight range of the frozen actor
				var sight = self.TraitOrDefault<RevealsShroud>();
				var range = sight != null ? sight.Range : 2;

				self.QueueActivity(move.MoveWithinRange(Target.FromPos(frozen.CenterPosition), WDist.FromCells(range)));
			}

			return Target.Invalid;
		}
	}
}
