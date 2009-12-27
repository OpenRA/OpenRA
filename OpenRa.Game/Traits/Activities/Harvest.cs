using System.Linq;

namespace OpenRa.Game.Traits.Activities
{
	class Harvest : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isHarvesting = false;

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();
			var mobile = self.traits.Get<Mobile>();

			if( isHarvesting ) return this;
			if( NextActivity != null ) return NextActivity;

			var harv = self.traits.Get<Harvester>();

			if( harv.IsFull )
				return new DeliverOre { NextActivity = NextActivity };

			if (HarvestThisTile(self))
				return this;
			else
			{
				FindMoreOre(self);
				return NextActivity;
			}
		}

		bool HarvestThisTile(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			var harv = self.traits.Get<Harvester>();
			var renderUnit = self.traits.WithInterface<RenderUnit>().First();	/* better have one of these! */

			var isGem = false;
			if (!Rules.Map.ContainsResource(self.Location) ||
				!Rules.Map.Harvest(self.Location, out isGem))
				return false;

			var harvestAnim = "harvest" + Util.QuantizeFacing(unit.Facing, 8);

			if (harvestAnim != renderUnit.anim.CurrentSequence.Name)
			{
				isHarvesting = true;
				renderUnit.PlayCustomAnimation(self, harvestAnim, () => isHarvesting = false);
			}
			harv.AcceptResource(isGem);
			return true;
		}

		void FindMoreOre(Actor self)
		{
			self.QueueActivity(new Move(
				() =>
				{
					var search = new PathSearch
					{
						heuristic = loc => (Rules.Map.ContainsResource(loc) ? 0 : 1),
						umt = UnitMovementType.Wheel,
						checkForBlocked = true
					};
					search.AddInitialCell(self.Location);
					return Game.PathFinder.FindPath(search);
				}));
			self.QueueActivity(new Harvest());
		}

		public void Cancel(Actor self) { }
	}
}
