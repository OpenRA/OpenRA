
namespace OpenRa.Traits
{
	class SeedsOreInfo : ITraitInfo
	{
		public readonly float Chance = .05f;
		public readonly int Interval = 5;

		public object Create(Actor self) { return new SeedsOre(); }
	}

	class SeedsOre : ITick
	{
		int ticks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<SeedsOreInfo>();

				for (var j = -1; j < 2; j++)
					for (var i = -1; i < 2; i++)
						if (Game.SharedRandom.NextDouble() < info.Chance)
							if (Game.world.OreCanSpreadInto(self.Location.X + i, self.Location.Y + j))
								Game.world.Map.AddOre(self.Location.X + i, self.Location.Y + j);

				ticks = info.Interval;
			}
		}
	}
}
