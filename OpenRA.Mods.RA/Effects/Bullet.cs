#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
		public readonly int Speed = 1;
		public readonly string Trail = null;
		[Desc("Pixels at maximum range")]
		public readonly float Inaccuracy = 0;
		public readonly string Image = null;
		[Desc("Check for whether an actor with Wall: trait blocks fire")]
		public readonly bool High = false;
		public readonly bool Shadow = false;
		public readonly float Angle = 0;
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

		ContrailRenderable trail;
		Animation anim;

		[Sync] WAngle angle;
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

			// Convert ProjectileArg definitions to world coordinates
			// TODO: Change the yaml definitions so we don't need this
			var range = new WRange((int)(1024 * args.Weapon.Range)); // Range in world units
			var inaccuracy = new WRange((int)(info.Inaccuracy * 1024 / Game.CellSize)); // Offset in world units at max range
			var speed = (int)(info.Speed * 4 * 1024 / (10 * Game.CellSize)); // Speed in world units per tick
			angle = WAngle.ArcTan((int)(info.Angle * 4 * 1024), 1024); // Angle in world angle

			target = args.PassiveTarget;
			if (info.Inaccuracy > 0)
			{
				var maxOffset = inaccuracy.Range * (target - pos).Length / range.Range;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			facing = Traits.Util.GetFacing(target - pos, 0);
			length = Math.Max((target - pos).Length / speed, 1);

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

				var palette = wr.Palette(args.Weapon.Underwater ? "shadow" : "effect");
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
