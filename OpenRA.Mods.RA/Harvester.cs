#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class HarvesterInfo : ITraitInfo
	{
		public readonly int Capacity = 28;
		public readonly int UnloadTicksPerBale = 4;
		public readonly int PipCount = 7;
		public readonly string[] Resources = { };
		public readonly decimal FullyLoadedSpeed = .85m;

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

		public Harvester(Actor self, HarvesterInfo info)
		{
			Info = info;
			self.QueueActivity(new CallFunc(() => ChooseNewProc(self, null)));
		}

		public void SetProcLines(Actor proc)
		{
			if (proc == null) return;

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

		Actor ClosestProc(Actor self, Actor ignore)
		{
			// Find all refineries and their occupancy count:
			var refs = (
				from r in self.World.ActorsWithTrait<IAcceptOre>()
				where r.Actor != ignore && r.Actor.Owner == self.Owner
				let linkedHarvs = self.World.ActorsWithTrait<Harvester>().Where(a => a.Trait.LinkedProc == r.Actor).Count()
				select new { Location = r.Actor.Location + r.Trait.DeliverOffset, Actor = r.Actor, Occupancy = linkedHarvs }
			).ToDictionary(r => r.Location);

			// Start a search from each refinery's delivery location:
			var mi = self.Info.Traits.Get<MobileInfo>();
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(
				PathSearch.FromPoints(self.World, mi, self.Owner, refs.Values.Select(r => r.Location), self.Location, false)
					.WithCustomCost((loc) =>
					{
						if (!refs.ContainsKey(loc)) return 0;

						int occupancy = refs[loc].Occupancy;
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
			if (!contents.ContainsKey(type.info)) contents[type.info] = 1;
			else contents[type.info]++;
		}

		public void UnblockRefinery(Actor self)
		{
			// Check that we're not in a critical location and being useless (refinery drop-off):
			var lastproc = LastLinkedProc ?? LinkedProc;
			if (lastproc != null)
			{
				var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptOre>().DeliverOffset;
				if (self.Location == deliveryLoc)
				{
					// Get out of the way:
					var mobile = self.Trait<Mobile>();
					var harv = self.Trait<Harvester>();

					var moveTo = harv.LastHarvestedCell ?? (deliveryLoc + new CVec(0, 4));
					self.QueueActivity(mobile.MoveTo(moveTo, 1));
					self.SetTargetLine(Target.FromCell(moveTo), Color.Gray, false);

					self.World.WorldActor.Trait<ResourceClaimLayer>().ClaimResource(self, moveTo);
					self.QueueActivity(new FindResources());
					return;
				}
			}
		}

		public void OnNotifyBlockingMove(Actor self, Actor blocking, CPos cell)
		{
			// I'm blocking someone else from moving to my location:
			Activity act = self.GetCurrentActivity();
			// If I'm just waiting around, then get out of the way:
			if (act.GetType() == typeof(Wait))
			{
				self.CancelActivity();
				var mobile = self.Trait<Mobile>();

				var moveTo = mobile.NearestMoveableCell(cell, 2, 5);
				self.QueueActivity(mobile.MoveTo(moveTo, 0));
				self.SetTargetLine(Target.FromCell(moveTo), Color.Gray, false);

				// Find more resources but not at this location:
				self.QueueActivity(new FindResources(cell));
			}
		}

		public void TickIdle(Actor self)
		{
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
			if (!proc.IsInWorld)
				return false;	// fail to deliver if there is no proc.

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
				yield return new EnterOrderTargeter<IAcceptOre>("Deliver", 5, false, true, _ => true, proc => !IsEmpty && proc.Trait<IAcceptOre>().AllowDocking);
				yield return new HarvestOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Deliver")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if (order.OrderID == "Harvest")
				return new Order(order.OrderID, self, queued) { TargetLocation = target.CenterLocation.ToCPos() };

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

				var mobile = self.Trait<Mobile>();
				self.CancelActivity();
				if (order.TargetLocation != CPos.Zero)
				{
					var loc = order.TargetLocation;
					var territory = self.World.WorldActor.Trait<ResourceClaimLayer>();

					// Find the nearest claimable cell to the order location (useful for group-select harvest):
					loc = mobile.NearestCell(loc, p => mobile.CanEnterCell(p) && territory.ClaimResource(self, p), 1, 6);

					self.QueueActivity(mobile.MoveTo(loc, 0));
					self.SetTargetLine(Target.FromCell(loc), Color.Red);

					LastOrderLocation = loc;
				}
				self.QueueActivity(new FindResources());
			}
			else if (order.OrderString == "Deliver")
			{
				// NOTE: An explicit deliver order forces the harvester to always deliver to this refinery.
				var iao = order.TargetActor.TraitOrDefault<IAcceptOre>();
				if (iao == null || !iao.AllowDocking)
					return;

				if (order.TargetActor != OwnerLinkedProc)
					LinkProc(self, OwnerLinkedProc = order.TargetActor);

				if (IsEmpty)
					return;

				self.SetTargetLine(Target.FromOrder(order), Color.Green);

				self.CancelActivity();
				self.QueueActivity(new DeliverResources());
			}
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
			int numPips = Info.PipCount;

			for (int i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		public bool ShouldExplode(Actor self) { return !IsEmpty; }

		public decimal GetSpeedModifier()
		{
			return 1m - (1m - Info.FullyLoadedSpeed) * contents.Values.Sum() / Info.Capacity;
		}

		class HarvestOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "Harvest"; } }
			public int OrderPriority { get { return 10; } }
			public bool IsQueued { get; protected set; }

			public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
			{
				return false;
			}

			public bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueued, ref string cursor)
			{
				// Don't leak info about resources under the shroud
				if (!self.World.LocalShroud.IsExplored(location)) return false;

				var res = self.World.WorldActor.Trait<ResourceLayer>().GetResource(location);
				var info = self.Info.Traits.Get<HarvesterInfo>();

				if (res == null) return false;
				if (!info.Resources.Contains(res.info.Name)) return false;
				cursor = "harvest";
				IsQueued = forceQueued;

				return true;
			}
		}
	}
}
