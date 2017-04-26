#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Gives additional cash when resources are delivered to refineries.")]
	public class ResourcePurifierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage value of the resource to grant as cash.")]
		public readonly int Modifier = 25;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new ResourcePurifier(init.Self, this); }
	}

	public class ResourcePurifier : ConditionalTrait<ResourcePurifierInfo>, ITick, IResourcePurifier, INotifyOwnerChanged
	{
		readonly ResourcePurifierInfo info;

		PlayerResources playerResources;
		int currentDisplayTick = 0;
		int currentDisplayValue = 0;

		public ResourcePurifier(Actor self, ResourcePurifierInfo info)
			: base(info)
		{
			this.info = info;

			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void IResourcePurifier.RefineAmount(int amount)
		{
			if (IsTraitDisabled)
				return;

			var cash = Util.ApplyPercentageModifiers(amount, new int[] { info.Modifier });
			playerResources.GiveCash(cash);

			if (info.ShowTicks)
				currentDisplayValue += cash;
		}

		void ITick.Tick(Actor self)
		{
			if (info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = info.TickRate;
				currentDisplayValue = 0;
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
