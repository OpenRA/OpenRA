
namespace OpenRa.Game.Traits
{
	class SeedsOreInfo : StatelessTraitInfo<SeedsOre> {}

	class SeedsOre : ITick
	{
		const double OreSeedProbability = .05;	// todo: push this out into rules

		public void Tick(Actor self)
		{
			for (var j = -1; j < 2; j++)
				for (var i = -1; i < 2; i++)
					if (Game.SharedRandom.NextDouble() < OreSeedProbability)
						if (Ore.CanSpreadInto(self.Location.X + i, self.Location.Y + j))
							Rules.Map.AddOre(self.Location.X + i, self.Location.Y + j);
		}
	}
}
