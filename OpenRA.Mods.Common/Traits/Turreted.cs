#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TurretedInfo : ITraitInfo, UsesInit<TurretFacingInit>, Requires<BodyOrientationInfo>
	{
		public readonly string Turret = "primary";
		[Desc("Rate of Turning")]
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		[Desc("Number of ticks before turret is realigned. (-1 turns off realignment)")]
		public readonly int RealignDelay = 40;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		public virtual object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : ITick, ISync, INotifyCreated, IDeathActorInitModifier
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

		public static int GetInitialTurretFacing(IActorInitializer init, int def, string turret = null)
		{
			if (turret != null && init.Contains<TurretFacingsInit>())
			{
				int facing;
				if (init.Get<TurretFacingsInit, Dictionary<string, int>>().TryGetValue(turret, out facing))
					return facing;
			}

			if (init.Contains<TurretFacingInit>())
				return init.Get<TurretFacingInit, int>();

			if (init.Contains<FacingInit>())
				return init.Get<FacingInit, int>();

			return def;
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
		{
			this.info = info;
			TurretFacing = GetInitialTurretFacing(init, info.InitialFacing, info.Turret);
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
			TurretFacing = Util.TickFacing(TurretFacing, df, info.ROT);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			var delta = target.CenterPosition - self.CenterPosition;
			DesiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : TurretFacing;
			MoveTurret();
			return TurretFacing == DesiredFacing.Value;
		}

		// Turret offset in world-space
		public WVec Position(Actor self)
		{
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			return body.LocalToWorld(Offset.Rotate(bodyOrientation));
		}

		// Orientation in unit-space
		public WRot LocalOrientation(Actor self)
		{
			// Hack: turretFacing is relative to the world, so subtract the body yaw
			var local = WRot.FromYaw(WAngle.FromFacing(TurretFacing) - self.Orientation.Yaw);

			if (QuantizedFacings == 0)
				return local;

			// Quantize orientation to match a rendered sprite
			// Implies no pitch or yaw
			var facing = Util.QuantizeFacing(local.Yaw.Angle / 4, QuantizedFacings) * (256 / QuantizedFacings);
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

			facings.Value(self.World).Add(Name, TurretFacing);
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
}
