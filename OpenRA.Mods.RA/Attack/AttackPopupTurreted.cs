#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor's turret rises from the ground before attacking.")]
	class AttackPopupTurretedInfo : AttackTurretedInfo, Requires<BuildingInfo>, Requires<RenderBuildingInfo>
	{
		[Desc("How many game ticks should pass before closing the actor's turret.")]
		public int CloseDelay = 125;
		public int DefaultFacing = 0;

		[Desc("The factor damage received is multiplied by while this actor is closed.")]
		public float ClosedDamageMultiplier = 0.5f;

		[Desc("The factor disableticks is multiplied by while this actor is closed.")]
		public float ClosedDisableTicksMultiplier = 1f;

		public override object Create(ActorInitializer init) { return new AttackPopupTurreted(init, this); }
	}

	class AttackPopupTurreted : AttackTurreted, INotifyBuildComplete, INotifyIdle, IDamageModifier, IDisableTicksModifier
	{
		enum PopupState { Open, Rotating, Transitioning, Closed }

		AttackPopupTurretedInfo info;
		RenderBuilding rb;

		int idleTicks = 0;
		PopupState state = PopupState.Open;
		Turreted turret;
		bool skippedMakeAnimation;

		public AttackPopupTurreted(ActorInitializer init, AttackPopupTurretedInfo info)
			: base(init.self, info)
		{
			this.info = info;
			turret = turrets.FirstOrDefault();
			rb = init.self.Trait<RenderBuilding>();
			skippedMakeAnimation = init.Contains<SkipMakeAnimsInit>();
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (state == PopupState.Transitioning || !building.Value.BuildComplete)
				return false;

			if (!base.CanAttack(self, target))
				return false;

			idleTicks = 0;
			if (state == PopupState.Closed)
			{
				state = PopupState.Transitioning;
				rb.PlayCustomAnimThen(self, "opening", () =>
				{
					state = PopupState.Open;
					rb.PlayCustomAnimRepeating(self, "idle");
				});
				return false;
			}

			if (!turret.FaceTarget(self, target))
				return false;

			return true;
		}

		public void TickIdle(Actor self)
		{
			if (state == PopupState.Open && idleTicks++ > info.CloseDelay)
			{
				turret.desiredFacing = info.DefaultFacing;
				state = PopupState.Rotating;
			}
			else if (state == PopupState.Rotating && turret.turretFacing == info.DefaultFacing)
			{
				state = PopupState.Transitioning;
				rb.PlayCustomAnimThen(self, "closing", () =>
				{
					state = PopupState.Closed;
					rb.PlayCustomAnimRepeating(self, "closed-idle");
					turret.desiredFacing = null;
				});
			}
		}

		public void BuildingComplete(Actor self)
		{
			if (skippedMakeAnimation)
			{
				state = PopupState.Closed;
				rb.PlayCustomAnimRepeating(self, "closed-idle");
				turret.desiredFacing = null;
			}
		}

		public float GetDamageModifier(Actor attacker, DamagerWarheadInfo warhead)
		{
			return state == PopupState.Closed ? info.ClosedDamageMultiplier : 1f;
		}

		public float GetDisableTicksModifier(Actor attacker, DisablerWarheadInfo warhead)
		{
			return state == PopupState.Closed ? info.ClosedDisableTicksMultiplier : 1f;
		}
	}
}
