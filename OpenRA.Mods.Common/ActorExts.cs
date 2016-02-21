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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public static class ActorExts
	{
		public static bool IsAtGroundLevel(this Actor self)
		{
			if (self.IsDead)
				return false;

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

			return stance == Stance.Ally;
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

		public static Target ResolveFrozenActorOrder(this Actor self, Order order, Color targetLine)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0)
				return Target.FromOrder(self.World, order);

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
				var range = sight != null ? sight.Range : WDist.FromCells(2);

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

		public static bool CanHarvestAt(this Actor self, CPos pos, ResourceLayer resLayer, HarvesterInfo harvInfo,
			ResourceClaimLayer territory)
		{
			var resType = resLayer.GetResource(pos);
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			if (!harvInfo.Resources.Contains(resType.Info.Name))
				return false;

			if (territory != null)
			{
				// Another harvester has claimed this resource:
				ResourceClaim claim;
				if (territory.IsClaimedByAnyoneElse(self as Actor, pos, out claim))
					return false;
			}

			return true;
		}

		public static CPos ClosestCell(this Actor self, IEnumerable<CPos> cells)
		{
			return cells.MinByOrDefault(c => (self.Location - c).LengthSquared);
		}
	}
}
