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

using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public static class TargetExtensions
	{
		/// <summary>
		/// Update (Frozen)Actor targets to account for visibility changes or actor replacement.
		/// If the target actor becomes hidden without a FrozenActor, the target is invalidated.
		/// /// </summary>
		public static Target RecalculateInvalidatingHiddenTargets(this Target t, Player viewer)
		{
			var updated = t.Recalculate(viewer, out var targetIsHiddenActor);
			return targetIsHiddenActor ? Target.Invalid : updated;
		}

		/// <summary>
		/// Update (Frozen)Actor targets to account for visibility changes or actor replacement.
		/// If the target actor becomes hidden without a FrozenActor, the target actor is kept
		/// and the actorHidden flag is set to true.
		/// </summary>
		public static Target Recalculate(this Target t, Player viewer, out bool targetIsHiddenActor)
		{
			targetIsHiddenActor = false;

			// Check whether the target has transformed into something else
			// HACK: This relies on knowing the internal implementation details of Target
			if (t.Type == TargetType.Invalid && t.Actor != null && t.Actor.ReplacedByActor != null)
				t = Target.FromActor(t.Actor.ReplacedByActor);

			// Bot-controlled units aren't yet capable of understanding visibility changes
			if (viewer.IsBot)
			{
				// Prevent that bot-controlled units endlessly fire at frozen actors.
				// TODO: Teach the AI to support long range artillery units with units that provide line of sight
				if (t.Type == TargetType.FrozenActor)
				{
					if (t.FrozenActor.Actor != null)
						return Target.FromActor(t.FrozenActor.Actor);

					// Original actor was killed
					return Target.Invalid;
				}

				return t;
			}

			if (t.Type == TargetType.Actor)
			{
				// Actor has been hidden under the fog
				if (!t.Actor.CanBeViewedByPlayer(viewer))
				{
					// Replace with FrozenActor if applicable, otherwise return target unmodified
					var frozen = viewer.FrozenActorLayer.FromID(t.Actor.ActorID);
					if (frozen != null)
						return Target.FromFrozenActor(frozen);

					targetIsHiddenActor = true;
					return t;
				}
			}
			else if (t.Type == TargetType.FrozenActor)
			{
				// Frozen actor has been revealed
				if (!t.FrozenActor.Visible || !t.FrozenActor.IsValid)
				{
					// Original actor is still alive
					if (t.FrozenActor.Actor != null && t.FrozenActor.Actor.CanBeViewedByPlayer(viewer))
						return Target.FromActor(t.FrozenActor.Actor);

					// Original actor was killed while hidden
					return Target.Invalid;
				}
			}

			return t;
		}
	}
}
