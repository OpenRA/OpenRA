#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Laser effect with helix coiling made of sprite animations around.")]
	public class SpriteRailgunInfo : IProjectileInfo
	{
		[Desc("Damage all units hit by the beam instead of just the target?")]
		public readonly bool DamageActorsInLine = false;

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Can this projectile be blocked when hitting actors with an IBlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Duration of the beam.")]
		public readonly int Duration = 15;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The width of the main trajectory. (\"beam\").")]
		public readonly WDist BeamWidth = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape BeamShape = BeamRenderableShape.Cylindrical;

		[Desc("Beam color in (A),R,G,B.")]
		public readonly Color BeamColor = Color.FromArgb(128, 255, 255, 255);

		[Desc("When true, this will override BeamColor parameter and draw the laser with player color."
			+ " (Still uses BeamColor's alpha information)")]
		public readonly bool BeamPlayerColor = false;

		[Desc("Beam alpha gets + this value per tick during drawing; hence negative value makes it fade over time.")]
		public readonly int BeamAlphaDeltaPerTick = -8;

		[Desc("The radius of the spiral effect. (WDist)")]
		public readonly WDist HelixRadius = new WDist(64);

		[Desc("Height of one complete helix turn, measured parallel to the axis of the helix (WDist)")]
		public readonly WDist HelixPitch = new WDist(512);

		[Desc("Draw each cycle of helix with this many quantization steps")]
		public readonly int QuantizationCount = 16;

		[Desc("Helix animation.")]
		public readonly string HelixAnim = null;

		[Desc("Sequence of helix animation to use.")]
		[SequenceReference("HelixAnim")]
		public readonly string HelixAnimSequence = "idle";

		[PaletteReference]
		public readonly string HelixAnimPalette = "effect";

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference("HitAnim")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		[Desc("Scan radius for actors damaged by beam. If set to zero (default), it will automatically scale to the largest health shape.",
			"Only set custom values if you know what you're doing.")]
		public WDist AreaVictimScanRadius = WDist.Zero;

		[Desc("Scan radius for actors with projectile-blocking trait. If set to zero (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BlockerScanRadius = WDist.Zero;

		public IProjectile Create(ProjectileArgs args)
		{
			var bc = BeamPlayerColor ? Color.FromArgb(BeamColor.A, args.SourceActor.Owner.Color.RGB) : BeamColor;
			return new SpriteRailgun(args, this, bc);
		}

		public void RulesetLoaded(Ruleset rules, WeaponInfo wi)
		{
			if (BlockerScanRadius == WDist.Zero)
				BlockerScanRadius = OpenRA.Mods.Common.Util.MinimumRequiredBlockerScanRadius(rules);

			if (AreaVictimScanRadius == WDist.Zero)
				AreaVictimScanRadius = OpenRA.Mods.Common.Util.MinimumRequiredVictimScanRadius(rules);
		}
	}

	public class SpriteRailgun : IProjectile
	{
		readonly ProjectileArgs args;
		readonly SpriteRailgunInfo info;
		readonly Animation hitanim;
		public readonly Color BeamColor;

		int ticks = 0;
		bool animationComplete;
		WPos target;

		int cycleCount;
		WVec forwardStep;
		WVec leftVector;
		WVec upVector;
		WAngle angleStep;

		public SpriteRailgun(ProjectileArgs args, SpriteRailgunInfo info, Color beamColor)
		{
			this.args = args;
			this.info = info;
			target = args.PassiveTarget;

			this.BeamColor = beamColor;

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);

			CalculateVectors();

			var pos = args.Source;
			var angle = WAngle.Zero;
			for (var i = cycleCount * info.QuantizationCount - 1; i >= 0; i--)
			{
				// Make it narrower near the end.
				var rad = i < info.QuantizationCount ? info.HelixRadius / 4 :
					i < 2 * info.QuantizationCount ? info.HelixRadius / 2 :
					info.HelixRadius;

				// Note: WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
				var offset = rad.Length * angle.Cos() * leftVector / (1024 * 1024)
					+ rad.Length * angle.Sin() * upVector / (1024 * 1024);
				var animpos = pos + offset;
				args.SourceActor.World.AddFrameEndTask(w => w.Add(new SpriteEffect(animpos, w, info.HelixAnim,
					info.HelixAnimSequence, info.HelixAnimPalette, facing: angle.Facing)));

				pos += forwardStep;
				angle += angleStep;
			}
		}

		void CalculateVectors()
		{
			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(args.SourceActor.World, target, args.Source,
					info.BeamWidth, info.BlockerScanRadius, out blockedPos))
				target = blockedPos;

			// Note: WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
			angleStep = new WAngle(1024 / info.QuantizationCount);

			var sourceToTarget = target - args.Source;

			// Forward step, pointing from src to target.
			// QuantizationCont * forwardStep == One cycle of beam in src2target direction.
			forwardStep = (info.HelixPitch.Length * sourceToTarget) / (info.QuantizationCount * sourceToTarget.Length);

			if (forwardStep == WVec.Zero)
				return;

			// An easy vector to find which is perpendicular vector to forwardStep, with 0 Z component
			leftVector = new WVec(forwardStep.Y, -forwardStep.X, 0);
			leftVector = 1024 * leftVector / leftVector.Length;

			// Vector that is pointing upwards from the ground
			upVector = new WVec(
				-forwardStep.X * forwardStep.Z,
				-forwardStep.Z * forwardStep.Y,
				forwardStep.X * forwardStep.X + forwardStep.Y * forwardStep.Y);
			upVector = 1024 * upVector / upVector.Length;

			//// LeftVector and UpVector are unit vectors of size 1024.

			cycleCount = sourceToTarget.Length / info.HelixPitch.Length;
			if (sourceToTarget.Length % info.HelixPitch.Length != 0)
				cycleCount += 1; // math.ceil, int version.
		}

		public void Tick(World world)
		{
			if (ticks == 0)
			{
				if (hitanim != null)
					hitanim.PlayThen(info.HitAnimSequence, () => animationComplete = true);
				else
					animationComplete = true;

				if (!info.DamageActorsInLine)
					args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				else
				{
					var actors = world.FindActorsOnLine(args.Source, target, info.BeamWidth, info.AreaVictimScanRadius);
					foreach (var a in actors)
						args.Weapon.Impact(Target.FromActor(a), args.SourceActor, args.DamageModifiers);
				}
			}

			if (hitanim != null)
				hitanim.Tick();

			if (ticks++ > info.Duration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(args.Source))
				yield break;

			if (info.BeamWidth != WDist.Zero && ticks < info.Duration)
			{
				yield return new BeamRenderable(args.Source, info.ZOffset, args.PassiveTarget - args.Source, info.BeamShape, info.BeamWidth,
					Color.FromArgb(BeamColor.A + info.BeamAlphaDeltaPerTick * ticks, BeamColor));
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
