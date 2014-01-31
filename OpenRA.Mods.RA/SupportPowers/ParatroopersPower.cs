﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

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

		public readonly int FlareTime = 25 * 60 * 2;	// 2 minutes

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

				if (flare != null)
				{
					flare.QueueActivity(new Wait(info.FlareTime));
					flare.QueueActivity(new RemoveSelf());
				}

				var altitude = Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var a = w.CreateActor(info.UnitType, new TypeDictionary
				{
					new CenterPositionInit(startPos.CenterPosition + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(self.Owner),
					new FacingInit(Util.GetFacing(order.TargetLocation - startPos, 0))
				});

				a.CancelActivity();
				a.QueueActivity(new FlyAttack(Target.FromOrder(order)));
				a.Trait<ParaDrop>().SetLZ(order.TargetLocation);

				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, self.World.CreateActor(false, i.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit( a.Owner ) }));
			});
		}
	}
}
