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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

/*
Sort of works without engine mod if you get docking right.
If you want "legit" OP Mod docking behavior where the slaves dock any cells near the Master,
then you need to modify harvester logics, which is the very core of the engine!
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This actor is a harvester that uses its spawns to indirectly harvest resources. i.e., Slave Miner.")]
	public class SpawnerHarvesterInfo : ITraitInfo, Requires<IOccupySpaceInfo>,
			Requires<MobileInfo>, Requires<GrantConditionOnDeployInfo>
	{
		[Desc("Number of spawned units")]
		public readonly int Count = 5;

		[Desc("Spawn unit type")]
		public readonly string SpawnUnit;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int RespawnTicks = 15;

		// This can be computed but that this should be faster.
		// Interesting... flying slaved harvester units...
		[Desc("Air units and ground units have different mobile trait so...")]
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
	    public readonly int KickDelay = 301;

		public object Create(ActorInitializer init) { return new SpawnerHarvester(init, this); }
	}

	public enum MiningState
	{
		Scan, // Scan ore and move there
		TryDeploy, // Try to deploy
		Deploying, // Playing deploy animation.
		Mining, // Slaves are mining. We get kicked sometimes to move closer to ore.
		Kick // Check if there's ore field is close enough.
	}

	public class SpawnerHarvester : INotifyCreated, INotifyKilled, INotifyIdle,
		INotifyOwnerChanged, ITick, INotifySold, INotifyActorDisposing,
		IIssueOrder, IResolveOrder, IOrderVoice, INotifyBuildComplete, INotifyDeploy
	{
		readonly SpawnerHarvesterInfo info;
		readonly Actor self;
		readonly ResourceLayer resLayer;
		readonly Mobile mobile;

		// Because activities don't remember states, we remember states here for them.
		public CPos? LastOrderLocation = null;
		public MiningState MiningState = MiningState.Scan;

		// keep track of launched ones so spawner can call them in or designate another target.
		readonly HashSet<Actor> launched = new HashSet<Actor>();
		readonly Lazy<IFacing> facing;
		readonly ExitInfo[] exits;

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SpawnerHarvestOrderTargeter(); }
		}

		int respawnTicks; // allowed to spawn a new slave when <= 0.
		int kickTicks;
		bool allowKicks = true; // allow kicks?

		public SpawnerHarvester(ActorInitializer init, SpawnerHarvesterInfo info)
		{
			self = init.Self;
			this.info = info;

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();

			mobile = self.Trait<Mobile>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();

			kickTicks = info.KickDelay;
		}

		void FindResourcesOnCreation(Actor self)
		{
			if (info.SearchOnCreation)
				self.QueueActivity(new SpawnerHarvesterHarvest(self));
		}

		public void Created(Actor self)
		{
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

		public void SpawnedRemoved(Actor self, Actor slave)
		{
			// master.Killed() invokes slave.kill() and slave.kill() goes back to master. Break loop.
			if (self.IsDead || self.Disposed || sold)
				return;

			if (launched.Contains(slave))
				launched.Remove(slave);
		}

		// Production.cs use random to select an exit.
		// Here, we choose one by round robin.
		// Start from -1 so that +1 logic below will make it 0.
		int exitRoundRobin = -1;
		ExitInfo ChooseExit(Actor self)
		{
			if (exits.Length == 0)
				return null;
			exitRoundRobin = (exitRoundRobin + 1) % exits.Length;
			return exits[exitRoundRobin];
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
					if (e.Attacker.Owner != self.Owner)
						a.Trait<SpawnedHarvester>().Unslave(a, e.Attacker);
					else
						a.Kill(e.Attacker);
			launched.Clear();
		}

		public void Disposing(Actor self)
		{
			var toKill = launched.ToArray();
			foreach (var a in toKill)
				if (!a.IsDead && !a.Disposed)
					a.Kill(a);
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
				// Slaves owner change too but...
				// What happens after that depends on slave's OnOwnerChanged().
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
					if (LastOrderLocation.HasValue)
						Launch(self, LastOrderLocation.Value);
					else
						Launch(self, self.Location);
				respawnTicks = info.RespawnTicks;
			}
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
			foreach (var s in launched)
			{
				// Don't cancel but "queue" it.
				AssignTargetForSpawned(s, LastOrderLocation.Value);
			}
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
			// wake up on idle for long
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
			foreach (var s in launched)
			{
				s.CancelActivity();
				AssignTargetForSpawned(s, self.Location);
				s.QueueActivity(new DeliverResources(s));
			}
		}

		public void OnUndeployed(Actor self)
		{
			allowKicks = false;

			// Interrupt harvesters and order them to follow me.
			foreach (var s in launched)
			{
				s.CancelActivity();
				s.QueueActivity(new Follow(s, Target.FromActor(self), WDist.FromCells(1), WDist.FromCells(3)));
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
			var info = self.Info.TraitInfo<SpawnerHarvesterInfo>();

			if (res == null || !info.Resources.Contains(res.Info.Type))
				return false;

			cursor = "harvest";
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			return true;
		}
	}
}
