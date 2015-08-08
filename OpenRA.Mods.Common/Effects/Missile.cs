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
	class MissileInfo : IProjectileInfo
	{
		public readonly string Image = null;
		[SequenceReference("Image")] public readonly string Sequence = "idle";

		public readonly string Palette = "effect";

		public readonly bool Shadow = false;

		[Desc("Projectile speed in WDist / tick")]
		public readonly WDist Speed = new WDist(8);

		[Desc("Maximum vertical pitch when changing altitude.")]
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(30);

		[Desc("How many ticks before this missile is armed and can explode.")]
		public readonly int Arm = 0;

		[Desc("Is the missile blocked by actors with BlocksProjectiles: trait.")]
		public readonly bool Blockable = true;

		[Desc("Maximum offset at the maximum range")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Probability of locking onto and following target.")]
		public readonly int LockOnProbability = 100;

		[Desc("In n/256 per tick.")]
		public readonly int RateOfTurn = 5;

		[Desc("Explode when following the target longer than this many ticks.")]
		public readonly int RangeLimit = 0;

		[Desc("Trail animation.")]
		public readonly string Trail = null;

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		public readonly string TrailPalette = "effect";
		public readonly bool TrailUsePlayerPalette = false;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;

		[Desc("Should missile targeting be thrown off by nearby actors with JamsMissiles.")]
		public readonly bool Jammable = true;

		[Desc("Explodes when leaving the following terrain type, e.g., Water for torpedoes.")]
		public readonly string BoundToTerrainType = "";

		[Desc("Explodes when inside this proximity radius to target.",
			"Note: If this value is lower than the missile speed, this check might",
			"not trigger fast enough, causing the missile to fly past the target.")]
		public readonly WDist CloseEnough = new WDist(298);

		public IEffect Create(ProjectileArgs args) { return new Missile(this, args); }
	}

	class Missile : IEffect, ISync
	{
		readonly MissileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		int ticksToNextSmoke;
		ContrailRenderable contrail;
		string trailPalette;

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

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Traits.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				offset = WVec.FromPDF(world.SharedRandom, 2) * inaccuracy / 1024;
			}

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, () => facing);
				anim.PlayRepeating("idle");
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
			if ((tp.Actor.CenterPosition - pos).HorizontalLengthSquared > tp.Trait.Range * tp.Trait.Range)
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

			facing = OpenRA.Traits.Util.TickFacing(facing, desiredFacing, info.RateOfTurn);
			var move = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing)) * info.Speed.Length / 1024;

			if (pos.Z != desiredAltitude)
			{
				var delta = move.HorizontalLength * info.MaximumPitch.Tan() / 1024;
				var dz = (targetPosition.Z - pos.Z).Clamp(-delta, delta);
				move += new WVec(0, 0, dz);
			}

			pos += move;

			if (!string.IsNullOrEmpty(info.Trail) && --ticksToNextSmoke < 0)
			{
				world.AddFrameEndTask(w => w.Add(new Smoke(w, pos - 3 * move / 2, info.Trail, trailPalette, info.Sequence)));
				ticksToNextSmoke = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			var cell = world.Map.CellContaining(pos);

			var shouldExplode = (pos.Z < 0) // Hit the ground
				|| (dist.LengthSquared < info.CloseEnough.LengthSquared) // Within range
				|| (info.RangeLimit != 0 && ticks > info.RangeLimit) // Ran out of fuel
				|| (info.Blockable && world.ActorMap.GetUnitsAt(cell).Any(a => a.HasTrait<IBlocksProjectiles>())) // Hit a wall or other blocking obstacle
				|| !world.Map.Contains(cell) // This also avoids an IndexOutOfRangeException in GetTerrainInfo below.
				|| (!string.IsNullOrEmpty(info.BoundToTerrainType) && world.Map.GetTerrainInfo(cell).Type != info.BoundToTerrainType); // Hit incompatible terrain

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
