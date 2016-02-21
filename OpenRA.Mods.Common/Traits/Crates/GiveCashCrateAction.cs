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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives cash to the collector.")]
	class GiveCashCrateActionInfo : CrateActionInfo
	{
		[Desc("Amount of cash to give.")]
		public int Amount = 2000;

		[Desc("Should the collected amount be displayed as a cash tick?")]
		public bool UseCashTick = false;

		public override object Create(ActorInitializer init) { return new GiveCashCrateAction(init.Self, this); }
	}

	class GiveCashCrateAction : CrateAction
	{
		readonly GiveCashCrateActionInfo info;
		public GiveCashCrateAction(Actor self, GiveCashCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				collector.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.Amount);

				if (info.UseCashTick)
					w.Add(new FloatingText(collector.CenterPosition, collector.Owner.Color.RGB, FloatingText.FormatCashTick(info.Amount), 30));
			});

			base.Activate(collector);
		}
	}
}
