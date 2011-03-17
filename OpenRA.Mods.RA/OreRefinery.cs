#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;
using OpenRA.Mods.RA.Move;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	public class OreRefineryInfo : ITraitInfo
	{
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Red;
		public readonly int2 DockOffset = new int2 (1, 2);
		public readonly int Capacity = 0;
		public readonly int ProcessTick = 25;
		public readonly int ProcessAmount = 50;
		public readonly int LowPowerProcessTick = 50;

		public virtual object Create(ActorInitializer init) { return new OreRefinery(init.self, this); }
	}

	public class OreRefinery : ITick, IAcceptOre, INotifyDamage, INotifySold, INotifyCapture, IExplodeModifier, ISync
	{
		readonly Actor self;
		readonly OreRefineryInfo Info;
		PlayerResources PlayerResources;

		[Sync]
		public int Ore = 0;

		[Sync]
		Actor dockedHarv = null;
		[Sync]
		bool preventDock = false;
		
		public int2 DeliverOffset { get { return Info.DockOffset; } }
		public virtual IActivity DockSequence(Actor harv, Actor self)
		{
			return new RAHarvesterDockSequence(harv, self);
		}
		
		public OreRefinery (Actor self, OreRefineryInfo info)
		{
			this.self = self;
			Info = info;
			PlayerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

        public IEnumerable<TraitPair<Harvester>> GetLinkedHarvesters()
        {
            return self.World.ActorsWithTrait<Harvester>()
                .Where(a => a.Trait.LinkedProc == self);
        }
		
		public bool CanGiveOre(int amount)
		{
			return PlayerResources.CanGiveOre(amount);
		}

		public void GiveOre(int amount)
		{
			PlayerResources.GiveOre(amount);
		}

		void CancelDock(Actor self)
		{
			preventDock = true;

			// Cancel the dock sequence
			if (dockedHarv != null && !dockedHarv.IsDead())
				dockedHarv.CancelActivity();
		}
		
		public void Tick (Actor self)
		{
			// Harvester was killed while unloading
			if (dockedHarv != null && dockedHarv.IsDead())
			{
				self.Trait<RenderBuilding>().CancelCustomAnim(self);
				dockedHarv = null;
			}
		}

		public void Damaged (Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				CancelDock(self);
				foreach (var harv in GetLinkedHarvesters())
					harv.Trait.UnlinkProc(harv.Actor, self);
			}
		}

		public void OnDock (Actor harv, DeliverResources dockOrder)
		{
			var mobile = harv.Trait<Mobile>();
			var harvester = harv.Trait<Harvester>();
			
			if (!preventDock)
			{
				harv.QueueActivity( new CallFunc( () => dockedHarv = harv, false ) );
				harv.QueueActivity( DockSequence(harv, self) );
				harv.QueueActivity( new CallFunc( () => dockedHarv = null, false ) );			
			}
			
			// Tell the harvester to start harvesting
			// TODO: This belongs on the harv idle activity
			harv.QueueActivity( new CallFunc( () =>
			{
				if (harvester.LastHarvestedCell != int2.Zero)
				{
					harv.QueueActivity( mobile.MoveTo(harvester.LastHarvestedCell, 5) );
					harv.SetTargetLine(Target.FromCell(harvester.LastHarvestedCell), Color.Red, false);
				}
				harv.QueueActivity( new Harvest() );
			}));
		}
		
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
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

		public void Selling (Actor self) { CancelDock(self); }
		public void Sold (Actor self)
		{
            foreach (var harv in GetLinkedHarvesters())
                harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public bool ShouldExplode(Actor self) { return Ore > 0; }
	}
}
