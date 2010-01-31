using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class OreRefineryInfo : ITraitInfo
	{
		public object Create(Actor self) { return new OreRefinery(self); }
	}

	class OreRefinery : IAcceptOre
	{
		public OreRefinery(Actor self)
		{
			self.World.AddFrameEndTask(
				w =>
				{		/* create the free harvester! */
					var harvester = w.CreateActor("harv", self.Location + new int2(1, 2), self.Owner);
					var unit = harvester.traits.Get<Unit>();
					var mobile = harvester.traits.Get<Mobile>();
					unit.Facing = 64;
					harvester.QueueActivity(new Harvest());
				});
		}
		
		public void OnDock(Actor harv) {}
	}
}
