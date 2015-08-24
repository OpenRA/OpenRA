#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class ShrapnelInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new WDist(17) };

		[Desc("Image to display.")]
		public readonly string Image = null;

		[Desc("Loop these sequences of Image while this projectile is moving.")]
		[SequenceReference("Image")] public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		public readonly string ShadowPalette = "shadow";

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] Angle = { WAngle.Zero };

		public IEffect Create(ProjectileArgs args) { return new Shrapnel(this, args); }
	}

	public class Shrapnel : IEffect, ISync
	{
		readonly ShrapnelInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;
		[Sync] readonly WAngle angle;
		[Sync] readonly WDist speed;

		[Sync] WPos pos, target;
		[Sync] int length;
		[Sync] int facing;
		[Sync] int ticks;

		[Sync] public Actor SourceActor { get { return args.SourceActor; } }

		public Shrapnel(ShrapnelInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			this.pos = args.Source;

			var world = args.SourceActor.World;

			if (info.Angle.Length > 1)
				angle = new WAngle(world.SharedRandom.Next(info.Angle[0].Angle, info.Angle[1].Angle));
			else
				angle = info.Angle[0];

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			target = args.PassiveTarget;

			facing = OpenRA.Traits.Util.GetFacing(target - pos, 0);
			length = Math.Max((target - pos).Length / speed.Length, 1);

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, GetEffectiveFacing);
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}
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

			if (ticks++ >= length)
				Explode(world);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
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
			world.AddFrameEndTask(w => w.Remove(this));

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
		}
	}
}
