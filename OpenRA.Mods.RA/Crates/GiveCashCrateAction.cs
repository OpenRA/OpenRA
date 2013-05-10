#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	class GiveCashCrateActionInfo : CrateActionInfo
	{
		public int Amount = 2000;
		public bool UseCashTick = false;

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
				collector.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(amount);

				if ((info as GiveCashCrateActionInfo).UseCashTick)
					w.Add(new CashTick(amount, 20, 1, collector.CenterLocation, collector.Owner.Color.RGB));
			});

			base.Activate(collector);
		}
	}
}
