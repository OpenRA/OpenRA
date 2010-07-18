#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class BulletInfo : IProjectileInfo
	{
		public readonly int Speed = 1;
		public readonly string Trail = null;
		public readonly float Inaccuracy = 0;			// pixels at maximum range
		public readonly string Image = null;
		public readonly bool High = false;
		public readonly int RangeLimit = 0;
		public readonly int Arm = 0;
		public readonly bool Shadow = false;
		public readonly bool Proximity = false;
		public readonly float Angle = 0;

		public IEffect Create(ProjectileArgs args) { return new Bullet( this, args ); }
	}

	public class Bullet : IEffect
	{
		readonly BulletInfo Info;
		readonly ProjectileArgs Args;
		
		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			Info = info;
			Args = args;

			if (info.Inaccuracy > 0)
			{
				var factor = ((Args.dest - Args.src).Length / Game.CellSize) / args.weapon.Range;
				Args.dest += (info.Inaccuracy * factor * args.firedBy.World.SharedRandom.Gauss2D(2)).ToInt2();
			}

			if (Info.Image != null)
			{
				anim = new Animation(Info.Image, GetEffectiveFacing);
				anim.PlayRepeating("idle");
			}
		}

		int TotalTime() { return (Args.dest - Args.src).Length * BaseBulletSpeed / Info.Speed; }

		float GetAltitude()
		{
			var at = (float)t / TotalTime();
			return (Args.dest - Args.src).Length * Info.Angle * 4 * at * (1 - at);
		}

		int GetEffectiveFacing()
		{
			var at = (float)t / TotalTime();
			var attitude = Info.Angle * (1 - 2 * at);

			var rawFacing = Traits.Util.GetFacing(Args.dest - Args.src, 0);
			var u = (rawFacing % 128) / 128f;
			var scale = 512 * u * (1 - u);

			return (int)(rawFacing < 128 
				? rawFacing - scale * attitude 
				: rawFacing + scale * attitude);
		}

		public void Tick( World world )
		{
			t += 40;

			if (anim != null) anim.Tick();

			if (t > TotalTime()) Explode( world );

			if (Info.Trail != null)
			{
				var at = (float)t / TotalTime();
				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp(Args.src, Args.dest, at) - new float2(0, altitude);

				var highPos = (Info.High || Info.Angle > 0)
					? (pos - new float2(0, GetAltitude()))
					: pos;

				world.AddFrameEndTask(w => w.Add(
					new Smoke(w, highPos.ToInt2(), Info.Trail)));
			}

			if (!Info.High)		// check for hitting a wall
			{
				var at = (float)t / TotalTime();
				var pos = float2.Lerp(Args.src, Args.dest, at);
				var cell = Traits.Util.CellContaining(pos);

				if (world.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).Any(
					a => a.traits.Contains<IBlocksBullets>()))
				{
					Args.dest = pos.ToInt2();
					Explode(world);
				}
			}
		}

		const float height = .1f;

		public IEnumerable<Renderable> Render()
		{
			if (anim != null)
			{
				var at = (float)t / TotalTime();

				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp(Args.src, Args.dest, at) - new float2(0, altitude);

				if (Info.High || Info.Angle > 0)
				{
					if (Info.Shadow)
						yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, "shadow");

					var highPos = pos - new float2(0, GetAltitude());

					yield return new Renderable(anim.Image, highPos - .5f * anim.Image.size, Args.firedBy.Owner.Palette);
				}
				else
					yield return new Renderable(anim.Image, pos - .5f * anim.Image.size,
						Args.weapon.Underwater ? "shadow" : Args.firedBy.Owner.Palette);
			}
		}

		void Explode( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoImpacts(Args);
		}
	}
}
