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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Transports actors with the `Carryable` trait.")]
	public class CarryallInfo : ITraitInfo, Requires<BodyOrientationInfo>, Requires<AircraftInfo>
	{
		[Desc("Delay on the ground while attaching an actor to the carryall.")]
		public readonly int LoadingDelay = 0;

		[Desc("Delay on the ground while detacting an actor to the carryall.")]
		public readonly int UnloadingDelay = 0;

		[Desc("Carryable attachment point relative to body.")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Radius around the target drop location that are considered if the target tile is blocked.")]
		public readonly WDist DropRange = WDist.FromCells(5);

		[VoiceReference]
		public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Carryall(init.Self, this); }
	}

	public class Carryall : INotifyKilled, ISync, IRender, INotifyActorDisposing, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public enum CarryallState
		{
			Idle,
			Reserved,
			Carrying
		}

		public readonly CarryallInfo Info;
		readonly AircraftInfo aircraftInfo;
		readonly BodyOrientation body;
		readonly IMove move;
		readonly IFacing facing;

		// The actor we are currently carrying.
		[Sync] public Actor Carryable { get; private set; }
		public CarryallState State { get; private set; }

		IActorPreview[] carryablePreview = null;

		/// <summary>Offset between the carryall's and the carried actor's CenterPositions</summary>
		public WVec CarryableOffset { get; private set; }

		public Carryall(Actor self, CarryallInfo info)
		{
			this.Info = info;

			Carryable = null;
			State = CarryallState.Idle;

			aircraftInfo = self.Info.TraitInfoOrDefault<AircraftInfo>();
			body = self.Trait<BodyOrientation>();
			move = self.Trait<IMove>();
			facing = self.Trait<IFacing>();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (State == CarryallState.Carrying)
			{
				Carryable.Dispose();
				Carryable = null;
			}

			UnreserveCarryable(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (State == CarryallState.Carrying)
			{
				if (Carryable.IsInWorld && !Carryable.IsDead)
					Carryable.Kill(e.Attacker);
				Carryable = null;
			}

			UnreserveCarryable(self);
		}

		public virtual bool RequestTransportNotify(Actor self, Actor carryable, CPos destination)
		{
			return false;
		}

		public virtual WVec OffsetForCarryable(Actor self, Actor carryable)
		{
			return Info.LocalOffset - carryable.Info.TraitInfo<CarryableInfo>().LocalOffset;
		}

		public virtual bool AttachCarryable(Actor self, Actor carryable)
		{
			if (State == CarryallState.Carrying)
				return false;

			Carryable = carryable;
			State = CarryallState.Carrying;

			CarryableOffset = OffsetForCarryable(self, carryable);
			return true;
		}

		public virtual void DetachCarryable(Actor self)
		{
			UnreserveCarryable(self);

			carryablePreview = null;
			CarryableOffset = WVec.Zero;
		}

		public virtual bool ReserveCarryable(Actor self, Actor carryable)
		{
			if (State == CarryallState.Reserved)
				UnreserveCarryable(self);

			if (State != CarryallState.Idle || !carryable.Trait<Carryable>().Reserve(carryable, self))
				return false;

			Carryable = carryable;
			State = CarryallState.Reserved;
			return true;
		}

		public virtual void UnreserveCarryable(Actor self)
		{
			if (Carryable != null && Carryable.IsInWorld && !Carryable.IsDead)
				Carryable.Trait<Carryable>().UnReserve(Carryable);

			Carryable = null;
			State = CarryallState.Idle;
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (State == CarryallState.Carrying)
			{
				if (carryablePreview == null)
				{
					var carryableInits = new TypeDictionary()
					{
						new OwnerInit(Carryable.Owner),
						new DynamicFacingInit(() => facing.Facing),
					};

					foreach (var api in Carryable.TraitsImplementing<IActorPreviewInitModifier>())
						api.ModifyActorPreviewInit(Carryable, carryableInits);

					var init = new ActorPreviewInitializer(Carryable.Info, wr, carryableInits);
					carryablePreview = Carryable.Info.TraitInfos<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();
				}

				var offset = body.LocalToWorld(CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation)));
				var previewRenderables = carryablePreview
					.SelectMany(p => p.Render(wr, self.CenterPosition + offset))
					.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey);

				foreach (var r in previewRenderables)
					yield return r;
			}
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (State != CarryallState.Carrying)
					yield return new CarryallPickupOrderTargeter();
				else
					yield return new CarryallDeliverUnitTargeter(aircraftInfo, CarryableOffset);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PickupUnit")
			{
				if (target.Type == TargetType.FrozenActor)
					return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
			}
			else if (order.OrderID == "DeliverUnit")
			{
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
			}
			else if (order.OrderID == "Unload")
			{
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
			}

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (State == CarryallState.Carrying)
			{
				if (order.OrderString == "DeliverUnit")
				{
					var cell = self.World.Map.Clamp(order.TargetLocation);

					if (!aircraftInfo.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
						return;

					var targetLocation = move.NearestMoveableCell(order.TargetLocation);
					self.SetTargetLine(Target.FromCell(self.World, targetLocation), Color.Yellow);
					self.QueueActivity(order.Queued, new DeliverUnit(self, targetLocation));
				}
				else if (order.OrderString == "Unload")
				{
					var targetLocation = move.NearestMoveableCell(self.Location);
					self.SetTargetLine(Target.FromCell(self.World, targetLocation), Color.Yellow);
					self.QueueActivity(order.Queued, new DeliverUnit(self, targetLocation));
				}
			}
			else
			{
				if (order.OrderString == "PickupUnit")
				{
					var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
					if (target.Type != TargetType.Actor)
						return;

					if (!ReserveCarryable(self, target.Actor))
						return;

					if (!order.Queued)
						self.CancelActivity();

					self.SetTargetLine(target, Color.Yellow);
					self.QueueActivity(order.Queued, new PickupUnit(self, target.Actor, Info.LoadingDelay));
				}
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "DeliverUnit":
				case "Unload":
				case "PickupUnit":
					return Info.Voice;
				default:
					return null;
			}
		}

		class CarryallPickupOrderTargeter : UnitOrderTargeter
		{
			public CarryallPickupOrderTargeter()
				: base("PickupUnit", 5, "ability", false, true)
			{
			}

			static bool CanTarget(Actor self, Actor target)
			{
				if (!target.AppearsFriendlyTo(self))
					return false;
				var carryable = target.TraitOrDefault<Carryable>();
				if (carryable == null)
					return false;
				if (carryable.Reserved && carryable.Carrier != self)
					return false;
				return true;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return CanTarget(self, target);
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return CanTarget(self, target.Actor);
			}
		}

		class CarryallDeliverUnitTargeter : AircraftMoveOrderTargeter
		{
			readonly AircraftInfo aircraftInfo;
			readonly WVec carryableOffset;

			public CarryallDeliverUnitTargeter(AircraftInfo aircraftInfo, WVec carryableOffset)
				: base(aircraftInfo)
			{
				OrderID = "DeliverUnit";
				OrderPriority = 6;
				this.carryableOffset = carryableOffset;
				this.aircraftInfo = aircraftInfo;
			}

			public override bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var type = target.Type;
				if (type == TargetType.Actor && self == target.Actor)
				{
					var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
					if (altitude.Length - carryableOffset.Z < aircraftInfo.MinAirborneAltitude)
					{
						cursor = "deploy";
						OrderID = "Unload";
						return true;
					}
				}
				else if ((type == TargetType.Actor && target.Actor.Info.HasTraitInfo<BuildingInfo>())
					|| (target.Type == TargetType.FrozenActor && target.FrozenActor.Info.HasTraitInfo<BuildingInfo>()))
				{
					cursor = "move-blocked";
					return true;
				}

				return base.CanTarget(self, target, othersAtTarget, ref modifiers, ref cursor);
			}
		}
	}
}
