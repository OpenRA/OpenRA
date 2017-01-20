#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	public class BulletInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new WDist(17) };

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		[SequenceReference("Image")] public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference] public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference("TrailImage")] public readonly string[] TrailSequences = { "idle" };

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new WDist(1);

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] LaunchAngle = { WAngle.Zero };

		[Desc("Up to how many times does this bullet bounce when touching ground without hitting a target.",
			"0 implies exploding on contact with the originally targeted position.")]
		public readonly int BounceCount = 0;

		[Desc("Modify distance of each bounce by this percentage of previous distance.")]
		public readonly int BounceRangeModifier = 60;

		[Desc("If projectile touches an actor with one of these stances during or after the first bounce, trigger explosion.")]
		public readonly Stance ValidBounceBlockerStances = Stance.Enemy | Stance.Neutral;

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference("TrailUsePlayerPalette")] public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		public readonly int ContrailLength = 0;
		public readonly int ContrailZOffset = 2047;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;
		public readonly WDist ContrailWidth = new WDist(64);

		[Desc("Scan radius for actors with projectile-blocking trait. If set to zero (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BlockerScanRadius = WDist.Zero;

		[Desc("Extra search radius beyond path for actors with ValidBounceBlockerStances. If set to zero (default), ",
			"it will automatically scale to the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BounceBlockerScanRadius = WDist.Zero;

		public IProjectile Create(ProjectileArgs args) { return new Bullet(this, args); }

		public void RulesetLoaded(Ruleset rules, WeaponInfo wi)
		{
			if (BlockerScanRadius == WDist.Zero)
				BlockerScanRadius = Util.MinimumRequiredBlockerScanRadius(rules);

			if (BounceBlockerScanRadius == WDist.Zero)
				BounceBlockerScanRadius = Util.MinimumRequiredVictimScanRadius(rules);
		}
	}

	public class Bullet : IProjectile, ISync
	{
		readonly BulletInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;
		[Sync] readonly WAngle angle;
		[Sync] readonly WDist speed;

		ContrailRenderable contrail;
		string trailPalette;

		[Sync] WPos pos, target, source;
		int length;
		[Sync] int facing;
		int ticks, smokeTicks;
		int remainingBounces;

		public Actor SourceActor { get { return args.SourceActor; } }

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			source = args.Source;

			var world = args.SourceActor.World;

			if (info.LaunchAngle.Length > 1)
				angle = new WAngle(world.SharedRandom.Next(info.LaunchAngle[0].Angle, info.LaunchAngle[1].Angle));
			else
				angle = info.LaunchAngle[0];

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var range = Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
				var maxOffset = inaccuracy * (target - pos).Length / range;
				target += WVec.FromPDF(world.SharedRandom, 2) * maxOffset / 1024;
			}

			if (info.AirburstAltitude > WDist.Zero)
				target += new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);

			facing = (target - pos).Yaw.Facing;
			length = Math.Max((target - pos).Length / speed.Length, 1);

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<int>(GetEffectiveFacing));
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				contrail = new ContrailRenderable(world, color, info.ContrailWidth, info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			smokeTicks = info.TrailDelay;
			remainingBounces = info.BounceCount;
		}

		int GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = (facing % 128) / 128f;
			var scale = 512 * u * (1 - u);

			return (int)(facing < 128
				? facing - scale * attitude
				: facing + scale * attitude);
		}

		public void Tick(World world)
		{
			if (anim != null)
				anim.Tick();

			var lastPos = pos;
			pos = WPos.LerpQuadratic(source, target, angle, ticks, length);

			// Check for walls or other blocking obstacles
			var shouldExplode = false;
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, lastPos, pos, info.Width,
				info.BlockerScanRadius, out blockedPos))
			{
				pos = blockedPos;
				shouldExplode = true;
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(source, target, angle, ticks - info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(delayedPos, w, info.TrailImage, info.TrailSequences.Random(world.SharedRandom),
					trailPalette, false, false, GetEffectiveFacing())));

				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			var flightLengthReached = ticks++ >= length;
			var shouldBounce = remainingBounces > 0;

			if (flightLengthReached && shouldBounce)
			{
				shouldExplode |= AnyValidTargetsInRadius(world, pos, info.Width + info.BounceBlockerScanRadius, args.SourceActor, true);
				target += (pos - source) * info.BounceRangeModifier / 100;
				var dat = world.Map.DistanceAboveTerrain(target);
				target += new WVec(0, 0, -dat.Length);
				length = Math.Max((target - pos).Length / speed.Length, 1);
				ticks = 0;
				source = pos;
				remainingBounces--;
			}

			// Flight length reached / exceeded
			shouldExplode |= flightLengthReached && !shouldBounce;

			// Driving into cell with higher height level
			shouldExplode |= world.Map.DistanceAboveTerrain(pos).Length < 0;

			// After first bounce, check for targets each tick
			if (remainingBounces < info.BounceCount)
				shouldExplode |= AnyValidTargetsInRadius(world, pos, info.Width + info.BounceBlockerScanRadius, args.SourceActor, true);

			if (shouldExplode)
				Explode(world);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (anim == null || ticks >= length)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette(info.ShadowPalette)))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}

		void Explode(World world)
		{
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
		}

		bool AnyValidTargetsInRadius(World world, WPos pos, WDist radius, Actor firedBy, bool checkTargetType)
		{
			foreach (var victim in world.FindActorsInCircle(pos, radius))
			{
				if (checkTargetType && !Target.FromActor(victim).IsValidFor(firedBy))
					continue;

				if (!info.ValidBounceBlockerStances.HasStance(victim.Owner.Stances[firedBy.Owner]))
					continue;

				var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo == null)
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				if (healthInfo.Shape.DistanceFromEdge(pos, victim).Length <= 0)
					return true;
			}

			return false;
		}
	}
}
