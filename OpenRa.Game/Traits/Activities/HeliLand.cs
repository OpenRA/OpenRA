
namespace OpenRa.Traits.Activities
{
	class HeliLand : IActivity
	{
		public HeliLand(bool requireSpace) { this.requireSpace = requireSpace; }

		bool requireSpace;
		bool isCanceled;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			if (unit.Altitude == 0)
				return NextActivity;

			if (requireSpace && !Game.IsCellBuildable(self.Location, UnitMovementType.Foot))
				return this;	// fail to land if no space

			--unit.Altitude;
			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
