#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderInfantryInfo : RenderSimpleInfo, Requires<MobileInfo>
	{
		public readonly int MinIdleWaitTicks = 30;
		public readonly int MaxIdleWaitTicks = 110;
		public readonly string[] IdleAnimations = { };
		public readonly string[] StandAnimations = { "stand" };

		public override object Create(ActorInitializer init) { return new RenderInfantry(init.self, this); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyKilled, INotifyIdle
	{
		public enum AnimationState
		{
			Idle,
			Attacking,
			Moving,
			Waiting,
			IdleAnimating
		}

		protected bool dirty = false;

		public RenderInfantryInfo Info;
		string idleSequence;
		int idleDelay;
		Mobile mobile;

		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			return baseSequence;
		}

		protected virtual bool AllowIdleAnimation(Actor self)
		{
			return Info.IdleAnimations.Length > 0;
		}

		public AnimationState State { get; private set; }

		public RenderInfantry(Actor self, RenderInfantryInfo info)
			: base(self, MakeFacingFunc(self))
		{
			this.Info = info;
			Anim.PlayFetchIndex(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			State = AnimationState.Waiting;
			mobile = self.Trait<Mobile>();

			self.Trait<IBodyOrientation>().SetAutodetectedFacings(Anim.CurrentSequence.Facings);
		}

		public void Attacking(Actor self, Target target)
		{
			State = AnimationState.Attacking;
			if (Anim.HasSequence(NormalizeInfantrySequence(self, "shoot")))
				Anim.PlayThen(NormalizeInfantrySequence(self, "shoot"), () => State = AnimationState.Idle);
			else if (Anim.HasSequence(NormalizeInfantrySequence(self, "heal")))
				Anim.PlayThen(NormalizeInfantrySequence(self, "heal"), () => State = AnimationState.Idle);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			Attacking(self, target);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if ((State == AnimationState.Moving || dirty) && !mobile.IsMoving)
			{
				State = AnimationState.Waiting;
				Anim.PlayFetchIndex(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			}
			else if ((State != AnimationState.Moving || dirty) && mobile.IsMoving)
			{
				State = AnimationState.Moving;
				Anim.PlayRepeating(NormalizeInfantrySequence(self, "run"));
			}

			dirty = false;
		}

		public void TickIdle(Actor self)
		{
			if (State != AnimationState.Idle && State != AnimationState.IdleAnimating)
			{
				Anim.PlayFetchIndex(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
				State = AnimationState.Idle;

				if (Info.IdleAnimations.Length > 0)
				{
					idleSequence = Info.IdleAnimations.Random(self.World.SharedRandom);
					idleDelay = self.World.SharedRandom.Next(Info.MinIdleWaitTicks, Info.MaxIdleWaitTicks);
				}
			}
			else if (AllowIdleAnimation(self) && idleDelay > 0 && --idleDelay == 0)
			{
				if (Anim.HasSequence(idleSequence))
				{
					State = AnimationState.IdleAnimating;
					Anim.PlayThen(idleSequence,	() =>
					{
						Anim.PlayRepeating(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)));
						State = AnimationState.Waiting;
					});
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means
			if (e.Warhead == null)
				return;

			Sound.PlayVoice("Die", self, self.Owner.Country.Race);
			SpawnCorpse(self, "die{0}".F(e.Warhead.InfDeath));
		}

		public void SpawnCorpse(Actor self, string sequence)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Destroyed)
					w.Add(new Corpse(w, self.CenterPosition, GetImage(self),
						sequence, Info.PlayerPalette + self.Owner.InternalName));
			});
		}
	}
}
