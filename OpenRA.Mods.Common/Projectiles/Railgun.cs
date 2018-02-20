#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Laser effect with helix coiling around.")]
	public class RailgunInfo : IProjectileInfo
	{
		[Desc("Damage all units hit by the beam instead of just the target?")]
		public readonly bool DamageActorsInLine = false;

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Can this projectile be blocked when hitting actors with an IBlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Duration of the beam and helix")]
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

		[Desc("Thickness of the helix")]
		public readonly WDist HelixThickness = new WDist(32);

		[Desc("The radius of the spiral effect. (WDist)")]
		public readonly WDist HelixRadius = new WDist(64);

		[Desc("Height of one complete helix turn, measured parallel to the axis of the helix (WDist)")]
		public readonly WDist HelixPitch = new WDist(512);

		[Desc("Helix radius gets + this value per tick during drawing")]
		public readonly int HelixRadiusDeltaPerTick = 8;

		[Desc("Helix alpha gets + this value per tick during drawing; hence negative value makes it fade over time.")]
		public readonly int HelixAlphaDeltaPerTick = -8;

		[Desc("Helix spins by this much over time each tick.")]
		public readonly WAngle HelixAngleDeltaPerTick = new WAngle(16);

		[Desc("Draw each cycle of helix with this many quantization steps")]
		public readonly int QuantizationCount = 16;

		[Desc("Helix color in (A),R,G,B.")]
		public readonly Color HelixColor = Color.FromArgb(128, 255, 255, 255);

		[Desc("Draw helix in PlayerColor? Overrides RGB part of the HelixColor. (Still uses HelixColor's alpha information)")]
		public readonly bool HelixPlayerColor = false;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference("HitAnim")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			var bc = BeamPlayerColor ? Color.FromArgb(BeamColor.A, args.SourceActor.Owner.Color.RGB) : BeamColor;
			var hc = HelixPlayerColor ? Color.FromArgb(HelixColor.A, args.SourceActor.Owner.Color.RGB) : HelixColor;
			return new Railgun(args, this, bc, hc);
		}
	}

	public class Railgun : IProjectile
	{
		readonly ProjectileArgs args;
		readonly RailgunInfo info;
		readonly Animation hitanim;
		public readonly Color BeamColor;
		public readonly Color HelixColor;

		int ticks = 0;
		bool animationComplete;
		WPos target;

		// Computing these in Railgun instead of RailgunRenderable saves Info.Duration ticks of computation.
		// Fortunately, railguns don't track the target.
		public int CycleCount { get; private set; }
		public WVec SourceToTarget { get; private set; }
		public WVec ForwardStep { get; private set; }
		public WVec LeftVector { get; private set; }
		public WVec UpVector { get; private set; }
		public WAngle AngleStep { get; private set; }

		public Railgun(ProjectileArgs args, RailgunInfo info, Color beamColor, Color helixColor)
		{
			this.args = args;
			this.info = info;
			target = args.PassiveTarget;

			this.BeamColor = beamColor;
			this.HelixColor = helixColor;

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);

			CalculateVectors();
		}

		void CalculateVectors()
		{
			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(args.SourceActor.World, target, args.Source,
					info.BeamWidth, out blockedPos))
				target = blockedPos;

			// Note: WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
			AngleStep = new WAngle(1024 / info.QuantizationCount);

			SourceToTarget = target - args.Source;

			// Forward step, pointing from src to target.
			// QuantizationCont * forwardStep == One cycle of beam in src2target direction.
			ForwardStep = (info.HelixPitch.Length * SourceToTarget) / (info.QuantizationCount * SourceToTarget.Length);

			// An easy vector to find which is perpendicular vector to forwardStep, with 0 Z component
			LeftVector = new WVec(ForwardStep.Y, -ForwardStep.X, 0);
			LeftVector = 1024 * LeftVector / LeftVector.Length;

			// Vector that is pointing upwards from the ground
			UpVector = new WVec(
				-ForwardStep.X * ForwardStep.Z,
				-ForwardStep.Z * ForwardStep.Y,
				ForwardStep.X * ForwardStep.X + ForwardStep.Y * ForwardStep.Y);
			UpVector = 1024 * UpVector / UpVector.Length;

			//// LeftVector and UpVector are unit vectors of size 1024.

			CycleCount = SourceToTarget.Length / info.HelixPitch.Length;
			if (SourceToTarget.Length % info.HelixPitch.Length != 0)
				CycleCount += 1; // math.ceil, int version.

			// Using ForwardStep * CycleCount, the helix and the main beam gets "out of sync"
			// if drawn from source to target. Instead, the main beam is drawn from source to end point of helix.
			// Trade-off between computation vs Railgun weapon range.
			// Modders must not have too large range for railgun weapons.
			SourceToTarget = info.QuantizationCount * CycleCount * ForwardStep;
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
					var actors = world.FindActorsOnLine(args.Source, target, info.BeamWidth);
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

			if (ticks < info.Duration)
			{
				yield return new RailgunHelixRenderable(args.Source, info.ZOffset, this, info, ticks);
				yield return new BeamRenderable(args.Source, info.ZOffset, SourceToTarget, info.BeamShape, info.BeamWidth,
					Color.FromArgb(BeamColor.A + info.BeamAlphaDeltaPerTick * ticks, BeamColor));
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
