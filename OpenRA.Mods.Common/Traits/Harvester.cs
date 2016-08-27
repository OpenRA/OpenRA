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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HarvesterInfo : ITraitInfo, Requires<MobileInfo>
	{
		public readonly HashSet<string> DeliveryBuildings = new HashSet<string>();

		[Desc("How long (in ticks) to wait until (re-)checking for a nearby available DeliveryBuilding if not yet linked to one.")]
		public readonly int SearchForDeliveryBuildingDelay = 125;

		[Desc("Cell to move to when automatically unblocking DeliveryBuilding.")]
		public readonly CVec UnblockCell = new CVec(0, 4);

		[Desc("How much resources it can carry.")]
		public readonly int Capacity = 28;

		public readonly int BaleLoadDelay = 4;

		[Desc("How fast it can dump it's carryage.")]
		public readonly int BaleUnloadDelay = 4;

		[Desc("How many squares to show the fill level.")]
		public readonly int PipCount = 7;

		public readonly int HarvestFacings = 0;

		[Desc("Which resources it can harvest.")]
		public readonly HashSet<string> Resources = new HashSet<string>();

		[Desc("Percentage of maximum speed when fully loaded.")]
		public readonly int FullyLoadedSpeed = 85;

		[Desc("Automatically scan for resources when created.")]
		public readonly bool SearchOnCreation = true;

		[Desc("Initial search radius (in cells) from the refinery that created us.")]
		public readonly int SearchFromProcRadius = 24;

		[Desc("Search radius (in cells) from the last harvest order location to find more resources.")]
		public readonly int SearchFromOrderRadius = 12;

		[Desc("Maximum duration of being idle before queueing a Wait activity.")]
		public readonly int MaxIdleDuration = 25;

		[Desc("Duration to wait before becoming idle again.")]
		public readonly int WaitDuration = 25;

		[VoiceReference] public readonly string HarvestVoice = "Action";
		[VoiceReference] public readonly string DeliverVoice = "Action";

		public object Create(ActorInitializer init) { return new Harvester(init.Self, this); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, IPips,
		IExplodeModifier, IOrderVoice, ISpeedModifier, ISync, INotifyCreated,
		INotifyResourceClaimLost, INotifyIdle, INotifyBlockingMove, INotifyBuildComplete
	{
		public readonly HarvesterInfo Info;
		readonly Mobile mobile;
		Dictionary<ResourceTypeInfo, int> contents = new Dictionary<ResourceTypeInfo, int>();
		bool idleSmart = true;

		[Sync] public Actor OwnerLinkedProc = null;
		[Sync] public Actor LastLinkedProc = null;
		[Sync] public Actor LinkedProc = null;
		[Sync] int currentUnloadTicks;
		public CPos? LastHarvestedCell = null;
		public CPos? LastOrderLocation = null;
		[Sync]
		public int ContentValue
		{
			get
			{
				var value = 0;
				foreach (var c in contents)
					value += c.Key.ValuePerUnit * c.Value;
				return value;
			}
		}

		public Harvester(Actor self, HarvesterInfo info)
		{
			Info = info;
			mobile = self.Trait<Mobile>();
			self.QueueActivity(new CallFunc(() => ChooseNewProc(self, null)));
		}

		public void Created(Actor self)
		{
			if (Info.SearchOnCreation)
				self.QueueActivity(new FindResources(self));
		}

		public void BuildingComplete(Actor self)
		{
			if (Info.SearchOnCreation)
				self.QueueActivity(new FindResources(self));
		}

		public void SetProcLines(Actor proc)
		{
			if (proc == null) return;
			if (proc.Disposed) return;

			var linkedHarvs = proc.World.ActorsHavingTrait<Harvester>(h => h.LinkedProc == proc)
				.Select(a => Target.FromActor(a))
				.ToList();

			proc.SetTargetLines(linkedHarvs, Color.Gold);
		}

		public void LinkProc(Actor self, Actor proc)
		{
			var oldProc = LinkedProc;
			LinkedProc = proc;
			SetProcLines(oldProc);
			SetProcLines(proc);
		}

		public void UnlinkProc(Actor self, Actor proc)
		{
			if (LinkedProc == proc)
				ChooseNewProc(self, proc);
		}

		public void ChooseNewProc(Actor self, Actor ignore)
		{
			LastLinkedProc = null;
			LinkProc(self, ClosestProc(self, ignore));
		}

		public void ContinueHarvesting(Actor self)
		{
			// Move out of the refinery dock and continue harvesting
			UnblockRefinery(self);
			self.QueueActivity(new FindResources(self));
		}

		bool IsAcceptableProcType(Actor proc)
		{
			return Info.DeliveryBuildings.Count == 0 ||
				Info.DeliveryBuildings.Contains(proc.Info.Name);
		}

		public Actor ClosestProc(Actor self, Actor ignore)
		{
			// Find all refineries and their occupancy count:
			var refs = self.World.ActorsWithTrait<IAcceptResources>()
				.Where(r => r.Actor != ignore && r.Actor.Owner == self.Owner && IsAcceptableProcType(r.Actor))
				.Select(r => new {
					Location = r.Actor.Location + r.Trait.DeliveryOffset,
					Actor = r.Actor,
					Occupancy = self.World.ActorsHavingTrait<Harvester>(h => h.LinkedProc == r.Actor).Count() })
				.ToDictionary(r => r.Location);

			// Start a search from each refinery's delivery location:
			List<CPos> path;
			var mi = self.Info.TraitInfo<MobileInfo>();
			using (var search = PathSearch.FromPoints(self.World, mi, self, refs.Values.Select(r => r.Location), self.Location, false)
				.WithCustomCost(loc =>
				{
					if (!refs.ContainsKey(loc))
						return 0;

					var occupancy = refs[loc].Occupancy;

					// 4 harvesters clogs up the refinery's delivery location:
					if (occupancy >= 3)
						return Constants.InvalidNode;

					// Prefer refineries with less occupancy (multiplier is to offset distance cost):
					return occupancy * 12;
				}))
				path = self.World.WorldActor.Trait<IPathFinder>().FindPath(search);

			if (path.Count != 0)
				return refs[path.Last()].Actor;

			return null;
		}

		public bool IsFull { get { return contents.Values.Sum() == Info.Capacity; } }
		public bool IsEmpty { get { return contents.Values.Sum() == 0; } }
		public int Fullness { get { return contents.Values.Sum() * 100 / Info.Capacity; } }

		public void AcceptResource(ResourceType type)
		{
			if (!contents.ContainsKey(type.Info)) contents[type.Info] = 1;
			else contents[type.Info]++;
		}

		public void UnblockRefinery(Actor self)
		{
			// Check that we're not in a critical location and being useless (refinery drop-off):
			var lastproc = LastLinkedProc ?? LinkedProc;
			if (lastproc != null && !lastproc.Disposed)
			{
				var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptResources>().DeliveryOffset;
				if (self.Location == deliveryLoc)
				{
					// Get out of the way:
					var unblockCell = LastHarvestedCell ?? (deliveryLoc + Info.UnblockCell);
					var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);

					// TODO: The harvest-deliver-return sequence is a horrible mess of duplicated code and edge-cases
					var notify = self.TraitsImplementing<INotifyHarvesterAction>();
					var findResources = new FindResources(self);
					foreach (var n in notify)
						n.MovingToResources(self, moveTo, findResources);

					self.QueueActivity(mobile.MoveTo(moveTo, 1));
					self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);
				}
			}
		}

		public void OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			// I'm blocking someone else from moving to my location:
			var act = self.GetCurrentActivity();

			// If I'm just waiting around then get out of the way:
			if (act is Wait)
			{
				self.CancelActivity();

				var cell = self.Location;
				var moveTo = mobile.NearestMoveableCell(cell, 2, 5);
				self.QueueActivity(mobile.MoveTo(moveTo, 0));
				self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);

				// Find more resources but not at this location:
				self.QueueActivity(new FindResources(self, cell));
			}
		}

		int idleDuration;
		public void TickIdle(Actor self)
		{
			// Should we be intelligent while idle?
			if (!idleSmart) return;

			// Are we not empty? Deliver resources:
			if (!IsEmpty)
			{
				self.QueueActivity(new DeliverResources(self));
				return;
			}

			UnblockRefinery(self);
			idleDuration += 1;

			// Wait a bit before queueing Wait activity
			if (idleDuration > Info.MaxIdleDuration)
			{
				idleDuration = 0;

				// Wait for a bit before becoming idle again:
				self.QueueActivity(new Wait(Info.WaitDuration));
			}
		}

		// Returns true when unloading is complete
		public bool TickUnload(Actor self, Actor proc)
		{
			// Wait until the next bale is ready
			if (--currentUnloadTicks > 0)
				return false;

			if (contents.Keys.Count > 0)
			{
				var type = contents.First().Key;
				var iao = proc.Trait<IAcceptResources>();
				if (!iao.CanGiveResource(type.ValuePerUnit))
					return false;

				iao.GiveResource(type.ValuePerUnit);
				if (--contents[type] == 0)
					contents.Remove(type);

				currentUnloadTicks = Info.BaleUnloadDelay;
			}

			return contents.Count == 0;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<IAcceptResourcesInfo>("Deliver", 5,
					proc => IsAcceptableProcType(proc),
					proc => proc.Trait<IAcceptResources>().AllowDocking);
				yield return new HarvestOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Deliver")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if (order.OrderID == "Harvest")
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
				return Info.HarvestVoice;

			if (order.OrderString == "Deliver" && !IsEmpty)
				return Info.DeliverVoice;

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
				// NOTE: An explicit harvest order allows the harvester to decide which refinery to deliver to.
				LinkProc(self, OwnerLinkedProc = null);
				idleSmart = true;

				self.CancelActivity();

				CPos? loc;
				if (order.TargetLocation != CPos.Zero)
				{
					loc = order.TargetLocation;

					var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
					if (territory != null)
					{
						// Find the nearest claimable cell to the order location (useful for group-select harvest):
						loc = mobile.NearestCell(loc.Value, p => mobile.CanEnterCell(p) && territory.ClaimResource(self, p), 1, 6);
					}
					else
					{
						// Find the nearest cell to the order location (useful for group-select harvest):
						var taken = new HashSet<CPos>();
						loc = mobile.NearestCell(loc.Value, p => mobile.CanEnterCell(p) && taken.Add(p), 1, 6);
					}
				}
				else
				{
					// A bot order gives us a CPos.Zero TargetLocation.
					loc = self.Location;
				}

				var findResources = new FindResources(self);
				self.QueueActivity(findResources);
				self.SetTargetLine(Target.FromCell(self.World, loc.Value), Color.Red);

				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				foreach (var n in notify)
					n.MovingToResources(self, loc.Value, findResources);

				LastOrderLocation = loc;

				// This prevents harvesters returning to an empty patch when the player orders them to a new patch:
				LastHarvestedCell = LastOrderLocation;
			}
			else if (order.OrderString == "Deliver")
			{
				// NOTE: An explicit deliver order forces the harvester to always deliver to this refinery.
				var iao = order.TargetActor.TraitOrDefault<IAcceptResources>();
				if (iao == null || !iao.AllowDocking || !IsAcceptableProcType(order.TargetActor))
					return;

				if (order.TargetActor != OwnerLinkedProc)
					LinkProc(self, OwnerLinkedProc = order.TargetActor);

				idleSmart = true;

				self.SetTargetLine(Target.FromOrder(self.World, order), Color.Green);

				self.CancelActivity();

				var deliver = new DeliverResources(self);
				self.QueueActivity(deliver);

				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				foreach (var n in notify)
					n.MovingToRefinery(self, order.TargetLocation, deliver);
			}
			else if (order.OrderString == "Stop" || order.OrderString == "Move")
			{
				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				foreach (var n in notify)
					n.MovementCancelled(self);

				// Turn off idle smarts to obey the stop/move:
				idleSmart = false;
			}
		}

		public void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer)
		{
			if (self == claimer) return;

			// Our claim on a resource was stolen, find more unclaimed resources:
			self.CancelActivity();
			self.QueueActivity(new FindResources(self));
		}

		PipType GetPipAt(int i)
		{
			var n = i * Info.Capacity / Info.PipCount;

			foreach (var rt in contents)
				if (n < rt.Value)
					return rt.Key.PipColor;
				else
					n -= rt.Value;

			return PipType.Transparent;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = Info.PipCount;

			for (var i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		public bool ShouldExplode(Actor self) { return !IsEmpty; }

		public int GetSpeedModifier()
		{
			return 100 - (100 - Info.FullyLoadedSpeed) * contents.Values.Sum() / Info.Capacity;
		}

		class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "Harvest"; } }
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
				var info = self.Info.TraitInfo<HarvesterInfo>();

				if (res == null || !info.Resources.Contains(res.Info.Name))
					return false;

				cursor = "harvest";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return true;
			}
		}
	}
}
