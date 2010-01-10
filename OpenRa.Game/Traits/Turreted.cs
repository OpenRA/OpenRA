
namespace OpenRa.Game.Traits
{
	class TurretedInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Turreted(self); }
	}

	class Turreted : ITick
	{
		[Sync]
		public int turretFacing = 0;
		public int? desiredFacing;

		public Turreted(Actor self)
		{
			turretFacing = self.Info.InitialFacing;
		}

		public void Tick( Actor self )
		{
			var df = desiredFacing ?? ( self.traits.Contains<Unit>() ? self.traits.Get<Unit>().Facing : turretFacing );
			Util.TickFacing( ref turretFacing, df, self.Info.ROT );
		}
	}
}
