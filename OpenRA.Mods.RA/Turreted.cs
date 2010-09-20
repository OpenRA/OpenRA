#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TurretedInfo : ITraitInfo
	{
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		public object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick
	{
		[Sync]
		public int turretFacing = 0;
		public int? desiredFacing;
		TurretedInfo info;
		IFacing facing;
		
		public Turreted(ActorInitializer init, TurretedInfo info)
		{
			this.info = info;
			turretFacing = info.InitialFacing;
			turretFacing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			facing = init.self.TraitOrDefault<IFacing>();
		}

		public void Tick( Actor self )
		{
			var df = desiredFacing ?? ( facing != null ? facing.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, info.ROT);
		}
	}
}
