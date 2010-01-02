using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class RepairBuilding : IOrderGenerator
	{
		readonly Actor Producer;
		readonly BuildingInfo Building;

		public RepairBuilding(Actor producer, string name)
		{
			Producer = producer;
			Building = (BuildingInfo)Rules.UnitInfo[name];
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var actorBuilding = Game.FindUnits(xy.ToFloat2(), xy.ToFloat2())
					.FirstOrDefault(a => a.traits.Contains<RenderBuilding>());
					
				yield return new Order("RepairBuilding", Producer.Owner.PlayerActor, null, xy, Building.Name);
			}
			else // rmb
			{
				Game.world.AddFrameEndTask(_ => { Game.controller.orderGenerator = null; });
			}
		}

		public void Tick()
		{
			//var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem(Rules.UnitCategory[Building.Name]);
			//if (producing == null || producing.Item != Building.Name || producing.RemainingTime != 0)
				//Game.world.AddFrameEndTask(_ => { Game.controller.orderGenerator = null; });


		//const int ticksPerPoint = 15;
		//const int hpPerPoint = 8;
		//int remainingTicks = ticksPerPoint;
			
		//    if (--remainingTicks == 0)
		//    {
		//        self.Health += hpPerPoint;
				
		//        if (self.Health >= self.Info.Strength)
		//        {
		//            self.Health = self.Info.Strength;
		//            return NextActivity;
		//        }
		}

		public void Render()
		{
			//Game.worldRenderer.uiOverlay.DrawBuildingGrid(Building);
		}
	}
}
