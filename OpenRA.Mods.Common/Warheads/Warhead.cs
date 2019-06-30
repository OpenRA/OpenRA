#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Flags]
	public enum ImpactType
	{
		None = 0,
		Terrain = 1,
		Actor = 2,
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

		[Desc("Warheads detonating above this altitude that don't hit an actor directly will check target validity against the 'TargetTypeAir' target types.")]
		public readonly WDist AirThreshold = new WDist(128);

		[Desc("Target types to use when the warhead detonated at an altitude greater than 'AirThreshold'.")]
		static readonly BitSet<TargetableType> TargetTypeAir = new BitSet<TargetableType>("Air");

		[Desc("On which impact types the warhead is allowed to trigger (if there's a valid target).",
			"Current options are Terrain and Actor. If only Terrain is listed, warhead will never trigger on actors.",
			"If only Actor is listed, the warhead will never trigger on empty terrain.",
			"If both are listed, the warhead will trigger if either any touched actor or the underlying terrain has a valid target type.")]
		public readonly ImpactType ValidImpactTypes = ImpactType.Terrain | ImpactType.Actor;

		[Desc("Can this warhead affect the actor that fired it.")]
		public readonly bool AffectsParent = false;

		[Desc("Delay in ticks before applying the warhead effect.", "0 = instant (old model).")]
		public readonly int Delay = 0;

		int IWarhead.Delay { get { return Delay; } }

		[Desc("The color used for this warhead's visualization in the world's `WarheadDebugOverlay` trait.")]
		public readonly Color DebugOverlayColor = Color.Red;

		public bool IsValidTarget(BitSet<TargetableType> targetTypes)
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

		/// <summary>Checks if the warhead is valid against any actor or the terrain at impact position.</summary>
		public virtual bool IsValidImpact(World world, WPos pos, Actor firedBy)
		{
			if (ValidImpactTypes == ImpactType.None)
				return false;

			var targetTile = world.Map.CellContaining(pos);
			if (!world.Map.Contains(targetTile))
				return false;

			// Check whether the impact position overlaps with an actor's hitshape
			var potentialVictims = world.FindActorsOnCircle(pos, WDist.Zero);
			foreach (var victim in potentialVictims)
			{
				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any(i => i.Info.Type.DistanceFromEdge(pos, victim).Length <= 0))
					continue;

				// If we got here, the impact touches an actors' hitshape.
				// If Actor is not a valid ImpactType, we return 'false' immediately regardless of terrain.
				if (!ValidImpactTypes.HasFlag(ImpactType.Actor))
					return false;
				else if (IsValidAgainst(victim, firedBy))
					return true;
			}

			if (!ValidImpactTypes.HasFlag(ImpactType.Terrain))
				return false;

			var dat = world.Map.DistanceAboveTerrain(pos);
			var tileInfo = world.Map.GetTerrainInfo(targetTile);
			return IsValidTarget(dat > AirThreshold ? TargetTypeAir : tileInfo.TargetTypes);
		}
	}
}
