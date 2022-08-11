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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RefineryInfo : DockInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Store resources in silos. Adds cash directly without storing if set to false.")]
		public readonly bool UseStorage = true;

		[Desc("Discard resources once silo capacity has been reached.")]
		public readonly bool DiscardExcessResources = false;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new Refinery(init.Self, this); }
	}

	public class Refinery : Dock, IAcceptResources, INotifyCreated, ITick, INotifyOwnerChanged
	{
		readonly RefineryInfo info;
		PlayerResources playerResources;
		IEnumerable<int> resourceValueModifiers;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;
		public Refinery(Actor self, RefineryInfo info)
			: base(self, info)
		{
			this.info = info;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = info.TickRate;
		}

		void INotifyCreated.Created(Actor self)
		{
			resourceValueModifiers = self.TraitsImplementing<IResourceValueModifier>().ToArray().Select(m => m.GetResourceValueModifier());
		}

		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new SpriteHarvesterDockSequence(harv, self, DeliveryAngle, IsDragRequired, DragOffset, DragLength);
		}

		int IAcceptResources.AcceptResources(string resourceType, int count)
		{
			if (!playerResources.Info.ResourceValues.TryGetValue(resourceType, out var resourceValue))
				return 0;

			var value = Util.ApplyPercentageModifiers(count * resourceValue, resourceValueModifiers);

			if (info.UseStorage)
			{
				var storageLimit = Math.Max(playerResources.ResourceCapacity - playerResources.Resources, 0);
				if (!info.DiscardExcessResources)
				{
					// Reduce amount if needed until it will fit the available storage
					while (value > storageLimit)
						value = Util.ApplyPercentageModifiers(--count * resourceValue, resourceValueModifiers);
				}
				else
					value = Math.Min(value, playerResources.ResourceCapacity - playerResources.Resources);

				playerResources.GiveResources(value);
			}
			else
				value = playerResources.ChangeCash(value);

			foreach (var notify in Self.World.ActorsWithTrait<INotifyResourceAccepted>())
			{
				if (notify.Actor.Owner != Self.Owner)
					continue;

				notify.Trait.OnResourceAccepted(notify.Actor, Self, resourceType, count, value);
			}

			if (info.ShowTicks)
				currentDisplayValue += value;

			return count;
		}

		void ITick.Tick(Actor self)
		{
			if (info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(temp), 30)));
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
