#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SpyPlanePowerInfo : SupportPowerInfo
	{
		public readonly int RevealTime = 6;	// seconds
		public override object Create(ActorInitializer init) { return new SpyPlanePower(init.self,this); }
	}

	class SpyPlanePower : SupportPower
	{
		public SpyPlanePower(Actor self, SpyPlanePowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var enterCell = self.World.ChooseRandomEdgeCell();
			var altitude = self.World.Map.Rules.Actors["u2"].Traits.Get<PlaneInfo>().CruiseAltitude;

			var plane = self.World.CreateActor("u2", new TypeDictionary
			{
				new CenterPositionInit(enterCell.CenterPosition + new WVec(WRange.Zero, WRange.Zero, altitude)),
				new OwnerInit(self.Owner),
				new FacingInit(Util.GetFacing(order.TargetLocation - enterCell, 0))
			});

			plane.CancelActivity();
			plane.QueueActivity(new Fly(plane, Target.FromCell(order.TargetLocation)));
			plane.QueueActivity(new CallFunc(() => plane.World.AddFrameEndTask( w =>
				{
					var camera = w.CreateActor("camera", new TypeDictionary
					{
						new LocationInit( order.TargetLocation ),
						new OwnerInit( self.Owner ),
					});

					camera.QueueActivity(new Wait(25 * (Info as SpyPlanePowerInfo).RevealTime));
					camera.QueueActivity(new RemoveSelf());
				})));
			plane.QueueActivity(new FlyOffMap());
			plane.QueueActivity(new RemoveSelf());
		}
	}
}
