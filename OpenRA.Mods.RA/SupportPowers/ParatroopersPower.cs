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
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class ParatroopersPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public string[] DropItems = { };
		[ActorReference]
		public string UnitType = "badr";
		[ActorReference]
		public string FlareType = "flare";

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init.self, this); }
	}

	class ParatroopersPower : SupportPower, IResolveOrder
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.world.OrderGenerator = 
				new GenericSelectTarget( Owner.PlayerActor, "ParatroopersActivate", "ability" );
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "ParatroopersActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.CancelInputMode();

				DoParadrop(Owner, order.TargetLocation, 
					self.Info.Traits.Get<ParatroopersPowerInfo>().DropItems);

				FinishActivate();
			}
		}

		void DoParadrop(Player owner, int2 p, string[] items)
		{
			var startPos = owner.World.ChooseRandomEdgeCell();
			owner.World.AddFrameEndTask(w =>
			{
				var info = (Info as ParatroopersPowerInfo);
				var flare = info.FlareType != null ? w.CreateActor(info.FlareType, new TypeDictionary
				{
					new LocationInit( p ),
					new OwnerInit( owner ),
				}) : null;

				var a = w.CreateActor(info.UnitType, new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( owner ),
					new FacingInit( Util.GetFacing(p - startPos, 0) ),
					new AltitudeInit( Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude ),
				});
				
				a.CancelActivity();
				a.QueueActivity(new FlyCircle(p));
				a.Trait<ParaDrop>().SetLZ(p, flare);

				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, owner.World.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary { new OwnerInit( a.Owner ) }));
			});
		}
	}
}
