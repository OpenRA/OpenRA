#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HarvesterInfo : TraitInfo, Requires<MobileInfo>
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

		[Desc("How many bales can it dump at once.")]
		public readonly int BaleUnloadAmount = 1;

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
		public readonly int SearchFromHarvesterRadius = 12;

		[Desc("Interval to wait between searches when there are no resources nearby.")]
		public readonly int WaitDuration = 25;

		[Desc("Find a new refinery to unload at if more than this many harvesters are already waiting.")]
		public readonly int MaxUnloadQueue = 3;

		[Desc("The pathfinding cost penalty applied for each harvester waiting to unload at a refinery.")]
		public readonly int UnloadQueueCostModifier = 12;

		[Desc("The pathfinding cost penalty applied for cells directly away from the refinery.")]
		public readonly int ResourceRefineryDirectionPenalty = 200;

		[Desc("Does the unit queue harvesting runs instead of individual harvest actions?")]
		public readonly bool QueueFullLoad = false;

		[GrantedConditionReference]
		[Desc("Condition to grant while empty.")]
		public readonly string EmptyCondition = null;

		[VoiceReference]
		public readonly string HarvestVoice = "Action";

		[VoiceReference]
		public readonly string DeliverVoice = "Action";

		[Desc("Color to use for the target line of harvest orders.")]
		public readonly Color HarvestLineColor = Color.Crimson;

		[Desc("Color to use for the target line of harvest orders.")]
		public readonly Color DeliverLineColor = Color.Green;

		[Desc("Cursor to display when able to unload at target actor.")]
		public readonly string EnterCursor = "enter";

		[Desc("Cursor to display when unable to unload at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new Harvester(init.Self, this); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, IOrderVoice,
		ISpeedModifier, ISync, INotifyCreated
	{
		public readonly HarvesterInfo Info;
		public readonly IReadOnlyDictionary<ResourceTypeInfo, int> Contents;

		readonly Mobile mobile;
		readonly ResourceLayer resLayer;
		readonly ResourceClaimLayer claimLayer;
		readonly Dictionary<ResourceTypeInfo, int> contents = new Dictionary<ResourceTypeInfo, int>();
		int conditionToken = Actor.InvalidConditionToken;
		HarvesterResourceMultiplier[] resourceMultipliers;

		[Sync]
		public Actor LastLinkedProc = null;

		[Sync]
		public Actor LinkedProc = null;

		[Sync]
		int currentUnloadTicks;

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
			Contents = new ReadOnlyDictionary<ResourceTypeInfo, int>(contents);

			mobile = self.Trait<Mobile>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
		}

		void INotifyCreated.Created(Actor self)
		{
			resourceMultipliers = self.TraitsImplementing<HarvesterResourceMultiplier>().ToArray();
			UpdateCondition(self);

			self.QueueActivity(new CallFunc(() => ChooseNewProc(self, null)));

			// Note: This is queued in a FrameEndTask because otherwise the activity is dropped/overridden while moving out of a factory.
			if (Info.SearchOnCreation)
				self.World.AddFrameEndTask(w => self.QueueActivity(new FindAndDeliverResources(self)));
		}

		public void LinkProc(Actor self, Actor proc)
		{
			LinkedProc = proc;
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

		bool IsAcceptableProcType(Actor proc)
		{
			return Info.DeliveryBuildings.Count == 0 ||
				Info.DeliveryBuildings.Contains(proc.Info.Name);
		}

		public Actor ClosestProc(Actor self, Actor ignore)
		{
			// Find all refineries and their occupancy count:
			var refineries = self.World.ActorsWithTrait<IAcceptResources>()
				.Where(r => r.Actor != ignore && r.Actor.Owner == self.Owner && IsAcceptableProcType(r.Actor))
				.Select(r => new
				{
					Location = r.Actor.Location + r.Trait.DeliveryOffset,
					Actor = r.Actor,
					Occupancy = self.World.ActorsHavingTrait<Harvester>(h => h.LinkedProc == r.Actor).Count()
				}).ToLookup(r => r.Location);

			// Start a search from each refinery's delivery location:
			List<CPos> path;

			using (var search = PathSearch.FromPoints(self.World, mobile.Locomotor, self, refineries.Select(r => r.Key), self.Location, BlockedByActor.None)
				.WithCustomCost(location =>
				{
					if (!refineries.Contains(location))
						return 0;

					var occupancy = refineries[location].First().Occupancy;

					// Too many harvesters clogs up the refinery's delivery location:
					if (occupancy >= Info.MaxUnloadQueue)
						return PathGraph.CostForInvalidCell;

					// Prefer refineries with less occupancy (multiplier is to offset distance cost):
					return occupancy * Info.UnloadQueueCostModifier;
				}))
				path = self.World.WorldActor.Trait<IPathFinder>().FindPath(search);

			if (path.Count != 0)
				return refineries[path.Last()].First().Actor;

			return null;
		}

		public bool IsFull { get { return contents.Values.Sum() == Info.Capacity; } }
		public bool IsEmpty { get { return contents.Values.Sum() == 0; } }
		public int Fullness { get { return contents.Values.Sum() * 100 / Info.Capacity; } }

		void UpdateCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.EmptyCondition))
				return;

			var enabled = IsEmpty;

			if (enabled && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.EmptyCondition);
			else if (!enabled && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		public void AcceptResource(Actor self, ResourceType type)
		{
			if (!contents.ContainsKey(type.Info))
				contents[type.Info] = 1;
			else
				contents[type.Info]++;

			UpdateCondition(self);
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
				var count = Math.Min(contents[type], Info.BaleUnloadAmount);
				var value = Util.ApplyPercentageModifiers(type.ValuePerUnit * count, resourceMultipliers.Select(m => m.GetModifier()));

				if (!iao.CanGiveResource(value))
					return false;

				iao.GiveResource(value);
				contents[type] -= count;
				if (contents[type] == 0)
					contents.Remove(type);

				currentUnloadTicks = Info.BaleUnloadDelay;
				UpdateCondition(self);
			}

			return contents.Count == 0;
		}

		public bool CanHarvestCell(Actor self, CPos cell)
		{
			// Resources only exist in the ground layer
			if (cell.Layer != 0)
				return false;

			var resType = resLayer.GetResourceType(cell);
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			return Info.Resources.Contains(resType.Info.Type);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<IAcceptResourcesInfo>(
					"Deliver",
					5,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					(proc, _) => IsAcceptableProcType(proc),
					proc => proc.Trait<IAcceptResources>().AllowDocking);
				yield return new HarvestOrderTargeter();
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Deliver" || order.OrderID == "Harvest")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
				return Info.HarvestVoice;

			if (order.OrderString == "Deliver" && !IsEmpty)
				return Info.DeliverVoice;

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
				// NOTE: An explicit harvest order allows the harvester to decide which refinery to deliver to.
				LinkProc(self, null);

				CPos loc;
				if (order.Target.Type != TargetType.Invalid)
				{
					// Find the nearest claimable cell to the order location (useful for group-select harvest):
					var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
					loc = mobile.NearestCell(cell, p => mobile.CanEnterCell(p) && claimLayer.TryClaimCell(self, p), 1, 6);
				}
				else
				{
					// A bot order gives us a CPos.Zero TargetLocation.
					loc = self.Location;
				}

				// FindResources takes care of calling INotifyHarvesterAction
				self.QueueActivity(order.Queued, new FindAndDeliverResources(self, loc));
				self.ShowTargetLines();
			}
			else if (order.OrderString == "Deliver")
			{
				// Deliver orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				var targetActor = order.Target.Actor;
				var iao = targetActor.TraitOrDefault<IAcceptResources>();
				if (iao == null || !iao.AllowDocking || !IsAcceptableProcType(targetActor))
					return;

				self.QueueActivity(order.Queued, new FindAndDeliverResources(self, targetActor));
				self.ShowTargetLines();
			}
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			return 100 - (100 - Info.FullyLoadedSpeed) * contents.Values.Sum() / Info.Capacity;
		}

		class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "Harvest"; } }
			public int OrderPriority { get { return 10; } }
			public bool IsQueued { get; protected set; }
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);

				// Don't leak info about resources under the shroud
				if (!self.Owner.Shroud.IsExplored(location))
					return false;

				var info = self.Info.TraitInfo<HarvesterInfo>();
				var res = self.World.WorldActor.TraitsImplementing<ResourceRenderer>()
					.Select(r => r.GetRenderedResourceType(location))
					.FirstOrDefault(r => r != null && info.Resources.Contains(r.Info.Type));

				if (res == null)
					return false;

				cursor = "harvest";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return true;
			}
		}
	}
}
