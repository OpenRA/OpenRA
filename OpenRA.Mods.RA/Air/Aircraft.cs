#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class AircraftInfo : ITraitInfo, IFacingInfo, IOccupySpaceInfo, UsesInit<LocationInit>, UsesInit<FacingInit>
	{
		public readonly WRange CruiseAltitude = new WRange(1280);

		[ActorReference]
		public readonly string[] RepairBuildings = { "fix" };
		[ActorReference]
		public readonly string[] RearmBuildings = { };
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;
		public readonly string[] LandableTerrainTypes = { };

		public virtual object Create(ActorInitializer init) { return new Aircraft(init, this); }
		public int GetInitialFacing() { return InitialFacing; }
	}

	public class Aircraft : IFacing, IPositionable, ISync, INotifyKilled, IIssueOrder, IOrderVoice, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly Pair<CPos, SubCell>[] NoCells = new Pair<CPos, SubCell>[] { };

		readonly AircraftInfo info;
		readonly Actor self;

		[Sync] public int Facing { get; set; }
		[Sync] public WPos CenterPosition { get; private set; }
		public CPos TopLeft { get { return CenterPosition.ToCPos(); } }
		public IDisposable Reservation;
		public int ROT { get { return info.ROT; } }

		public Aircraft(ActorInitializer init, AircraftInfo info)
		{
			this.info = info;
			this.self = init.self;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());

			if (init.Contains<CenterPositionInit>())
				SetPosition(self, init.Get<CenterPositionInit, WPos>());

			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;
		}

		public Actor GetActorBelow()
		{
			if (self.CenterPosition.Z != 0)
				return null;	// not on the ground.

			return self.World.ActorMap.GetUnitsAt(self.Location)
				.FirstOrDefault(a => a.HasTrait<Reservable>());
		}

		protected void ReserveSpawnBuilding()
		{
			/* HACK: not spawning in the air, so try to assoc. with our afld. */
			var afld = GetActorBelow();
			if (afld == null)
				return;

			var res = afld.Trait<Reservable>();

			if (res != null)
			{
				UnReserve();
				Reservation = res.Reserve(afld, self, this);
			}
		}

		public void UnReserve()
		{
			if (Reservation != null)
			{
				Reservation.Dispose();
				Reservation = null;
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			UnReserve();
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (self.IsInWorld)
			{
				self.World.ScreenMap.Update(self);
				self.World.ActorMap.UpdatePosition(self, this);
			}
		}

		// Changes position, but not altitude
		public void SetPosition(Actor self, CPos cell) { SetPosition(self, cell.CenterPosition + new WVec(0, 0, CenterPosition.Z)); }
		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);
			self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);
			self.World.ScreenMap.Remove(self);
		}

		public bool AircraftCanEnter(Actor a)
		{
			if (self.AppearsHostileTo(a))
				return false;

			return info.RearmBuildings.Contains(a.Info.Name)
				|| info.RepairBuildings.Contains(a.Info.Name);
		}

		public bool CanEnterCell(CPos location) { return true; }
		public bool CanEnterCell(CPos cell, Actor ignoreActor, bool checkTransientActors) { return true; }

		public int MovementSpeed
		{
			get
			{
				decimal ret = info.Speed;
				foreach (var t in self.TraitsImplementing<ISpeedModifier>())
					ret *= t.GetSpeedModifier();
				return (int)ret;
			}
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return NoCells; }

		public WVec FlyStep(int facing)
		{
			var speed = MovementSpeed;
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing));
			return speed * dir / 1024;
		}

		public bool CanLand(CPos cell)
		{
			if (!self.World.Map.IsInMap(cell))
				return false;

			if (self.World.ActorMap.AnyUnitsAt(cell))
				return false;

			var type = self.World.Map.GetTerrainInfo(cell).Type;
			return info.LandableTerrainTypes.Contains(type);
		}

		public virtual IEnumerable<Activity> GetResupplyActivities(Actor a)
		{
			var name = a.Info.Name;
			if (info.RearmBuildings.Contains(name))
				yield return new Rearm(self);
			if (info.RepairBuildings.Contains(name))
				yield return new Repair(a);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<Building>("Enter", 5,
					target => AircraftCanEnter(target), target => !Reservable.IsReserved(target));

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Enter")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if (order.OrderID == "Move")
				return new Order(order.OrderID, self, queued) { TargetLocation = target.CenterPosition.ToCPos() };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
			case "Move":
			case "Enter":
			case "ReturnToBase":
			case "Stop":
				return "Move";
			default: return null;
			}
		}
	}

	public class ResupplyAircraft : Activity
	{
		public override Activity Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			var host = aircraft.GetActorBelow();

			if (host == null)
				return NextActivity;

			return Util.SequenceActivities(
				aircraft.GetResupplyActivities(host).Append(NextActivity).ToArray());
		}
	}

	class AircraftMoveOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "Move"; } }
		public int OrderPriority { get { return 4; } }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
		{
			if (target.Type != TargetType.Terrain)
				return false;

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			cursor = self.World.Map.IsInMap(target.CenterPosition.ToCPos()) ? "move" : "move-blocked";
			return true;
		}

		public bool IsQueued { get; protected set; }
	}
}
