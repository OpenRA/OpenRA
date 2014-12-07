#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderInfantryInfo : RenderSimpleInfo, Requires<IMoveInfo>
	{
		public readonly int MinIdleWaitTicks = 30;
		public readonly int MaxIdleWaitTicks = 110;
		public readonly string MoveAnimation = "run";
		public readonly string AttackAnimation = "shoot";
		public readonly string[] IdleAnimations = { };
		public readonly string[] StandAnimations = { "stand" };

		public override object Create(ActorInitializer init) { return new RenderInfantry(init.self, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var facing = 0;
			var ifacing = init.Actor.Traits.GetOrDefault<IFacingInfo>();
			if (ifacing != null)
				facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing();

			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(StandAnimations.First());
			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public override int QuantizedBodyFacings(SequenceProvider sequenceProvider, ActorInfo ai)
		{
			return sequenceProvider.GetSequence(RenderSprites.GetImage(ai), StandAnimations.First()).Facings;
		}
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyIdle
	{
		readonly RenderInfantryInfo info;
		readonly IMove move;
		bool dirty = false;
		string idleSequence;
		int idleDelay;
		AnimationState state;

		IRenderInfantrySequenceModifier rsm;
		bool isModifyingSequence { get { return rsm != null && rsm.IsModifyingSequence; } }
		bool wasModifying;

		public RenderInfantry(Actor self, RenderInfantryInfo info)
			: base(self, MakeFacingFunc(self))
		{
			this.info = info;
			DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			state = AnimationState.Waiting;
			move = self.Trait<IMove>();
			rsm = self.TraitOrDefault<IRenderInfantrySequenceModifier>();
		}

		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = isModifyingSequence ? rsm.SequencePrefix : "";

			if (DefaultAnimation.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;
			else
				return baseSequence;
		}

		protected virtual bool AllowIdleAnimation(Actor self)
		{
			return !isModifyingSequence;
		}

		public void Attacking(Actor self, Target target)
		{
			state = AnimationState.Attacking;
			if (DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, info.AttackAnimation)))
				DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, info.AttackAnimation), () => state = AnimationState.Idle);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			Attacking(self, target);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (rsm != null)
			{
				if (wasModifying != rsm.IsModifyingSequence)
					dirty = true;

				wasModifying = rsm.IsModifyingSequence;
			}

			if ((state == AnimationState.Moving || dirty) && !move.IsMoving)
			{
				state = AnimationState.Waiting;
				DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
			}
			else if ((state != AnimationState.Moving || dirty) && move.IsMoving)
			{
				state = AnimationState.Moving;
				DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.MoveAnimation));
			}

			dirty = false;
		}

		public void TickIdle(Actor self)
		{
			if (state != AnimationState.Idle && state != AnimationState.IdleAnimating)
			{
				DefaultAnimation.PlayFetchIndex(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)), () => 0);
				state = AnimationState.Idle;

				if (info.IdleAnimations.Length > 0)
				{
					idleSequence = info.IdleAnimations.Random(self.World.SharedRandom);
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
							DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)));
							state = AnimationState.Waiting;
						});
					}
				}
				else
				{
					DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, info.StandAnimations.Random(Game.CosmeticRandom)));
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
