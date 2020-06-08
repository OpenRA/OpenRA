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
		public readonly int TurnSpeed = 255;
		public readonly int InitialFacing = 0;

		[Desc("Number of ticks before turret is realigned. (-1 turns off realignment)")]
		public readonly int RealignDelay = 40;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly int PreviewFacing = 96;

		[Desc("Display order for the turret facing slider in the map editor")]
		public readonly int EditorTurretFacingDisplayOrder = 4;

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new TurretFacingInit(this, PreviewFacing);
		}

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorSlider("Turret", EditorTurretFacingDisplayOrder, 0, 255, 8,
				actor =>
				{
					var init = actor.GetInitOrDefault<TurretFacingInit>(this);
					if (init != null)
						return init.Value;

					var facingInit = actor.GetInitOrDefault<FacingInit>(this);
					if (facingInit != null)
						return facingInit.Value;

					return InitialFacing;
				},
				(actor, value) =>
				{
					actor.RemoveInit<TurretFacingsInit>();
					actor.ReplaceInit(new TurretFacingInit(this, (int)value), this);
				});
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

		public static Func<int> TurretFacingFromInit(IActorInitializer init, TurretedInfo info)
		{
			return TurretFacingFromInit(init, info, info.InitialFacing, info.Turret);
		}

		public static Func<int> TurretFacingFromInit(IActorInitializer init, TraitInfo info, int defaultFacing, string turret = null)
		{
			if (turret != null)
			{
				Func<int> getFacing;
				var dynamicTurretFacingsInit = init.GetOrDefault<DynamicTurretFacingsInit>();
				if (dynamicTurretFacingsInit != null && dynamicTurretFacingsInit.Value.TryGetValue(turret, out getFacing))
					return getFacing;

				int facing;
				var turretFacingsInit = init.GetOrDefault<TurretFacingsInit>();
				if (turretFacingsInit != null && turretFacingsInit.Value.TryGetValue(turret, out facing))
					return () => facing;
			}

			var turretFacingInit = init.GetOrDefault<TurretFacingInit>(info);
			if (turretFacingInit != null)
			{
				var facing = turretFacingInit.Value;
				return () => facing;
			}

			var dynamicFacingInit = init.GetOrDefault<DynamicFacingInit>();
			if (dynamicFacingInit != null)
				return dynamicFacingInit.Value;

			var facingInit = init.GetOrDefault<FacingInit>();
			if (facingInit != null)
			{
				var facing = facingInit.Value;
				return () => facing;
			}

			return () => defaultFacing;
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
			: base(info)
		{
			TurretFacing = TurretFacingFromInit(init, Info)();
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
			TurretFacing = Util.TickFacing(TurretFacing, df, Info.TurnSpeed);
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
			var facings = init.GetOrDefault<TurretFacingsInit>();
			if (facings == null)
			{
				facings = new TurretFacingsInit(new Dictionary<string, int>());
				init.Add(facings);
			}

			if (!facings.Value.ContainsKey(Name))
				facings.Value.Add(Name, TurretFacing);
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			var facings = inits.GetOrDefault<DynamicTurretFacingsInit>();
			if (facings == null)
			{
				facings = new DynamicTurretFacingsInit(new Dictionary<string, Func<int>>());
				inits.Add(facings);
			}

			Func<int> bodyFacing = () => facing.Facing.Facing;
			var dynamicFacing = inits.GetOrDefault<DynamicFacingInit>();
			var staticFacing = inits.GetOrDefault<FacingInit>();
			if (dynamicFacing != null)
				bodyFacing = dynamicFacing.Value;
			else if (staticFacing != null)
				bodyFacing = () => staticFacing.Value;

			// Freeze the relative turret facing to its current value
			var facingOffset = TurretFacing - bodyFacing();
			facings.Value.Add(Name, () => bodyFacing() + facingOffset);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (attack != null && attack.IsAiming)
				attack.OnStopOrder(self);
		}
	}

	public class TurretFacingInit : ValueActorInit<int>
	{
		public TurretFacingInit(TraitInfo info, int value)
			: base(info, value) { }

		public TurretFacingInit(string instanceName, int value)
			: base(instanceName, value) { }

		public TurretFacingInit(int value)
			: base(value) { }
	}

	public class TurretFacingsInit : ValueActorInit<Dictionary<string, int>>, ISingleInstanceInit
	{
		public TurretFacingsInit(Dictionary<string, int> value)
			: base(value) { }
	}

	public class DynamicTurretFacingsInit : ValueActorInit<Dictionary<string, Func<int>>>, ISingleInstanceInit
	{
		public DynamicTurretFacingsInit(Dictionary<string, Func<int>> value)
			: base(value) { }
	}
}
