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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TurretedInfo : PausableConditionalTraitInfo, Requires<BodyOrientationInfo>, IActorPreviewInitInfo, IEditorActorOptions
	{
		public readonly string Turret = "primary";

		[Desc("Speed at which the turret turns.")]
		public readonly WAngle TurnSpeed = new WAngle(512);

		public readonly WAngle InitialFacing = WAngle.Zero;

		[Desc("Number of ticks before turret is realigned. (-1 turns off realignment)")]
		public readonly int RealignDelay = 40;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Display order for the turret facing slider in the map editor")]
		public readonly int EditorTurretFacingDisplayOrder = 4;

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new TurretFacingInit(this, InitialFacing);
		}

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorSlider("Turret", EditorTurretFacingDisplayOrder, 0, 1023, 8,
				actor =>
				{
					var init = actor.GetInitOrDefault<TurretFacingInit>(this);
					if (init != null)
						return init.Value.Angle;

					return InitialFacing.Angle;
				},
				(actor, value) => actor.ReplaceInit(new TurretFacingInit(this, new WAngle((int)value)), this));
		}

		public override object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : PausableConditionalTrait<TurretedInfo>, ITick, IDeathActorInitModifier, IActorPreviewInitModifier
	{
		AttackTurreted attack;
		IFacing facing;
		BodyOrientation body;

		[Sync]
		public int QuantizedFacings = 0;

		[Sync]
		public int TurretFacing = 0;

		public int? DesiredFacing;
		int realignTick = 0;

		// For subclasses that want to move the turret relative to the body
		protected WVec localOffset = WVec.Zero;

		public WVec Offset { get { return Info.Offset + localOffset; } }
		public string Name { get { return Info.Turret; } }

		public static Func<WAngle> TurretFacingFromInit(IActorInitializer init, TurretedInfo info)
		{
			return TurretFacingFromInit(init, info, info.InitialFacing);
		}

		public static Func<WAngle> TurretFacingFromInit(IActorInitializer init, TraitInfo info, WAngle defaultFacing)
		{
			// (Dynamic)TurretFacingInit is specified relative to the actor body.
			// We need to add the body facing to return an absolute world angle.
			Func<WAngle> bodyFacing = null;
			var facingInit = init.GetOrDefault<FacingInit>();
			if (facingInit != null)
			{
				var facing = facingInit.Value;
				bodyFacing = () => facing;
			}

			var turretFacingInit = init.GetOrDefault<TurretFacingInit>(info);
			if (turretFacingInit != null)
			{
				var facing = turretFacingInit.Value;
				return bodyFacing != null ? (Func<WAngle>)(() => bodyFacing() + facing) : () => facing;
			}

			var dynamicFacingInit = init.GetOrDefault<DynamicFacingInit>();
			if (dynamicFacingInit != null)
				return bodyFacing != null ? () => bodyFacing() + dynamicFacingInit.Value() : dynamicFacingInit.Value;

			return bodyFacing ?? (Func<WAngle>)(() => defaultFacing);
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
			: base(info)
		{
			TurretFacing = TurretFacingFromInit(init, Info)().Facing;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			attack = self.TraitsImplementing<AttackTurreted>().SingleOrDefault(at => ((AttackTurretedInfo)at.Info).Turrets.Contains(Info.Turret));
			facing = self.TraitOrDefault<IFacing>();
			body = self.Trait<BodyOrientation>();
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			// NOTE: FaceTarget is called in AttackTurreted.CanAttack if the turret has a target.
			if (attack != null)
			{
				// Only realign while not attacking anything
				if (attack.IsAiming)
					return;

				if (realignTick < Info.RealignDelay)
					realignTick++;
				else if (Info.RealignDelay > -1)
					DesiredFacing = null;

				MoveTurret();
			}
			else
			{
				realignTick = 0;
				MoveTurret();
			}
		}

		void MoveTurret()
		{
			var df = DesiredFacing ?? (facing != null ? facing.Facing.Facing : TurretFacing);
			TurretFacing = Util.TickFacing(TurretFacing, df, Info.TurnSpeed.Facing);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			if (IsTraitDisabled || IsTraitPaused || attack == null || attack.IsTraitDisabled || attack.IsTraitPaused)
				return false;

			var pos = self.CenterPosition;
			var targetPos = attack.GetTargetPosition(pos, target);
			var delta = targetPos - pos;
			DesiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : TurretFacing;
			MoveTurret();
			return HasAchievedDesiredFacing;
		}

		public virtual bool HasAchievedDesiredFacing
		{
			get { return DesiredFacing == null || TurretFacing == DesiredFacing.Value; }
		}

		// Turret offset in world-space
		public WVec Position(Actor self)
		{
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			return body.LocalToWorld(Offset.Rotate(bodyOrientation));
		}

		// Orientation in world-space
		public WRot WorldOrientation(Actor self)
		{
			// Hack: turretFacing is relative to the world, so subtract the body yaw
			var world = WRot.FromYaw(WAngle.FromFacing(TurretFacing));

			if (QuantizedFacings == 0)
				return world;

			// Quantize orientation to match a rendered sprite
			// Implies no pitch or yaw
			return WRot.FromYaw(body.QuantizeFacing(world.Yaw, QuantizedFacings));
		}

		public void ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			var turretFacing = WAngle.FromFacing(TurretFacing);
			if (facing != null)
				turretFacing -= facing.Facing;

			init.Add(new TurretFacingInit(Info, turretFacing));
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			Func<WAngle> bodyFacing = () => facing.Facing;
			var dynamicFacing = inits.GetOrDefault<DynamicFacingInit>();
			var staticFacing = inits.GetOrDefault<FacingInit>();
			if (dynamicFacing != null)
				bodyFacing = dynamicFacing.Value;
			else if (staticFacing != null)
				bodyFacing = () => staticFacing.Value;

			// Freeze the relative turret facing to its current value
			var facingOffset = WAngle.FromFacing(TurretFacing) - bodyFacing();
			inits.Add(new DynamicTurretFacingInit(Info, () => bodyFacing() + facingOffset));
		}

		protected override void TraitDisabled(Actor self)
		{
			if (attack != null && attack.IsAiming)
				attack.OnStopOrder(self);
		}
	}

	public class TurretFacingInit : ValueActorInit<WAngle>
	{
		public TurretFacingInit(TraitInfo info, WAngle value)
			: base(info, value) { }

		public TurretFacingInit(string instanceName, WAngle value)
			: base(instanceName, value) { }

		public TurretFacingInit(WAngle value)
			: base(value) { }
	}

	public class DynamicTurretFacingInit : ValueActorInit<Func<WAngle>>
	{
		public DynamicTurretFacingInit(TraitInfo info, Func<WAngle> value)
			: base(info, value) { }
	}
}
