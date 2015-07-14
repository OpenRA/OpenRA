#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class MissileInfo : IProjectileInfo
	{
		[Desc("Name of the image containing the projectile sequence.")]
		public readonly string Image = null;

		[Desc("Projectile sequence name.")]
		[SequenceReference("Image")] public readonly string Sequence = "idle";

		[Desc("Palette used to render the projectile sequence.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Should the projectile's shadow be rendered?")]
		public readonly bool Shadow = false;

		[Desc("Projectile speed in WDist / tick")]
		public readonly WDist InitialSpeed = new WDist(8);

		[Desc("Vertical launch angle (pitch).")]
		public readonly WAngle LaunchAngle = WAngle.Zero;

		[Desc("Maximum projectile speed in WDist / tick")]
		public readonly WDist MaximumSpeed = new WDist(512);

		[Desc("Projectile acceleration when propulsion activated.")]
		public readonly WDist Acceleration = WDist.Zero;

		[Desc("How many ticks before this missile is armed and can explode.")]
		public readonly int Arm = 0;

		[Desc("Is the missile blocked by actors with BlocksProjectiles: trait.")]
		public readonly bool Blockable = true;

		[Desc("Maximum offset at the maximum range")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Probability of locking onto and following target.")]
		public readonly int LockOnProbability = 100;

		[Desc("Horizontal rate of turn.")]
		public readonly int HorizontalRateOfTurn = 5;

		[Desc("Vertical rate of turn.")]
		public readonly int VerticalRateOfTurn = 5;

		[Desc("Run out of fuel after being activated this many ticks. Zero for unlimited fuel.")]
		public readonly int RangeLimit = 0;

		[Desc("Explode when running out of fuel.")]
		public readonly bool ExplodeWhenEmpty = true;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		[Desc("Cruise altitude. Zero means no cruise altitude used.")]
		public readonly WDist CruiseAltitude = new WDist(512);

		[Desc("Activate homing mechanism after this many ticks.")]
		public readonly int HomingActivationDelay = 0;

		[Desc("Image that contains the trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Smoke sequence name.")]
		[SequenceReference("Trail")] public readonly string TrailSequence = "idle";

		[Desc("Palette used to render the smoke sequence.")]
		[PaletteReference("TrailUsePlayerPalette")] public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the smoke sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		[Desc("Interval in ticks between spawning smoke animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Should smoke animation be spawned when the propulsion is not activated.")]
		public readonly bool TrailWhenDeactivated = false;

		public readonly int ContrailLength = 0;

		public readonly Color ContrailColor = Color.White;

		public readonly bool ContrailUsePlayerColor = false;

		public readonly int ContrailDelay = 1;

		[Desc("Should missile targeting be thrown off by nearby actors with JamsMissiles.")]
		public readonly bool Jammable = true;

		[Desc("Range of facings by which jammed missiles can stray from current path.")]
		public readonly int JammedDiversionRange = 20;

		[Desc("Explodes when leaving the following terrain type, e.g., Water for torpedoes.")]
		public readonly string BoundToTerrainType = "";

		[Desc("Explodes when inside this proximity radius to target.",
			"Note: If this value is lower than the missile speed, this check might",
			"not trigger fast enough, causing the missile to fly past the target.")]
		public readonly WDist CloseEnough = new WDist(298);

		public IEffect Create(ProjectileArgs args) { return new Missile(this, args); }
	}

	public class Missile : IEffect, ISync
	{
		readonly MissileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		readonly WVec gravity = new WVec(0, 0, -10);

		int ticksToNextSmoke;
		ContrailRenderable contrail;
		string trailPalette;
		int terrainHeight;

		[Sync] WPos pos;
		[Sync] WVec velocity;
		[Sync] int hFacing;
		[Sync] int vFacing;
		[Sync] bool activated;
		[Sync] int speed;

		[Sync] WPos targetPosition;
		[Sync] WVec offset;
		[Sync] int ticks;

		[Sync] bool lockOn = false;

		[Sync] public Actor SourceActor { get { return args.SourceActor; } }
		[Sync] public Target GuidedTarget { get { return args.GuidedTarget; } }

		public Missile(MissileInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			pos = args.Source;
			hFacing = args.Facing;
			vFacing = info.LaunchAngle.Angle / 4;

			speed = info.InitialSpeed.Length;
			velocity = new WVec(WDist.Zero, -info.InitialSpeed, WDist.Zero)
				.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
				.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)));

			targetPosition = args.PassiveTarget;

			var world = args.SourceActor.World;

			if (world.SharedRandom.Next(100) <= info.LockOnProbability)
				lockOn = true;

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Traits.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				offset = WVec.FromPDF(world.SharedRandom, 2) * inaccuracy / 1024;
			}

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, () => hFacing);
				anim.PlayRepeating(info.Sequence);
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				contrail = new ContrailRenderable(world, color, info.ContrailLength, info.ContrailDelay, 0);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;
		}

		bool JammedBy(TraitPair<JamsMissiles> tp)
		{
			if ((tp.Actor.CenterPosition - pos).HorizontalLengthSquared > tp.Trait.Range.LengthSquared)
				return false;

			if (tp.Actor.Owner.Stances[args.SourceActor.Owner] == Stance.Ally && !tp.Trait.AlliedMissiles)
				return false;

			return tp.Actor.World.SharedRandom.Next(100 / tp.Trait.Chance) == 0;
		}

		public void Tick(World world)
		{
			ticks++;
			if (anim != null)
				anim.Tick();

			var cell = world.Map.CellContaining(pos);
			terrainHeight = world.Map.MapHeight.Value[cell] * 512;

			// Switch from freefall mode to homing mode
			if (ticks == info.HomingActivationDelay + 1)
			{
				activated = true;
				hFacing = OpenRA.Traits.Util.GetFacing(velocity, hFacing);
				speed = velocity.Length;
			}

			// Switch from homing mode to freefall mode
			if (info.RangeLimit != 0 && ticks == info.RangeLimit + 1)
			{
				activated = false;
				velocity = new WVec(0, -speed, 0)
					.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
					.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)));
			}

			// Check if target position should be updated (actor visible & locked on)
			if (args.GuidedTarget.IsValidFor(args.SourceActor) && lockOn)
				targetPosition = args.GuidedTarget.CenterPosition + new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);

			// Compute current distance from target position
			var dist = targetPosition + offset - pos;
			var len = dist.Length;
			var hLenCurr = dist.HorizontalLength;

			WVec move;
			if (activated)
			{
				// If target is within range, keep speed constant and aim for the target.
				// The speed needs to be kept constant to keep range computation relatively simple.

				// If target is not within range, accelerate the projectile. If cruise altitudes
				// are not used, aim for the target. If the cruise altitudes are used, aim for the
				// target horizontally and for cruise altitude vertically.

				// Target is considered in range if after an additional tick of accelerated motion
				// the horizontal distance from the target would be less than
				// the diameter of the circle that the missile travels along when
				// turning vertically at the maximum possible rate.
				// This should work because in the worst case, the missile will have to make
				// a semi-loop before hitting the target.

				// Get underestimate of distance from target in next tick, so that inRange would
				// become true a little sooner than the theoretical "in range" condition is met.
				var hLenNext = (long)(hLenCurr - speed - info.Acceleration.Length).Clamp(0, hLenCurr);

				// Check if target in range
				bool inRange = hLenNext * hLenNext * info.VerticalRateOfTurn * info.VerticalRateOfTurn * 314 * 314
					<= 2L * 2 * speed * speed * 128 * 128 * 100 * 100;

				// Basically vDist is the representation in the x-y plane
				// of the projection of dist in the z-hDist plane,
				// where hDist is the projection of dist in the x-y plane.

				// This allows applying vertical rate of turn in the same way as the
				// horizontal rate of turn is applied.
				WVec vDist;
				if (inRange || info.CruiseAltitude.Length == 0)
					vDist = new WVec(-dist.Z, -hLenCurr, 0);
				else
					vDist = new WVec(-(dist.Z - targetPosition.Z + info.CruiseAltitude.Length + terrainHeight), -speed, 0);

				// Accelerate if out of range
				if (!inRange)
					speed = (speed + info.Acceleration.Length).Clamp(0, info.MaximumSpeed.Length);

				// Compute which direction the projectile should be facing
				var desiredHFacing = OpenRA.Traits.Util.GetFacing(dist, hFacing);
				var desiredVFacing = OpenRA.Traits.Util.GetFacing(vDist, vFacing);

				// Check whether the homing mechanism is jammed
				var jammed = info.Jammable && world.ActorsWithTrait<JamsMissiles>().Any(JammedBy);
				if (jammed)
				{
					desiredHFacing = hFacing + world.SharedRandom.Next(-info.JammedDiversionRange, info.JammedDiversionRange + 1);
					desiredVFacing = vFacing + world.SharedRandom.Next(-info.JammedDiversionRange, info.JammedDiversionRange + 1);
				}
				else if (!args.GuidedTarget.IsValidFor(args.SourceActor))
					desiredHFacing = hFacing;

				// Compute new direction the projectile will be facing
				hFacing = OpenRA.Traits.Util.TickFacing(hFacing, desiredHFacing, info.HorizontalRateOfTurn);
				vFacing = OpenRA.Traits.Util.TickFacing(vFacing, desiredVFacing, info.VerticalRateOfTurn);

				// Compute the projectile's guided displacement
				move = new WVec(0, -1024 * speed, 0)
					.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
					.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)))
					/ 1024;
			}
			else
			{
				// Compute the projectile's freefall displacement
				move = velocity + gravity / 2;
				velocity += gravity;
				var velRatio = info.MaximumSpeed.Length * 1024 / velocity.Length;
				if (velRatio < 1024)
					velocity = velocity * velRatio / 1024;
			}

			// When move (speed) is large, check for impact during the following next tick
			// Shorten the move to have its length match the distance from the target
			// and check for impact with the shortened move
			var movLen = move.Length;
			if (len < movLen)
			{
				var npos = pos + move * 1024 * len / movLen / 1024;
				if (world.Map.DistanceAboveTerrain(npos).Length <= 0 // Hit the ground
					|| (targetPosition + offset - npos).LengthSquared < info.CloseEnough.LengthSquared) // Within range
					pos = npos;
				else
					pos += move;
			}
			else
				pos += move;

			// Create the smoke trail effect
			if (!string.IsNullOrEmpty(info.TrailImage) && --ticksToNextSmoke < 0 && (activated || info.TrailWhenDeactivated))
			{
				world.AddFrameEndTask(w => w.Add(new Smoke(w, pos - 3 * move / 2, info.TrailImage, trailPalette, info.TrailSequence)));
				ticksToNextSmoke = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			var height = world.Map.DistanceAboveTerrain(pos);
			var shouldExplode = (height.Length <= 0) // Hit the ground
				|| (len < info.CloseEnough.Length) // Within range
				|| (info.ExplodeWhenEmpty && info.RangeLimit != 0 && ticks > info.RangeLimit) // Ran out of fuel
				|| (info.Blockable && BlocksProjectiles.AnyBlockingActorAt(world, pos)) // Hit a wall or other blocking obstacle
				|| !world.Map.Contains(cell) // This also avoids an IndexOutOfRangeException in GetTerrainInfo below.
				|| (!string.IsNullOrEmpty(info.BoundToTerrainType) && world.Map.GetTerrainInfo(cell).Type != info.BoundToTerrainType) // Hit incompatible terrain
				|| (height.Length < info.AirburstAltitude.Length && hLenCurr < info.CloseEnough.Length); // Airburst

			if (shouldExplode)
				Explode(world);
		}

		void Explode(World world)
		{
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));

			// Don't blow up in our launcher's face!
			if (ticks <= info.Arm)
				return;

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
