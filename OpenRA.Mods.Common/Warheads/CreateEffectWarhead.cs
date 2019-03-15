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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class CreateEffectWarhead : Warhead
	{
		[SequenceReference("Image")]
		[Desc("List of explosion sequences that can be used.")]
		public readonly string[] Explosions = new string[0];

		[Desc("Image containing explosion effect sequence.")]
		public readonly string Image = "explosion";

		[PaletteReference("UsePlayerPalette")]
		[Desc("Palette to use for explosion effect.")]
		public readonly string ExplosionPalette = "effect";

		[Desc("Remap explosion effect to player color, if art supports it.")]
		public readonly bool UsePlayerPalette = false;

		[Desc("Display explosion effect at ground level, regardless of explosion altitude.")]
		public readonly bool ForceDisplayAtGroundLevel = false;

		[Desc("List of sounds that can be played on impact.")]
		public readonly string[] ImpactSounds = new string[0];

		[Desc("Chance of impact sound to play.")]
		public readonly int ImpactSoundChance = 100;

		[Desc("Explosions above this altitude that don't impact an actor will check target validity against the 'TargetTypeAir' target types.")]
		public readonly WDist AirThreshold = new WDist(128);

		[Desc("Target types to use when the warhead detonated at an altitude greater than 'AirThreshold'.")]
		static readonly BitSet<TargetableType> TargetTypeAir = new BitSet<TargetableType>("Air");

		[Desc("Check for direct hits against nearby actors for use in the target validity checks.")]
		public readonly bool ImpactActors = true;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			var isValid = IsValidImpact(pos, firedBy);

			if ((!world.Map.Contains(targetTile)) || (!isValid))
				return;

			var palette = ExplosionPalette;
			if (UsePlayerPalette)
				palette += firedBy.Owner.InternalName;

			var explosion = Explosions.RandomOrDefault(world.LocalRandom);
			if (Image != null && explosion != null)
			{
				var dat = world.Map.DistanceAboveTerrain(pos);
				if (ForceDisplayAtGroundLevel)
					pos -= new WVec(0, 0, dat.Length);

				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, Image, explosion, palette)));
			}

			var impactSound = ImpactSounds.RandomOrDefault(world.LocalRandom);
			if (impactSound != null && world.LocalRandom.Next(0, 100) < ImpactSoundChance)
				Game.Sound.Play(SoundType.World, impactSound, pos);
		}

		public bool IsValidImpact(WPos pos, Actor firedBy)
		{
			var world = firedBy.World;

			if (ImpactActors)
			{
				// Check whether the explosion overlaps with an actor's hitshape
				var potentialVictims = world.FindActorsOnCircle(pos, WDist.Zero);
				foreach (var victim in potentialVictims)
				{
					if (!AffectsParent && victim == firedBy)
						continue;

					var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
					if (!activeShapes.Any(i => i.Info.Type.DistanceFromEdge(pos, victim).Length <= 0))
						continue;

					if (IsValidAgainst(victim, firedBy))
						return true;
				}
			}

			var targetTile = world.Map.CellContaining(pos);
			if (!world.Map.Contains(targetTile))
				return false;

			var dat = world.Map.DistanceAboveTerrain(pos);
			var tileInfo = world.Map.GetTerrainInfo(targetTile);
			return IsValidTarget(dat > AirThreshold ? TargetTypeAir : tileInfo.TargetTypes);
		}
	}
}
