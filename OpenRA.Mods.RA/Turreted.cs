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
using System.Linq;
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
		public readonly int[] LegacyOffset = {0,0};
		public readonly bool AlignWhenIdle = false;

		public CoordinateModel OffsetModel = CoordinateModel.Legacy;
		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;


		bool HasWorldOffset(ArmamentInfo ai)
		{
			return ai.OffsetModel == CoordinateModel.World || ai.LocalOffset.Length > 0;
		}

		public virtual object Create(ActorInitializer init)
		{
			// Auto-detect coordinate type
			var arms = init.self.Info.Traits.WithInterface<ArmamentInfo>();
			if (Offset != WVec.Zero || arms.Any(ai => HasWorldOffset(ai)))
				OffsetModel = CoordinateModel.World;

			return new Turreted(init, this);
		}
	}

	public class Turreted : ITick, ISync, IResolveOrder
	{
		[Sync] public int turretFacing = 0;
		public int? desiredFacing;
		TurretedInfo info;
		protected Turret turret;
		IFacing facing;

		// For subclasses that want to move the turret relative to the body
		protected WVec LocalOffset = WVec.Zero;

		public WVec Offset { get { return info.Offset + LocalOffset; } }
		public string Name { get { return info.Turret; } }
		public CoordinateModel CoordinateModel { get { return info.OffsetModel; } }

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
			turret = new Turret(info.LegacyOffset);
		}

		public virtual void Tick(Actor self)
		{
			var df = desiredFacing ?? ( facing != null ? facing.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, info.ROT);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			desiredFacing = Util.GetFacing(target.CenterLocation - self.CenterLocation, turretFacing);
			return turretFacing == desiredFacing;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			if (info.AlignWhenIdle && order.OrderString != "Attack" && order.OrderString != "AttackHold")
				desiredFacing = null;
		}

		public PVecFloat PxPosition(Actor self, IFacing facing)
		{
			// Hack for external code unaware of world coordinates
			if (info.OffsetModel == CoordinateModel.World)
				return (PVecFloat)PPos.FromWPosHackZ(WPos.Zero + Position(self)).ToFloat2();

			return turret.PxPosition(self, facing);
		}

		// Turret offset in world-space
		public WVec Position(Actor self)
		{
			if (info.OffsetModel != CoordinateModel.World)
				throw new InvalidOperationException("Turreted.Position requires a world coordinate offset");

			var coords = self.Trait<ILocalCoordinatesModel>();
			var bodyOrientation = coords.QuantizeOrientation(self, self.Orientation);
			return coords.LocalToWorld(Offset.Rotate(bodyOrientation));
		}

		// Orientation in unit-space
		public WRot LocalOrientation(Actor self)
		{
			// Hack: turretFacing is relative to the world, so subtract the body yaw
			return WRot.FromYaw(WAngle.FromFacing(turretFacing) - self.Orientation.Yaw);
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
