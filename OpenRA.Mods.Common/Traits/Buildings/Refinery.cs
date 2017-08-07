#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class RefineryInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<DockManagerInfo>
	{
		[Desc("Discard resources once silo capacity has been reached.")]
		public readonly bool DiscardExcessResources = false;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;

		public virtual object Create(ActorInitializer init) { return new Refinery(init.Self, this); }
	}

	public class Refinery : ITick, IResourceExchange, INotifySold, INotifyCapture, INotifyOwnerChanged, IExplodeModifier, ISync, INotifyActorDisposing
	{
		readonly Actor self;
		readonly RefineryInfo info;
		readonly DockManager docks;
		readonly WithSpriteBody wsb;
		PlayerResources playerResources;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;

		[Sync] public int Ore = 0;
		[Sync] bool preventDock = false;

		public bool AllowDocking { get { return !preventDock; } }

		public Refinery(Actor self, RefineryInfo info)
		{
			this.self = self;
			this.info = info;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = info.TickRate;
			wsb = self.Trait<WithSpriteBody>();
			docks = self.Trait<DockManager>();
		}

		public virtual Activity DockSequence(Actor harv, Actor self, Dock dock)
		{
			return new SpriteHarvesterDockSequence(harv, self, dock.Location,
				dock.Info.DockAngle, dock.Info.IsDragRequired, dock.Info.DragOffset, dock.Info.DragLength);
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
		}

		void ITick.Tick(Actor self)
		{
			// Refining animation cancelation.
			// When everything docked all get killed in one shot then refining anim should be canceled.
			// (Nukes...) Lets not care about chrono miners. Won't look too odd.
			var dockedHarvs = docks.DockedUnits;
			if (dockedHarvs.Count() > 0 && dockedHarvs.Count() == dockedHarvs.Where(a => a.IsDead).Count())
				wsb.CancelCustomAnimation(self);

			if (info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = info.TickRate;
				currentDisplayValue = 0;
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			CancelDock(self);
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Unlink any harvesters
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);

			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			// Steal any docked harv too
			var harvs = docks.DockedUnits;
			if (harvs.Count() == 0)
				return;

			foreach (var dockedHarv in harvs)
			{
				dockedHarv.ChangeOwner(newOwner);

				// Relink to this refinery
				dockedHarv.Trait<Harvester>().LinkProc(dockedHarv, self);
			}
		}

		void INotifySold.Selling(Actor self) { CancelDock(self); }
		void INotifySold.Sold(Actor self)
		{
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public bool ShouldExplode(Actor self) { return Ore > 0; }
	}
}
