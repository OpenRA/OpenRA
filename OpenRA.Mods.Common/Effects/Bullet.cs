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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class BulletInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new WDist(17) };

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[Desc("Loop these sequences of Image while this projectile is moving.")]
		[SequenceReference("Image")] public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference] public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string Trail = null;

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] Angle = { WAngle.Zero };

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[PaletteReference("TrailUsePlayerPalette")] public readonly string TrailPalette = "effect";
		public readonly bool TrailUsePlayerPalette = false;

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
		readonly Animation anim;
		[Sync] readonly WAngle angle;
		[Sync] readonly WDist speed;

		ContrailRenderable contrail;
		string trailPalette;

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
			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Traits.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var range = OpenRA.Traits.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
				var maxOffset = inaccuracy * (target - pos).Length / range;
				target += WVec.FromPDF(world.SharedRandom, 2) * maxOffset / 1024;
			}

			facing = OpenRA.Traits.Util.GetFacing(target - pos, 0);
			length = Math.Max((target - pos).Length / speed.Length, 1);

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, GetEffectiveFacing);
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				contrail = new ContrailRenderable(world, color, info.ContrailLength, info.ContrailDelay, 0);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

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

			if (!string.IsNullOrEmpty(info.Trail) && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(args.Source, target, angle, ticks - info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new Smoke(w, delayedPos, info.Trail, trailPalette, info.Sequences.Random(world.SharedRandom))));
				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			var shouldExplode = ticks++ >= length // Flight length reached/exceeded
				|| (info.Blockable && BlocksProjectiles.AnyBlockingActorAt(world, pos)); // Hit a wall or other blocking obstacle

			if (shouldExplode)
				Explode(world);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

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
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));

			args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
		}
	}
}
