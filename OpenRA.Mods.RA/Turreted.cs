#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TurretedInfo : ITraitInfo, UsesInit<TurretFacingInit>
	{
		public readonly string Turret = "primary";
		[Desc("Rate of Turning")]
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;
		public readonly bool AlignWhenIdle = false;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		public virtual object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick, ISync, IResolveOrder
	{
		[Sync] public int QuantizedFacings = 0;
		[Sync] public int turretFacing = 0;
		public int? desiredFacing;
		TurretedInfo info;
		IFacing facing;

		// For subclasses that want to move the turret relative to the body
		protected WVec LocalOffset = WVec.Zero;

		public WVec Offset { get { return info.Offset + LocalOffset; } }
		public string Name { get { return info.Turret; } }

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
		}

		public virtual void Tick(Actor self)
		{
			var df = desiredFacing ?? ( facing != null ? facing.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, info.ROT);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			desiredFacing = Util.GetFacing(target.CenterPosition - self.CenterPosition, turretFacing);
			return turretFacing == desiredFacing;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			if (info.AlignWhenIdle && order.OrderString != "Attack")
				desiredFacing = null;
		}

		// Turret offset in world-space
		public WVec Position(Actor self)
		{
			var body = self.Trait<IBodyOrientation>();
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			return body.LocalToWorld(Offset.Rotate(bodyOrientation));
		}

		// Orientation in unit-space
		public WRot LocalOrientation(Actor self)
		{

			// Hack: turretFacing is relative to the world, so subtract the body yaw
			var local = WRot.FromYaw(WAngle.FromFacing(turretFacing) - self.Orientation.Yaw);

			if (QuantizedFacings == 0)
				return local;

			// Quantize orientation to match a rendered sprite
			// Implies no pitch or yaw
			var facing = Util.QuantizeFacing(local.Yaw.Angle / 4, QuantizedFacings) * (256 / QuantizedFacings);
			return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
		}
	}
}
