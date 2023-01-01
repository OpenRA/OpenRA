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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Gives additional cash when resources are delivered to refineries.")]
	public class ResourcePurifierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage value of the resource to grant as cash.")]
		public readonly int Modifier = 0;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long the cash ticks stay on the screen.")]
		public readonly int TickLifetime = 30;

		[Desc("How often the cash ticks can appear.")]
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new ResourcePurifier(this); }
	}

	public class ResourcePurifier : ConditionalTrait<ResourcePurifierInfo>, INotifyResourceAccepted, ITick, INotifyOwnerChanged
	{
		readonly int[] modifier;

		PlayerResources playerResources;
		int currentDisplayTick;
		int currentDisplayValue;

		public ResourcePurifier(ResourcePurifierInfo info)
			: base(info)
		{
			modifier = new int[] { Info.Modifier };
			currentDisplayTick = Info.TickRate;
		}

		protected override void Created(Actor self)
		{
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			base.Created(self);
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor self, Actor refinery, string resourceType, int count, int value)
		{
			if (IsTraitDisabled)
				return;

			var cash = Common.Util.ApplyPercentageModifiers(value, modifier);
			playerResources.GiveCash(cash);

			if (Info.ShowTicks && self.Info.HasTraitInfo<IOccupySpaceInfo>())
				currentDisplayValue += cash;
		}

		void ITick.Tick(Actor self)
		{
			if (currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(temp), Info.TickLifetime)));

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
