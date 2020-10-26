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

		// ActorTypeAtImpact already checks AffectsParent beforehand, to avoid parent HitShape look-ups
		// (and to prevent returning ImpactActorType.Invalid on AffectsParent=false)
		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			var stance = firedBy.Owner.Stances[victim.Owner];
			if (!ValidStances.HasStance(stance))
				return false;

			// A target type is valid if it is in the valid targets list, and not in the invalid targets list.
			if (!IsValidTarget(victim.GetEnabledTargetTypes()))
				return false;

			return true;
		}

		public override void DoImpact(Target target, WarheadArgs args)
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
	}
}
