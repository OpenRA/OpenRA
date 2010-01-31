using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class OreRefineryInfo : ITraitInfo
	{
		public object Create(Actor self) { return new OreRefinery(self); }
	}

	class OreRefinery : IAcceptOre
	{
		Actor self;
		public OreRefinery(Actor self)
		{
			this.self = self;
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
		public int2 DeliverOffset {	get { return new int2(1, 2); } }
		public void OnDock(Actor harv, DeliverOre dockOrder)
		{
			var unit = harv.traits.Get<Unit>();
			if (unit.Facing != 64)
				harv.QueueActivity(new Turn(64));
			
			// TODO: This should be delayed until the turn order is complete
			harv.traits.Get<Harvester>().Deliver(harv, self);
			harv.QueueActivity(new Harvest());
		}
	}
}
