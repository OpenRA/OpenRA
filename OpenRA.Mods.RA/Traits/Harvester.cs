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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class HarvesterInfo : ITraitInfo
	{
		public readonly string[] DeliveryBuildings = { };
		[Desc("How much resources it can carry.")]
		public readonly int Capacity = 28;
		public readonly int LoadTicksPerBale = 4;
		[Desc("How fast it can dump it's carryage.")]
		public readonly int UnloadTicksPerBale = 4;
		[Desc("How many squares to show the fill level.")]
		public readonly int PipCount = 7;
		public readonly int HarvestFacings = 0;
		[Desc("Which resources it can harvest.")]
		public readonly string[] Resources = { };
		[Desc("Percentage of maximum speed when fully loaded.")]
		public readonly int FullyLoadedSpeed = 85;
		[Desc("Initial search radius (in cells) from the refinery that created us.")]
		public readonly int SearchFromProcRadius = 24;
		[Desc("Search radius (in cells) from the last harvest order location to find more resources.")]
		public readonly int SearchFromOrderRadius = 12;

		public object Create(ActorInitializer init) { return new Harvester(init.self, this); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, IPips,
		IExplodeModifier, IOrderVoice, ISpeedModifier, ISync,
		INotifyResourceClaimLost, INotifyIdle, INotifyBlockingMove
	{
		Dictionary<ResourceTypeInfo, int> contents = new Dictionary<ResourceTypeInfo, int>();

		[Sync] public Actor OwnerLinkedProc = null;
		[Sync] public Actor LastLinkedProc = null;
		[Sync] public Actor LinkedProc = null;
		[Sync] int currentUnloadTicks;
		public CPos? LastHarvestedCell = null;
		public CPos? LastOrderLocation = null;
		[Sync] public int ContentValue { get { return contents.Sum(c => c.Key.ValuePerUnit * c.Value); } }
		readonly HarvesterInfo Info;
		bool idleSmart = true;

		public Harvester(Actor self, HarvesterInfo info)
		{
			Info = info;
			self.QueueActivity(new CallFunc(() => ChooseNewProc(self, null)));
		}

		public void SetProcLines(Actor proc)
		{
			if (proc == null) return;
			if (proc.Destroyed) return;

			var linkedHarvs = proc.World.ActorsWithTrait<Harvester>()
				.Where(a => a.Trait.LinkedProc == proc)
				.Select(a => Target.FromActor(a.Actor))
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
			// Move out of the refinery dock and continue harvesting:
			UnblockRefinery(self);
			self.QueueActivity(new FindResources());
		}

		bool IsAcceptableProcType(Actor proc)
		{
			return Info.DeliveryBuildings.Length == 0 ||
				Info.DeliveryBuildings.Contains(proc.Info.Name);
		}

		Actor ClosestProc(Actor self, Actor ignore)
		{
			// Find all refineries and their occupancy count:
			var refs = (
				from r in self.World.ActorsWithTrait<IAcceptOre>()
				where r.Actor != ignore && r.Actor.Owner == self.Owner && IsAcceptableProcType(r.Actor)
				let linkedHarvs = self.World.ActorsWithTrait<Harvester>().Where(a => a.Trait.LinkedProc == r.Actor).Count()
				select new { Location = r.Actor.Location + r.Trait.DeliverOffset, Actor = r.Actor, Occupancy = linkedHarvs }
			).ToDictionary(r => r.Location);

			// Start a search from each refinery's delivery location:
			var mi = self.Info.Traits.Get<MobileInfo>();
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(
				PathSearch.FromPoints(self.World, mi, self, refs.Values.Select(r => r.Location), self.Location, false)
					.WithCustomCost((loc) =>
					{
						if (!refs.ContainsKey(loc)) return 0;

						var occupancy = refs[loc].Occupancy;
						// 4 harvesters clogs up the refinery's delivery location:
						if (occupancy >= 3) return int.MaxValue;

						// Prefer refineries with less occupancy (multiplier is to offset distance cost):
						return occupancy * 12;
					})
			);

			// Reverse the found-path to find the refinery location instead of our location:
			path.Reverse();

			if (path.Count != 0)
				return refs[path[0]].Actor;

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
			if (lastproc != null && !lastproc.Destroyed)
			{
				var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptOre>().DeliverOffset;
				if (self.Location == deliveryLoc)
				{
					// Get out of the way:
					var mobile = self.Trait<Mobile>();
					var harv = self.Trait<Harvester>();

					var moveTo = harv.LastHarvestedCell ?? (deliveryLoc + new CVec(0, 4));
					self.QueueActivity(mobile.MoveTo(moveTo, 1));
					self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);

					var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
					if (territory != null) territory.ClaimResource(self, moveTo);

					var notify = self.TraitsImplementing<INotifyHarvesterAction>();
					var next = new FindResources();
					foreach (var n in notify)
						n.MovingToResources(self, moveTo, next);

					self.QueueActivity(next);
					return;
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
				var mobile = self.Trait<Mobile>();

				var cell = self.Location;
				var moveTo = mobile.NearestMoveableCell(cell, 2, 5);
				self.QueueActivity(mobile.MoveTo(moveTo, 0));
				self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);

				// Find more resources but not at this location:
				self.QueueActivity(new FindResources(cell));
			}
		}

		public void TickIdle(Actor self)
		{
			// Should we be intelligent while idle?
			if (!idleSmart) return;

			// Are we not empty? Deliver resources:
			if (!IsEmpty)
			{
				self.QueueActivity(new DeliverResources());
				return;
			}

			UnblockRefinery(self);

			// Wait for a bit before becoming idle again:
			self.QueueActivity(new Wait(10));
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
				var iao = proc.Trait<IAcceptOre>();
				if (!iao.CanGiveOre(type.ValuePerUnit))
					return false;

				iao.GiveOre(type.ValuePerUnit);
				if (--contents[type] == 0)
					contents.Remove(type);

				currentUnloadTicks = Info.UnloadTicksPerBale;
			}

			return contents.Count == 0;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<IAcceptOre>("Deliver", 5,
					proc => IsAcceptableProcType(proc),
					proc => !IsEmpty && proc.Trait<IAcceptOre>().AllowDocking);
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
			return (order.OrderString == "Harvest" || (order.OrderString == "Deliver" && !IsEmpty)) ? "Move" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
				// NOTE: An explicit harvest order allows the harvester to decide which refinery to deliver to.
				LinkProc(self, OwnerLinkedProc = null);
				idleSmart = true;

				self.CancelActivity();
				
				var mobile = self.Trait<Mobile>();
				if (order.TargetLocation != CPos.Zero)
				{
					var loc = order.TargetLocation;
					var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();

					if (territory != null)
					{
						// Find the nearest claimable cell to the order location (useful for group-select harvest):
						loc = mobile.NearestCell(loc, p => mobile.CanEnterCell(p) && territory.ClaimResource(self, p), 1, 6);
					}
					else
					{
						// Find the nearest cell to the order location (useful for group-select harvest):
						var taken = new HashSet<CPos>();
						loc = mobile.NearestCell(loc, p => mobile.CanEnterCell(p) && taken.Add(p), 1, 6);
					}

					self.QueueActivity(mobile.MoveTo(loc, 0));
					self.SetTargetLine(Target.FromCell(self.World, loc), Color.Red);

					var notify = self.TraitsImplementing<INotifyHarvesterAction>();
					var next = new FindResources();
					foreach (var n in notify)
						n.MovingToResources(self, loc, next);

					LastOrderLocation = loc;
				}
				else
				{
					// A bot order gives us a CPos.Zero TargetLocation, so find some good resources for him:
					var loc = FindNextResourceForBot(self);
					// No more resources? Oh well.
					if (!loc.HasValue)
						return;

					self.QueueActivity(mobile.MoveTo(loc.Value, 0));
					self.SetTargetLine(Target.FromCell(self.World, loc.Value), Color.Red);

					LastOrderLocation = loc;
				}

				// This prevents harvesters returning to an empty patch when the player orders them to a new patch:
				LastHarvestedCell = LastOrderLocation;
				self.QueueActivity(new FindResources());
			}
			else if (order.OrderString == "Deliver")
			{
				// NOTE: An explicit deliver order forces the harvester to always deliver to this refinery.
				var iao = order.TargetActor.TraitOrDefault<IAcceptOre>();
				if (iao == null || !iao.AllowDocking || !IsAcceptableProcType(order.TargetActor))
					return;

				if (order.TargetActor != OwnerLinkedProc)
					LinkProc(self, OwnerLinkedProc = order.TargetActor);

				if (IsEmpty)
					return;

				idleSmart = true;

				self.SetTargetLine(Target.FromOrder(self.World, order), Color.Green);

				self.CancelActivity();
				self.QueueActivity(new DeliverResources());

				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				var next = new FindResources();
				foreach (var n in notify)
					n.MovingToResources(self, order.TargetLocation, next);
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

		static CPos? FindNextResourceForBot(Actor self)
		{
			// NOTE: This is only used for the AI to find the next available resource to harvest.
			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();

			// Find any harvestable resources:
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(
				PathSearch.Search(self.World, mobileInfo, self, true)
					.WithHeuristic(loc =>
					{
						// Get the resource at this location:
						var resType = resLayer.GetResource(loc);

						if (resType == null) return 1;
						// Can the harvester collect this kind of resource?
						if (!harvInfo.Resources.Contains(resType.Info.Name)) return 1;

						// Another harvester has claimed this resource:
						if (territory != null)
						{
							ResourceClaim claim;
							if (territory.IsClaimedByAnyoneElse(self, loc, out claim)) return 1;
						}

						return 0;
					})
					.FromPoint(self.Location)
			);

			if (path.Count == 0)
				return (CPos?)null;

			return path[0];
		}

		public void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer)
		{
			if (self == claimer) return;

			// Our claim on a resource was stolen, find more unclaimed resources:
			self.CancelActivity();
			self.QueueActivity(new FindResources());
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

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
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
				var info = self.Info.Traits.Get<HarvesterInfo>();

				if (res == null || !info.Resources.Contains(res.Info.Name))
					return false;

				cursor = "harvest";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return true;
			}
		}
	}
}
