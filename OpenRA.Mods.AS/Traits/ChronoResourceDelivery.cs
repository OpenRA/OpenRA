#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Activities;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When returning to a refinery to deliver resources, this actor will teleport if possible.")]
	public class ChronoResourceDeliveryInfo : ITraitInfo, Requires<HarvesterInfo>
	{
		[Desc("The number of ticks between each check to see if we can teleport to the refinery.")]
		public readonly int CheckTeleportDelay = 10;

		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for the effect played where the harvester jumped from.")]
		[SequenceReference("Image")] public readonly string WarpInSequence = null;

		[Desc("Sequence used for the effect played where the harvester jumped to.")]
		[SequenceReference("Image")] public readonly string WarpOutSequence = null;

		[Desc("Palette to render the warp in/out sprites in.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Sound played where the harvester jumped from.")]
		public readonly string WarpInSound = null;

		[Desc("Sound where the harvester jumped to.")]
		public readonly string WarpOutSound = null;

		public virtual object Create(ActorInitializer init) { return new ChronoResourceDelivery(init.Self, this); }
	}

	public class ChronoResourceDelivery : INotifyHarvesterAction, ITick
	{
		readonly ChronoResourceDeliveryInfo info;

		CPos? destination = null;
		Activity nextActivity = null;
		int ticksTillCheck = 0;

		public ChronoResourceDelivery(Actor self, ChronoResourceDeliveryInfo info)
		{
			this.info = info;
		}

		public void Tick(Actor self)
		{
			if (destination == null)
				return;

			if (ticksTillCheck <= 0)
			{
				ticksTillCheck = info.CheckTeleportDelay;

				TeleportIfPossible(self);
			}
			else
				ticksTillCheck--;
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next)
		{
			Reset();
		}

		public void MovingToRefinery(Actor self, CPos targetCell, Activity next)
		{
			if (destination != null && destination.Value != targetCell)
				ticksTillCheck = 0;

			destination = targetCell;
			nextActivity = next;
		}

		public void MovementCancelled(Actor self)
		{
			Reset();
		}

		public void Harvested(Actor self, ResourceType resource) { }
		public void Docked() { }
		public void Undocked() { }

		void TeleportIfPossible(Actor self)
		{
			// We're already here; no need to interfere.
			if (self.Location == destination.Value)
			{
				Reset();
				return;
			}

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination.Value))
			{
				self.CancelActivity();
				self.QueueActivity(new ChronoResourceTeleport(destination.Value, info));
				self.QueueActivity(nextActivity);
				Reset();
			}
		}

		void Reset()
		{
			ticksTillCheck = 0;
			destination = null;
			nextActivity = null;
		}
	}
}
