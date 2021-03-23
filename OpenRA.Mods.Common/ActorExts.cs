#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
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
			var stance = toActor.Owner.RelationshipWith(self.Owner);
			if (stance == PlayerRelationship.Ally)
				return true;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.RelationshipWith(self.EffectiveOwner.Owner) == PlayerRelationship.Ally;

			return false;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.RelationshipWith(self.Owner);
			if (stance == PlayerRelationship.Ally)
				return false;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.RelationshipWith(self.EffectiveOwner.Owner) == PlayerRelationship.Enemy;

			return stance == PlayerRelationship.Enemy;
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<Actor> blockers)
		{
			foreach (var blocker in blockers)
				foreach (var moveBlocked in blocker.TraitsImplementing<INotifyBlockingMove>())
					moveBlocked.OnNotifyBlockingMove(blocker, self);
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
