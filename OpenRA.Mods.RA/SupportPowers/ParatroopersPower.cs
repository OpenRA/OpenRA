#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Air;

namespace OpenRA.Mods.RA
{
	public class ParatroopersPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public string[] DropItems = { };
		[ActorReference]
		public string UnitType = "badr";
		[ActorReference]
		public string FlareType = "flare";

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init.self, this); }
	}

	public class ParatroopersPower : SupportPower
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order)
		{
			var items = (Info as ParatroopersPowerInfo).DropItems;
			var startPos = self.World.ChooseRandomEdgeCell();
			
			self.World.AddFrameEndTask(w =>
			{
				var info = (Info as ParatroopersPowerInfo);
				var flare = info.FlareType != null ? w.CreateActor(info.FlareType, new TypeDictionary
				{
					new LocationInit( order.TargetLocation ),
					new OwnerInit( self.Owner ),
				}) : null;

				var a = w.CreateActor(info.UnitType, new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( self.Owner ),
					new FacingInit( Util.GetFacing(order.TargetLocation - startPos, 0) ),
					new AltitudeInit( Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude ),
				});
				
				a.CancelActivity();
				a.QueueActivity(new FlyCircle(order.TargetLocation));
				a.Trait<ParaDrop>().SetLZ(order.TargetLocation, flare);

				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, self.World.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary { new OwnerInit( a.Owner ) }));
			});
		}
	}
}
