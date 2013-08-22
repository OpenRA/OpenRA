#region Copyright & License Information
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
	class AirstrikePowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public readonly string UnitType = "badr.bomber";
		[ActorReference]
		public readonly string FlareType = null;

		public readonly int FlareTime = 25 * 60 * 2;	// 2 minutes

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.self, this); }
	}

	class AirstrikePower : SupportPower
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order)
		{
			var startPos = self.World.ChooseRandomEdgeCell();
			self.World.AddFrameEndTask(w =>
			{
				var info = (Info as AirstrikePowerInfo);

				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					Sound.Play(Info.LaunchSound);
				else
					Sound.Play(Info.IncomingSound);

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

				var a = w.CreateActor(info.UnitType, new TypeDictionary
				{
					new LocationInit( startPos ),
					new OwnerInit( self.Owner ),
					new FacingInit( Util.GetFacing(order.TargetLocation - startPos, 0) ),
					new AltitudeInit( Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude ),
				});
				a.Trait<CarpetBomb>().SetTarget(order.TargetLocation);

				a.CancelActivity();
				a.QueueActivity(Fly.ToCell(order.TargetLocation));

				if (flare != null)
					a.QueueActivity(new CallFunc(() => flare.Destroy()));

				a.QueueActivity(new FlyOffMap());
				a.QueueActivity(new RemoveSelf());
			});
		}
	}
}
