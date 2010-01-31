using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class TiberiumRefineryInfo : ITraitInfo
	{
		public object Create(Actor self) { return new TiberiumRefinery(self); }
	}

	class TiberiumRefinery : IAcceptOre
	{
		Actor self;
		public TiberiumRefinery(Actor self)
		{
			this.self = self;
			self.World.AddFrameEndTask(
				w =>
				{		/* create the free harvester! */
					var harvester = w.CreateActor("harv", self.Location + new int2(0, 2), self.Owner);
					var unit = harvester.traits.Get<Unit>();
					var mobile = harvester.traits.Get<Mobile>();
					unit.Facing = 64;
					harvester.QueueActivity(new Harvest());
				});
		}

		public int2 DeliverOffset {	get { return new int2(0, 2); } }
		public void OnDock(Actor harv, DeliverOre dockOrder)
		{
			// Todo: need to be careful about cancellation and multiple harvs
			var unit = harv.traits.Get<Unit>();
			harv.QueueActivity(new Move(self.Location + new int2(1,1), self));
			harv.QueueActivity(new Turn(96));
			harv.QueueActivity( new CallFunc( () => 
				self.traits.Get<RenderBuilding>().PlayCustomAnimThen(self, "active", () => {
					harv.traits.Get<Harvester>().Deliver(harv, self);
					harv.QueueActivity(new Move(self.Location + DeliverOffset, self));
					harv.QueueActivity(new Harvest());
			})));
		}
	}
}
