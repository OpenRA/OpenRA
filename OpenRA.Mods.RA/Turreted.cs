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
using OpenRA.Mods.RA.Render;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TurretedInfo : ITraitInfo, UsesInit<TurretFacingInit>
	{
		public readonly string Turret = "primary";
		[Desc("Rate of Turning")]
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;
		public readonly int[] Offset = {0,0};
		public readonly bool AlignWhenIdle = false;

		public virtual object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick, ISync, IResolveOrder
	{
		[Sync] public int turretFacing = 0;
		public int? desiredFacing;
		public TurretedInfo info;
		protected Turret turret;
		IFacing facing;

		public static int GetInitialTurretFacing(ActorInitializer init, int def)
		{
			if (init.Contains<TurretFacingInit>())
				return init.Get<TurretFacingInit,int>();

			if (init.Contains<FacingInit>())
				return init.Get<FacingInit,int>();

			return def;
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
		{
			this.info = info;
			turretFacing = GetInitialTurretFacing(init, info.InitialFacing);
			facing = init.self.TraitOrDefault<IFacing>();
			turret = new Turret(info.Offset);
		}

		public virtual void Tick(Actor self)
		{
			var df = desiredFacing ?? ( facing != null ? facing.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, info.ROT);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turretFacing );
			return turretFacing == desiredFacing;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			if (info.AlignWhenIdle && order.OrderString != "Attack" && order.OrderString != "AttackHold")
				desiredFacing = null;
		}

		public PVecFloat PxPosition(Actor self, IFacing facing)
		{
			return turret.PxPosition(self, facing);
		}
	}

	public class Turret
	{
		public PVecInt UnitSpacePosition;	// where, in the unit's local space.
		public PVecInt ScreenSpacePosition;	// screen-space hack to make things line up good.

		public Turret(int[] offset)
		{
			ScreenSpacePosition = (PVecInt) offset.AbsOffset().ToInt2();
			UnitSpacePosition = (PVecInt) offset.RelOffset().ToInt2();
		}

		public PVecFloat PxPosition(Actor self, IFacing facing)
		{
			// Things that don't have a rotating base don't need the turrets repositioned
			if (facing == null) return ScreenSpacePosition;

			var ru = self.TraitOrDefault<RenderUnit>();
			var numDirs = (ru != null) ? ru.anim.CurrentSequence.Facings : 8;
			var bodyFacing = facing.Facing;
			var quantizedFacing = Util.QuantizeFacing(bodyFacing, numDirs) * (256 / numDirs);

			return (PVecFloat)Util.RotateVectorByFacing(UnitSpacePosition.ToFloat2(), quantizedFacing, .7f)
				+ (PVecFloat)ScreenSpacePosition.ToFloat2();
		}
	}
}
