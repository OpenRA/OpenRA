#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return true;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.EffectiveOwner.Owner] == Stance.Ally;

			return stance == Stance.Ally;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.HasTrait<IgnoresDisguise>())
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
				var range = sight != null ? sight.Range : WRange.FromCells(2);

				self.QueueActivity(move.MoveWithinRange(Target.FromPos(frozen.CenterPosition), range));
			}

			return Target.Invalid;
		}

		public static void PlayVoice(this Actor self, Actor actor, string phrase, string variant)
		{
			foreach (var voiced in self.TraitsImplementing<IVoiced>())
			{
				if (phrase == null)
					return;

				if (string.IsNullOrEmpty(voiced.VoiceSet))
					return;

				voiced.PlayVoice(self, phrase, variant);
			}
		}

		public static void PlayVoiceLocal(this Actor self, Actor actor, string phrase, string variant, float volume)
		{
			foreach (var voiced in self.TraitsImplementing<IVoiced>())
			{
				if (phrase == null)
					return;

				if (string.IsNullOrEmpty(voiced.VoiceSet))
					return;

				voiced.PlayVoiceLocal(self, phrase, variant, volume);
			}
		}

		public static bool HasVoice(this Actor self, string voice)
		{
			return self.TraitsImplementing<IVoiced>().Any(x => x.HasVoice(self, voice));
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
			NotifyBlocker(self, self.World.ActorMap.GetUnitsAt(position));
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<CPos> positions)
		{
			NotifyBlocker(self, positions.SelectMany(p => self.World.ActorMap.GetUnitsAt(p)));
		}
	}
}
