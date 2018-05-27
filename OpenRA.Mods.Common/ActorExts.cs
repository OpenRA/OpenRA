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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public static class ActorExts
	{
		public static bool IsAtGroundLevel(this Actor self)
		{
			if (self.OccupiesSpace == null)
				return false;

			if (!self.IsInWorld)
				return false;

			var map = self.World.Map;

			if (!map.Contains(self.Location))
				return false;

			return map.DistanceAboveTerrain(self.CenterPosition).Length == 0;
		}

		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return true;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.Stances[self.EffectiveOwner.Owner] == Stance.Ally;

			return false;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.Stances[self.EffectiveOwner.Owner] == Stance.Enemy;

			return stance == Stance.Enemy;
		}

		/// <summary>
		/// DEPRECATED: Write code that can handle FrozenActors correctly instead.
		/// </summary>
		public static Target ResolveFrozenActorOrder(this Actor self, Order order, Color targetLine)
		{
			// Not targeting a frozen actor
			if (order.Target.Type != TargetType.FrozenActor)
				return order.Target;

			var frozen = order.Target.FrozenActor;

			self.SetTargetLine(order.Target, targetLine, true);

			// Target is still alive - resolve the real order
			if (frozen.Actor != null && frozen.Actor.IsInWorld)
				return Target.FromActor(frozen.Actor);

			if (!order.Queued)
				self.CancelActivity();

			var move = self.TraitOrDefault<IMove>();
			if (move != null)
			{
				// Move within sight range of the frozen actor
				var range = self.TraitsImplementing<RevealsShroud>()
					.Where(s => !s.IsTraitDisabled)
					.Select(s => s.Range)
					.Append(WDist.FromCells(2))
					.Max();

				self.QueueActivity(move.MoveWithinRange(Target.FromPos(frozen.CenterPosition), range));
			}

			return Target.Invalid;
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<Actor> blockers)
		{
			foreach (var blocker in blockers)
			{
				foreach (var moveBlocked in blocker.TraitsImplementing<INotifyBlockingMove>())
					moveBlocked.OnNotifyBlockingMove(blocker, self);
			}
		}

		public static void NotifyBlocker(this Actor self, CPos position)
		{
			NotifyBlocker(self, self.World.ActorMap.GetActorsAt(position));
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<CPos> positions)
		{
			NotifyBlocker(self, positions.SelectMany(p => self.World.ActorMap.GetActorsAt(p)));
		}

		public static CPos ClosestCell(this Actor self, IEnumerable<CPos> cells)
		{
			return cells.MinByOrDefault(c => (self.Location - c).LengthSquared);
		}
	}
}
