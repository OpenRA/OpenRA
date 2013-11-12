#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class MissileInfo : IProjectileInfo
	{
		public readonly int Speed = 1;
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(30);
		public readonly int Arm = 0;
		[Desc("Check for whether an actor with Wall: trait blocks fire")]
		public readonly bool High = false;
		public readonly string Trail = null;
		public readonly float Inaccuracy = 0;
		public readonly string Image = null;
		[Desc("Rate of Turning")]
		public readonly int ROT = 5;
		[Desc("Explode when following the target longer than this.")]
		public readonly int RangeLimit = 0;
		public readonly bool TurboBoost = false;
		public readonly int TrailInterval = 2;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;
		public readonly bool Jammable = true;

		public IEffect Create(ProjectileArgs args) { return new Missile(this, args); }
	}

	class Missile : IEffect, ISync
	{
		static readonly WRange MissileCloseEnough = new WRange(7 * 1024 / Game.CellSize);

		readonly MissileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;
		readonly int speed;

		int ticksToNextSmoke;
		ContrailRenderable trail;

		[Sync] WPos pos;
		[Sync] int facing;

		[Sync] WPos targetPosition;
		[Sync] WVec offset;
		[Sync] int ticks;

		[Sync] public Actor SourceActor { get { return args.SourceActor; } }
		[Sync] public Target GuidedTarget { get { return args.GuidedTarget; } }

		public Missile(MissileInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			pos = args.Source;
			facing = args.Facing;

			targetPosition = args.PassiveTarget;

			// Convert ProjectileArg definitions to world coordinates
			// TODO: Change the yaml definitions so we don't need this
			var inaccuracy = (int)(info.Inaccuracy * 1024 / Game.CellSize);
			speed = info.Speed * 1024 / (5 * Game.CellSize);

			if (info.Inaccuracy > 0)
				offset = WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * inaccuracy / 1024;

			if (info.Image != null)
			{
				anim = new Animation(info.Image, () => facing);
				anim.PlayRepeating("idle");
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				trail = new ContrailRenderable(args.SourceActor.World, color, info.ContrailLength, info.ContrailDelay, 0);
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
			if (args.GuidedTarget.IsValidFor(args.SourceActor))
				targetPosition = args.GuidedTarget.CenterPosition;

			var dist = targetPosition + offset - pos;
			var desiredFacing = Traits.Util.GetFacing(dist, facing);
			var desiredAltitude = targetPosition.Z;
			var jammed = info.Jammable && world.ActorsWithTrait<JamsMissiles>().Any(j => JammedBy(j));

			if (jammed)
			{
				desiredFacing = facing + world.SharedRandom.Next(-20, 21);
				desiredAltitude = world.SharedRandom.Next(-43, 86);
			}
			else if (!args.GuidedTarget.IsValidFor(args.SourceActor))
				desiredFacing = facing;

			facing = Traits.Util.TickFacing(facing, desiredFacing, info.ROT);
			var move = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing)) * speed / 1024;
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

			var shouldExplode = (pos.Z < 0) // Hit the ground
				|| (dist.LengthSquared < MissileCloseEnough.Range * MissileCloseEnough.Range) // Within range
				|| (info.RangeLimit != 0 && ticks > info.RangeLimit) // Ran out of fuel
				|| (!info.High && world.ActorMap.GetUnitsAt(pos.ToCPos())
					.Any(a => a.HasTrait<IBlocksBullets>())); // Hit a wall

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

			Combat.DoImpacts(pos, args.SourceActor, args.Weapon, args.FirepowerModifier);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return trail;

			if (!args.SourceActor.World.FogObscures(pos.ToCPos()))
			{
				var palette = wr.Palette(args.Weapon.Underwater ? "shadow" : "effect");
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
