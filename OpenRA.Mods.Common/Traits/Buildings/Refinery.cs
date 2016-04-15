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
	public class RefineryInfo : IAcceptResourcesInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Actual harvester facing when docking, 0-255 counter-clock-wise.")]
		public readonly int DockAngle = 0;

		[Desc("Docking cell relative to top-left cell.")]
		public readonly CVec DockOffset = CVec.Zero;

		[Desc("Does the refinery require the harvester to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the harvester will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;

		[Desc("Discard resources once silo capacity has been reached.")]
		public readonly bool DiscardExcessResources = false;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;

		public virtual object Create(ActorInitializer init) { return new Refinery(init.Self, this); }
	}

	public class Refinery : ITick, IAcceptResources, INotifySold, INotifyCapture, INotifyOwnerChanged, IExplodeModifier, ISync, INotifyActorDisposing
	{
		readonly Actor self;
		readonly RefineryInfo info;
		readonly WithSpriteBody wsb;
		PlayerResources playerResources;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;

		[Sync] public int Ore = 0;
		[Sync] Actor dockedHarv = null;
		[Sync] bool preventDock = false;

		public bool AllowDocking { get { return !preventDock; } }
		public CVec DeliveryOffset { get { return info.DockOffset; } }
		public int DeliveryAngle { get { return info.DockAngle; } }
		public bool IsDragRequired { get { return info.IsDragRequired; } }
		public WVec DragOffset { get { return info.DragOffset; } }
		public int DragLength { get { return info.DragLength; } }

		public Refinery(Actor self, RefineryInfo info)
		{
			this.self = self;
			this.info = info;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = info.TickRate;
			wsb = self.Trait<WithSpriteBody>();
		}

		public virtual Activity DockSequence(Actor harv, Actor self)
		{
			return new SpriteHarvesterDockSequence(harv, self, DeliveryAngle, IsDragRequired, DragOffset, DragLength);
		}

		public IEnumerable<TraitPair<Harvester>> GetLinkedHarvesters()
		{
			return self.World.ActorsWithTrait<Harvester>()
				.Where(a => a.Trait.LinkedProc == self);
		}

		public bool CanGiveResource(int amount) { return info.DiscardExcessResources || playerResources.CanGiveResources(amount); }

		public void GiveResource(int amount)
		{
			if (info.DiscardExcessResources)
				amount = Math.Min(amount, playerResources.ResourceCapacity - playerResources.Resources);
			playerResources.GiveResources(amount);
			if (info.ShowTicks)
				currentDisplayValue += amount;
		}

		void CancelDock(Actor self)
		{
			preventDock = true;

			// Cancel the dock sequence
			if (dockedHarv != null && !dockedHarv.IsDead)
				dockedHarv.CancelActivity();
		}

		public void Tick(Actor self)
		{
			// Harvester was killed while unloading
			if (dockedHarv != null && dockedHarv.IsDead)
			{
				wsb.CancelCustomAnimation(self);
				dockedHarv = null;
			}

			if (info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = info.TickRate;
				currentDisplayValue = 0;
			}
		}

		public void Disposing(Actor self)
		{
			CancelDock(self);
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			if (!preventDock)
			{
				harv.QueueActivity(new CallFunc(() => dockedHarv = harv, false));
				harv.QueueActivity(DockSequence(harv, self));
				harv.QueueActivity(new CallFunc(() => dockedHarv = null, false));
			}

			harv.QueueActivity(new CallFunc(() => harv.Trait<Harvester>().ContinueHarvesting(harv)));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Unlink any harvesters
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);

			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			// Steal any docked harv too
			if (dockedHarv != null)
			{
				dockedHarv.ChangeOwner(newOwner);

				// Relink to this refinery
				dockedHarv.Trait<Harvester>().LinkProc(dockedHarv, self);
			}
		}

		public void Selling(Actor self) { CancelDock(self); }
		public void Sold(Actor self)
		{
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public bool ShouldExplode(Actor self) { return Ore > 0; }
	}
}
