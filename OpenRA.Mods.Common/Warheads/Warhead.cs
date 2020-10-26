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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public enum ImpactActorType
	{
		None,
		Invalid,
		Valid,
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

		static readonly BitSet<TargetableType> TargetTypeAir = new BitSet<TargetableType>("Air");

		[Desc("Whether to consider actors in determining whether the explosion should happen. If false, only terrain will be considered.")]
		public readonly bool ImpactActors = true;

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

		/// <summary>Checks if there are any actors at impact position and if the warhead is valid against any of them.</summary>
		protected ImpactActorType ActorTypeAtImpact(World world, WPos pos, Actor firedBy)
		{
			var anyInvalidActor = false;

			// Check whether the impact position overlaps with an actor's hitshape
			var potentialVictims = world.FindActorsOnCircle(pos, WDist.Zero);
			foreach (var victim in potentialVictims)
			{
				if (!AffectsParent && victim == firedBy)
					continue;

				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any(s => s.DistanceFromEdge(victim, pos).Length <= 0))
					continue;

				if (IsValidAgainst(victim, firedBy))
					return ImpactActorType.Valid;

				anyInvalidActor = true;
			}

			return anyInvalidActor ? ImpactActorType.Invalid : ImpactActorType.None;
		}

		/// <summary>Checks if the warhead is valid against the terrain at impact position.</summary>
		protected bool IsValidAgainstTerrain(World world, WPos pos)
		{
			var cell = world.Map.CellContaining(pos);
			if (!world.Map.Contains(cell))
				return false;

			var dat = world.Map.DistanceAboveTerrain(pos);
			return IsValidTarget(dat > AirThreshold ? TargetTypeAir : world.Map.GetTerrainInfo(cell).TargetTypes);
		}
	}
}
