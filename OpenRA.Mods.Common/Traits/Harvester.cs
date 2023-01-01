#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HarvesterInfo : ConditionalTraitInfo, Requires<MobileInfo>
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

		[CursorReference]
		[Desc("Cursor to display when able to unload at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to unload at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[CursorReference]
		[Desc("Cursor to display when ordering to harvest resources.")]
		public readonly string HarvestCursor = "harvest";

		public override object Create(ActorInitializer init) { return new Harvester(init.Self, this); }
	}

	public class Harvester : ConditionalTrait<HarvesterInfo>, IIssueOrder, IResolveOrder, IOrderVoice,
		ISpeedModifier, ISync, INotifyCreated
	{
		public readonly IReadOnlyDictionary<string, int> Contents;

		readonly Mobile mobile;
		readonly IResourceLayer resourceLayer;
		readonly ResourceClaimLayer claimLayer;
		readonly Dictionary<string, int> contents = new Dictionary<string, int>();
		int conditionToken = Actor.InvalidConditionToken;

		[Sync]
		public Actor LastLinkedProc = null;

		[Sync]
		public Actor LinkedProc = null;

		[Sync]
		int currentUnloadTicks;

		[Sync]
		public int ContentHash
		{
			get
			{
				var value = 0;
				foreach (var c in contents)
					value += c.Value << c.Key.Length;
				return value;
			}
		}

		public Harvester(Actor self, HarvesterInfo info)
			: base(info)
		{
			Contents = new ReadOnlyDictionary<string, int>(contents);
			mobile = self.Trait<Mobile>();
			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
		}

		protected override void Created(Actor self)
		{
			UpdateCondition(self);

			// Note: This is queued in a FrameEndTask because otherwise the activity is dropped/overridden while moving out of a factory.
			if (Info.SearchOnCreation)
				self.World.AddFrameEndTask(w => self.QueueActivity(new FindAndDeliverResources(self)));

			base.Created(self);
		}

		public void LinkProc(Actor proc)
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
			LinkProc(ClosestProc(self, ignore));
		}

		bool IsAcceptableProcType(Actor proc)
		{
			return Info.DeliveryBuildings.Count == 0 ||
				Info.DeliveryBuildings.Contains(proc.Info.Name);
		}

		public Actor ClosestProc(Actor self, Actor ignore)
		{
			// Find all refineries and their occupancy count:
			// Exclude refineries with too many harvesters clogging the delivery location.
			var refineries = self.World.ActorsWithTrait<IAcceptResources>()
				.Where(r => r.Actor != ignore && r.Actor.Owner == self.Owner && IsAcceptableProcType(r.Actor))
				.Select(r => new
				{
					Location = r.Actor.Location + r.Trait.DeliveryOffset,
					Actor = r.Actor,
					Occupancy = self.World.ActorsHavingTrait<Harvester>(h => h.LinkedProc == r.Actor).Count()
				})
				.Where(r => r.Occupancy < Info.MaxUnloadQueue)
				.ToDictionary(r => r.Location);

			if (refineries.Count == 0)
				return null;

			// Start a search from each refinery's delivery location:
			var path = mobile.PathFinder.FindPathToTargetCell(
				self, refineries.Select(r => r.Key), self.Location, BlockedByActor.None,
				location =>
				{
					if (!refineries.ContainsKey(location))
						return 0;

					// Prefer refineries with less occupancy (multiplier is to offset distance cost):
					var occupancy = refineries[location].Occupancy;
					return occupancy * Info.UnloadQueueCostModifier;
				});

			if (path.Count > 0)
				return refineries[path.Last()].Actor;

			return null;
		}

		public bool IsFull => contents.Values.Sum() == Info.Capacity;
		public bool IsEmpty => contents.Values.Sum() == 0;
		public int Fullness => contents.Values.Sum() * 100 / Info.Capacity;

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

		public void AcceptResource(Actor self, string resourceType)
		{
			if (!contents.ContainsKey(resourceType))
				contents[resourceType] = 1;
			else
				contents[resourceType]++;

			UpdateCondition(self);
		}

		// Returns true when unloading is complete
		public virtual bool TickUnload(Actor self, Actor proc)
		{
			// Wait until the next bale is ready
			if (--currentUnloadTicks > 0)
				return false;

			if (contents.Keys.Count > 0)
			{
				var acceptResources = proc.Trait<IAcceptResources>();
				foreach (var c in contents)
				{
					var resourceType = c.Key;
					var count = Math.Min(c.Value, Info.BaleUnloadAmount);
					var accepted = acceptResources.AcceptResources(resourceType, count);
					if (accepted == 0)
						continue;

					contents[resourceType] -= accepted;
					if (contents[resourceType] <= 0)
						contents.Remove(resourceType);

					currentUnloadTicks = Info.BaleUnloadDelay;
					UpdateCondition(self);
					return false;
				}
			}

			return contents.Count == 0;
		}

		public bool CanHarvestCell(CPos cell)
		{
			// Resources only exist in the ground layer
			if (cell.Layer != 0)
				return false;

			var resourceType = resourceLayer.GetResource(cell).Type;
			if (resourceType == null)
				return false;

			// Can the harvester collect this kind of resource?
			return Info.Resources.Contains(resourceType);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

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
				LinkProc(null);

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

		protected override void TraitDisabled(Actor self)
		{
			LastLinkedProc = null;
			LinkedProc = null;
			contents.Clear();

			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID => "Harvest";
			public int OrderPriority => 10;
			public bool IsQueued { get; protected set; }
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
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
				var res = self.World.WorldActor.TraitsImplementing<IResourceRenderer>()
					.Select(r => r.GetRenderedResourceType(location))
					.FirstOrDefault(r => r != null && info.Resources.Contains(r));

				if (res == null)
					return false;

				cursor = info.HarvestCursor;
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return true;
			}
		}
	}
}
