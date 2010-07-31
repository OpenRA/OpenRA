#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits
{
	public class TurretedInfo : ITraitInfo
	{
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		public object Create(ActorInitializer init) { return new Turreted(init.self, this); }
	}

	public class Turreted : ITick
	{
		[Sync]
		public int turretFacing = 0;
		public int? desiredFacing;
		TurretedInfo Info;
		IMove Move;
		
		public Turreted(Actor self, TurretedInfo info)
		{
			Info = info;
			turretFacing = info.InitialFacing;
			Move = self.traits.GetOrDefault<IMove>();
		}

		public void Tick( Actor self )
		{
			var df = desiredFacing ?? ( Move != null ? Move.Facing : turretFacing );
			turretFacing = Util.TickFacing(turretFacing, df, Info.ROT);
		}
	}
}
