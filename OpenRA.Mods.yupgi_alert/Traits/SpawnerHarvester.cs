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

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.yupgi_alert.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("This actor is a harvester that uses its spawns to indirectly harvest resources. i.e., Slave Miner.")]
	public class SpawnerHarvesterInfo : ITraitInfo, Requires<IOccupySpaceInfo>, Requires<MobileInfo>, Requires<GrantConditionOnDeployInfo>
	{
		[Desc("Number of spawned units")]
		public readonly int Count = 5;

		[Desc("Spawn unit type")]
		public readonly string SpawnUnit;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int RespawnTicks = 15;

		[Desc("For slaves to dock, we need this condition.")]
		public readonly string RequiredDockingCondition = null;

		[Desc("The spawner must have this condition to launch the spawned.")]
		public readonly string RequiredLaunchCondition = null;

		[Desc("Air units and ground units have different mobile trait so...")]
		// This can be computed but that requires a few cycles of cpu time XD
		// Interesting... flying slaved harvester units...
		public readonly bool SpawnIsGroundUnit = true;

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
	    public readonly int KickDelay = 150;

		public object Create(ActorInitializer init) { return new SpawnerHarvester(init, this); }
	}

	public enum MiningState
	{
		Scan, // Scan ore and move there
		CheckDeploy, // Check we can catually deploy and do it if possible
		Deploying, // Playing deploy animation.
		Mining, // Slaves are mining. We get kicked sometimes to move closer to ore.
		Kick // Check if there's ore field is close enough.
	}

	public class SpawnerHarvester : INotifyCreated, INotifyKilled,
		INotifyOwnerChanged, ITick, INotifySold, INotifyActorDisposing,
		IIssueOrder, IResolveOrder, IOrderVoice, INotifyBuildComplete
	{
		readonly SpawnerHarvesterInfo info;
		readonly Actor self;
		readonly Mobile mobile;

		// Because activities don't remember states, we remember states here for them.
		public CPos? LastOrderLocation = null;
		public MiningState MiningState = MiningState.Scan;

		// keep track of launched ones so spawner can call them in or designate another target.
		readonly HashSet<Actor> launched = new HashSet<Actor>();
		readonly Lazy<IFacing> facing;
		readonly ExitInfo[] exits;

		// condition management
		ConditionManager conditionManager;

		public int SpawnCount { get { return launched.Count; } }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SpawnerHarvestOrderTargeter(); }
		}

		int respawnTicks; // allowed to spawn a new slave when <= 0.

		public SpawnerHarvester(ActorInitializer init, SpawnerHarvesterInfo info)
		{
			self = init.Self;
			this.info = info;

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();

			mobile = self.Trait<Mobile>();
		}

		void FindResourcesOnCreation(Actor self)
		{
			if (info.SearchOnCreation)
				self.QueueActivity(new SpawnerHarvesterHarvest(self));
		}

		public void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
			FindResourcesOnCreation(self);
		}

		public void BuildingComplete(Actor self)
		{
			FindResourcesOnCreation(self);
		}

		IEnumerable<CPos> GetAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return true; // can always load slaves, unless the airbourne carrier has to land (not implemented)
		}

		public virtual void OnBecomingIdle(Actor self)
		{
		}

		public void SpawnedRemoved(Actor self, Actor slave)
		{
			if (self.IsDead || self.Disposed || sold)
				// Well, complicated. Killed() invokes slave.kill(), whichi invokes this logic.
				// That's a bad loop. Don't let it be a loop.
				return;

			if (launched.Contains(slave))
				launched.Remove(slave);
		}

		// Production.cs use random to select an exit.
		// Here, we choose one by round robin.
		// Start from -1 so that +1 logic below will make it 0.
		int exit_round_robin = -1;
		ExitInfo ChooseExit(Actor self)
		{
			if (exits.Length == 0)
				return null;
			exit_round_robin = (exit_round_robin + 1) % exits.Length;
			return exits[exit_round_robin];
		}

		// target: target to mine.
		void EjectSpawned(Actor self, Actor spawned, ExitInfo exit, CPos targetLocation)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || self.Disposed)
					return;

				var spawn_offset = exit == null ? WVec.Zero : exit.SpawnOffset;
				spawned.Trait<IPositionable>().SetVisualPosition(spawned, self.CenterPosition + spawn_offset);

				spawned.CancelActivity(); // Reset any activity.

				// Move into world, if not. Ground units get stuck without this.
				if (info.SpawnIsGroundUnit)
				{
					var mv = spawned.Trait<IMove>().MoveIntoWorld(spawned, self.Location);
					if (mv != null)
						spawned.QueueActivity(mv);
				}

				AssignTargetForSpawned(spawned, targetLocation);
				spawned.QueueActivity(new FindResources(spawned));
				w.Add(spawned);
			});
		}

		void AssignTargetForSpawned(Actor s, CPos targetLocation)
		{
			// set target spot to mine
			var sh = s.Trait<Harvester>();
			sh.LastOrderLocation = targetLocation;

			// This prevents harvesters returning to an empty patch when the player orders them to a new patch:
			sh.LastHarvestedCell = sh.LastOrderLocation;
		}

		// Launch a slave spawn to do mining "target".
		// Returns true when spawned something.
		public bool Launch(Actor self, CPos targetLocation)
		{
			if (launched.Count >= info.Count)
				return false;

			if (respawnTicks > 0)
				return false;

			var s = CreateSpawned(self);
			var exit = ChooseExit(self);
			EjectSpawned(self, s, exit, targetLocation);
			return true;
		}

		Actor CreateSpawned(Actor self)
		{
			var actor = self.World.CreateActor(false, info.SpawnUnit.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner) });
			var sh = actor.Trait<Harvester>();
			sh.Master = self; // let the spawned actor resolve me.

			launched.Add(actor);
			return actor;
		}

		void SetSpawnedFacing(Actor spawned, Actor spawner, ExitInfo exit)
		{
			if (facing.Value == null)
				return;
			var launch_angle = exit != null ? exit.Facing : 0;

			var spawnFacing = spawned.TraitOrDefault<IFacing>();
			if (spawnFacing != null)
				spawnFacing.Facing = (facing.Value.Facing + launch_angle) % 256;

			foreach (var t in spawned.TraitsImplementing<Turreted>())
				t.TurretFacing = (facing.Value.Facing + launch_angle) % 256;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var a in launched)
				if (!a.IsDead && !a.Disposed)
					a.Kill(e.Attacker);
			launched.Clear();
		}

		public void Disposing(Actor self)
		{
			foreach (var a in launched)
				if (!a.IsDead && !a.Disposed)
					a.Dispose();
			launched.Clear();
		}

		public void Selling(Actor self) { }

		bool sold = false;
		public void Sold(Actor self)
		{
			if (sold)
				return;
			sold = true;

			// Kill launched.
			foreach (var c in launched)
				if (!c.IsDead)
					c.Kill(self);
			launched.Clear();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				// Slaves just work for the new owner.
				foreach (var a in launched)
					a.ChangeOwner(newOwner);
			});
		}

		public void Tick(Actor self)
		{
			if (launched.Count >= info.Count)
				return;

			// Keep launching slaves if we can.
			if (respawnTicks-- <= 0)
			{
				if (MiningState == MiningState.Mining)
					Launch(self, LastOrderLocation.Value);
				respawnTicks = info.RespawnTicks;
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "SpawnerHarvest")
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
			return null;
		}

		CPos resolveHarvestLocation(Actor self, Order order)
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

		void handleSpawnerHarvest(Actor self, Order order, MiningState state)
		{
			MiningState = state;
			if (state != MiningState.Deploying)
				// state == Deploying implies order string of SpawnerHarvestDeploying
				// and must not cancel deploy activity!
				self.CancelActivity();

			CPos loc = resolveHarvestLocation(self, order);
			LastOrderLocation = loc;
			var findResources = new SpawnerHarvesterHarvest(self);
			self.QueueActivity(findResources);
			self.SetTargetLine(Target.FromCell(self.World, loc), Color.Red);

			// Assign new targets for slaves too.
			foreach (var s in launched)
			{
				// Don't cancel but "queue" it.
				AssignTargetForSpawned(s, loc);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpawnerHarvest")
				handleSpawnerHarvest(self, order, MiningState.Scan);
			else if (order.OrderString == "SpawnerHarvestDeploying")
				handleSpawnerHarvest(self, order, MiningState.Deploying);
			else if (order.OrderString == "Stop" || order.OrderString == "Move")
				MiningState = MiningState.Scan;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "SpawnerHarvest" ? info.HarvestVoice : null;
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
			var info = self.Info.TraitInfo<SpawnerHarvesterInfo>();

			if (res == null || !info.Resources.Contains(res.Info.Type))
				return false;

			cursor = "harvest";
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			return true;
		}
	}

	public static class ActorExts
	{
		public static bool CanHarvestAt(this Actor self, CPos pos, ResourceLayer resLayer, SpawnerHarvesterInfo harvInfo,
		ResourceClaimLayer territory)
		{
			var resType = resLayer.GetResource(pos);
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			if (!harvInfo.Resources.Contains(resType.Info.Type))
				return false;

			if (territory != null)
			{
				// Another harvester has claimed this resource:
				ResourceClaim claim;
				if (territory.IsClaimedByAnyoneElse(self as Actor, pos, out claim))
					return false;
			}

			if (self.Location == pos)
				return true;
			return self.Trait<Mobile>().CanEnterCell(pos);
		}
	}
}
