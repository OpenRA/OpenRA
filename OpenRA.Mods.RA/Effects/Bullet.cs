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
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class BulletInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WRange / tick, two values indicate variable velocity")]
		public readonly WRange[] Speed = { new WRange(17) };
		public readonly string Trail = null;
		[Desc("Maximum offset at the maximum range")]
		public readonly WRange Inaccuracy = WRange.Zero;
		public readonly string Image = null;
		[Desc("Check for whether an actor with Wall: trait blocks fire")]
		public readonly bool High = false;
		public readonly bool Shadow = false;
		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] Angle = { WAngle.Zero };
		public readonly int TrailInterval = 2;
		public readonly int TrailDelay = 1;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;

		public IEffect Create(ProjectileArgs args) { return new Bullet(this, args); }
	}

	public class Bullet : IEffect, ISync
	{
		readonly BulletInfo info;
		readonly ProjectileArgs args;
		[Sync] readonly WAngle angle;
		[Sync] readonly WRange speed;

		ContrailRenderable trail;
		Animation anim;

		[Sync] WPos pos, target;
		[Sync] int length;
		[Sync] int facing;
		[Sync] int ticks, smokeTicks;

		[Sync] public Actor SourceActor { get { return args.SourceActor; } }

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			this.pos = args.Source;

			if (info.Angle.Length > 1 && info.Speed.Length > 1)
			{
				angle = new WAngle(args.SourceActor.World.SharedRandom.Next(info.Angle[0].Angle, info.Angle[1].Angle));
				speed = new WRange(args.SourceActor.World.SharedRandom.Next(info.Speed[0].Range, info.Speed[1].Range));
			}
			else
			{
				angle = info.Angle[0];
				speed = info.Speed[0];
			}

			target = args.PassiveTarget;
			if (info.Inaccuracy.Range > 0)
			{
				var maxOffset = info.Inaccuracy.Range * (target - pos).Length / args.Weapon.Range.Range;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			facing = Traits.Util.GetFacing(target - pos, 0);
			length = Math.Max((target - pos).Length / speed.Range, 1);

			if (info.Image != null)
			{
				anim = new Animation(info.Image, GetEffectiveFacing);
				anim.PlayRepeating("idle");
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				trail = new ContrailRenderable(args.SourceActor.World, color, info.ContrailLength, info.ContrailDelay, 0);
			}

			smokeTicks = info.TrailDelay;
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

			if (info.Trail != null && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(args.Source, target, angle, ticks - info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new Smoke(w, delayedPos, info.Trail)));
				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				trail.Update(pos);

			if (ticks++ >= length || (!info.High && world.ActorMap
				.GetUnitsAt(pos.ToCPos()).Any(a => a.HasTrait<IBlocksBullets>())))
			{
				Explode(world);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return trail;

			if (anim == null || ticks >= length)
				yield break;

			var cell = pos.ToCPos();
			if (!args.SourceActor.World.FogObscures(cell))
			{
				if (info.Shadow)
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
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, trail)));

			world.AddFrameEndTask(w => w.Remove(this));

			Combat.DoImpacts(pos, args.SourceActor, args.Weapon, args.FirepowerModifier);
		}
	}
}
