namespace OpenRa.Game.Traits.Activities
{
	class Rearm : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		int remainingTicks = ticksPerPip;

		const int ticksPerPip = 25 * 2;

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo == null) return NextActivity;

			if (--remainingTicks == 0)
			{
				if (!limitedAmmo.GiveAmmo()) return NextActivity;
				remainingTicks = ticksPerPip;
			}

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
