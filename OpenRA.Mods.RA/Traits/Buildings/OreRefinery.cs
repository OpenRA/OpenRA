#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class OreRefineryInfo : ITraitInfo
	{
		[Desc("Docking cell relative to top-left cell.")]
		public readonly CVec DockOffset = new CVec(1, 2);

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;
		[Desc("Actually harvester facing when docking, 0-255 counter-clock-wise.")]
		public readonly int DockAngle = 64;

		public virtual object Create(ActorInitializer init) { return new OreRefinery(init.self, this); }
	}

	public class OreRefinery : ITick, IAcceptOre, INotifyKilled, INotifySold, INotifyCapture, INotifyOwnerChanged, IExplodeModifier, ISync
	{
		readonly Actor self;
		readonly OreRefineryInfo Info;
		PlayerResources PlayerResources;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;

		[Sync] public int Ore = 0;
		[Sync] Actor dockedHarv = null;
		[Sync] bool preventDock = false;

		public bool AllowDocking { get { return !preventDock; } }
		public CVec DeliverOffset { get { return Info.DockOffset; } }

		public virtual Activity DockSequence(Actor harv, Actor self) { return new RAHarvesterDockSequence(harv, self, Info.DockAngle); }

		public OreRefinery(Actor self, OreRefineryInfo info)
		{
			this.self = self;
			Info = info;
			PlayerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = Info.TickRate;
		}

		public IEnumerable<TraitPair<Harvester>> GetLinkedHarvesters()
		{
			return self.World.ActorsWithTrait<Harvester>()
				.Where(a => a.Trait.LinkedProc == self);
		}

		public bool CanGiveOre(int amount) { return PlayerResources.CanGiveResources(amount); }

		public void GiveOre(int amount)
		{
			PlayerResources.GiveResources(amount);
			if (Info.ShowTicks)
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
				self.Trait<RenderBuilding>().CancelCustomAnim(self);
				dockedHarv = null;
			}

			if (Info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = Info.TickRate;
				currentDisplayValue = 0;
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			CancelDock(self);
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			if (!preventDock)
			{
				harv.QueueActivity(new CallFunc( () => dockedHarv = harv, false));
				harv.QueueActivity(DockSequence(harv, self));
				harv.QueueActivity(new CallFunc( () => dockedHarv = null, false));
			}
			harv.QueueActivity(new CallFunc(() => harv.Trait<Harvester>().ContinueHarvesting(harv)));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Unlink any harvesters
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);

			PlayerResources = newOwner.PlayerActor.Trait<PlayerResources>();
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
