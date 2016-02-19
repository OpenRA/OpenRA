#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	public class GravityBombInfo : IProjectileInfo
	{
		public readonly string Image = null;

		[Desc("Loop a randomly chosen sequence of Image from this list while falling.")]
		[SequenceReference("Image")] public readonly string[] Sequences = { "idle" };

		[Desc("Sequence to play when launched. Skipped if null or empty.")]
		[SequenceReference("Image")] public readonly string OpenSequence = null;

		[PaletteReference] public readonly string Palette = "effect";

		public readonly bool Shadow = false;

		[PaletteReference] public readonly string ShadowPalette = "shadow";

		[Desc("Projectile movement vector per tick (forward, right, up), use negative values for opposite directions.")]
		public readonly WVec Velocity = WVec.Zero;

		[Desc("Value added to Velocity every tick.")]
		public readonly WVec Acceleration = new WVec(0, 0, -15);

		public IProjectile Create(ProjectileArgs args) { return new GravityBomb(this, args); }
	}

	public class GravityBomb : IProjectile, ISync
	{
		readonly GravityBombInfo info;
		readonly Animation anim;
		readonly ProjectileArgs args;
		readonly WVec acceleration;
		[Sync] WVec velocity;
		[Sync] WPos pos;

		public GravityBomb(GravityBombInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			var convertedVelocity = new WVec(info.Velocity.Y, -info.Velocity.X, info.Velocity.Z);
			velocity = convertedVelocity.Rotate(WRot.FromFacing(args.Facing));
			acceleration = new WVec(info.Acceleration.Y, -info.Acceleration.X, info.Acceleration.Z);

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(args.SourceActor.World, info.Image);

				if (!string.IsNullOrEmpty(info.OpenSequence))
					anim.PlayThen(info.OpenSequence, () => anim.PlayRepeating(info.Sequences.Random(args.SourceActor.World.SharedRandom)));
				else
					anim.PlayRepeating(info.Sequences.Random(args.SourceActor.World.SharedRandom));

				args.SourceActor.World.ScreenMap.Add(this, pos, anim.Image.Bounds);
			}
		}

		public void Tick(World world)
		{
			pos += velocity;
			velocity += acceleration;

			if (pos.Z <= args.PassiveTarget.Z)
			{
				pos += new WVec(0, 0, args.PassiveTarget.Z - pos.Z);
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
				args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
			}

			if (anim != null)
			{
				anim.Tick();
				world.ScreenMap.Update(this, pos, anim.Image.Bounds);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (anim == null)
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
	}
}
