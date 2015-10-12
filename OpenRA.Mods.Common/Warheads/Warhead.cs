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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Base warhead class. This can be used to derive other warheads from.")]
	public abstract class Warhead : IWarhead
	{
		[Desc("What types of targets are affected.")]
		public readonly HashSet<string> ValidTargets = new HashSet<string> { "Ground", "Water" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly HashSet<string> InvalidTargets = new HashSet<string>();

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Can this warhead affect the actor that fired it.")]
		public readonly bool AffectsParent = false;

		[Desc("Delay in ticks before applying the warhead effect.", "0 = instant (old model).")]
		public readonly int Delay = 0;
		int IWarhead.Delay { get { return Delay; } }

		public bool IsValidTarget(IEnumerable<string> targetTypes)
		{
			return ValidTargets.Overlaps(targetTypes) && !InvalidTargets.Overlaps(targetTypes);
		}

		/// <summary>Applies the warhead's effect against the target.</summary>
		public abstract void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers);

		/// <summary>Checks if the warhead is valid against (can do something to) the actor.</summary>
		public virtual bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!AffectsParent && victim == firedBy)
				return false;

			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			var targetable = victim.TraitsImplementing<ITargetable>().Where(Exts.IsTraitEnabled);
			if (!IsValidTarget(targetable.SelectMany(t => t.TargetTypes)))
				return false;

			return true;
		}

		/// <summary>Checks if the warhead is valid against (can do something to) the frozen actor.</summary>
		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			// AffectsParent checks do not make sense for FrozenActors, so skip to stance checks
			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			if (!IsValidTarget(victim.TargetTypes))
				return false;

			return true;
		}
	}
}
