using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AcceptsOreInfo : ITraitInfo
	{
		public object Create(Actor self) { return new AcceptsOre(self); }
	}

	class AcceptsOre
	{
		public AcceptsOre(Actor self)
		{
			Game.world.AddFrameEndTask(
				w =>
				{		/* create the free harvester! */
					var harvester = new Actor(Rules.UnitInfo["harv"], self.Location + new int2(1, 2), self.Owner);
					var unit = harvester.traits.Get<Unit>();
					var mobile = harvester.traits.Get<Mobile>();
					unit.Facing = 64;
					harvester.QueueActivity(new Harvest());
					w.Add(harvester);
				});
		}
	}
}
