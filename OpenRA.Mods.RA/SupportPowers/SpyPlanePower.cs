#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;

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
			Self.World.OrderGenerator = new GenericSelectTarget(Owner.PlayerActor, "SpyPlane", "ability");
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "SpyPlane")
			{
				FinishActivate();

				var enterCell = self.World.ChooseRandomEdgeCell();

				var plane = self.World.CreateActor("u2", new TypeDictionary 
				{
					new LocationInit( enterCell ),
					new OwnerInit( self.Owner ),
					new FacingInit( Util.GetFacing(order.TargetLocation - enterCell, 0) ),
					new AltitudeInit( Rules.Info["u2"].Traits.Get<PlaneInfo>().CruiseAltitude ),
				});

				plane.CancelActivity();
				plane.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
				plane.QueueActivity(new CallFunc(() => plane.World.AddFrameEndTask( w => 
					{
						var camera = w.CreateActor("camera", new TypeDictionary
					    {
							new LocationInit( order.TargetLocation ),
							new OwnerInit( Owner ),
						});

						camera.QueueActivity(new Wait((int)(25 * 60 * (Info as SpyPlanePowerInfo).RevealTime)));
						camera.QueueActivity(new RemoveSelf());
					})));
				plane.QueueActivity(new FlyOffMap { Interruptible = false });
				plane.QueueActivity(new RemoveSelf());
			}
		}
	}
}
