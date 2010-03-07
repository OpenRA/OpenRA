#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class SpyPlanePowerInfo : SupportPowerInfo
	{
		public readonly int Range = 10;
		public override object Create(Actor self) { return new SpyPlanePower(self,this); }
	}

	class SpyPlanePower : SupportPower, IResolveOrder
	{
		public SpyPlanePower(Actor self, SpyPlanePowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "spypln1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpyPlane")
			{
				FinishActivate();

				if (order.Player == Owner.World.LocalPlayer)
					Game.controller.CancelInputMode();

				var enterCell = self.World.ChooseRandomEdgeCell();

				var plane = self.World.CreateActor("U2", enterCell, self.Owner);
				plane.CancelActivity();
				plane.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
				plane.QueueActivity(new CallFunc(
					() => Owner.Shroud.Explore(Owner.World, order.TargetLocation,
						(Info as SpyPlanePowerInfo).Range)));
				plane.QueueActivity(new FlyOffMap(20));
				plane.QueueActivity(new RemoveSelf());
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					Game.controller.CancelInputMode();
					yield break;
				}

				yield return new Order("SpyPlane", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) {}
			public void Render(World world) {}

			public string GetCursor(World world, int2 xy, MouseInput mi) { return "ability"; }
		}
	}
}
