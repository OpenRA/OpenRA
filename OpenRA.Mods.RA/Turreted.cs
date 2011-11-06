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
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		public object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick, ISync
	{
		[Sync] public int turretFacing = 0;
		public int? desiredFacing;
		TurretedInfo info;
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
		}

		public void Tick(Actor self)
		{
			var df = desiredFacing ?? ( facing != null ? facing.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, info.ROT);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turretFacing );
			return turretFacing == desiredFacing;
		}
	}
}
