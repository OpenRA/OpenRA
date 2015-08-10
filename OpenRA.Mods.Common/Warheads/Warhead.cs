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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Base warhead class. This can be used to derive other warheads from.")]
	public abstract class Warhead : IWarhead
	{
		[Desc("What types of targets are affected.")]
		public readonly string[] ValidTargets = { "Ground", "Water" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly string[] InvalidTargets = { };

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Can this warhead affect the actor that fired it.")]
		public readonly bool AffectsParent = false;

		[Desc("Delay in ticks before applying the warhead effect.", "0 = instant (old model).")]
		public readonly int Delay = 0;
		int IWarhead.Delay { get { return Delay; } }

		HashSet<string> validTargetSet;
		HashSet<string> invalidTargetSet;

		public bool IsValidTarget(IEnumerable<string> targetTypes)
		{
			if (validTargetSet == null)
				validTargetSet = new HashSet<string>(ValidTargets);
			if (invalidTargetSet == null)
				invalidTargetSet = new HashSet<string>(InvalidTargets);
			return validTargetSet.Overlaps(targetTypes) && !invalidTargetSet.Overlaps(targetTypes);
		}

		/// <summary>Applies the warhead's effect against the target.</summary>
		public abstract void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers);

		// TODO: This can be removed after the legacy and redundant 0% = not targetable
		// assumption has been removed from the yaml definitions
		public virtual bool CanTargetActor(ActorInfo victim, Actor firedBy) { return false; }

		/// <summary>Checks if the warhead is valid against (can do something to) the actor.</summary>
		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!CanTargetActor(victim.Info, firedBy))
				return false;

			if (!AffectsParent && victim == firedBy)
				return false;

			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			var targetable = victim.TraitOrDefault<ITargetable>();
			if (targetable == null || !IsValidTarget(targetable.TargetTypes))
				return false;

			return true;
		}

		/// <summary>Checks if the warhead is valid against (can do something to) the frozen actor.</summary>
		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			if (!CanTargetActor(victim.Info, firedBy))
				return false;

			// AffectsParent checks do not make sense for FrozenActors, so skip to stance checks
			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			var targetable = victim.Info.TraitInfoOrDefault<ITargetableInfo>();
			if (targetable == null || !IsValidTarget(targetable.GetTargetTypes()))
				return false;

			return true;
		}
	}
}
