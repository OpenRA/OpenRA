#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

/*
Sort of works without engine mod if you get docking right.
If you want "legit" OP Mod docking behavior where the slaves dock any cells near the Master,
then you need to modify harvester logics, which is the very core of the engine!
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This actor is a harvester that uses its spawns to indirectly harvest resources. i.e., Slave Miner.")]
	public class SpawnerHarvesterMasterInfo : BaseSpawnerMasterInfo, Requires<IOccupySpaceInfo>, Requires<GrantConditionOnDeployInfo>
	{
		[VoiceReference] public readonly string HarvestVoice = "Action";

		[Desc("Which resources it can harvest. Make sure slaves can mine these too!")]
		public readonly HashSet<string> Resources = new HashSet<string>();

		[Desc("Automatically search for resources on creation?")]
		public readonly bool SearchOnCreation = true;

		[Desc("When deployed, use this scan radius.")]
		public readonly int ShortScanRadius = 8;

		[Desc("Look this far when Searching for Ore (in Cells)")]
		public readonly int LongScanRadius = 24;

		[Desc("Look this far when trying to find a deployable position from the target resource patch")]
		public readonly int DeployScanRadius = 8; // 8 * 8 * 3 should be enough candidates, seriously.

		[Desc("If no resource within range at each kick, move.")]
	    public readonly int KickScanRadius = 5;

		[Desc("If the SlaveMiner is idle for this long, he'll try to look for ore again at SlaveMinerShortScan range to find ore and wake up (in ticks)")]
	    public readonly int KickDelay = 301;

		public override object Create(ActorInitializer init) { return new SpawnerHarvesterMaster(init, this); }
	}

	public enum MiningState
	{
		Scan, // Scan ore and move there
		TryDeploy, // Try to deploy
		Deploying, // Playing deploy animation.
		Mining, // Slaves are mining. We get kicked sometimes to move closer to ore.
		Kick // Check if there's ore field is close enough.
	}

	public class SpawnerHarvesterMaster : BaseSpawnerMaster, INotifyBuildComplete, INotifyIdle,
		ITick, IIssueOrder, IResolveOrder, IOrderVoice, INotifyDeploy
	{
		readonly SpawnerHarvesterMasterInfo info;
		readonly Actor self;
		readonly ResourceLayer resLayer;
		readonly Mobile mobile;

		// Because activities don't remember states, we remember states here for them.
		public CPos? LastOrderLocation = null;
		public MiningState MiningState = MiningState.Scan;

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SpawnerHarvestOrderTargeter(); }
		}

		int respawnTicks; // allowed to spawn a new slave when <= 0.
		int kickTicks;
		bool allowKicks = true; // allow kicks?

		public SpawnerHarvesterMaster(ActorInitializer init, SpawnerHarvesterMasterInfo info) : base(init, info)
		{
			self = init.Self;
			this.info = info;

			mobile = self.Trait<Mobile>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();

			kickTicks = info.KickDelay;
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			// Search for resources upon creation.
			if (info.SearchOnCreation)
				self.QueueActivity(new SpawnerHarvesterHarvest(self));
		}	

		// Modify Harvester trait's states to do the mining.
		void AssignTargetForSpawned(Actor slave, CPos targetLocation)
		{
			var sh = slave.Trait<Harvester>();

			// set target spot to mine
			sh.LastOrderLocation = targetLocation;

			// This prevents harvesters returning to an empty patch when the player orders them to a new patch:
			sh.LastHarvestedCell = sh.LastOrderLocation;
		}

		// Launch a freshly created slave that isn't in world to the world.
		void Launch(Actor self, BaseSpawnerSlaveEntry se, CPos targetLocation)
		{
			var slave = se.Actor;

			SpawnIntoWorld(self, slave, self.CenterPosition);

			self.World.AddFrameEndTask(w =>
			{
				// Move into world, if not. Ground units get stuck without this.
				if (info.SpawnIsGroundUnit)
				{
					var mv = se.Actor.Trait<IMove>().MoveIntoWorld(slave, self.Location);
					if (mv != null)
						slave.QueueActivity(mv);
				}

				AssignTargetForSpawned(slave, targetLocation);
				slave.QueueActivity(new FindResources(slave));
			});
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			if (respawnTicks <= 0)
				respawnTicks = Info.RespawnTicks;
		}

		public void Tick(Actor self)
		{
			respawnTicks--;
			if (respawnTicks > 0)
				return;

			if (MiningState != MiningState.Mining)
				return;

			Replenish(self, SlaveEntries);

			CPos destination = LastOrderLocation.HasValue ? LastOrderLocation.Value : self.Location;

			// Launch whatever we can.
			bool hasInvalidEntry = false;
			foreach (var se in SlaveEntries)
				if (!se.IsValid)
					hasInvalidEntry = true;
				else if (!se.Actor.IsInWorld)
					Launch(self, se, destination);

			if (hasInvalidEntry)
				respawnTicks = info.RespawnTicks;
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "SpawnerHarvest")
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
			return null;
		}

		CPos ResolveHarvestLocation(Actor self, Order order)
		{
			if (order.TargetLocation == CPos.Zero)
				return self.Location;

			var loc = order.TargetLocation;

			var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			if (territory != null)
			{
				// Find the nearest claimable cell to the order location (useful for group-select harvest):
				return mobile.NearestCell(loc, p => mobile.CanEnterCell(p), 1, 6);
			}

			// Find the nearest cell to the order location (useful for group-select harvest):
			return mobile.NearestCell(loc, p => mobile.CanEnterCell(p), 1, 6);
		}

		void HandleSpawnerHarvest(Actor self, Order order)
		{
			allowKicks = true;

			// state == Deploying implies order string of SpawnerHarvestDeploying
			// and must not cancel deploy activity!
			if (MiningState != MiningState.Deploying)
				self.CancelActivity();

			MiningState = MiningState.Scan;

			LastOrderLocation = ResolveHarvestLocation(self, order);
			self.QueueActivity(new SpawnerHarvesterHarvest(self));
			self.SetTargetLine(Target.FromCell(self.World, LastOrderLocation.Value), Color.Red);

			// Assign new targets for slaves too.
			foreach (var se in SlaveEntries)
				if (se.IsValid && se.Actor.IsInWorld)
					AssignTargetForSpawned(se.Actor, LastOrderLocation.Value);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpawnerHarvest")
				HandleSpawnerHarvest(self, order);
			else if (order.OrderString == "Stop" || order.OrderString == "Move")
			{
				// Disable "smart idle"
				allowKicks = false;
				MiningState = MiningState.Scan;
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "SpawnerHarvest" ? info.HarvestVoice : null;
		}

		public void TickIdle(Actor self)
		{
			// wake up on idle for long (to find new resource patch. i.e., kick)
			if (allowKicks && self.IsIdle)
				kickTicks--;
			else
				kickTicks = info.KickDelay;

			if (kickTicks <= 0)
			{
				kickTicks = info.KickDelay;
				MiningState = MiningState.Kick;
				self.QueueActivity(new SpawnerHarvesterHarvest(self));
			}
		}

		public void OnDeployed(Actor self)
		{
			allowKicks = true;

			// rescan from where we are
			MiningState = MiningState.Scan;

			// Tell harvesters to unload and restart mining.
			foreach (var se in SlaveEntries)
			{
				if (!se.IsValid || !se.Actor.IsInWorld)
					continue;

				var s = se.Actor;
				se.SpawnerSlave.Stop(s);
				AssignTargetForSpawned(s, self.Location);
				s.QueueActivity(new FindResources(s));
			}
		}

		public void OnUndeployed(Actor self)
		{
			allowKicks = false;

			// Interrupt harvesters and order them to follow me.
			foreach (var se in SlaveEntries)
			{
				se.SpawnerSlave.Stop(se.Actor);
				se.Actor.QueueActivity(new Follow(se.Actor, Target.FromActor(self), WDist.FromCells(1), WDist.FromCells(3)));
			}
		}

		public bool CanHarvestCell(Actor self, CPos cell)
		{
			// Resources only exist in the ground layer
			if (cell.Layer != 0)
				return false;

			var resType = resLayer.GetResource(cell);
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			return info.Resources.Contains(resType.Info.Type);
		}
	}

	class SpawnerHarvestOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "SpawnerHarvest"; } }
		public int OrderPriority { get { return 10; } }
		public bool IsQueued { get; protected set; }
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			if (target.Type != TargetType.Terrain)
				return false;

			if (modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			var location = self.World.Map.CellContaining(target.CenterPosition);

			// Don't leak info about resources under the shroud
			if (!self.Owner.Shroud.IsExplored(location))
				return false;

			var res = self.World.WorldActor.Trait<ResourceLayer>().GetRenderedResource(location);
			var info = self.Info.TraitInfo<SpawnerHarvesterMasterInfo>();

			if (res == null || !info.Resources.Contains(res.Info.Type))
				return false;

			cursor = "harvest";
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			return true;
		}
	}
}
