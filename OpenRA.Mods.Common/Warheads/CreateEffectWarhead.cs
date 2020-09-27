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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class CreateEffectWarhead : Warhead
	{
		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("List of explosion sequences that can be used.")]
		public readonly string[] Explosions = new string[0];

		[Desc("Image containing explosion effect sequence.")]
		public readonly string Image = "explosion";

		[PaletteReference(nameof(UsePlayerPalette))]
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

		[Desc("Whether to consider actors in determining whether the explosion should happen. If false, only terrain will be considered.")]
		public readonly bool ImpactActors = true;

		[Desc("The maximum inaccuracy of the effect spawn position relative to actual impact position.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		static readonly BitSet<TargetableType> TargetTypeAir = new BitSet<TargetableType>("Air");

		/// <summary>Checks if there are any actors at impact position and if the warhead is valid against any of them.</summary>
		ImpactActorType ActorTypeAtImpact(World world, WPos pos, Actor firedBy)
		{
			var anyInvalidActor = false;

			// Check whether the impact position overlaps with an actor's hitshape
			foreach (var victim in world.FindActorsOnCircle(pos, WDist.Zero))
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

		// ActorTypeAtImpact already checks AffectsParent beforehand, to avoid parent HitShape look-ups
		// (and to prevent returning ImpactActorType.Invalid on AffectsParent=false)
		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			var stance = firedBy.Owner.RelationshipWith(victim.Owner);
			if (!ValidRelationships.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			if (!IsValidTarget(victim.GetEnabledTargetTypes()))
				return false;

			return true;
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var pos = target.CenterPosition;
			var world = firedBy.World;
			var actorAtImpact = ImpactActors ? ActorTypeAtImpact(world, pos, firedBy) : ImpactActorType.None;

			// Ignore the impact if there are only invalid actors within range
			if (actorAtImpact == ImpactActorType.Invalid)
				return;

			// Ignore the impact if there are no valid actors and no valid terrain
			// (impacts are allowed on valid actors sitting on invalid terrain!)
			if (actorAtImpact == ImpactActorType.None && !IsValidAgainstTerrain(world, pos))
				return;

			var explosion = Explosions.RandomOrDefault(world.LocalRandom);
			if (Image != null && explosion != null)
			{
				if (Inaccuracy.Length > 0)
					pos += WVec.FromPDF(world.SharedRandom, 2) * Inaccuracy.Length / 1024;

				if (ForceDisplayAtGroundLevel)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					pos -= new WVec(0, 0, dat.Length);
				}

				var palette = ExplosionPalette;
				if (UsePlayerPalette)
					palette += firedBy.Owner.InternalName;

				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, Image, explosion, palette)));
			}

			var impactSound = ImpactSounds.RandomOrDefault(world.LocalRandom);
			if (impactSound != null && world.LocalRandom.Next(0, 100) < ImpactSoundChance)
				Game.Sound.Play(SoundType.World, impactSound, pos);
		}

		/// <summary>Checks if the warhead is valid against the terrain at impact position.</summary>
		bool IsValidAgainstTerrain(World world, WPos pos)
		{
			var cell = world.Map.CellContaining(pos);
			if (!world.Map.Contains(cell))
				return false;

			var dat = world.Map.DistanceAboveTerrain(pos);
			return IsValidTarget(dat > AirThreshold ? TargetTypeAir : world.Map.GetTerrainInfo(cell).TargetTypes);
		}
	}
}
