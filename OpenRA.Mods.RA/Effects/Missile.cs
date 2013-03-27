#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
	class MissileInfo : IProjectileInfo
	{
		public readonly int Speed = 1;
		public readonly int Arm = 0;
		[Desc("Check for whether an actor with Wall: trait blocks fire")]
		public readonly bool High = false;
		public readonly bool Shadow = true;
		public readonly bool Proximity = false;
		public readonly string Trail = null;
		public readonly float Inaccuracy = 0;
		public readonly string Image = null;
		[Desc("Rate of Turning")]
		public readonly int ROT = 5;
		[Desc("Explode when following the target longer than this.")]
		public readonly int RangeLimit = 0;
		public readonly bool TurboBoost = false;
		public readonly int TrailInterval = 2;
		public readonly int ContrailLength = 0;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;

		public IEffect Create(ProjectileArgs args) { return new Missile( this, args ); }
	}

	class Missile : IEffect
	{
		readonly MissileInfo Info;
		readonly ProjectileArgs Args;

		PVecInt offset;
		public PSubPos SubPxPosition;
		public PPos PxPosition { get { return SubPxPosition.ToPPos(); } }

		readonly Animation anim;
		int Facing;
		int t;
		int Altitude;
		ContrailHistory Trail;

		public Missile(MissileInfo info, ProjectileArgs args)
		{
			Info = info;
			Args = args;

			SubPxPosition = Args.src.ToPSubPos();
			Altitude = Args.srcAltitude;
			Facing = Args.facing;

			if (info.Inaccuracy > 0)
				offset = (PVecInt)(info.Inaccuracy * args.firedBy.World.SharedRandom.Gauss2D(2)).ToInt2();

			if (Info.Image != null)
			{
				anim = new Animation(Info.Image, () => Facing);
				anim.PlayRepeating("idle");
			}

			if (Info.ContrailLength > 0)
			{
				Trail = new ContrailHistory(Info.ContrailLength,
					Info.ContrailUsePlayerColor ? ContrailHistory.ChooseColor(args.firedBy) : Info.ContrailColor,
					Info.ContrailDelay);
			}
		}

		// In pixels
		const int MissileCloseEnough = 7;
		int ticksToNextSmoke;

		public void Tick( World world )
		{
			t += 40;

			// In pixels
			var dist = Args.target.CenterLocation + offset - PxPosition;

			var targetAltitude = 0;
			if (Args.target.IsValid && Args.target.IsActor && Args.target.Actor.HasTrait<IMove>())
				targetAltitude =  Args.target.Actor.Trait<IMove>().Altitude;

			Altitude += Math.Sign(targetAltitude - Altitude);

			if (Args.target.IsValid)
				Facing = Traits.Util.TickFacing(Facing,
					Traits.Util.GetFacing(dist, Facing),
					Info.ROT);

			anim.Tick();

			if (dist.LengthSquared < MissileCloseEnough * MissileCloseEnough && Args.target.IsValid )
				Explode(world);

			// TODO: Replace this with a lookup table
			var dir = (-float2.FromAngle((float)(Facing / 128f * Math.PI))).ToPSubVec();

			var move = Info.Speed * dir;
			if (targetAltitude > 0 && Info.TurboBoost)
				move = (move * 3) / 2;
			move = move / 5;

			SubPxPosition += move;

			if (Info.Trail != null)
			{
				var sp = ((SubPxPosition - (move * 3) / 2)).ToPPos() - new PVecInt(0, Altitude);

				if (--ticksToNextSmoke < 0)
				{
					world.AddFrameEndTask(w => w.Add(new Smoke(w, sp, Info.Trail)));
					ticksToNextSmoke = Info.TrailInterval;
				}
			}

			if (Info.RangeLimit != 0 && t > Info.RangeLimit * 40)
				Explode(world);

			if (!Info.High)		// check for hitting a wall
			{
				var cell = PxPosition.ToCPos();
				if (world.ActorMap.GetUnitsAt(cell).Any(a => a.HasTrait<IBlocksBullets>()))
					Explode(world);
			}

			if (Trail != null)
				Trail.Tick(PxPosition - new PVecInt(0, Altitude));
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Args.dest = PxPosition;
			if (t > Info.Arm * 40)	/* don't blow up in our launcher's face! */
				Combat.DoImpacts(Args);
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			if (Args.firedBy.World.RenderedShroud.IsVisible(PxPosition.ToCPos()))
				yield return new Renderable(anim.Image, PxPosition.ToFloat2() - 0.5f * anim.Image.size - new float2(0, Altitude),
					wr.Palette(Args.weapon.Underwater ? "shadow" : "effect"), PxPosition.Y);

			if (Trail != null)
				Trail.Render(Args.firedBy);
		}
	}
}
