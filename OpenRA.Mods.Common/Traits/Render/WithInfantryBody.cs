#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithInfantryBodyInfo : ConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<IMoveInfo>, Requires<RenderSpritesInfo>
	{
		public readonly int MinIdleDelay = 30;
		public readonly int MaxIdleDelay = 110;

		[SequenceReference] public readonly string MoveSequence = "run";
		[SequenceReference] public readonly string DefaultAttackSequence = null;

		// TODO: [SequenceReference] isn't smart enough to use Dictionaries.
		[Desc("Attack sequence to use for each armament.")]
		public readonly Dictionary<string, string> AttackSequences = new Dictionary<string, string>();
		[SequenceReference] public readonly string[] IdleSequences = { };
		[SequenceReference] public readonly string[] StandSequences = { "stand" };

		public override object Create(ActorInitializer init) { return new WithInfantryBody(init, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image, init.GetFacing());
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), StandSequences.First()));
			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class WithInfantryBody : ConditionalTrait<WithInfantryBodyInfo>, ITick, INotifyAttack, INotifyIdle
	{
		readonly IMove move;
		protected readonly Animation DefaultAnimation;

		bool dirty;
		string idleSequence;
		int idleDelay;
		AnimationState state;
		IRenderInfantrySequenceModifier rsm;

		bool IsModifyingSequence { get { return rsm != null && rsm.IsModifyingSequence; } }
		bool wasModifying;

		public WithInfantryBody(ActorInitializer init, WithInfantryBodyInfo info)
			: base(info)
		{
			var self = init.Self;
			var rs = self.Trait<RenderSprites>();

			DefaultAnimation = new Animation(init.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
			rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled));
			PlayStandAnimation(self);

			move = init.Self.Trait<IMove>();
		}

		public void PlayStandAnimation(Actor self)
		{
			state = AnimationState.Waiting;

			var sequence = DefaultAnimation.GetRandomExistingSequence(Info.StandSequences, Game.CosmeticRandom);
			if (sequence != null)
			{
				var normalized = NormalizeInfantrySequence(self, sequence);
				DefaultAnimation.PlayRepeating(normalized);
			}
		}

		protected override void Created(Actor self)
		{
			rsm = self.TraitOrDefault<IRenderInfantrySequenceModifier>();

			base.Created(self);
		}

		protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = IsModifyingSequence ? rsm.SequencePrefix : "";

			if (DefaultAnimation.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;

			return baseSequence;
		}

		protected virtual bool AllowIdleAnimation(Actor self)
		{
			return !IsModifyingSequence;
		}

		public void Attacking(Actor self, Target target, Armament a)
		{
			string sequence;
			if (!Info.AttackSequences.TryGetValue(a.Info.Name, out sequence))
				sequence = Info.DefaultAttackSequence;

			if (!string.IsNullOrEmpty(sequence) && DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, sequence)))
			{
				state = AnimationState.Attacking;
				DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, sequence), () => PlayStandAnimation(self));
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			Attacking(self, target, a);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			if (rsm != null)
			{
				if (wasModifying != rsm.IsModifyingSequence)
					dirty = true;

				wasModifying = rsm.IsModifyingSequence;
			}

			if ((state != AnimationState.Moving || dirty) && move.CurrentMovementTypes.HasFlag(MovementType.Horizontal))
			{
				state = AnimationState.Moving;
				DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, Info.MoveSequence));
			}
			else if (((state == AnimationState.Moving || dirty) && !move.CurrentMovementTypes.HasFlag(MovementType.Horizontal))
				|| ((state == AnimationState.Idle || state == AnimationState.IdleAnimating) && !self.IsIdle))
				PlayStandAnimation(self);

			dirty = false;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (state != AnimationState.Idle && state != AnimationState.IdleAnimating && state != AnimationState.Attacking)
			{
				PlayStandAnimation(self);
				state = AnimationState.Idle;

				if (Info.IdleSequences.Length > 0)
				{
					idleSequence = Info.IdleSequences.Random(self.World.SharedRandom);
					idleDelay = self.World.SharedRandom.Next(Info.MinIdleDelay, Info.MaxIdleDelay);
				}
			}
			else if (AllowIdleAnimation(self))
			{
				if (idleSequence != null && DefaultAnimation.HasSequence(idleSequence))
				{
					if (idleDelay > 0 && --idleDelay == 0)
					{
						state = AnimationState.IdleAnimating;
						DefaultAnimation.PlayThen(idleSequence, () => PlayStandAnimation(self));
					}
				}
				else
					PlayStandAnimation(self);
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
