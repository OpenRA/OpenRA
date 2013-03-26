#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
	class AttackPopupTurretedInfo : AttackTurretedInfo
	{
		public int CloseDelay = 125;
		public int DefaultFacing = 0;
		public float ClosedDamageMultiplier = 0.5f;
		public override object Create(ActorInitializer init) { return new AttackPopupTurreted( init, this ); }
	}

	class AttackPopupTurreted : AttackTurreted, INotifyBuildComplete, INotifyIdle, IDamageModifier
	{
		enum PopupState { Open, Rotating, Transitioning, Closed };

		AttackPopupTurretedInfo Info;
		int IdleTicks = 0;
		PopupState State = PopupState.Open;
		Turreted turret;

		public AttackPopupTurreted(ActorInitializer init, AttackPopupTurretedInfo info) : base(init.self)
		{
			Info = info;
			buildComplete = init.Contains<SkipMakeAnimsInit>();
			turret = turrets.FirstOrDefault();
		}

		protected override bool CanAttack( Actor self, Target target )
		{
			if (State == PopupState.Transitioning)
				return false;

			if( self.HasTrait<Building>() && !buildComplete )
				return false;

			if (!base.CanAttack( self, target ))
				return false;

			IdleTicks = 0;
			if (State == PopupState.Closed)
			{
				State = PopupState.Transitioning;
				var rb = self.Trait<RenderBuilding>();
				rb.PlayCustomAnimThen(self, "opening", () =>
				{
					State = PopupState.Open;
					rb.PlayCustomAnimRepeating(self, "idle");
				});
				return false;
			}

			if (!turret.FaceTarget(self,target)) return false;

			return true;
		}

		public void TickIdle(Actor self)
		{
			if (State == PopupState.Open && IdleTicks++ > Info.CloseDelay)
			{
				turret.desiredFacing = Info.DefaultFacing;
				State = PopupState.Rotating;
			}
			else if (State == PopupState.Rotating && turret.turretFacing == Info.DefaultFacing)
			{
				State = PopupState.Transitioning;
				var rb = self.Trait<RenderBuilding>();
				rb.PlayCustomAnimThen(self, "closing", () =>
				{
					State = PopupState.Closed;
					rb.PlayCustomAnimRepeating(self, "closed-idle");
					turret.desiredFacing = null;
				});
			}
		}

		public override void BuildingComplete(Actor self)
		{
			// Set true for SkipMakeAnimsInit
			if (buildComplete)
			{
				State = PopupState.Closed;
				self.Trait<RenderBuilding>()
					.PlayCustomAnimRepeating(self, "closed-idle");
				turret.desiredFacing = null;
			}
			buildComplete = true;
		}

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return State == PopupState.Closed ? Info.ClosedDamageMultiplier : 1f;
		}
	}
}
