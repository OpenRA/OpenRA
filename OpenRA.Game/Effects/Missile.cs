#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
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

			var targetUnit = Args.target.traits.GetOrDefault<Unit>();
			var targetAltitude = targetUnit != null ? targetUnit.Altitude : 0;
			Altitude += Math.Sign(targetAltitude - Altitude);

			Traits.Util.TickFacing(ref Facing,
				Traits.Util.GetFacing(targetPosition - Pos, Facing),
				Info.ROT);

			anim.Tick();

			var dist = targetPosition - Pos;
			if (dist.LengthSquared < MissileCloseEnough * MissileCloseEnough || Args.target.IsDead)
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

				if (world.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).Any(
					a => a.traits.Contains<IBlocksBullets>()))
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
			yield return new Renderable(anim.Image, Pos - 0.5f * anim.Image.size - new float2(0, Altitude), "effect");
		}
	}
}
