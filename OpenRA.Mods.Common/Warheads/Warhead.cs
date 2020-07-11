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

using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public enum ImpactType
	{
		None,
		Ground,
		Air,
		TargetHit
	}

	public enum ImpactTargetType
	{
		NoActor,
		ValidActor,
		InvalidActor
	}

	[Desc("Base warhead class. This can be used to derive other warheads from.")]
	public abstract class Warhead : IWarhead
	{
		[Desc("What types of targets are affected.")]
		public readonly BitSet<TargetableType> ValidTargets = new BitSet<TargetableType>("Ground", "Water");

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly BitSet<TargetableType> InvalidTargets;

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Can this warhead affect the actor that fired it.")]
		public readonly bool AffectsParent = false;

		[Desc("If impact is above this altitude, warheads that would affect terrain ignore terrain target types (and either do nothing or perform their own checks).")]
		public readonly WDist AirThreshold = new WDist(128);

		[Desc("Delay in ticks before applying the warhead effect.", "0 = instant (old model).")]
		public readonly int Delay = 0;

		int IWarhead.Delay { get { return Delay; } }

		[Desc("The color used for this warhead's visualization in the world's `WarheadDebugOverlay` trait.")]
		public readonly Color DebugOverlayColor = Color.Red;

		protected bool IsValidTarget(BitSet<TargetableType> targetTypes)
		{
			return ValidTargets.Overlaps(targetTypes) && !InvalidTargets.Overlaps(targetTypes);
		}

		/// <summary>Applies the warhead's effect against the target.</summary>
		public abstract void DoImpact(Target target, WarheadArgs args);

		/// <summary>Checks if the warhead is valid against (can do something to) the actor.</summary>
		public virtual bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!AffectsParent && victim == firedBy)
				return false;

			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			if (!IsValidTarget(victim.GetEnabledTargetTypes()))
				return false;

			return true;
		}

		/// <summary>Checks if the warhead is valid against (can do something to) the frozen actor.</summary>
		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			if (!victim.IsValid)
				return false;

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
