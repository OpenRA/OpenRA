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
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HarvesterInfo : DockClientBaseInfo, Requires<MobileInfo>, Requires<IStoresResourcesInfo>, IRulesetLoaded
	{
		[Desc("Docking type")]
		public readonly BitSet<DockType> Type = new("Unload");

		[Desc("Cell to move to when automatically unblocking DeliveryBuilding.")]
		public readonly CVec UnblockCell = new(0, 4);

		public readonly int BaleLoadDelay = 4;

		[Desc("How fast it can dump its bales.")]
		public readonly int BaleUnloadDelay = 4;

		[Desc("How many bales can it dump at once.")]
		public readonly int BaleUnloadAmount = 1;

		public readonly int HarvestFacings = 0;

		[Desc("Which resources it can harvest.")]
		public readonly string[] Resources = Array.Empty<string>();

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

		[Desc("The pathfinding cost penalty applied for cells directly away from the refinery.")]
		public readonly int ResourceRefineryDirectionPenalty = 200;

		[Desc("Does the unit queue harvesting runs instead of individual harvest actions?")]
		public readonly bool QueueFullLoad = false;

		[GrantedConditionReference]
		[Desc("Condition to grant while empty.")]
		public readonly string EmptyCondition = null;

		[VoiceReference]
		public readonly string HarvestVoice = "Action";

		[Desc("Color to use for the target line of harvest orders.")]
		public readonly Color HarvestLineColor = Color.Crimson;

		[CursorReference]
		[Desc("Cursor to display when ordering to harvest resources.")]
		public readonly string HarvestCursor = "harvest";

		public override object Create(ActorInitializer init) { return new Harvester(init.Self, this); }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (Resources.Length == 0)
				throw new YamlException($"Harvester.{nameof(Resources)} is empty.");

			var resourceTypes = Resources.Except(info.TraitInfos<IStoresResourcesInfo>().SelectMany(sr => sr.ResourceTypes)).ToArray();
			if (resourceTypes.Length != 0)
				throw new YamlException($"Invalid Harvester.{nameof(Resources)} types: {string.Join(',', resourceTypes)}.");
		}
	}

	public class Harvester : DockClientBase<HarvesterInfo>, IIssueOrder, IResolveOrder, IOrderVoice,
		ISpeedModifier, ISync, INotifyCreated
	{
		readonly Mobile mobile;
		readonly IResourceLayer resourceLayer;
		readonly ResourceClaimLayer claimLayer;
		readonly IStoresResources[] storesResources;
		int conditionToken = Actor.InvalidConditionToken;

		public override BitSet<DockType> GetDockType => Info.Type;

		[Sync]
		int currentUnloadTicks;

		public Harvester(Actor self, HarvesterInfo info)
			: base(self, info)
		{
			mobile = self.Trait<Mobile>();
			storesResources = self.TraitsImplementing<IStoresResources>().Where(sr => info.Resources.Any(r => sr.HasType(r))).ToArray();
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

		public bool IsFull => storesResources.All(sr => sr.ContentsSum >= sr.Capacity);
		public bool IsEmpty => storesResources.All(sr => sr.ContentsSum == 0);
		public int Fullness => storesResources.Sum(sr => sr.ContentsSum * 100 / sr.Capacity) / storesResources.Length;

		protected override bool CanDock()
		{
			return !IsEmpty;
		}

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

		public void AddResource(Actor self, string resourceType)
		{
			foreach (var sr in storesResources)
				if (sr.AddResource(resourceType, 1) == 0)
					break;

			UpdateCondition(self);
		}

		IAcceptResources acceptResources;
		public override void OnDockStarted(Actor self, Actor hostActor, IDockHost host)
		{
			if (IsDockingPossible(host.GetDockType))
				acceptResources = hostActor.TraitOrDefault<IAcceptResources>();
		}

		public override bool OnDockTick(Actor self, Actor hostActor, IDockHost host)
		{
			if (acceptResources == null || IsTraitDisabled)
				return true;

			// Wait until the next bale is ready
			if (--currentUnloadTicks > 0)
				return false;

			foreach (var sr in storesResources)
			{
				foreach (var c in sr.Contents)
				{
					var count = Math.Min(c.Value, Info.BaleUnloadAmount);
					var accepted = acceptResources.AcceptResources(hostActor, c.Key, count);
					if (accepted == 0)
						continue;

					sr.RemoveResource(c.Key, accepted);
					currentUnloadTicks = Info.BaleUnloadDelay;
					UpdateCondition(self);
					return false;
				}
			}

			return IsEmpty;
		}

		public override void OnDockCompleted(Actor self, Actor hostActor, IDockHost dock)
		{
			acceptResources = null;

			// After having docked at a refinery make sure we are running FindAndDeliverResources activity.
			if (GetDockType.Overlaps(dock.GetDockType))
			{
				var currentActivity = self.CurrentActivity;
				if (currentActivity == null || (currentActivity is not FindAndDeliverResources && currentActivity.NextActivity == null))
					self.QueueActivity(true, new FindAndDeliverResources(self));
			}
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

				yield return new HarvestOrderTargeter();
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Harvest")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
				return Info.HarvestVoice;

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
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
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			return 100 - (100 - Info.FullyLoadedSpeed) * Fullness / 100;
		}

		protected override void TraitDisabled(Actor self)
		{
			base.TraitDisabled(self);

			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		sealed class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID => "Harvest";
			public int OrderPriority => 10;
			public bool IsQueued { get; private set; }
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
