#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DockManagerInfo : TraitInfo
	{
		[Desc("Go to DockTypes on deploy.")]
		public readonly BitSet<DockType> ReturnToBaseDockType;

		[Desc("Does this actor automatically take off after resupplying?")]
		public readonly bool TakeOffOnResupply = true;

		[Desc("Should this unit transform on dock?")]
		public readonly bool TransformOnDock = false;

		[Desc("How long (in ticks) to wait until (re-)checking for a nearby available DeliveryBuilding if not yet linked to one.")]
		public readonly int SearchForDockDelay = 125;

		[CursorReference]
		[Desc("Cursor to display when able to dock at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to dock at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		[Desc("Voice.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line of harvest orders.")]
		public readonly Color DockLineColor = Color.Green;
		public override object Create(ActorInitializer init) { return new DockManager(init.Self, this); }
	}

	public class DockManager : IIssueOrder, INotifyCreated, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		public readonly Actor Self;
		public readonly DockManagerInfo Info;
		HashSet<IDockable> dockables;
		HashSet<Transforms> transforms;
		public bool IsAliveAndInWorld => !Self.IsDead && Self.IsInWorld && !Self.Disposed;
		public Color DockLineColor => Info.DockLineColor;

		Dock linkedDock = null;
		bool Disabled => Info.TransformOnDock && (Self.CurrentActivity is Transform || !transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused));
		bool CanIssueDeployOrder => !Disabled && !Info.ReturnToBaseDockType.IsEmpty && DockingPossible(Info.ReturnToBaseDockType);

		public DockManager(Actor self, DockManagerInfo info)
		{
			Self = self;
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			dockables = self.TraitsImplementing<IDockable>().ToHashSet();
			transforms = self.TraitsImplementing<Transforms>().ToHashSet();
		}

		public Dock LinkedDock
		{
			get
			{
				if (linkedDock != null)
				{
					if (!linkedDock.IsAliveAndInWorld)
						UnlinkDock();

					linkedDock?.RefreshOccupancy();
				}

				return linkedDock;
			}
		}

		/// <summary>
		/// Should be called only from Dock, use IAcceptResources.UnlinkHarvester(Harvester) or UnlinkDock() instead
		/// </summary>
		public void UnlinkProc(Dock proc)
		{
			if (linkedDock == proc)
				linkedDock = null;
		}

		public void UnlinkDock()
		{
			LinkedDock?.Unreserve(this);
		}

		public void LinkDock(Dock dock)
		{
			if (dock == null || LinkedDock == dock)
				return;

			UnlinkDock();
			if (dock.IsAliveAndInWorld && dock.Reserve(this))
				linkedDock = dock;
		}

		public Dock ChooseNewDock(Dock ignore, BitSet<DockType> type = default, bool allowedToForceEnter = false)
		{
			var proc = type.IsEmpty ? ClosestDock(ignore, allowedToForceEnter) : ClosestDock(ignore, type, allowedToForceEnter);
			LinkDock(proc);
			return proc;
		}

		public Dock ClosestDock(Dock ignore, bool allowedToForceEnter = false)
		{
			return Self.World.ActorsWithTrait<Dock>()
				.Where(dock => dock.Trait != ignore && CanDockAt(dock.Trait, allowedToForceEnter))
				.Select(d => d.Trait)
				.ClosestDock(Self);
		}

		public Dock ClosestDock(Dock ignore, BitSet<DockType> type, bool allowedToForceEnter = false)
		{
			var dockables = AvailableDockables(type);

			// Find all docks and their occupancy count:
			return Self.World.ActorsWithTrait<Dock>()
				.Where(dock => dock.Trait != ignore && dockables.Any(d => d.CanDockAt(dock.Trait, allowedToForceEnter)))
				.Select(d => d.Trait)
				.ClosestDock(Self);
		}

		public static void DockStarted(IEnumerable<IDockable> dockables, Dock dock)
		{
			dockables.Do(d => d.DockStarted(dock));
			Game.Sound.PlayNotification(dock.Self.World.Map.Rules, dock.Self.Owner, "Speech", dock.Info.StartDockingNotification, dock.Self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(dock.Info.StartDockingTextNotification, dock.Self.Owner);
		}

		public void DockCompleted(Dock dock)
		{
			UnlinkDock();
			Game.Sound.PlayNotification(dock.Self.World.Map.Rules, dock.Self.Owner, "Speech", dock.Info.FinishDockingNotification, dock.Self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(dock.Info.FinishDockingTextNotification, dock.Self.Owner);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<DockInfo>(
					"Dock",
					5,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					DockingPossible,
					target => CanDockAt(target));
			}
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) => CanIssueDeployOrder;

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			if (!CanIssueDeployOrder)
				return null;

			return new Order("ReturnToBase", self, queued);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Dock")
			{
				var target = order.Target;

				// Deliver orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (target.Type != TargetType.Actor)
					return;

				if (!CanDockAt(target.Actor) || Disabled)
					return;

				if (Info.TransformOnDock)
				{
					var currentTransform = self.CurrentActivity as Transform;
					var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
					if (transform == null && currentTransform == null)
						return;

					// Manually manage the inner activity queue
					var activity = currentTransform ?? transform.GetTransformActivity();
					if (!order.Queued)
						activity.NextActivity?.Cancel(self);

					activity.Queue(new IssueOrderAfterTransform(order.OrderString, target, Info.DockLineColor));

					if (currentTransform == null)
						self.QueueActivity(order.Queued, activity);
				}
				else
				{
					self.QueueActivity(order.Queued, new DockActivity(this, null, AvailableDocks(target.Actor).ClosestDock(self)));
				}

				self.ShowTargetLines();
			}
			else if (order.OrderString == "ReturnToBase")
			{
				if (CanIssueDeployOrder)
				{
					self.QueueActivity(order.Queued, new DockActivity(this, Info.ReturnToBaseDockType, null));
					self.ShowTargetLines();
				}
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Dock" && CanDockAt(order.Target.Actor))
				return Info.Voice;
			else if (order.OrderString == "ReturnToBase")
				return Info.Voice;

			return null;
		}

		public bool DockingPossible(Actor target, TargetModifiers modifiers)
		{
			return target.TraitsImplementing<Dock>().Any(dock => dockables.Any(d => d.DockingPossible(dock.MyDockType, modifiers)));
		}

		public bool DockingPossible(BitSet<DockType> type)
		{
			if (Info.TransformOnDock)
				return false;

			return dockables.Any(d => d.DockingPossible(type));
		}

		public bool CanDockAt(Dock target, bool allowedToForceEnter = false)
		{
			if (Disabled)
				return false;

			return dockables.Any(d => d.CanDockAt(target, allowedToForceEnter));
		}

		public bool CanDockAt(Actor target, bool allowedToForceEnter = false)
		{
			if (Disabled)
				return false;

			return target.TraitsImplementing<Dock>().Any(dock => dockables.Any(d => d.CanDockAt(dock, allowedToForceEnter)));
		}

		public IEnumerable<Dock> AvailableDocks(Actor target, bool allowedToForceEnter = false)
		{
			return target.TraitsImplementing<Dock>().Where(dock => dockables.Any(d => d.CanDockAt(dock, allowedToForceEnter)));
		}

		public IEnumerable<IDockable> AvailableDockables(Dock dock, bool allowedToForceEnter = false)
		{
			return dockables.Where(d => d.CanDockAt(dock, allowedToForceEnter));
		}

		public IEnumerable<IDockable> AvailableDockables(BitSet<DockType> type)
		{
			return dockables.Where(d => d.DockingPossible(type));
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Dock" && !Disabled)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}
	}

	public static class DockExts
	{
		public static Dock FindDock(this IDockable dockable, bool allowedToForceEnter = false)
		{
			// Find all docks and their occupancy count:
			var docks = dockable.Self.World.ActorsWithTrait<Dock>()
				.Where(d => dockable.CanDockAt(d.Trait, allowedToForceEnter));

			if (docks.Any())
				return docks.Select(d => d.Trait).ClosestDock(dockable.Self);

			return null;
		}

		public static Dock ClosestDock(this IEnumerable<Dock> docks, Actor self)
		{
			var lookup = docks.ToLookup(d => d.Location);
			var mobile = self.TraitOrDefault<Mobile>();

			if (mobile != null)
			{
				// Start a search from each refinery's delivery location:
				var path = mobile.PathFinder.FindPathToTargetCell(
					self, lookup.Select(r => r.Key), self.Location, BlockedByActor.None,
					location =>
					{
						if (!lookup.Contains(location))
							return 0;

						var dock = lookup[location].First();

						// Prefer docks with less occupancy (multiplier is to offset distance cost):
						return dock.Cost + (dock.Self.Owner == self.Owner ? 0 : dock.Info.AlliedCostModifier);
					});

				if (path.Count > 0)
					return lookup[path.Last()].First();
			}
			else
			{
				return docks
					.OrderBy(dock => (self.Location - dock.Location).LengthSquared + (dock.Self.Owner == self.Owner ? 0 : dock.Info.AlliedCostModifier + dock.Cost))
					.FirstOrDefault();
			}

			return null;
		}
	}
}
