#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Projectile with smart tracking.")]
	public class MissileInfo : IProjectileInfo
	{
		[Desc("Name of the image containing the projectile sequence.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Palette used to render the projectile sequence.")]
		public readonly string Palette = "effect";

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Color to draw shadow if Shadow is true.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		[Desc("Minimum vertical launch angle (pitch).")]
		public readonly WAngle MinimumLaunchAngle = new(-64);

		[Desc("Maximum vertical launch angle (pitch).")]
		public readonly WAngle MaximumLaunchAngle = new(128);

		[Desc("Minimum launch speed in WDist / tick. Defaults to Speed if -1.")]
		public readonly WDist MinimumLaunchSpeed = new(-1);

		[Desc("Maximum launch speed in WDist / tick. Defaults to Speed if -1.")]
		public readonly WDist MaximumLaunchSpeed = new(-1);

		[Desc("Maximum projectile speed in WDist / tick")]
		public readonly WDist Speed = new(384);

		[Desc("Projectile acceleration when propulsion activated.")]
		public readonly WDist Acceleration = new(5);

		[Desc("How many ticks before this missile is armed and can explode.")]
		public readonly int Arm = 0;

		[Desc("Is the missile blocked by actors with BlocksProjectiles: trait.")]
		public readonly bool Blockable = true;

		[Desc("Is the missile aware of terrain height levels. Only needed for mods with real, non-visual height levels.")]
		public readonly bool TerrainHeightAware = false;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new(1);

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Absolute;

		[Desc("Inaccuracy override when successfully locked onto target. Defaults to Inaccuracy if negative.")]
		public readonly WDist LockOnInaccuracy = new(-1);

		[Desc("Probability of locking onto and following target.")]
		public readonly int LockOnProbability = 100;

		[Desc("Horizontal rate of turn.")]
		public readonly WAngle HorizontalRateOfTurn = new(20);

		[Desc("Vertical rate of turn.")]
		public readonly WAngle VerticalRateOfTurn = new(24);

		[Desc("Gravity applied while in free fall.")]
		public readonly int Gravity = 10;

		[Desc("Run out of fuel after covering this distance. Zero for defaulting to weapon range. Negative for unlimited fuel.")]
		public readonly WDist RangeLimit = WDist.Zero;

		[Desc("Explode when running out of fuel.")]
		public readonly bool ExplodeWhenEmpty = true;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		[Desc("Cruise altitude. Zero means no cruise altitude used.")]
		public readonly WDist CruiseAltitude = new(512);

		[Desc("Activate homing mechanism after this many ticks.")]
		public readonly int HomingActivationDelay = 0;

		[Desc("Image that contains the trail animation.")]
		public readonly string TrailImage = null;

		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		public readonly string[] TrailSequences = { "idle" };

		[PaletteReference(nameof(TrailUsePlayerPalette))]
		[Desc("Palette used to render the trail sequence.")]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		[Desc("Interval in ticks between spawning trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Should trail animation be spawned when the propulsion is not activated.")]
		public readonly bool TrailWhenDeactivated = false;

		[Desc("When set, display a line behind the actor. Length is measured in ticks after appearing.")]
		public readonly int ContrailLength = 0;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int ContrailDelay = 1;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ContrailZOffset = 2047;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist ContrailStartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(ContrailStartWidth) + " if left undefined")]
		public readonly WDist? ContrailEndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color ContrailStartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool ContrailStartColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int ContrailStartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Will default to " + nameof(ContrailStartColor) + " if left undefined")]
		public readonly Color? ContrailEndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool ContrailEndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int ContrailEndColorAlpha = 0;

		[Desc("Should missile targeting be thrown off by nearby actors with JamsMissiles.")]
		public readonly bool Jammable = true;

		[Desc("Range of facings by which jammed missiles can stray from current path.")]
		public readonly int JammedDiversionRange = 20;

		[Desc("Explodes when leaving the following terrain type, e.g., Water for torpedoes.")]
		public readonly string BoundToTerrainType = "";

		[Desc("Allow the missile to snap to the target, meaning jumping to the target immediately when",
			"the missile enters the radius of the current speed around the target.")]
		public readonly bool AllowSnapping = false;

		[Desc("Explodes when inside this proximity radius to target.",
			"Note: If this value is lower than the missile speed, this check might",
			"not trigger fast enough, causing the missile to fly past the target.")]
		public readonly WDist CloseEnough = new(298);

		public IProjectile Create(ProjectileArgs args) { return new Missile(this, args); }
	}

	// TODO: double check square roots!!!
	public class Missile : IProjectile, ISync
	{
		enum States
		{
			Freefall,
			Homing,
			Hitting
		}

		readonly MissileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		readonly WVec gravity;
		readonly int minLaunchSpeed;
		readonly int maxLaunchSpeed;
		readonly int maxSpeed;
		readonly WAngle minLaunchAngle;
		readonly WAngle maxLaunchAngle;

		readonly float3 shadowColor;
		readonly float shadowAlpha;

		int ticks;

		int ticksToNextSmoke;
		readonly ContrailRenderable contrail;
		readonly string trailPalette;

		States state;
		bool targetPassedBy;
		readonly bool lockOn;
		bool allowPassBy; // TODO: use this also with high minimum launch angle settings

		WPos targetPosition;
		readonly WVec offset;

		WVec tarVel;
		WVec predVel;

		[Sync]
		WPos pos;

		WVec velocity;
		int speed;
		int loopRadius;
		WDist distanceCovered;
		readonly WDist rangeLimit;

		WAngle renderFacing;

		[Sync]
		int hFacing;

		[Sync]
		int vFacing;

		public Missile(MissileInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			pos = args.Source;
			hFacing = args.Facing.Facing;
			gravity = new WVec(0, 0, -info.Gravity);
			targetPosition = args.PassiveTarget;
			var limit = info.RangeLimit != WDist.Zero ? info.RangeLimit : args.Weapon.Range;
			rangeLimit = new WDist(Util.ApplyPercentageModifiers(limit.Length, args.RangeModifiers));
			minLaunchSpeed = info.MinimumLaunchSpeed.Length > -1 ? info.MinimumLaunchSpeed.Length : info.Speed.Length;
			maxLaunchSpeed = info.MaximumLaunchSpeed.Length > -1 ? info.MaximumLaunchSpeed.Length : info.Speed.Length;
			maxSpeed = info.Speed.Length;
			minLaunchAngle = info.MinimumLaunchAngle;
			maxLaunchAngle = info.MaximumLaunchAngle;

			var world = args.SourceActor.World;

			if (world.SharedRandom.Next(100) <= info.LockOnProbability)
				lockOn = true;

			var inaccuracy = lockOn && info.LockOnInaccuracy.Length > -1 ? info.LockOnInaccuracy.Length : info.Inaccuracy.Length;
			if (inaccuracy > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(inaccuracy, info.InaccuracyType, args);
				offset = WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			DetermineLaunchSpeedAndAngle(world, out speed, out vFacing);

			velocity = new WVec(0, -speed, 0)
				.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
				.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)));

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, () => renderFacing);
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var startcolor = Color.FromArgb(info.ContrailStartColorAlpha, info.ContrailStartColor);
				var endcolor = Color.FromArgb(info.ContrailEndColorAlpha, info.ContrailEndColor ?? startcolor);
				contrail = new ContrailRenderable(world, args.SourceActor,
					startcolor, info.ContrailStartColorUsePlayerColor,
					endcolor, info.ContrailEndColor == null ? info.ContrailStartColorUsePlayerColor : info.ContrailEndColorUsePlayerColor,
					info.ContrailStartWidth,
					info.ContrailEndWidth ?? info.ContrailStartWidth,
					info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;
		}

		static int LoopRadius(int speed, int rot)
		{
			// loopRadius in w-units = speed in w-units per tick / angular speed in radians per tick
			// angular speed in radians per tick = rot in facing units per tick * (pi radians / 128 facing units)
			// pi = 314 / 100
			// ==> loopRadius = (speed * 128 * 100) / (314 * rot)
			return speed * 6400 / (157 * rot);
		}

		void DetermineLaunchSpeedAndAngleForIncline(int predClfDist, int diffClfMslHgt, int relTarHorDist,
			out int speed, out int vFacing)
		{
			speed = maxLaunchSpeed;

			// Find smallest vertical facing, for which the missile will be able to climb terrAltDiff w-units
			// within hHeightChange w-units all the while ending the ascent with vertical facing 0
			vFacing = maxLaunchAngle.Angle >> 2;

			// Compute minimum speed necessary to both be able to face directly upwards and have enough space
			// to hit the target without passing it by (and thus having to do horizontal loops)
			var minSpeed = (System.Math.Min(predClfDist * 1024 / (1024 - WAngle.FromFacing(vFacing).Sin()),
					(relTarHorDist + predClfDist) * 1024 / (2 * (2048 - WAngle.FromFacing(vFacing).Sin())))
				* info.VerticalRateOfTurn.Facing * 157 / 6400).Clamp(minLaunchSpeed, maxLaunchSpeed);

			if ((sbyte)vFacing < 0)
				speed = minSpeed;
			else if (!WillClimbWithinDistance(vFacing, loopRadius, predClfDist, diffClfMslHgt)
				&& !WillClimbAroundInclineTop(vFacing, loopRadius, predClfDist, diffClfMslHgt))
			{
				// Find highest speed greater than the above minimum that allows the missile
				// to surmount the incline
				var vFac = vFacing;
				speed = BisectionSearch(minSpeed, maxLaunchSpeed, spd =>
				{
					var lpRds = LoopRadius(spd, info.VerticalRateOfTurn.Facing);
					return WillClimbWithinDistance(vFac, lpRds, predClfDist, diffClfMslHgt)
						|| WillClimbAroundInclineTop(vFac, lpRds, predClfDist, diffClfMslHgt);
				});
			}
			else
			{
				// Find least vertical facing that will allow the missile to climb
				// terrAltDiff w-units within hHeightChange w-units
				// all the while ending the ascent with vertical facing 0
				vFacing = BisectionSearch(System.Math.Max((sbyte)(minLaunchAngle.Angle >> 2), (sbyte)0),
					(sbyte)(maxLaunchAngle.Angle >> 2),
					vFac => !WillClimbWithinDistance(vFac, loopRadius, predClfDist, diffClfMslHgt)) + 1;
			}
		}

		// TODO: Double check Launch parameter determination
		void DetermineLaunchSpeedAndAngle(World world, out int speed, out int vFacing)
		{
			speed = maxLaunchSpeed;
			loopRadius = LoopRadius(speed, info.VerticalRateOfTurn.Facing);

			// Compute current distance from target position
			var tarDistVec = targetPosition + offset - pos;
			var relTarHorDist = tarDistVec.HorizontalLength;

			var predClfHgt = 0;
			var predClfDist = 0;
			var lastHt = 0;

			if (info.TerrainHeightAware)
				InclineLookahead(world, relTarHorDist, out predClfHgt, out predClfDist, out _, out lastHt);

			// Height difference between the incline height and missile height
			var diffClfMslHgt = predClfHgt - pos.Z;

			// Incline coming up
			if (info.TerrainHeightAware && diffClfMslHgt >= 0 && predClfDist > 0)
				DetermineLaunchSpeedAndAngleForIncline(predClfDist, diffClfMslHgt, relTarHorDist, out speed, out vFacing);
			else if (lastHt != 0)
			{
				vFacing = System.Math.Max((sbyte)(minLaunchAngle.Angle >> 2), (sbyte)0);
				speed = maxLaunchSpeed;
			}
			else
			{
				// Set vertical facing so that the missile faces its target
				var vDist = new WVec(-tarDistVec.Z, -relTarHorDist, 0);
				vFacing = (sbyte)vDist.Yaw.Facing;

				// Do not accept -1 as valid vertical facing since it is usually a numerical error
				// and will lead to premature descent and crashing into the ground
				if (vFacing == -1)
					vFacing = 0;

				// Make sure the chosen vertical facing adheres to prescribed bounds
				vFacing = vFacing.Clamp((sbyte)(minLaunchAngle.Angle >> 2),
					(sbyte)(maxLaunchAngle.Angle >> 2));
			}
		}

		// Will missile be able to climb terrAltDiff w-units within hHeightChange w-units
		// all the while ending the ascent with vertical facing 0
		// Calling this function only makes sense when vFacing is nonnegative
		static bool WillClimbWithinDistance(int vFacing, int loopRadius, int predClfDist, int diffClfMslHgt)
		{
			// Missile's horizontal distance from loop's center
			var missDist = loopRadius * WAngle.FromFacing(vFacing).Sin() / 1024;

			// Missile's height below loop's top
			var missHgt = loopRadius * (1024 - WAngle.FromFacing(vFacing).Cos()) / 1024;

			// Height that would be climbed without changing vertical facing
			// for a horizontal distance hHeightChange - missDist
			var hgtChg = (predClfDist - missDist) * WAngle.FromFacing(vFacing).Tan() / 1024;

			// Check if total manoeuvre height enough to overcome the incline's height
			return hgtChg + missHgt >= diffClfMslHgt;
		}

		// This function checks if the missile's vertical facing is
		// nonnegative, and the incline top's horizontal distance from the missile is
		// less than loopRadius * (1024 - WAngle.FromFacing(vFacing).Sin()) / 1024
		static bool IsNearInclineTop(int vFacing, int loopRadius, int predClfDist)
		{
			return vFacing >= 0 && predClfDist <= loopRadius * (1024 - WAngle.FromFacing(vFacing).Sin()) / 1024;
		}

		// Will missile climb around incline top if bringing vertical facing
		// down to zero on an arc of radius loopRadius
		// Calling this function only makes sense when IsNearInclineTop returns true
		static bool WillClimbAroundInclineTop(int vFacing, int loopRadius, int predClfDist, int diffClfMslHgt)
		{
			// Vector from missile's current position pointing to the loop's center
			var radius = new WVec(loopRadius, 0, 0)
				.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(System.Math.Max(0, 64 - vFacing))));

			// Vector from loop's center to incline top + 64 hardcoded in height buffer zone
			var topVector = new WVec(predClfDist, diffClfMslHgt + 64, 0) - radius;

			// Check if incline top inside of the vertical loop
			return topVector.Length <= loopRadius;
		}

		static int BisectionSearch(int lowerBound, int upperBound, System.Func<int, bool> testCriterion)
		{
			// Assuming that there exists an integer N between lowerBound and upperBound
			// for which testCriterion returns true as well as all integers less than N,
			// and for which testCriterion returns false for all integers greater than N,
			// this function finds N.
			while (upperBound - lowerBound > 1)
			{
				var middle = (upperBound + lowerBound) / 2;

				if (testCriterion(middle))
					lowerBound = middle;
				else
					upperBound = middle;
			}

			return lowerBound;
		}

		bool JammedBy(TraitPair<JamsMissiles> tp)
		{
			if ((tp.Actor.CenterPosition - pos).HorizontalLengthSquared > tp.Trait.Range.LengthSquared)
				return false;

			if (!tp.Trait.DeflectionStances.HasRelationship(tp.Actor.Owner.RelationshipWith(args.SourceActor.Owner)))
				return false;

			return tp.Actor.World.SharedRandom.Next(100) < tp.Trait.Chance;
		}

		void ChangeSpeed(int sign = 1)
		{
			speed = (speed + sign * info.Acceleration.Length).Clamp(0, maxSpeed);

			// Compute the vertical loop radius
			loopRadius = LoopRadius(speed, info.VerticalRateOfTurn.Facing);
		}

		WVec FreefallTick()
		{
			// Compute the projectile's freefall displacement
			var move = velocity + gravity / 2;
			velocity += gravity;
			var velRatio = maxSpeed * 1024 / velocity.Length;
			if (velRatio < 1024)
				velocity = velocity * velRatio / 1024;

			return move;
		}

		// NOTE: It might be desirable to make lookahead more intelligent by outputting more information
		// than just the highest point in the lookahead distance
		void InclineLookahead(World world, int distCheck, out int predClfHgt, out int predClfDist, out int lastHtChg, out int lastHt)
		{
			predClfHgt = 0; // Highest probed terrain height
			predClfDist = 0; // Distance from highest point
			lastHtChg = 0; // Distance from last time the height changes
			lastHt = 0; // Height just before the last height change

			// NOTE: Might be desired to unhardcode the lookahead step size
			const int StepSize = 32;
			var step = new WVec(0, -StepSize, 0)
				.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing))); // Step vector of length 128

			// Probe terrain ahead of the missile
			// NOTE: Might be desired to unhardcode maximum lookahead distance
			var maxLookaheadDistance = loopRadius * 4;
			var posProbe = pos;
			var curDist = 0;
			var tickLimit = System.Math.Min(maxLookaheadDistance, distCheck) / StepSize;
			var prevHt = 0;

			// TODO: Make sure cell on map!!!
			for (var tick = 0; tick <= tickLimit; tick++)
			{
				posProbe += step;
				if (!world.Map.Contains(world.Map.CellContaining(posProbe)))
					break;

				var ht = world.Map.Height[world.Map.CellContaining(posProbe)] * 512;

				curDist += StepSize;
				if (ht > predClfHgt)
				{
					predClfHgt = ht;
					predClfDist = curDist;
				}

				if (prevHt != ht)
				{
					lastHtChg = curDist;
					lastHt = prevHt;
					prevHt = ht;
				}
			}
		}

		int IncreaseAltitude(int predClfDist, int diffClfMslHgt, int relTarHorDist, int vFacing)
		{
			var desiredVFacing = vFacing;

			// If missile is below incline top height and facing downwards, bring back
			// its vertical facing above zero as soon as possible
			if ((sbyte)vFacing < 0)
				desiredVFacing = info.VerticalRateOfTurn.Facing;

			// Missile will climb around incline top if bringing vertical facing
			// down to zero on an arc of radius loopRadius
			else if (IsNearInclineTop(vFacing, loopRadius, predClfDist)
				&& WillClimbAroundInclineTop(vFacing, loopRadius, predClfDist, diffClfMslHgt))
				desiredVFacing = 0;

			// Missile will not climb terrAltDiff w-units within hHeightChange w-units
			// all the while ending the ascent with vertical facing 0
			else if (!WillClimbWithinDistance(vFacing, loopRadius, predClfDist, diffClfMslHgt))

				// Find smallest vertical facing, attainable in the next tick,
				// for which the missile will be able to climb terrAltDiff w-units
				// within hHeightChange w-units all the while ending the ascent
				// with vertical facing 0
				for (var vFac = System.Math.Min(vFacing + info.VerticalRateOfTurn.Facing - 1, 63); vFac >= vFacing; vFac--)
					if (!WillClimbWithinDistance(vFac, loopRadius, predClfDist, diffClfMslHgt)
						&& !(predClfDist <= loopRadius * (1024 - WAngle.FromFacing(vFac).Sin()) / 1024
							&& WillClimbAroundInclineTop(vFac, loopRadius, predClfDist, diffClfMslHgt)))
					{
						desiredVFacing = vFac + 1;
						break;
					}

			// Attained height after ascent as predicted from upper part of incline surmounting manoeuvre
			var predAttHght = loopRadius * (1024 - WAngle.FromFacing(vFacing).Cos()) / 1024 - diffClfMslHgt;

			// Should the missile be slowed down in order to make it more maneuverable
			var slowDown = info.Acceleration.Length != 0 // Possible to decelerate
				&& ((desiredVFacing != 0 // Lower part of incline surmounting manoeuvre

						// Incline will be hit before vertical facing attains 64
						&& (predClfDist <= loopRadius * (1024 - WAngle.FromFacing(vFacing).Sin()) / 1024

							// When evaluating this the incline will be *not* be hit before vertical facing attains 64
							// At current speed target too close to hit without passing it by
							|| relTarHorDist <= 2 * loopRadius * (2048 - WAngle.FromFacing(vFacing).Sin()) / 1024 - predClfDist))

					|| (desiredVFacing == 0 // Upper part of incline surmounting manoeuvre
						&& relTarHorDist <= loopRadius * WAngle.FromFacing(vFacing).Sin() / 1024
							+ Exts.ISqrt(predAttHght * (2 * loopRadius - predAttHght)))); // Target too close to hit at current speed

			if (slowDown)
				ChangeSpeed(-1);

			return desiredVFacing;
		}

		int HomingInnerTick(int predClfDist, int diffClfMslHgt, int relTarHorDist, int lastHtChg, int lastHt,
			int relTarHgt, int vFacing, bool targetPassedBy)
		{
			int desiredVFacing;

			// Incline coming up -> attempt to reach the incline so that after predClfDist
			// the height above the terrain is positive but as close to 0 as possible
			// Also, never change horizontal facing and never travel backwards
			// Possible techniques to avoid close cliffs are deceleration, turning
			// as sharply as possible to travel directly upwards and then returning
			// to zero vertical facing as low as possible while still not hitting the
			// high terrain. A last technique (and the preferred one, normally used when
			// the missile hasn't been fired near a cliff) is simply finding the smallest
			// vertical facing that allows for a smooth climb to the new terrain's height
			// and coming in at predClfDist at exactly zero vertical facing
			if (info.TerrainHeightAware && diffClfMslHgt >= 0 && !allowPassBy)
				desiredVFacing = IncreaseAltitude(predClfDist, diffClfMslHgt, relTarHorDist, vFacing);
			else if (relTarHorDist <= 3 * loopRadius || state == States.Hitting)
			{
				// No longer travel at cruise altitude
				state = States.Hitting;

				if (lastHt >= targetPosition.Z)
					allowPassBy = true;

				if (!allowPassBy && (lastHt < targetPosition.Z || targetPassedBy))
				{
					// Aim for the target
					var vDist = new WVec(-relTarHgt, -relTarHorDist, 0);
					desiredVFacing = (sbyte)vDist.HorizontalLengthSquared != 0 ? vDist.Yaw.Facing : vFacing;

					// Do not accept -1  as valid vertical facing since it is usually a numerical error
					// and will lead to premature descent and crashing into the ground
					if (desiredVFacing == -1)
						desiredVFacing = 0;

					// If the target has been passed by, limit the absolute value of
					// vertical facing by the maximum vertical rate of turn
					// Do this because the missile will be looping horizontally
					// and thus needs smaller vertical facings so as not
					// to hit the ground prematurely
					if (targetPassedBy)
						desiredVFacing = desiredVFacing.Clamp(-info.VerticalRateOfTurn.Facing, info.VerticalRateOfTurn.Facing);
					else if (lastHt == 0)
					{
						// Before the target is passed by, missile speed should be changed
						// Target's height above loop's center
						var tarHgt = (loopRadius * WAngle.FromFacing(vFacing).Cos() / 1024 - System.Math.Abs(relTarHgt)).Clamp(0, loopRadius);

						// Target's horizontal distance from loop's center
						var tarDist = Exts.ISqrt(loopRadius * loopRadius - tarHgt * tarHgt);

						// Missile's horizontal distance from loop's center
						var missDist = loopRadius * WAngle.FromFacing(vFacing).Sin() / 1024;

						// If the current height does not permit the missile
						// to hit the target before passing it by, lower speed
						// Otherwise, increase speed
						if (relTarHorDist <= tarDist - System.Math.Sign(relTarHgt) * missDist)
							ChangeSpeed(-1);
						else
							ChangeSpeed();
					}
				}
				else if (allowPassBy || (lastHt != 0 && relTarHorDist - lastHtChg < loopRadius))
				{
					// Only activate this part if target too close to cliff
					allowPassBy = true;

					// Vector from missile's current position pointing to the loop's center
					var radius = new WVec(loopRadius, 0, 0)
						.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(64 - vFacing)));

					// Vector from loop's center to incline top hardcoded in height buffer zone
					var edgeVector = new WVec(lastHtChg, lastHt - pos.Z, 0) - radius;

					if (!targetPassedBy)
					{
						// Climb to critical height
						if (relTarHorDist > 2 * loopRadius)
						{
							// Target's distance from cliff
							var d1 = relTarHorDist - lastHtChg;
							if (d1 < 0)
								d1 = 0;
							if (d1 > 2 * loopRadius)
								return 0;

							// Find critical height at which the missile must be once it is at one loopRadius
							// away from the target
							var h1 = loopRadius - Exts.ISqrt(d1 * (2 * loopRadius - d1)) - (pos.Z - lastHt);

							if (h1 > loopRadius * (1024 - WAngle.FromFacing(vFacing).Cos()) / 1024)
								desiredVFacing = WAngle.ArcTan(Exts.ISqrt(h1 * (2 * loopRadius - h1)), loopRadius - h1).Angle >> 2;
							else
								desiredVFacing = 0;

							// TODO: deceleration checks!!!
						}
						else
						{
							// Avoid the cliff edge
							if (info.TerrainHeightAware && edgeVector.Length > loopRadius && lastHt > targetPosition.Z)
							{
								int vFac;
								for (vFac = vFacing + 1; vFac <= vFacing + info.VerticalRateOfTurn.Facing - 1; vFac++)
								{
									// Vector from missile's current position pointing to the loop's center
									radius = new WVec(loopRadius, 0, 0)
										.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(64 - vFac)));

									// Vector from loop's center to incline top + 64 hardcoded in height buffer zone
									edgeVector = new WVec(lastHtChg, lastHt - pos.Z, 0) - radius;
									if (edgeVector.Length <= loopRadius)
										break;
								}

								desiredVFacing = vFac;
							}
							else
							{
								// Aim for the target
								var vDist = new WVec(-relTarHgt, -relTarHorDist, 0);
								desiredVFacing = (sbyte)vDist.HorizontalLengthSquared != 0 ? vDist.Yaw.Facing : vFacing;
								if (desiredVFacing < 0 && info.VerticalRateOfTurn.Facing < (sbyte)vFacing)
									desiredVFacing = 0;
							}
						}
					}
					else
					{
						// Aim for the target
						var vDist = new WVec(-relTarHgt, relTarHorDist, 0);
						desiredVFacing = (sbyte)vDist.HorizontalLengthSquared != 0 ? vDist.Yaw.Facing : vFacing;
						if (desiredVFacing < 0 && info.VerticalRateOfTurn.Facing < (sbyte)vFacing)
							desiredVFacing = 0;
					}
				}
				else
				{
					// Aim to attain cruise altitude as soon as possible while having the absolute value
					// of vertical facing bound by the maximum vertical rate of turn
					var vDist = new WVec(-diffClfMslHgt - info.CruiseAltitude.Length, -speed, 0);
					desiredVFacing = (sbyte)vDist.HorizontalLengthSquared != 0 ? vDist.Yaw.Facing : vFacing;

					// If the missile is launched above CruiseAltitude, it has to descend instead of climbing
					if (-diffClfMslHgt > info.CruiseAltitude.Length)
						desiredVFacing = -desiredVFacing;

					desiredVFacing = desiredVFacing.Clamp(-info.VerticalRateOfTurn.Facing, info.VerticalRateOfTurn.Facing);

					ChangeSpeed();
				}
			}
			else
			{
				// Aim to attain cruise altitude as soon as possible while having the absolute value
				// of vertical facing bound by the maximum vertical rate of turn
				var vDist = new WVec(-diffClfMslHgt - info.CruiseAltitude.Length, -speed, 0);
				desiredVFacing = (sbyte)vDist.HorizontalLengthSquared != 0 ? vDist.Yaw.Facing : vFacing;

				// If the missile is launched above CruiseAltitude, it has to descend instead of climbing
				if (-diffClfMslHgt > info.CruiseAltitude.Length)
					desiredVFacing = -desiredVFacing;

				desiredVFacing = desiredVFacing.Clamp(-info.VerticalRateOfTurn.Facing, info.VerticalRateOfTurn.Facing);

				ChangeSpeed();
			}

			return desiredVFacing;
		}

		WVec HomingTick(World world, in WVec tarDistVec, int relTarHorDist)
		{
			var predClfHgt = 0;
			var predClfDist = 0;
			var lastHtChg = 0;
			var lastHt = 0;

			if (info.TerrainHeightAware)
				InclineLookahead(world, relTarHorDist, out predClfHgt, out predClfDist, out lastHtChg, out lastHt);

			// Height difference between the incline height and missile height
			var diffClfMslHgt = predClfHgt - pos.Z;

			// Get underestimate of distance from target in next tick
			var nxtRelTarHorDist = (relTarHorDist - speed - info.Acceleration.Length).Clamp(0, relTarHorDist);

			// Target height relative to the missile
			var relTarHgt = tarDistVec.Z;

			// Compute which direction the projectile should be facing
			var velVec = tarDistVec + predVel;
			var desiredHFacing = velVec.HorizontalLengthSquared != 0 ? velVec.Yaw.Facing : hFacing;

			var delta = Util.NormalizeFacing(hFacing - desiredHFacing);
			if (allowPassBy && delta > 64 && delta < 192)
			{
				desiredHFacing = (desiredHFacing + 128) & 0xFF;
				targetPassedBy = true;
			}
			else
				targetPassedBy = false;

			var desiredVFacing = HomingInnerTick(predClfDist, diffClfMslHgt, relTarHorDist, lastHtChg, lastHt,
				relTarHgt, vFacing, targetPassedBy);

			// The target has been passed by
			if (tarDistVec.HorizontalLength < speed * WAngle.FromFacing(vFacing).Cos() / 1024)
				targetPassedBy = true;

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
			hFacing = Util.TickFacing(hFacing, desiredHFacing, info.HorizontalRateOfTurn.Facing);
			vFacing = Util.TickFacing(vFacing, desiredVFacing, info.VerticalRateOfTurn.Facing);

			// Compute the projectile's guided displacement
			return new WVec(0, -1024 * speed, 0)
				.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
				.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)))
				/ 1024;
		}

		public void Tick(World world)
		{
			ticks++;
			anim?.Tick();

			// Switch from freefall mode to homing mode
			if (ticks == info.HomingActivationDelay + 1)
			{
				state = States.Homing;
				speed = velocity.Length;

				// Compute the vertical loop radius
				loopRadius = LoopRadius(speed, info.VerticalRateOfTurn.Facing);
			}

			// Switch from homing mode to freefall mode
			if (rangeLimit >= WDist.Zero && distanceCovered > rangeLimit)
			{
				state = States.Freefall;
				velocity = new WVec(0, -speed, 0)
					.Rotate(new WRot(WAngle.FromFacing(vFacing), WAngle.Zero, WAngle.Zero))
					.Rotate(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(hFacing)));
			}

			// Check if target position should be updated (actor visible & locked on)
			var newTarPos = targetPosition;
			if (args.GuidedTarget.IsValidFor(args.SourceActor) && lockOn)
				newTarPos = (args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.ClosestToIgnoringPath(args.Source))
					+ new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);

			// Compute target's predicted velocity vector (assuming uniform circular motion)
			var yaw1 = tarVel.HorizontalLengthSquared != 0 ? tarVel.Yaw : WAngle.FromFacing(hFacing);
			tarVel = newTarPos - targetPosition;
			var yaw2 = tarVel.HorizontalLengthSquared != 0 ? tarVel.Yaw : WAngle.FromFacing(hFacing);
			predVel = tarVel.Rotate(WRot.FromYaw(yaw2 - yaw1));
			targetPosition = newTarPos;

			// Compute current distance from target position
			var tarDistVec = targetPosition + offset - pos;
			var relTarDist = tarDistVec.Length;
			var relTarHorDist = tarDistVec.HorizontalLength;

			WVec move;
			if (state == States.Freefall)
				move = FreefallTick();
			else
				move = HomingTick(world, tarDistVec, relTarHorDist);

			renderFacing = new WVec(move.X, move.Y - move.Z, 0).Yaw;

			// Move the missile
			var lastPos = pos;
			if (info.AllowSnapping && state != States.Freefall && relTarDist < move.Length)
				pos = targetPosition + offset;
			else
				pos += move;

			// Check for walls or other blocking obstacles
			var shouldExplode = false;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, pos, info.Width, out var blockedPos))
			{
				pos = blockedPos;
				shouldExplode = true;
			}

			// Create the sprite trail effect
			if (!string.IsNullOrEmpty(info.TrailImage) && --ticksToNextSmoke < 0 && (state != States.Freefall || info.TrailWhenDeactivated))
			{
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos - 3 * move / 2, renderFacing, w,
					info.TrailImage, info.TrailSequences.Random(world.SharedRandom), trailPalette)));

				ticksToNextSmoke = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			distanceCovered += new WDist(speed);
			var cell = world.Map.CellContaining(pos);
			var height = world.Map.DistanceAboveTerrain(pos);
			shouldExplode |= height.Length < 0 // Hit the ground
				|| relTarDist < info.CloseEnough.Length // Within range
				|| (info.ExplodeWhenEmpty && rangeLimit >= WDist.Zero && distanceCovered > rangeLimit) // Ran out of fuel
				|| !world.Map.Contains(cell) // This also avoids an IndexOutOfRangeException in GetTerrainInfo below.
				|| (!string.IsNullOrEmpty(info.BoundToTerrainType) && world.Map.GetTerrainInfo(cell).Type != info.BoundToTerrainType) // Hit incompatible terrain
				|| (height.Length < info.AirburstAltitude.Length && relTarHorDist < info.CloseEnough.Length); // Airburst

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

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, WAngle.FromFacing(vFacing), WAngle.FromFacing(hFacing)),
				ImpactPosition = pos,
			};

			args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (anim == null)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				var paletteName = info.Palette;
				if (paletteName != null && info.IsPlayerPalette)
					paletteName += args.SourceActor.Owner.InternalName;

				var palette = wr.Palette(paletteName);

				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, palette))
						yield return ((IModifyableRenderable)r)
							.WithTint(shadowColor, ((IModifyableRenderable)r).TintModifiers | TintModifiers.ReplaceColor)
							.WithAlpha(shadowAlpha);
				}

				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
