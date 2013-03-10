#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public readonly int TrailInterval = 2;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;

		public IEffect Create(ProjectileArgs args) { return new Bullet( this, args ); }
	}

	public class Bullet : IEffect
	{
		readonly BulletInfo Info;
		readonly ProjectileArgs Args;

		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */
		ContrailHistory Trail;

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			Info = info;
			Args = args;

			if (info.Inaccuracy > 0)
			{
				var factor = ((Args.dest - Args.src).ToCVec().Length) / args.weapon.Range;
				Args.dest += (PVecInt) (info.Inaccuracy * factor * args.firedBy.World.SharedRandom.Gauss2D(2)).ToInt2();
				Log.Write("debug", "Bullet with Inaccuracy; factor: #{0}; Projectile dest: {1}", factor, Args.dest);
			}

			if (Info.Image != null)
			{
				anim = new Animation(Info.Image, GetEffectiveFacing);
				anim.PlayRepeating("idle");
			}

			if (Info.ContrailLength > 0)
			{
				Trail = new ContrailHistory(Info.ContrailLength,
					Info.ContrailUsePlayerColor ? ContrailHistory.ChooseColor(args.firedBy) : Info.ContrailColor,
					Info.ContrailDelay);
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

		int ticksToNextSmoke;

		public void Tick( World world )
		{
			t += 40;

			if (anim != null) anim.Tick();

			if (t > TotalTime()) Explode( world );

			{
				var at = (float)t / TotalTime();
				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp(Args.src.ToFloat2(), Args.dest.ToFloat2(), at) - new float2(0, altitude);

				var highPos = (Info.High || Info.Angle > 0)
					? (pos - new float2(0, GetAltitude()))
					: pos;

				if (Info.Trail != null && --ticksToNextSmoke < 0)
				{
					world.AddFrameEndTask(w => w.Add(
						new Smoke(w, (PPos) highPos.ToInt2(), Info.Trail)));
					ticksToNextSmoke = Info.TrailInterval;
				}

				if (Trail != null)
					Trail.Tick((PPos)highPos.ToInt2());
			}

			if (!Info.High)		// check for hitting a wall
			{
				var at = (float)t / TotalTime();
				var pos = float2.Lerp(Args.src.ToFloat2(), Args.dest.ToFloat2(), at);
				var cell = ((PPos) pos.ToInt2()).ToCPos();

				if (world.ActorMap.GetUnitsAt(cell).Any(
					a => a.HasTrait<IBlocksBullets>()))
				{
					Args.dest = (PPos) pos.ToInt2();
					Explode(world);
				}
			}
		}

		const float height = .1f;

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			if (anim != null)
			{
				var at = (float)t / TotalTime();

				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp(Args.src.ToFloat2(), Args.dest.ToFloat2(), at) - new float2(0, altitude);

				if (Args.firedBy.World.RenderedShroud.IsVisible(((PPos) pos.ToInt2()).ToCPos()))
				{
					if (Info.High || Info.Angle > 0)
					{
						if (Info.Shadow)
							yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, wr.Palette("shadow"), (int)pos.Y);

						var highPos = pos - new float2(0, GetAltitude());

						yield return new Renderable(anim.Image, highPos - .5f * anim.Image.size, wr.Palette("effect"), (int)pos.Y);
					}
					else
						yield return new Renderable(anim.Image, pos - .5f * anim.Image.size,
							wr.Palette(Args.weapon.Underwater ? "shadow" : "effect"), (int)pos.Y);
				}
			}

			if (Trail != null)
				Trail.Render(Args.firedBy);
		}

		void Explode( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoImpacts(Args);
		}
	}
}
