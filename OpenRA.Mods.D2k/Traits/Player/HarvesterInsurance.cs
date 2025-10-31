#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("A player with this trait will receive a free harvester when his last one gets eaten by a sandworm, provided he has at least one refinery.")]
	public class HarvesterInsuranceInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new HarvesterInsurance(init.Self); }
	}

	public class HarvesterInsurance
	{
		readonly Actor self;

		public HarvesterInsurance(Actor self)
		{
			this.self = self;
		}

		public void TryActivate()
		{
			var harvesters = self.World.ActorsHavingTrait<Harvester>().Where(x => x.Owner == self.Owner);
			if (harvesters.Any())
				return;

			var refinery = self.World.ActorsHavingTrait<Refinery>().FirstOrDefault(x => x.Owner == self.Owner && x.Info.HasTraitInfo<FreeActorWithDeliveryInfo>());
			if (refinery == null)
				return;

			var delivery = refinery.Trait<FreeActorWithDelivery>();
			var deliveryInfo = delivery.Info as FreeActorWithDeliveryInfo;
			delivery.DoDelivery(refinery.Location + deliveryInfo.DeliveryOffset, deliveryInfo.Actor, deliveryInfo.DeliveringActor);
		}
	}
}
