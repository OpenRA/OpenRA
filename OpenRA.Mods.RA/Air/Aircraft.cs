#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{

	public class AircraftInfo : ITraitInfo, IFacingInfo, UsesInit<AltitudeInit>, UsesInit<LocationInit>, UsesInit<FacingInit>
	{
		public readonly int CruiseAltitude = 30;
		[ActorReference]
		public readonly string[] RepairBuildings = { "fix" };
		[ActorReference]
		public readonly string[] RearmBuildings = { "hpad", "afld" };
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;
		public readonly string[] LandableTerrainTypes = { };

		public virtual object Create( ActorInitializer init ) { return new Aircraft( init , this ); }
		public int GetInitialFacing() { return InitialFacing; }
	}

	public class Aircraft : IMove, IFacing, IOccupySpace, ISync, INotifyKilled, IIssueOrder, IOrderVoice
	{
		public IDisposable reservation;

		public void UnReserve()
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			UnReserve();
		}


		protected readonly Actor self;
		[Sync] public int Facing { get; set; }
		[Sync] public int Altitude { get; set; }
		[Sync] public PSubPos SubPxPosition;
		public PPos PxPosition { get { return SubPxPosition.ToPPos(); } }
		public CPos TopLeft { get { return PxPosition.ToCPos(); } }

		readonly AircraftInfo Info;

		public Aircraft(ActorInitializer init , AircraftInfo info)
		{
			this.self = init.self;
			if( init.Contains<LocationInit>() )
				this.SubPxPosition = Util.CenterOfCell( init.Get<LocationInit, CPos>() ).ToPSubPos();

			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit,int>() : 0;
			Info = info;
		}

		public Actor GetActorBelow()
		{
			if (self.Trait<IMove>().Altitude != 0)
				return null;	// not on the ground.

			return self.World.FindUnits(self.CenterLocation, self.CenterLocation)
				.FirstOrDefault( a => a.HasTrait<Reservable>() );
		}

		protected void ReserveSpawnBuilding()
		{
			/* HACK: not spawning in the air, so try to assoc. with our afld. */
			var afld = GetActorBelow();
			if (afld == null)
				return;

			var res = afld.Trait<Reservable>();
			if (res != null)
				reservation = res.Reserve(afld, self, this);
		}

		public int ROT { get { return Info.ROT; } }

		public void SetPosition(Actor self, CPos cell)
		{
			SetPxPosition(self, Util.CenterOfCell(cell));
		}

		public void SetPxPosition(Actor self, PPos px)
		{
			SubPxPosition = px.ToPSubPos();
		}

		public void AdjustPxPosition(Actor self, PPos px) { SetPxPosition(self, px); }

		public bool AircraftCanEnter(Actor a)
		{
			if (self.AppearsHostileTo(a)) return false;
			return Info.RearmBuildings.Contains(a.Info.Name)
				|| Info.RepairBuildings.Contains(a.Info.Name);
		}

		public bool CanEnterCell(CPos location) { return true; }

		public int MovementSpeed
		{
			get
			{
				decimal ret = Info.Speed;
				foreach (var t in self.TraitsImplementing<ISpeedModifier>())
					ret *= t.GetSpeedModifier();
				return (int)ret;
			}
		}

		Pair<CPos, SubCell>[] noCells = new Pair<CPos, SubCell>[] { };
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return noCells; }

		public void TickMove(int speed, int facing)
		{
			var rawspeed = speed * 7 / (32 * PSubPos.PerPx);
			SubPxPosition += rawspeed * -Util.SubPxVector[facing];
		}

		public bool CanLand(CPos cell)
		{
			if (!self.World.Map.IsInMap(cell))
				return false;

			if (self.World.ActorMap.AnyUnitsAt(cell))
				return false;

			var type = self.World.GetTerrainType(cell);
			return Info.LandableTerrainTypes.Contains(type);
		}

		public IEnumerable<Activity> GetResupplyActivities(Actor a)
		{
			var name = a.Info.Name;
			if (Info.RearmBuildings.Contains(name))
				yield return new Rearm(self);
			if (Info.RepairBuildings.Contains(name))
				yield return new Repair(a);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>("Enter", 5, false, true,
					target => AircraftCanEnter(target), target => !Reservable.IsReserved(target));

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Enter")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if (order.OrderID == "Move")
				return new Order(order.OrderID, self, queued) { TargetLocation = target.CenterLocation.ToCPos() };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
			case "Move":
			case "Enter":
			case "ReturnToBase":
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

		public bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}

		public bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, TargetModifiers modifiers, ref string cursor)
		{
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			cursor = self.World.Map.IsInMap(location) ? "move" : "move-blocked";
			return true;
		}
		public bool IsQueued { get; protected set; }
	}
}
