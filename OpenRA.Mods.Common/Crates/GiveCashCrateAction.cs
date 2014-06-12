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
using OpenRA.Mods.Common.Effects;

namespace OpenRA.Mods.Common
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
				var crateInfo = (GiveCashCrateActionInfo)info;
				var amount = crateInfo.Amount;
				collector.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(amount);

				if (crateInfo.UseCashTick)
					w.Add(new CashTick(collector.CenterPosition, collector.Owner.Color.RGB, amount));
			});

			base.Activate(collector);
		}
	}
}
