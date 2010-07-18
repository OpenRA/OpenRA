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
	class GiveCashCrateActionInfo : CrateActionInfo
	{
		public int Amount = 2000;
		public override object Create(ActorInitializer init) { return new GiveCashCrateAction(init.self, this); }
	}

	class GiveCashCrateAction : CrateAction
	{
		public GiveCashCrateAction(Actor self, GiveCashCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var amount = (info as GiveCashCrateActionInfo).Amount;
				collector.Owner.PlayerActor.traits.Get<PlayerResources>().GiveCash(amount);
			});
			base.Activate(collector);
		}
	}
}
