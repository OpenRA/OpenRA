#region Copyright & License Information
/*
 * Modded from LaserZap by Boolbada of OP Mod
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Yupgi_alert.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Projectiles
{
	[Desc("Railgun effect, Tiberium Wars style")]
	public class RailgunInfo : IProjectileInfo
	{
		[Desc("The thickness of the main beam. (WDist)")]
		public readonly int BeamThickness = 64;

		[Desc("Height of one complete helix turn, measured parallel to the axis of the helix (WDist)")]
		public readonly int HelixThichkess = 64;

		[Desc("The radius of the spiral effect. (WDist)")]
		public readonly int HelixRadius = 64;

		[Desc("Height of one complete helix turn, measured parallel to the axis of the helix (WDist)")]
		public readonly int HelixPitch = 512;

		[Desc("Helix radius gets + this value per tick during drawing")]
		public readonly int HelixRadiusDeltaPerTick = 4;

		[Desc("Helix alpha gets + this value per tick during drawing; hence negative value makes it fade over time.")]
		public readonly int HelixAlphaDeltaPerTick = -8;

		[Desc("Helix spins by this much over time each tick.")]
		public readonly int HelixAngleDeltaPerTick = 16;

		[Desc("Draw each cycle with this many quantization steps")]
		public readonly int QuantizationCount = 16;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Duration of the beam.")]
		public readonly int BeamDuration = 15;

		[Desc("Helix color in (A),R,G,B.")]
		public readonly Color HelixColor = Color.FromArgb(64, 255, 255, 255);
		public readonly bool HelixPlayerColor = false;

		[Desc("Beam color in (A),R,G,B.")]
		public readonly Color BeamColor = Color.FromArgb(128, 255, 255, 255);
		public readonly bool BeamPlayerColor = false;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference("HitAnim")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			var bc = BeamPlayerColor ? Color.FromArgb(128, args.SourceActor.Owner.Color.RGB) : BeamColor;
			var hc = HelixPlayerColor ? Color.FromArgb(128, args.SourceActor.Owner.Color.RGB) : HelixColor;
			return new Railgun(args, this, bc, hc);
		}
	}

	public class Railgun : IProjectile
	{
		readonly ProjectileArgs args;
		readonly RailgunInfo info;
		readonly Animation hitanim;
		int ticks = 0;
		bool doneDamage;
		bool animationComplete;
		WPos target;

		// Lets' save computation
		public readonly Color BeamColor;
		public readonly Color HelixColor;
		public readonly WVec SourceToTarget;
		public readonly WVec ForwardStep;
		public readonly WVec RightVector;
		public readonly WVec UpVector;
		public readonly WAngle AngleStep;

		public Railgun(ProjectileArgs args, RailgunInfo info, Color beamColor, Color helixColor)
		{
			this.args = args;
			this.info = info;
			target = args.PassiveTarget;

			this.BeamColor = beamColor;
			this.HelixColor = helixColor;

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);

			AngleStep = new WAngle(1024 / info.QuantizationCount);

			SourceToTarget = target - args.Source;

			/*
			 * With the following in mind:
			 * WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
			 */

			// Forward step, pointing from src to target.
			// QuantizationCont * forwardStep == One cycle of beam in src2target direction.
			ForwardStep = (info.HelixPitch * SourceToTarget) / (info.QuantizationCount * SourceToTarget.Length);

			// Easy to find perpendicular vector to forwardStep, with 0 Z component
			RightVector = new WVec(ForwardStep.Y, -ForwardStep.X, 0);
			RightVector = 1024 * RightVector / RightVector.Length;

			// Vector that is pointing upwards from the ground
			UpVector = new WVec(
				-ForwardStep.X * ForwardStep.Z,
				-ForwardStep.Z * ForwardStep.Y,
				ForwardStep.X * ForwardStep.X + ForwardStep.Y * ForwardStep.Y);
			UpVector = 1024 * UpVector / UpVector.Length;

			//// RightVector and UpVector are unit vectors of size 1024.
		}

		public void Tick(World world)
		{
			/*
			WARNING: Railguns don't track target
			// Beam tracks target
			if (args.GuidedTarget.IsValidFor(args.SourceActor))
			target = args.GuidedTarget.CenterPosition;
			*/

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen(info.HitAnimSequence, () => animationComplete = true);
				else
					animationComplete = true;

				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				doneDamage = true;
			}

			if (hitanim != null)
				hitanim.Tick();

			if (ticks++ > info.BeamDuration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(args.Source))
				yield break;

			if (ticks < info.BeamDuration)
			{
				yield return new RailgunRenderable(args.Source, info.ZOffset, this, info, ticks);
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
