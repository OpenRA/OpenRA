#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	class MissileInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WRange / tick")]
		public readonly WRange Speed = new WRange(8);
		[Desc("Maximum vertical pitch when changing altitude.")]
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(30);
		[Desc("How many ticks before this missile is armed and can explode.")]
		public readonly int Arm = 0;
		[Desc("Check for whether an actor with BlocksBullets: trait blocks fire")]
		public readonly bool High = false;
		public readonly bool Shadow = false;
		public readonly string Trail = null;
		[Desc("Maximum offset at the maximum range")]
		public readonly WRange Inaccuracy = WRange.Zero;
		[Desc("Probability of locking onto and following target.")]
		public readonly int LockOnProbability = 100;
		public readonly string Image = null;
		[Desc("Rate of Turning")]
		public readonly int ROT = 5;
		[Desc("Explode when following the target longer than this.")]
		public readonly int RangeLimit = 0;
		[Desc("If fired at aircraft, increase speed by 50%.")]
		public readonly bool TurboBoost = false;
		public readonly int TrailInterval = 2;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;
		public readonly bool Jammable = true;
		[Desc("Explodes when leaving the following terrain type, e.g., Water for torpedoes.")]
		public readonly string BoundToTerrainType = "";
		[Desc("Explodes when inside this proximity radius to target.",
			"Note: If this value is lower than the missile speed, this check might", 
			"not trigger fast enough, causing the missile to fly past the target.")]
		public readonly WRange CloseEnough = new WRange(298);

		public IEffect Create(ProjectileArgs args) { return new Missile(this, args); }
	}

	class Missile : IEffect, ISync
	{
		readonly MissileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		int ticksToNextSmoke;
		ContrailRenderable trail;

		[Sync] WPos pos;
		[Sync] int facing;

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
			facing = args.Facing;

			targetPosition = args.PassiveTarget;

			var world = args.SourceActor.World;

			if (world.SharedRandom.Next(100) <= info.LockOnProbability)
				lockOn = true;

			if (info.Inaccuracy.Range > 0)
			{
				var inaccuracy = OpenRA.Traits.Util.ApplyPercentageModifiers(info.Inaccuracy.Range, args.InaccuracyModifiers);
				offset = WVec.FromPDF(world.SharedRandom, 2) * inaccuracy / 1024;
			}

			if (info.Image != null)
			{
				anim = new Animation(world, info.Image, () => facing);
				anim.PlayRepeating("idle");
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				trail = new ContrailRenderable(world, color, info.ContrailLength, info.ContrailDelay, 0);
			}
		}

		bool JammedBy(TraitPair<JamsMissiles> tp)
		{
			if ((tp.Actor.CenterPosition - pos).HorizontalLengthSquared > tp.Trait.Range * tp.Trait.Range)
				return false;

			if (tp.Actor.Owner.Stances[args.SourceActor.Owner] == Stance.Ally && !tp.Trait.AlliedMissiles)
				return false;

			return tp.Actor.World.SharedRandom.Next(100 / tp.Trait.Chance) == 0;
		}

		public void Tick(World world)
		{
			ticks++;
			anim.Tick();

			// Missile tracks target
			if (args.GuidedTarget.IsValidFor(args.SourceActor) && lockOn)
				targetPosition = args.GuidedTarget.CenterPosition;

			var dist = targetPosition + offset - pos;
			var desiredFacing = OpenRA.Traits.Util.GetFacing(dist, facing);
			var desiredAltitude = targetPosition.Z;
			var jammed = info.Jammable && world.ActorsWithTrait<JamsMissiles>().Any(JammedBy);

			if (jammed)
			{
				desiredFacing = facing + world.SharedRandom.Next(-20, 21);
				desiredAltitude = world.SharedRandom.Next(-43, 86);
			}
			else if (!args.GuidedTarget.IsValidFor(args.SourceActor))
				desiredFacing = facing;

			facing = OpenRA.Traits.Util.TickFacing(facing, desiredFacing, info.ROT);
			var move = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing)) * info.Speed.Range / 1024;
			if (targetPosition.Z > 0 && info.TurboBoost)
				move = (move * 3) / 2;

			if (pos.Z != desiredAltitude)
			{
				var delta = move.HorizontalLength * info.MaximumPitch.Tan() / 1024;
				var dz = (targetPosition.Z - pos.Z).Clamp(-delta, delta);
				move += new WVec(0, 0, dz);
			}

			pos += move;

			if (info.Trail != null && --ticksToNextSmoke < 0)
			{
				world.AddFrameEndTask(w => w.Add(new Smoke(w, pos - 3 * move / 2, info.Trail)));
				ticksToNextSmoke = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				trail.Update(pos);

			var cell = world.Map.CellContaining(pos);

			var shouldExplode = (pos.Z < 0) // Hit the ground
				|| (dist.LengthSquared < info.CloseEnough.Range * info.CloseEnough.Range) // Within range
				|| (info.RangeLimit != 0 && ticks > info.RangeLimit) // Ran out of fuel
				|| (!info.High && world.ActorMap.GetUnitsAt(cell).Any(a => a.HasTrait<IBlocksBullets>())) // Hit a wall
				|| !world.Map.Contains(cell) // This also avoids an IndexOutOfRangeException in GetTerrainInfo below.
				|| (!string.IsNullOrEmpty(info.BoundToTerrainType) && world.Map.GetTerrainInfo(cell).Type != info.BoundToTerrainType); // Hit incompatible terrain

			if (shouldExplode)
				Explode(world);
		}

		void Explode(World world)
		{
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, trail)));

			world.AddFrameEndTask(w => w.Remove(this));

			// Don't blow up in our launcher's face!
			if (ticks <= info.Arm)
				return;

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return trail;

			if (!args.SourceActor.World.FogObscures(wr.world.Map.CellContaining(pos)))
			{
				if (info.Shadow)
				{
					var shadowPos = new WPos(pos.X, pos.Y, 0);
					foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
						yield return r;
				}

				var palette = wr.Palette(args.Weapon.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
