#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class RefineryInfo : ITraitInfo
	{
		public readonly int2 DockOffset = new int2(1, 2);

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;
		public readonly int TickRate = 10;
		public readonly int DockAngle = 64;

		public virtual object Create(ActorInitializer init) { return new Refinery(init.self, this); }
	}

	public class Refinery : ITick, IAcceptOre, INotifyKilled, INotifySold, INotifyCapture, IExplodeModifier, ISync
	{
		protected readonly Actor self;
		protected readonly RefineryInfo Info;
		PlayerResources PlayerResources;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;

		[Sync] public int Ore = 0;
		[Sync] Actor dockedHarv = null;
		[Sync] bool preventDock = false;

		public bool AllowDocking { get { return !preventDock; } }

		public CVec DeliverOffset { get { return (CVec)Info.DockOffset; } }

		public virtual Activity DockSequence(Actor harv, Actor self) { throw new System.NotImplementedException(); }

		public Refinery(Actor self, RefineryInfo info)
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

		public bool CanGiveOre(int amount) { return PlayerResources.CanGiveOre(amount); }

		public void GiveOre(int amount)
		{
			PlayerResources.GiveOre(amount);
			if (Info.ShowTicks)
				currentDisplayValue += amount;
		}

		void CancelDock(Actor self)
		{
			preventDock = true;

			// Cancel the dock sequence
			if (dockedHarv != null && !dockedHarv.IsDead())
				dockedHarv.CancelActivity();
		}

		public void Tick(Actor self)
		{
			// Harvester was killed while unloading
			if (dockedHarv != null && dockedHarv.IsDead())
			{
				self.Trait<RenderBuilding>().CancelCustomAnim(self);
				dockedHarv = null;
			}

			if (Info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new CashTick(self.CenterPosition, self.Owner.Color.RGB, temp)));
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

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			// Steal any docked harv too
			if (dockedHarv != null)
				dockedHarv.ChangeOwner(newOwner);

			// Unlink any non-docked harvs
			foreach (var harv in GetLinkedHarvesters())
				if (harv.Actor.Owner == oldOwner)
					harv.Trait.UnlinkProc(harv.Actor, self);

			PlayerResources = newOwner.PlayerActor.Trait<PlayerResources>();
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
