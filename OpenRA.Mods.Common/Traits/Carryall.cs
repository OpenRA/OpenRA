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
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries.")]
	public class CarryallInfo : ITraitInfo, Requires<BodyOrientationInfo>, Requires<AircraftInfo>
	{
		public readonly int LoadingDelay = 0;
		public readonly int UnLoadingDelay = 0;

		[VoiceReference]
		public readonly string Voice = "Move";

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

		readonly Actor self;
		readonly CarryallInfo info;
		readonly AircraftInfo aircraftInfo;
		readonly IMove move;
		readonly IFacing facing;

		// The actor we are currently carrying.
		[Sync] public Actor Carryable { get; private set; }
		public CarryallState State { get; private set; }

		IActorPreview[] carryablePreview = null;
		WDist carryableHeight;

		public Carryall(Actor self, CarryallInfo info)
		{
			this.self = self;
			this.info = info;

			Carryable = null;
			State = CarryallState.Idle;

			aircraftInfo = self.Info.TraitInfoOrDefault<AircraftInfo>();
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

			UnreserveCarryable();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (State == CarryallState.Carrying)
			{
				if (Carryable.IsInWorld && !Carryable.IsDead)
					Carryable.Kill(e.Attacker);
				Carryable = null;
			}

			UnreserveCarryable();
		}

		public virtual bool RequestTransportNotify(Actor carryable)
		{
			return false;
		}

		public virtual bool AttachCarryable(Actor carryable)
		{
			if (State == CarryallState.Carrying)
				return false;

			Carryable = carryable;
			State = CarryallState.Carrying;
			carryableHeight = carryable.Trait<Carryable>().CarryableHeight;
			return true;
		}

		public virtual void DetachCarryable()
		{
			UnreserveCarryable();

			carryablePreview = null;
			carryableHeight = WDist.Zero;
		}

		public virtual bool ReserveCarryable(Actor carryable)
		{
			if (State == CarryallState.Reserved)
				UnreserveCarryable();

			if (State != CarryallState.Idle || !carryable.Trait<Carryable>().Reserve(self))
				return false;

			Carryable = carryable;
			State = CarryallState.Reserved;
			return true;
		}

		public virtual void UnreserveCarryable()
		{
			if (Carryable != null && Carryable.IsInWorld && !Carryable.IsDead)
				Carryable.Trait<Carryable>().UnReserve();
			Carryable = null;
			State = CarryallState.Idle;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (State == CarryallState.Carrying)
			{
				if (carryablePreview == null)
				{
					var iba = Carryable.TraitsImplementing<IBodyAnimation>().FirstOrDefault();
					var frame = iba != null ? iba.GetBodyAnimationFrame() : 0;

					var td = new TypeDictionary()
					{
						new FactionInit(Carryable.Owner.Faction.InternalName),
						new OwnerInit(Carryable.Owner),
						new DynamicFacingInit(() => facing.Facing),
						new BodyAnimationFrameInit(frame)
					};

					var init = new ActorPreviewInitializer(Carryable.Info, wr, td);
					carryablePreview = Carryable.Info.TraitInfos<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();
				}

				var previewRenderables = carryablePreview
					.SelectMany(p => p.Render(wr, self.CenterPosition + new WVec(0, 0, -carryableHeight.Length)))
					.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey);

				foreach (var r in previewRenderables)
					yield return r;
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (State != CarryallState.Carrying)
					yield return new CarryallPickupOrderTargeter();
				else
					yield return new CarryallDeliverUnitTargeter(aircraftInfo, carryableHeight);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
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

		public void ResolveOrder(Actor self, Order order)
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
					self.QueueActivity(order.Queued, new DeliverUnit(self, info.UnLoadingDelay, targetLocation));
				}
				else if (order.OrderString == "Unload")
				{
					var targetLocation = move.NearestMoveableCell(self.Location);
					self.SetTargetLine(Target.FromCell(self.World, targetLocation), Color.Yellow);
					self.QueueActivity(order.Queued, new DeliverUnit(self, info.UnLoadingDelay, targetLocation));
				}
			}
			else
			{
				if (order.OrderString == "PickupUnit")
				{
					var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
					if (target.Type != TargetType.Actor)
						return;

					if (!ReserveCarryable(target.Actor))
						return;

					if (!order.Queued)
						self.CancelActivity();

					self.SetTargetLine(target, Color.Yellow);
					self.QueueActivity(order.Queued, new PickupUnit(self, target.Actor, info.LoadingDelay));
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
					return info.Voice;
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
			readonly WDist carryableHeight;
			readonly AircraftInfo aircraftInfo;

			public CarryallDeliverUnitTargeter(AircraftInfo aircraftInfo, WDist carryableHeight)
				: base(aircraftInfo)
			{
				OrderID = "DeliverUnit";
				OrderPriority = 6;
				this.carryableHeight = carryableHeight;
				this.aircraftInfo = aircraftInfo;
			}

			public override bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				var type = target.Type;
				if (type == TargetType.Actor && self == target.Actor)
				{
					var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
					if ((altitude - carryableHeight).Length < aircraftInfo.MinAirborneAltitude)
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
