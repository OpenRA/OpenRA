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
	public class RenderInfantryInfo : RenderSimpleInfo, Requires<IMoveInfo>
	{
		public readonly int MinIdleWaitTicks = 30;
		public readonly int MaxIdleWaitTicks = 110;
		public readonly bool SpawnsCorpse = true;
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

		IMove move;
		RenderInfantryInfo info;
		public bool IsMoving { get; set; }
		protected bool dirty = false;
		string idleSequence;
		int idleDelay;

		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			return baseSequence;
		}

		protected virtual bool AllowIdleAnimation(Actor self)
		{
			return info.IdleAnimations.Length > 0;
		}

		public AnimationState State { get; private set; }

		public RenderInfantry(Actor self, RenderInfantryInfo info)
			: base(self, MakeFacingFunc(self))
		{
			this.info = info;
			anim.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			State = AnimationState.Waiting;
			move = self.Trait<IMove>();

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

			if ((State == AnimationState.Moving || dirty) && !move.IsMoving)
			{
				State = AnimationState.Waiting;
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			}
			else if ((State != AnimationState.Moving || dirty) && move.IsMoving)
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
				anim.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
				State = AnimationState.Idle;

				if (info.IdleAnimations.Length > 0)
				{
					idleSequence = info.IdleAnimations.Random(self.World.SharedRandom);
					idleDelay = self.World.SharedRandom.Next(info.MinIdleWaitTicks, info.MaxIdleWaitTicks);
				}
			}
			else if (AllowIdleAnimation(self) && idleDelay > 0 && --idleDelay == 0)
			{
				if (anim.HasSequence(idleSequence))
				{
					State = AnimationState.IdleAnimating;
					anim.PlayThen(idleSequence,	() =>
					{
						anim.PlayRepeating(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)));
						State = AnimationState.Waiting;
					});
				}
			}
		}

		// TODO: Possibly move this into a separate trait
		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means
			if (e.Warhead == null)
				return;

			if (info.SpawnsCorpse)
			{
				SpawnCorpse(self, "die" + (e.Warhead.InfDeath));
			}	
		}

		public void SpawnCorpse(Actor self, string sequence)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Destroyed)
					w.Add(new Corpse(w, self.CenterPosition, GetImage(self),
						sequence, info.PlayerPalette + self.Owner.InternalName));
			});
		}
	}
}
