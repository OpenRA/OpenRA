#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Render
{
	public class RenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RenderInfantry(init.self); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage, INotifyIdle
	{
		public enum AnimationState
		{
			Idle,
			Attacking,
			Moving,
			Waiting
		};
		
		public AnimationState State { get; private set; }
		Mobile mobile;
		public RenderInfantry(Actor self)
			: base(self, () => self.Trait<IFacing>().Facing)
		{
			anim.Play("stand");
			State = AnimationState.Idle;
			mobile = self.Trait<Mobile>();
		}

		public void Attacking(Actor self, Target target)
		{
			State = AnimationState.Attacking;
			if (anim.HasSequence("shoot"))
				anim.PlayThen("shoot", () => State = AnimationState.Idle);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => State = AnimationState.Idle);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			// If path is blocked, we can have !isMoving and !idle
			// Need to handle this case specially
			if (!mobile.IsMoving && State == AnimationState.Moving)
			{
				State = AnimationState.Waiting;
				anim.Play("stand");
			}
			else if (State != AnimationState.Moving && mobile.IsMoving)
			{
				State = AnimationState.Moving;
				anim.PlayRepeating("run");
			}
		}
		
		public void TickIdle(Actor self)
		{
			if (State != AnimationState.Idle)
			{
				anim.Play("stand");
				State = AnimationState.Idle;
			}
		}
		

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var death = e.Warhead != null ? e.Warhead.InfDeath : 0;
				Sound.PlayVoice("Die", self, self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new Corpse(self, death)));
			}
		}
	}
}
