#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class MissileInfo : IProjectileInfo
	{
		public readonly int Speed = 1;
		public readonly int Arm = 0;
		public readonly bool High = false;
		public readonly bool Shadow = true;
		public readonly bool Proximity = false;
		public readonly string Trail = null;
		public readonly float Inaccuracy = 0;
		public readonly string Image = null;
		public readonly int ROT = 5;
		public readonly int RangeLimit = 0;
		public readonly bool TurboBoost = false;

		public IEffect Create(ProjectileArgs args) { return new Missile( this, args ); }
	}

	class Missile : IEffect
	{
		readonly MissileInfo Info;
		readonly ProjectileArgs Args;

		int2 offset;
		float2 Pos;
		readonly Animation anim;
		int Facing;
		int t;
		int Altitude;

		public Missile(MissileInfo info, ProjectileArgs args)
		{
			Info = info;
			Args = args;

			Pos = Args.src;
			Altitude = Args.srcAltitude;
			Facing = Args.facing;

			if (info.Inaccuracy > 0)
				offset = (info.Inaccuracy * args.firedBy.World.SharedRandom.Gauss2D(2)).ToInt2();

			if (Info.Image != null)
			{
				anim = new Animation(Info.Image, () => Facing);
				anim.PlayRepeating("idle");
			}
		}

		const int MissileCloseEnough = 7;
		const float Scale = .2f;

		public void Tick( World world )
		{
			t += 40;

			var targetPosition = Args.target.CenterLocation + offset;

			var targetAltitude = 0;
			if (Args.target.IsValid && Args.target.IsActor && Args.target.Actor.HasTrait<IMove>())
				targetAltitude =  Args.target.Actor.Trait<IMove>().Altitude;
			Altitude += Math.Sign(targetAltitude - Altitude);

			Facing = Traits.Util.TickFacing(Facing,
				Traits.Util.GetFacing(targetPosition - Pos, Facing),
				Info.ROT);

			anim.Tick();

			var dist = targetPosition - Pos;
			if (dist.LengthSquared < MissileCloseEnough * MissileCloseEnough || !Args.target.IsValid )
				Explode(world);

			var speed = Scale * Info.Speed * ((targetAltitude > 0 && Info.TurboBoost) ? 1.5f : 1f);

			var angle = Facing / 128f * Math.PI;
			var move = speed * -float2.FromAngle((float)angle);
			Pos += move;

			if (Info.Trail != null)
				world.AddFrameEndTask(w => w.Add(
					new Smoke(w, (Pos - 1.5f * move - new int2(0, Altitude)).ToInt2(), Info.Trail)));

			if (Info.RangeLimit != 0 && t > Info.RangeLimit * 40)
				Explode(world);

			if (!Info.High)		// check for hitting a wall
			{
				var cell = Traits.Util.CellContaining(Pos);

				if (world.WorldActor.Trait<UnitInfluence>().GetUnitsAt(cell).Any(
					a => a.HasTrait<IBlocksBullets>()))
					Explode(world);
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Args.dest = Pos.ToInt2();
			if (t > Info.Arm * 40)	/* don't blow up in our launcher's face! */
				Combat.DoImpacts(Args);
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, Pos - 0.5f * anim.Image.size - new float2(0, Altitude), 
				Args.weapon.Underwater ? "shadow" : "effect", (int)Pos.Y);
		}
	}
}
