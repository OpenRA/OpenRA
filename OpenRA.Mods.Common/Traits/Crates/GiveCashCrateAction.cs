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

using OpenRA.Mods.Common.Effects;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives cash to the collector.")]
	sealed class GiveCashCrateActionInfo : CrateActionInfo
	{
		[Desc("Amount of cash to give.")]
		public readonly int Amount = 2000;

		[Desc("Should the collected amount be displayed as a cash tick?")]
		public readonly bool UseCashTick = false;

		public override object Create(ActorInitializer init) { return new GiveCashCrateAction(init.Self, this); }
	}

	sealed class GiveCashCrateAction : CrateAction
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
				var amount = collector.Owner.PlayerActor.Trait<PlayerResources>().ChangeCash(info.Amount);

				if (info.UseCashTick)
					w.Add(new FloatingText(collector.CenterPosition, collector.OwnerColor(), FloatingText.FormatCashTick(amount), 30));
			});

			base.Activate(collector);
		}

		public override int GetSelectionShares(Actor collector)
		{
			var pr = collector.Owner.PlayerActor.Trait<PlayerResources>();
			if (info.Amount < 0 && pr.GetCashAndResources() == 0)
				return 0;

			return base.GetSelectionShares(collector);
		}
	}
}
