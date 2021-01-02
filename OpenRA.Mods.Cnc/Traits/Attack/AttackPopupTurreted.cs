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

using System.Linq;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Actor's turret rises from the ground before attacking.")]
	class AttackPopupTurretedInfo : AttackTurretedInfo, Requires<BuildingInfo>, Requires<WithEmbeddedTurretSpriteBodyInfo>
	{
		[Desc("How many game ticks should pass before closing the actor's turret.")]
		public readonly int CloseDelay = 125;

		public readonly WAngle DefaultFacing = WAngle.Zero;

		[Desc("The percentage of damage that is received while this actor is closed.")]
		public readonly int ClosedDamageMultiplier = 50;

		[SequenceReference]
		[Desc("Sequence to play when opening.")]
		public readonly string OpeningSequence = "opening";

		[SequenceReference]
		[Desc("Sequence to play when closing.")]
		public readonly string ClosingSequence = "closing";

		[SequenceReference]
		[Desc("Idle sequence to play when closed.")]
		public readonly string ClosedIdleSequence = "closed-idle";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new AttackPopupTurreted(init, this); }
	}

	class AttackPopupTurreted : AttackTurreted, INotifyIdle, IDamageModifier
	{
		enum PopupState { Open, Rotating, Transitioning, Closed }

		readonly AttackPopupTurretedInfo info;
		readonly WithSpriteBody wsb;
		readonly Turreted turret;

		int idleTicks = 0;
		PopupState state = PopupState.Open;
		bool skippedMakeAnimation;

		public AttackPopupTurreted(ActorInitializer init, AttackPopupTurretedInfo info)
			: base(init.Self, info)
		{
			this.info = info;
			turret = turrets.FirstOrDefault();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
			skippedMakeAnimation = init.Contains<SkipMakeAnimsInit>(info);
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			// Map placed actors are created in the closed state
			if (skippedMakeAnimation)
			{
				state = PopupState.Closed;
				wsb.PlayCustomAnimationRepeating(self, info.ClosedIdleSequence);
				turret.FaceTarget(self, Target.Invalid);
			}
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			if (IsTraitPaused)
				return false;

			if (state == PopupState.Closed)
			{
				state = PopupState.Transitioning;
				wsb.PlayCustomAnimation(self, info.OpeningSequence, () =>
				{
					state = PopupState.Open;
					wsb.PlayCustomAnimationRepeating(self, wsb.Info.Sequence);
				});

				idleTicks = 0;
			}

			if (state == PopupState.Transitioning || !base.CanAttack(self, target))
				return false;

			idleTicks = 0;
			return true;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitPaused)
				return;

			if (state == PopupState.Open && idleTicks++ > info.CloseDelay)
			{
				var facingOffset = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(info.DefaultFacing));
				turret.FaceTarget(self, Target.FromPos(self.CenterPosition + facingOffset));
				state = PopupState.Rotating;
			}
			else if (state == PopupState.Rotating && turret.HasAchievedDesiredFacing)
			{
				state = PopupState.Transitioning;
				wsb.PlayCustomAnimation(self, info.ClosingSequence, () =>
				{
					state = PopupState.Closed;
					wsb.PlayCustomAnimationRepeating(self, info.ClosedIdleSequence);
					turret.FaceTarget(self, Target.Invalid);
				});
			}
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return state == PopupState.Closed ? info.ClosedDamageMultiplier : 100;
		}
	}
}
