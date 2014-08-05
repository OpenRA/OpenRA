#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	[Desc("Base warhead class. This can be used to derive other warheads from.")]
	public abstract class Warhead
	{
		[Desc("What types of targets are affected.")]
		public readonly string[] ValidTargets = { "Ground", "Water" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly string[] InvalidTargets = { };

		[Desc("Delay in ticks before applying the warhead effect.","0 = instant (old model).")]
		public readonly int Delay = 0;

		public abstract void DoImpact(Target target, Actor firedBy, float firepowerModifier);

		public abstract float EffectivenessAgainst(ActorInfo ai);

		public bool IsValidAgainst(Target target, World world, Actor firedBy)
		{
			if (target.Type == TargetType.Actor)
				return IsValidAgainst(target.Actor, firedBy);

			if (target.Type == TargetType.FrozenActor)
				return IsValidAgainst(target.FrozenActor, firedBy);

			if (target.Type == TargetType.Terrain)
			{
				var cell = world.Map.CellContaining(target.CenterPosition);
				if (!world.Map.Contains(cell))
					return false;

				var cellInfo = world.Map.GetTerrainInfo(cell);
				if (!ValidTargets.Intersect(cellInfo.TargetTypes).Any()
					|| InvalidTargets.Intersect(cellInfo.TargetTypes).Any())
					return false;

				return true;
			}

			return false;
		}

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			return InTargetList(victim, firedBy, ValidTargets) &&
				!InTargetList(victim, firedBy, InvalidTargets);
		}

		public static bool InTargetList(Actor victim, Actor firedBy, string[] targetList)
		{
			if (!targetList.Any())
				return false;

			var targetable = victim.TraitOrDefault<ITargetable>();
			if (targetable == null)
				return false;
			if (!targetList.Intersect(targetable.TargetTypes).Any())
				return false;

			return true;
		}

		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			return InTargetList(victim, firedBy, ValidTargets) &&
				!InTargetList(victim, firedBy, InvalidTargets);
		}

		public static bool InTargetList(FrozenActor victim, Actor firedBy, string[] targetList)
		{
			// Frozen Actors need to be handled slightly differently. Since FrozenActor.Actor can be null if the Actor is dead.
			if (!targetList.Any())
				return false;

			var targetable = victim.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null)
				return false;
			if (!targetList.Intersect(targetable.GetTargetTypes()).Any())
				return false;
			
			return true;
		}
   }
}
