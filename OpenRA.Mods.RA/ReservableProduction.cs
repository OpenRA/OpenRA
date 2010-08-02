#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion


using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	// a small hack to teach Production about Reservable.

	public class ReservableProductionInfo : ProductionInfo, ITraitPrerequisite<ReservableInfo>
	{
		public override object Create(ActorInitializer init) { return new ReservableProduction(this); }
	}

	class ReservableProduction : Production
	{
		public ReservableProduction(ReservableProductionInfo info) : base(info) {}

		public override bool Produce(Actor self, ActorInfo producee)
		{
			if (Reservable.IsReserved(self))
				return false;

			return base.Produce(self, producee);
		}
	}
}
