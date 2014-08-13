#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class LineImpactProjectileInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WRange / tick,", "two values indicate variable velocity")]
		public readonly WRange[] Speed = { new WRange(17) };

		[Desc("The distance between applying the", "warhead effects")]
		public readonly WRange RangeBetweenImpacts = new WRange(16);

		[Desc("How far beyond the target the", "projectile keeps on travelling")]
		public readonly WRange BeyondTargetRange = new WRange(128);

		[Desc("How close the target must the", "projectile be to apply weapon effects")]
		public readonly WRange MaxTargetRange = new WRange(128);

		[Desc("How far away from its source must", "the projectile be before applying weapon effects")]
		public readonly WRange MinRangeSource = new WRange(256);

		[Desc("The maximum reduction to warhead effects")]
		public readonly int FalloffMaxReduction = 75;

		[Desc("The distance before the falloff starts")]
		public readonly WRange FalloffStartDistance = new WRange(128);

		[Desc("The distance before the falloff ends")]
		public readonly WRange FalloffEndDistance = new WRange(4096);

		[Desc("Trail the projectile leaves as it travels")]
		public readonly string Trail = null;

		[Desc("Maximum offset at the maximum range")]
		public readonly WRange Inaccuracy = WRange.Zero;

		[Desc("Image of the projectile")]
		public readonly string Image = null;

		[Desc("Check for whether an actor with Wall: trait blocks fire")]
		public readonly bool High = false;

		[Desc("Does the projectile cast a shadow")]
		public readonly bool Shadow = false;

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] Angle = { WAngle.Zero };

		[Desc("The number of ticks between drawing the trail")]
		public readonly int TrailInterval = 2;

		[Desc("Ticks delay before trail starts")]
		public readonly int TrailDelay = 1;

		[Desc("Length of the contrail")]
		public readonly int ContrailLength = 0;

		[Desc("The colour of the contrail")]
		public readonly Color ContrailColor = Color.White;

		[Desc("Does the contrail colour match the player colour")]
		public readonly bool ContrailUsePlayerColor = false;

		[Desc("The delay before the contrail starts")]
		public readonly int ContrailDelay = 1;

		public IEffect Create(ProjectileArgs args) { return new LineImpactProjectile(this, args); }
	}

	public class LineImpactProjectile : IEffect, ISync
	{
		readonly LineImpactProjectileInfo Info;
		readonly ProjectileArgs args;
		[Sync] readonly WAngle angle;
		[Sync] readonly WRange speed;

		ContrailRenderable trail;
		Animation anim;

		[Sync] WPos pos, target, prevImpactPos;
		[Sync] int length;
		[Sync] int facing;
		[Sync] int ticks, smokeTicks;

		[Sync] public Actor SourceActor { get { return args.SourceActor; } }

		public LineImpactProjectile(LineImpactProjectileInfo info, ProjectileArgs args)
		{
			Info = info;
			this.args = args;
			pos = args.Source;
			prevImpactPos = pos;

			var world = args.SourceActor.World;

			if (Info.Angle.Length > 1 && Info.Speed.Length > 1)
			{
				angle = new WAngle(args.SourceActor.World.SharedRandom.Next(Info.Angle[0].Angle, Info.Angle[1].Angle));
				speed = new WRange(args.SourceActor.World.SharedRandom.Next(Info.Speed[0].Range, Info.Speed[1].Range));
			}
			else
			{
				angle = Info.Angle[0];
				speed = Info.Speed[0];
			}

			target = args.PassiveTarget;
			if (Info.Inaccuracy.Range > 0)
			{
				var maxOffset = Info.Inaccuracy.Range * (target - pos).Length / args.Weapon.Range.Range;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			facing = Traits.Util.GetFacing(target - pos, 0);
			// Update the target position with the range we shoot beyond the target by
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing));
			var overshoot = dir * Info.BeyondTargetRange.Range / 1024;
			target = target + overshoot;

			length = Math.Max(((target - pos).Length) / speed.Range, 1);

			if (Info.Image != null)
			{
				anim = new Animation(world, Info.Image, GetEffectiveFacing);
				anim.PlayRepeating("idle");
			}

			if (Info.ContrailLength > 0)
			{
				var color = Info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : Info.ContrailColor;
				trail = new ContrailRenderable(args.SourceActor.World, color, Info.ContrailLength, Info.ContrailDelay, 0);
			}

			smokeTicks = Info.TrailDelay;
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

			pos = WPos.LerpQuadratic(args.Source, target, angle, ticks, length);

			if (Info.Trail != null && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(args.Source, target, angle, ticks - Info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new Smoke(w, delayedPos, Info.Trail)));
				smokeTicks = Info.TrailInterval;
			}

			var killProjectile = ticks++ >= length;
			var coveredSections = (pos - prevImpactPos).Length / Info.RangeBetweenImpacts.Range;
			var falloff = 100;
			if (coveredSections > 0)
			{
				// Start at the last impact location. Make a copy, do not just create a pointer.
				var currentPos = new WPos(prevImpactPos.X, prevImpactPos.Y, prevImpactPos.Z);
				// Divide the distance traveled into steps and iterate over them
				for (var i = 1; i <= coveredSections; i++)
				{
					currentPos = WPos.LerpQuadratic(prevImpactPos, pos, angle, i, coveredSections);
					var distFromSource = (args.Source - currentPos).Length;
					var distFromTarget = (args.PassiveTarget - currentPos).Length;
					// Current falloff multiplier = 100 - ((Current distance - Falloff Start Range) / (Falloff End Range - Falloff Start Range) * Max Falloff);
					// With the distance limited between 0 and falloff range.
					var falloffRange = Math.Max((Info.FalloffEndDistance - Info.FalloffStartDistance).Range, 1);
					falloff = (currentPos - args.Source).Length - Info.FalloffStartDistance.Range;
					falloff = Math.Min(falloff, falloffRange);
					falloff = Math.Max(falloff, 0);
					falloff = 100 - (falloff * Info.FalloffMaxReduction / falloffRange);
					falloff = Math.Max(Math.Min(falloff, 100),0);
					if (!Info.High && world.ActorMap.GetUnitsAt(world.Map.CellContaining(currentPos)).Any(a => a.HasTrait<IBlocksBullets>()))
					{
						pos = currentPos;
						killProjectile = true;
						// Do not care about range restrictions if impacting an obstacle
						Impact(currentPos, falloff);
						break;
					}
					else
						// Only allow impacts if far enough from source, and close enough to target.
						if (distFromSource > Info.MinRangeSource.Range && distFromTarget < Info.MaxTargetRange.Range)
							Impact(currentPos, falloff);
				}
				// Finally, update the lastImpactPos for next time
				prevImpactPos = new WPos(currentPos.X, currentPos.Y, currentPos.Z);
			}

			if (Info.ContrailLength > 0)
				trail.Update(pos);

			if (killProjectile)
			{
				Explode(world);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (Info.ContrailLength > 0)
				yield return trail;

			if (anim == null || ticks >= length)
				yield break;

			var cell = wr.world.Map.CellContaining(pos);
			if (!args.SourceActor.World.FogObscures(cell))
			{
				if (Info.Shadow)
				{
					var shadowPos = pos - new WVec(0, 0, pos.Z);
					foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
						yield return r;
				}

				var palette = wr.Palette(args.Weapon.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}

		void Explode(World world)
		{
			if (Info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, trail)));

			world.AddFrameEndTask(w => w.Remove(this));
		}

		void Impact(WPos pos, int falloffFactor)
		{
			var adjustedModifiers = new List<int>();
			for (var i = 0; i < args.DamageModifiers.Count(); i++)
				adjustedModifiers.Add(falloffFactor * args.DamageModifiers.ElementAt(i) / 100);

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, adjustedModifiers);
		}
	}
}
