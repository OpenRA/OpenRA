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
using OpenRA.Orders;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class SpyPlanePowerInfo : SupportPowerInfo
	{
		public readonly float RevealTime = .1f;	// minutes
		public override object Create(ActorInitializer init) { return new SpyPlanePower(init.self,this); }
	}

	class SpyPlanePower : SupportPower, IResolveOrder
	{
		public SpyPlanePower(Actor self, SpyPlanePowerInfo info) : base(self, info) { }

		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "spypln1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new GenericSelectTarget(Owner.PlayerActor, "SpyPlane", "ability");
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "SpyPlane")
			{
				FinishActivate();

				if (order.Player == Owner.World.LocalPlayer)
					Game.controller.CancelInputMode();

				var enterCell = self.World.ChooseRandomEdgeCell();

				var plane = self.World.CreateActor("U2", enterCell, self.Owner);
				plane.traits.Get<Unit>().Altitude = plane.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
				plane.traits.Get<Unit>().Facing = Util.GetFacing(order.TargetLocation - enterCell, 0);

				plane.CancelActivity();
				plane.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
				plane.QueueActivity(new CallFunc(() => plane.World.AddFrameEndTask( w => 
					{
						var camera = w.CreateActor("camera", order.TargetLocation, Owner);
						camera.QueueActivity(new Wait((int)(25 * 60 * (Info as SpyPlanePowerInfo).RevealTime)));
						camera.QueueActivity(new RemoveSelf());
					})));
				plane.QueueActivity(new FlyOffMap { Interruptible = false });
				plane.QueueActivity(new RemoveSelf());
			}
		}
	}
}
