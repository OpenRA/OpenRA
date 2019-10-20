#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

		[Desc("Turret facing relative to body when the actor is idle")]
		public readonly int InitialFacing = 0;

		[Desc("Number of ticks before turret is realigned. (-1 turns off realignment)")]
		public readonly int RealignDelay = 40;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly int PreviewFacing = 0;

		[Desc("Display order for the turret facing slider in the map editor")]
		public readonly int EditorTurretFacingDisplayOrder = 4;

		IEnumerable<object> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			// HACK: The ActorInit system does not support multiple instances of the same type
			// Make sure that we only return one TurretFacingInit, even for actors with multiple turrets
			if (ai.TraitInfos<TurretedInfo>().FirstOrDefault() == this)
				yield return new TurretFacingInit(PreviewFacing);
		}

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			// TODO: Handle multiple turrets properly (will probably require a rewrite of the Init system)
			if (ai.TraitInfos<TurretedInfo>().FirstOrDefault() != this)
				yield break;

			yield return new EditorActorSlider("Turret", EditorTurretFacingDisplayOrder, 0, 255, 8,
				actor =>
				{
					var init = actor.Init<TurretFacingInit>();
					if (init != null)
						return init.Value(world);

					return InitialFacing;
				},
				(actor, value) =>
				{
					actor.RemoveInit<TurretFacingsInit>();
					actor.ReplaceInit(new TurretFacingInit((int)value));
				});
		}

		public override object Create(ActorInitializer init) { return new Turreted(init, this); }
	}

	public class Turreted : PausableConditionalTrait<TurretedInfo>, ITick, IDeathActorInitModifier, IActorPreviewInitModifier
	{
		AttackTurreted attack;
		IFacing facing;
		BodyOrientation body;

		int facingOffset = 0;

		[Sync]
		public int QuantizedFacings = 0;

		[Sync]
		public int TurretFacing
		{
			get { return facing == null ? facingOffset : Util.NormalizeFacing(facingOffset + facing.Facing); }
		}

		public int? DesiredFacing;
		int realignTick = 0;

		// For subclasses that want to move the turret relative to the body
		protected WVec localOffset = WVec.Zero;

		public WVec Offset { get { return Info.Offset + localOffset; } }
		public string Name { get { return Info.Turret; } }

		static Func<int> FacingOffsetFromInit(IActorInitializer init, int def, string turret = null)
		{
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

			return () => def;
		}

		public static Func<int> TurretFacingFromInit(IActorInitializer init, int def, Func<int> facing, string turret = null)
		{
			var facingOffset = FacingOffsetFromInit(init, def, turret);
			return () => facingOffset() + facing();
		}

		public Turreted(ActorInitializer init, TurretedInfo info)
			: base(info)
		{
			facingOffset = FacingOffsetFromInit(init, Info.InitialFacing, Info.Turret)();
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
			var df = DesiredFacing ?? Info.InitialFacing;
			facingOffset = Util.TickFacing(facingOffset, df, Info.TurnSpeed);
		}

		public bool FaceTarget(Actor self, Target target)
		{
			if (IsTraitDisabled || IsTraitPaused || attack == null || attack.IsTraitDisabled || attack.IsTraitPaused)
				return false;

			var pos = self.CenterPosition;
			var targetPos = attack.GetTargetPosition(pos, target);
			var delta = targetPos - pos;
			var df = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : TurretFacing;
			DesiredFacing = facing == null ? df : Util.NormalizeFacing(df - facing.Facing);
			MoveTurret();
			return HasAchievedDesiredFacing;
		}

		public virtual bool HasAchievedDesiredFacing
		{
			get { return DesiredFacing == null || facingOffset == DesiredFacing.Value; }
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
				facings.Value(self.World).Add(Name, facingOffset);
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			var facings = inits.GetOrDefault<TurretFacingsInit>();
			if (facings == null)
			{
				facings = new TurretFacingsInit();
				inits.Add(facings);
			}

			// Freeze the relative turret facing to its current value
			facings.Value(self.World).Add(Name, facingOffset);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (attack != null && attack.IsAiming)
				attack.OnStopOrder(self);
		}
	}

	public class TurretFacingInit : IActorInit<int>
	{
		[FieldFromYamlKey]
		readonly int value = 128;

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
