#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	[Desc("A player with this trait will receive a free harvester when his last one gets eaten by a sandworm, provided he has at least one refinery.")]
	public class HarvesterInsuranceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new HarvesterInsurance(init.Self); }
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

			var refineries = self.World.ActorsHavingTrait<Refinery>().Where(x => x.Owner == self.Owner);
			if (!refineries.Any())
				return;

			var refinery = refineries.First();
			var delivery = refinery.Trait<FreeActorWithDelivery>();
			delivery.DoDelivery(refinery.Location + delivery.Info.DeliveryOffset, delivery.Info.Actor,
				delivery.Info.DeliveringActor);
		}
	}
}
