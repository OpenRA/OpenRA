using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class GpsSatellite : ITick
	{
		int frame = 0;
		int revealTicks = 30 * 25; // 30 second delay between launch and reveal
		bool fired = false;
		
		public GpsSatellite(Actor self) {}
		public void Tick(Actor self)
		{
			// HACK: Launch after 5 seconds
			if (++frame == 150)
				Activate(self);

			if (fired && --revealTicks == 0)
			{
				self.Owner.Shroud.HasGPS = true;
			}
		}
		public void Activate(Actor self)
		{
			Game.world.AddFrameEndTask(w => w.Add(new SatelliteLaunch(self)));
			fired = true;
		}
	}
}
