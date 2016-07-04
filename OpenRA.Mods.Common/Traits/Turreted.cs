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

using System;
using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TurretedInfo : ITraitInfo, UsesInit<TurretFacingInit>, Requires<BodyOrientationInfo>
	{
		public readonly string Turret = "primary";
		[Desc("Speed at which the turret turns.")]
		public readonly int TurnSpeed = 255;
		public readonly int InitialFacing = 0;

		[Desc("Number of ticks before turret is realigned. (-1 turns off realignment)")]
		public readonly int RealignDelay = 40;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		public virtual object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick, ISync, INotifyCreated, IDeathActorInitModifier, IActorPreviewInitModifier
	{
		readonly TurretedInfo info;
		AttackTurreted attack;
		IFacing facing;
		BodyOrientation body;

		[Sync] public int QuantizedFacings = 0;
		[Sync] public int TurretFacing = 0;
		public int? DesiredFacing;
		int realignTick = 0;

		// For subclasses that want to move the turret relative to the body
		protected WVec localOffset = WVec.Zero;

		public WVec Offset { get { return info.Offset + localOffset; } }
		public string Name { get { return info.Turret; } }

		public static Func<int> TurretFacingFromInit(IActorInitializer init, int def, string turret = null)
		{
			if (turret != null && init.Contains<DynamicTurretFacingsInit>())
			{
				Func<int> facing;
				if (init.Get<DynamicTurretFacingsInit, Dictionary<string, Func<int>>>().TryGetValue(turret, out facing))
					return facing;
			}

			if (turret != null && init.Contains<TurretFacingsInit>())
			{
				int facing;
				if (init.Get<TurretFacingsInit, Dictionary<string, int>>().TryGetValue(turret, out facing))
					return () => facing;
			}

			if (init.Contains<TurretFacingInit>())
			{
				var facing = init.Get<TurretFacingInit, int>();
				return () => facing;
			}

			if (init.Contains<DynamicFacingInit>())
				return init.Get<DynamicFacingInit, Func<int>>();

			if (init.Contains<FacingInit>())
			{
				var facing = init.Get<FacingInit, int>();
				return () => facing;
			}

			return () => def;
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
		{
			this.info = info;
			TurretFacing = TurretFacingFromInit(init, info.InitialFacing, info.Turret)();
		}

		public void Created(Actor self)
		{
			attack = self.TraitOrDefault<AttackTurreted>();
			facing = self.TraitOrDefault<IFacing>();
			body = self.Trait<BodyOrientation>();
		}

		public virtual void Tick(Actor self)
		{
			// NOTE: FaceTarget is called in AttackTurreted.CanAttack if the turret has a target.
			if (attack != null)
			{
				if (!attack.IsAttacking)
				{
					if (realignTick < info.RealignDelay)
						realignTick++;
					else if (info.RealignDelay > -1)
						DesiredFacing = null;

					MoveTurret();
				}
			}
			else
			{
				realignTick = 0;
				MoveTurret();
			}
		}

		void MoveTurret()
		{
			var df = DesiredFacing ?? (facing != null ? facing.Facing : TurretFacing);
			TurretFacing = Util.TickFacing(TurretFacing, df, info.TurnSpeed);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			if (self.IsDisabled())
				return false;

			var delta = target.CenterPosition - self.CenterPosition;
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
			var facing = body.QuantizeFacing(world.Yaw.Angle / 4, QuantizedFacings);
			return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
		}

		public void ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			var facings = init.GetOrDefault<TurretFacingsInit>();
			if (facings == null)
			{
				facings = new TurretFacingsInit();
				init.Add(facings);
			}

			if (!facings.Value(self.World).ContainsKey(Name))
				facings.Value(self.World).Add(Name, TurretFacing);
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			var facings = inits.GetOrDefault<DynamicTurretFacingsInit>();
			if (facings == null)
			{
				facings = new DynamicTurretFacingsInit();
				inits.Add(facings);
			}

			Func<int> bodyFacing = () => facing.Facing;
			var dynamicFacing = inits.GetOrDefault<DynamicFacingInit>();
			var staticFacing = inits.GetOrDefault<FacingInit>();
			if (dynamicFacing != null)
				bodyFacing = dynamicFacing.Value(self.World);
			else if (staticFacing != null)
				bodyFacing = () => staticFacing.Value(self.World);

			// Freeze the relative turret facing to its current value
			var facingOffset = TurretFacing - bodyFacing();
			facings.Value(self.World).Add(Name, () => bodyFacing() + facingOffset);
		}
	}

	public class TurretFacingInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 128;
		public TurretFacingInit() { }
		public TurretFacingInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class TurretFacingsInit : IActorInit<Dictionary<string, int>>
	{
		[DictionaryFromYamlKey]
		readonly Dictionary<string, int> value = new Dictionary<string, int>();
		public TurretFacingsInit() { }
		public TurretFacingsInit(Dictionary<string, int> init) { value = init; }
		public Dictionary<string, int> Value(World world) { return value; }
	}

	public class DynamicTurretFacingsInit : IActorInit<Dictionary<string, Func<int>>>
	{
		readonly Dictionary<string, Func<int>> value = new Dictionary<string, Func<int>>();
		public DynamicTurretFacingsInit() { }
		public DynamicTurretFacingsInit(Dictionary<string, Func<int>> init) { value = init; }
		public Dictionary<string, Func<int>> Value(World world) { return value; }
	}
}
