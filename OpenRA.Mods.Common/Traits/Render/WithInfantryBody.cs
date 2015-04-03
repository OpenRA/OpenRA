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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class WithInfantryBodyInfo : ITraitInfo, IQuantizeBodyOrientationInfo, IRenderActorPreviewSpritesInfo, Requires<IMoveInfo>, Requires<RenderSpritesInfo>
	{
		public readonly int MinIdleWaitTicks = 30;
		public readonly int MaxIdleWaitTicks = 110;
		public readonly string MoveSequence = "run";
		public readonly string AttackSequence = "shoot";
		public readonly string[] IdleSequences = { };
		public readonly string[] StandSequences = { "stand" };

		public virtual object Create(ActorInitializer init) { return new WithInfantryBody(init, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var facing = 0;
			var ifacing = init.Actor.Traits.GetOrDefault<IFacingInfo>();
			if (ifacing != null)
				facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing();

			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), StandSequences.First()));
			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race)
		{
			var rsi = ai.Traits.Get<RenderSpritesInfo>();
			return sequenceProvider.GetSequence(rsi.GetImage(ai, sequenceProvider, race), StandSequences.First()).Facings;
		}
	}

	public class WithInfantryBody : ITick, INotifyAttack, INotifyIdle
	{
		readonly WithInfantryBodyInfo info;
		readonly IMove move;
		protected readonly Animation DefaultAnimation;

		bool dirty = false;
		string idleSequence;
		int idleDelay;
		AnimationState state;

		IRenderInfantrySequenceModifier rsm;
		bool IsModifyingSequence { get { return rsm != null && rsm.IsModifyingSequence; } }
		bool wasModifying;

		public WithInfantryBody(ActorInitializer init, WithInfantryBodyInfo info)
		{
			this.info = info;
			var self = init.Self;
			var rs = self.Trait<RenderSprites>();

			DefaultAnimation = new Animation(init.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
			rs.Add("", DefaultAnimation);

			DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(init.Self, info.StandSequences.Random(Game.CosmeticRandom)), () => 0);
			state = AnimationState.Waiting;
			move = init.Self.Trait<IMove>();
			rsm = init.Self.TraitOrDefault<IRenderInfantrySequenceModifier>();
		}

		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = IsModifyingSequence ? rsm.SequencePrefix : "";

			if (DefaultAnimation.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;
			else
				return baseSequence;
		}

		protected virtual bool AllowIdleAnimation(Actor self)
		{
			return !IsModifyingSequence;
		}

		public void Attacking(Actor self, Target target)
		{
			state = AnimationState.Attacking;
			if (DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, info.AttackSequence)))
				DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, info.AttackSequence), () => state = AnimationState.Idle);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			Attacking(self, target);
		}

		public virtual void Tick(Actor self)
		{
			if (rsm != null)
			{
				if (wasModifying != rsm.IsModifyingSequence)
					dirty = true;

				wasModifying = rsm.IsModifyingSequence;
			}

			if ((state == AnimationState.Moving || dirty) && !move.IsMoving)
			{
				state = AnimationState.Waiting;
				DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandSequences.Random(Game.CosmeticRandom)), () => 0);
			}
			else if ((state != AnimationState.Moving || dirty) && move.IsMoving)
			{
				state = AnimationState.Moving;
				DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.MoveSequence));
			}

			dirty = false;
		}

		public void TickIdle(Actor self)
		{
			if (state != AnimationState.Idle && state != AnimationState.IdleAnimating)
			{
				DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandSequences.Random(Game.CosmeticRandom)), () => 0);
				state = AnimationState.Idle;

				if (info.IdleSequences.Length > 0)
				{
					idleSequence = info.IdleSequences.Random(self.World.SharedRandom);
					idleDelay = self.World.SharedRandom.Next(info.MinIdleWaitTicks, info.MaxIdleWaitTicks);
				}
			}
			else if (AllowIdleAnimation(self))
			{
				if (idleSequence != null && DefaultAnimation.HasSequence(idleSequence))
				{
					if (idleDelay > 0 && --idleDelay == 0)
					{
						state = AnimationState.IdleAnimating;
						DefaultAnimation.PlayThen(idleSequence, () =>
						{
							DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.StandSequences.Random(Game.CosmeticRandom)));
							state = AnimationState.Waiting;
						});
					}
				}
				else
				{
					DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.StandSequences.Random(Game.CosmeticRandom)));
					state = AnimationState.Waiting;
				}
			}
		}

		enum AnimationState
		{
			Idle,
			Attacking,
			Moving,
			Waiting,
			IdleAnimating
		}
	}
}
