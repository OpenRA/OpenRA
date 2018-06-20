#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	[Desc("Gives additional cash when resources are delivered to refineries.")]
	public class ExternalResourceMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage value of the resource to grant as cash.")]
		public readonly int Modifier;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new ExternalResourceMultiplier(init.Self, this); }
	}

	public class ExternalResourceMultiplier : ConditionalTrait<ExternalResourceMultiplierInfo>, ITick, INotifyOwnerChanged
	{
		PlayerResources playerResources;
		int currentDisplayTick;
		int currentDisplayValue;

		public ExternalResourceMultiplier(Actor self, ExternalResourceMultiplierInfo info)
			: base(info)
		{
			currentDisplayTick = Info.TickRate;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public void ProvideCash(int amount)
		{
			if (IsTraitDisabled)
				return;

			var cash = Util.ApplyPercentageModifiers(amount, new int[] { Info.Modifier });
			playerResources.GiveCash(cash);

			if (Info.ShowTicks)
				currentDisplayValue += cash;
		}

		void ITick.Tick(Actor self)
		{
			if (Info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = Info.TickRate;
				currentDisplayValue = 0;
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = Info.TickRate;
			currentDisplayValue = 0;
		}
	}
}
