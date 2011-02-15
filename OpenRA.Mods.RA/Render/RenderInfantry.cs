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
		public bool Panicked = false;
		public bool Prone = false;
		bool wasProne = false;
		
		public enum AnimationState
		{
			Idle,
			Attacking,
			Moving,
			Waiting
		};
		
		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = Prone ? "prone-" :
						 Panicked ? "panic-" : "";
			
			if (anim.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;
			else
				return baseSequence;
		}
		
		public AnimationState State { get; private set; }
		Mobile mobile;
		public RenderInfantry(Actor self)
			: base(self, () => self.Trait<IFacing>().Facing)
		{
			anim.PlayFetchIndex(NormalizeInfantrySequence(self, "stand"), () => 0);
			State = AnimationState.Idle;
			mobile = self.Trait<Mobile>();
		}

		public void Attacking(Actor self, Target target)
		{
			State = AnimationState.Attacking;
			if (anim.HasSequence(NormalizeInfantrySequence(self, "shoot")))
				anim.PlayThen(NormalizeInfantrySequence(self, "shoot"), () => State = AnimationState.Idle);
			else if (anim.HasSequence(NormalizeInfantrySequence(self, "heal")))
				anim.PlayThen(NormalizeInfantrySequence(self, "heal"), () => State = AnimationState.Idle);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			
			if ((State == AnimationState.Moving || wasProne != Prone) && !mobile.IsMoving)
			{
				State = AnimationState.Waiting;
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, "stand"), () => 0);
			}
			else if ((State != AnimationState.Moving || wasProne != Prone) && mobile.IsMoving)
			{
				State = AnimationState.Moving;
				anim.PlayRepeating(NormalizeInfantrySequence(self, "run"));
			}
			
			wasProne = Prone;
		}
		
		public void TickIdle(Actor self)
		{
			if (State != AnimationState.Idle)
			{
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, "stand"), () => 0);
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
