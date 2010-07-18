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
	class TurretedInfo : ITraitInfo
	{
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		public object Create(ActorInitializer init) { return new Turreted(init.self); }
	}

	public class Turreted : ITick
	{
		[Sync]
		public int turretFacing = 0;
		public int? desiredFacing;

		public Turreted(Actor self)
		{
			turretFacing = self.Info.Traits.Get<TurretedInfo>().InitialFacing;
		}

		public void Tick( Actor self )
		{
			var df = desiredFacing ?? ( self.traits.Contains<Unit>() ? self.traits.Get<Unit>().Facing : turretFacing );
			Util.TickFacing(ref turretFacing, df, self.Info.Traits.Get<TurretedInfo>().ROT);
		}
	}
}
