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
	class AirstrikePowerInfo : SupportPowerInfo
	{ 
		[ActorReference]
		public readonly string UnitType = "badr.bomber";
		[ActorReference]
		public readonly string FlareType = null;

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.self, this); }
	}

	class AirstrikePower : SupportPower, IResolveOrder
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.world.OrderGenerator = new GenericSelectTarget(Owner.PlayerActor, Info.OrderName, "ability");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == Info.OrderName)
			{
				var startPos = Owner.World.ChooseRandomEdgeCell();

				Owner.World.AddFrameEndTask(w =>
					{
						var info = (Info as AirstrikePowerInfo);
						var flare = info.FlareType != null ? w.CreateActor(info.FlareType, new TypeDictionary
					    {
							new LocationInit( order.TargetLocation ),
							new OwnerInit( Owner ),
						}) : null;
					
						var a = w.CreateActor(info.UnitType, new TypeDictionary
					    {
							new LocationInit( startPos ),
							new OwnerInit( Owner ),
							new FacingInit( Util.GetFacing(order.TargetLocation - startPos, 0) ),
							new AltitudeInit( Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude ),
						});
						a.traits.Get<CarpetBomb>().SetTarget(order.TargetLocation);

						a.CancelActivity();
						a.QueueActivity(new Fly(order.TargetLocation));

						if (flare != null)
							a.QueueActivity(new CallFunc(() => Owner.World.AddFrameEndTask(_w => _w.Remove(flare))));

						a.QueueActivity(new FlyOffMap { Interruptible = false });
						a.QueueActivity(new RemoveSelf());
					});

				if (Owner == Owner.World.LocalPlayer)
					self.World.CancelInputMode();

				FinishActivate();
			}
		}
	}
}
