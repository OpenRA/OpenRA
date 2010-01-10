using System.Linq;

namespace OpenRa.Game.Traits
{
	class TurretedInfo : ITraitInfo
	{
		public readonly int ROT = 0;
		public readonly int InitialFacing = 128;

		public object Create(Actor self) { return new Turreted(self); }
	}

	class Turreted : ITick
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
