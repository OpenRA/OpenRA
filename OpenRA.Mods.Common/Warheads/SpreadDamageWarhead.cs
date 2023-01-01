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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public enum DamageCalculationType { HitShape, ClosestTargetablePosition, CenterPosition }

	[Desc("Apply damage in a specified range.")]
	public class SpreadDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps.")]
		public readonly WDist Spread = new WDist(43);

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public readonly WDist[] Range = null;

		[Desc("Controls the way damage is calculated. Possible values are 'HitShape', 'ClosestTargetablePosition' and 'CenterPosition'.")]
		public readonly DamageCalculationType DamageCalculationType = DamageCalculationType.HitShape;

		WDist[] effectiveRange;

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");

				effectiveRange = Range;
			}
			else
				effectiveRange = Exts.MakeArray(Falloff.Length, i => i * Spread);
		}

		protected override void DoImpact(WPos pos, Actor firedBy, WarheadArgs args)
		{
			var debugVis = firedBy.World.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
				firedBy.World.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, effectiveRange, DebugOverlayColor);

			foreach (var victim in firedBy.World.FindActorsOnCircle(pos, effectiveRange[effectiveRange.Length - 1]))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				HitShape closestActiveShape = null;
				var closestDistance = int.MaxValue;

				// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
				foreach (var targetPos in victim.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						var distance = h.DistanceFromEdge(victim, pos).Length;
						if (distance < closestDistance)
						{
							closestDistance = distance;
							closestActiveShape = h;
						}
					}
				}

				// Cannot be damaged without an active HitShape.
				if (closestActiveShape == null)
					continue;

				var falloffDistance = 0;
				switch (DamageCalculationType)
				{
					case DamageCalculationType.HitShape:
						falloffDistance = closestDistance;
						break;
					case DamageCalculationType.ClosestTargetablePosition:
						falloffDistance = victim.GetTargetablePositions().Select(x => (x - pos).Length).Min();
						break;
					case DamageCalculationType.CenterPosition:
						falloffDistance = (victim.CenterPosition - pos).Length;
						break;
				}

				// The range to target is more than the range the warhead covers, so GetDamageFalloff() is going to give us 0 and we're going to do 0 damage anyway, so bail early.
				if (falloffDistance > effectiveRange[effectiveRange.Length - 1].Length)
					continue;

				var localModifiers = args.DamageModifiers.Append(GetDamageFalloff(falloffDistance));
				var impactOrientation = args.ImpactOrientation;

				// If a warhead lands outside the victim's HitShape, we need to calculate the vertical and horizontal impact angles
				// from impact position, rather than last projectile facing/angle.
				if (falloffDistance > 0)
				{
					var towardsTargetYaw = (victim.CenterPosition - args.ImpactPosition).Yaw;
					var impactAngle = Util.GetVerticalAngle(args.ImpactPosition, victim.CenterPosition);
					impactOrientation = new WRot(WAngle.Zero, impactAngle, towardsTargetYaw);
				}

				var updatedWarheadArgs = new WarheadArgs(args)
				{
					DamageModifiers = localModifiers.ToArray(),
					ImpactOrientation = impactOrientation,
				};

				InflictDamage(victim, firedBy, closestActiveShape, updatedWarheadArgs);
			}
		}

		int GetDamageFalloff(int distance)
		{
			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
