#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
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
		public readonly string CorpseSequence = "corpse";
		public readonly int[] CorpseInfDeathExceptions = { };

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

		public readonly RenderInfantryInfo Info;
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
			Info = info;
			anim.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			State = AnimationState.Waiting;
			mobile = self.Trait<Mobile>();

			self.Trait<IBodyOrientation>().SetAutodetectedFacings(anim.CurrentSequence.Facings);
		}

		public void Attacking(Actor self, Target target)
		{
			State = AnimationState.Attacking;
			if (anim.HasSequence(NormalizeInfantrySequence(self, "shoot")))
				anim.PlayThen(NormalizeInfantrySequence(self, "shoot"), () => State = AnimationState.Idle);
			else if (anim.HasSequence(NormalizeInfantrySequence(self, "heal")))
				anim.PlayThen(NormalizeInfantrySequence(self, "heal"), () => State = AnimationState.Idle);
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
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			}
			else if ((State != AnimationState.Moving || dirty) && mobile.IsMoving)
			{
				State = AnimationState.Moving;
				anim.PlayRepeating(NormalizeInfantrySequence(self, "run"));
			}

			dirty = false;
		}

		public void TickIdle(Actor self)
		{
			if (State != AnimationState.Idle && State != AnimationState.IdleAnimating)
			{
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
				State = AnimationState.Idle;

				if (Info.IdleAnimations.Length > 0)
				{
					idleSequence = Info.IdleAnimations.Random(self.World.SharedRandom);
					idleDelay = self.World.SharedRandom.Next(Info.MinIdleWaitTicks, Info.MaxIdleWaitTicks);
				}
			}
			else if (AllowIdleAnimation(self) && idleDelay > 0 && --idleDelay == 0)
			{
				if (anim.HasSequence(idleSequence))
				{
					State = AnimationState.IdleAnimating;
					anim.PlayThen(idleSequence,	() =>
					{
						anim.PlayRepeating(NormalizeInfantrySequence(self, Info.StandAnimations.Random(Game.CosmeticRandom)));
						State = AnimationState.Waiting;
					});
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means. This includes dying inside a transport
			// and being crushed by a vehicle (CrushableInfantry will spawn a corpse instead).
			if (e.Warhead == null)
				return;

			Sound.PlayVoice("Die", self, self.Owner.Country.Race);
			SpawnCorpse(self, "die{0}".F(e.Warhead.InfDeath), () =>
			{
				if (!Info.CorpseInfDeathExceptions.Contains(e.Warhead.InfDeath))
					SpawnCorpse(self, Info.CorpseSequence, null);
			});
		}

		public void SpawnCorpse(Actor self, string sequence, Action onComplete)
		{
			self.World.AddFrameEndTask(w =>
			{
				// A Destroyed check isn't required here because nothing we use here
				// will blow up if the actor is Destroyed, and it would prevent
				// the corpse animation from appearing after the death animation is finished anyway.
				if (anim.HasSequence(sequence))
					w.Add(new Corpse(w, self.CenterPosition, GetImage(self),
						sequence, Info.PlayerPalette + self.Owner.InternalName, onComplete));
			});
		}
	}
}
