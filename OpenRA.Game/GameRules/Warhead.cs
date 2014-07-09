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
		
		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;
		
		[Desc("Can this warhead affect the actor that fired it.")]
		public readonly bool AffectsParent = true;

		[Desc("Delay in ticks before applying the warhead effect.","0 = instant (old model).")]
		public readonly int Delay = 0;

		///<summary>Applies the warhead's effect against the target.</summary>
		public abstract void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers);

		///<summary>Checks if the warhead is valid against (can do something to) the target.</summary>
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

		// TODO: This can be removed after the legacy and redundant 0% = not targetable
		// assumption has been removed from the yaml definitions
		public virtual bool CanTargetActor(ActorInfo victim, Actor firedBy) { return false; }

		///<summary>Checks if the warhead is valid against (can do something to) the actor.</summary>
		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!CanTargetActor(victim.Info, firedBy))
				return false;

			if (!AffectsParent && victim == firedBy)
				return false;

			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasFlag(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			var targetable = victim.TraitOrDefault<ITargetable>();
			if (targetable == null || !ValidTargets.Intersect(targetable.TargetTypes).Any()
				|| InvalidTargets.Intersect(targetable.TargetTypes).Any())
				return false;

			return true;
		}

		///<summary>Checks if the warhead is valid against (can do something to) the frozen actor.</summary>
		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			if (!CanTargetActor(victim.Info, firedBy))
				return false;

			// AffectsParent checks do not make sense for FrozenActors, so skip to stance checks
			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasFlag(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			var targetable = victim.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null || !ValidTargets.Intersect(targetable.GetTargetTypes()).Any()
				|| InvalidTargets.Intersect(targetable.GetTargetTypes()).Any())
				return false;

			return true;
		}
	}
}
