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

using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Trigger a flash effect on the targeted actor, or actors within a circle.")]
	public class FlashTargetsInRadiusWarhead : Warhead
	{
		[Desc("The overlay color to display when ActorFlashType is Overlay.")]
		public readonly Color ActorFlashOverlayColor = Color.White;

		[Desc("The overlay transparency to display when ActorFlashType is Overlay.")]
		public readonly float ActorFlashOverlayAlpha = 0.5f;

		[Desc("The tint to apply when ActorFlashType is Tint.")]
		public readonly float3 ActorFlashTint = new(1.4f, 1.4f, 1.4f);

		[Desc("Number of times to flash actors.")]
		public readonly int ActorFlashCount = 2;

		[Desc("Number of ticks between actor flashes.")]
		public readonly int ActorFlashInterval = 2;

		[Desc("Radius of an area at which effect will be applied. If left default effect applies only to target actor.")]
		public readonly WDist Radius = new(0);

		[Desc("Controls the way damage is calculated. Possible values are 'HitShape', 'ClosestTargetablePosition' and 'CenterPosition'.")]
		public readonly DamageCalculationType DamageCalculationType = DamageCalculationType.HitShape;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var targetActor = target.Actor;
			var firedBy = args.SourceActor;
			var victims = Radius == WDist.Zero && targetActor != null ? [targetActor] : firedBy.World.FindActorsInCircle(target.CenterPosition, Radius);

			foreach (var victim in victims)
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				victim.World.AddFrameEndTask(w => w.Add(new FlashTarget(
									victim, ActorFlashOverlayColor, ActorFlashOverlayAlpha,
									ActorFlashCount, ActorFlashInterval)));
			}
		}
	}
}
