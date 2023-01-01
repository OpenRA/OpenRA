#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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

		[SequenceReference]
		public readonly string MoveSequence = "run";

		[SequenceReference]
		public readonly string DefaultAttackSequence = null;

		[SequenceReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Attack sequence to use for each armament.",
			"A dictionary of [armament name]: [sequence name(s)].",
			"Multiple sequence names can be defined to specify per-burst animations.")]
		public readonly Dictionary<string, string[]> AttackSequences = new Dictionary<string, string[]>();

		[SequenceReference]
		public readonly string[] IdleSequences = Array.Empty<string>();

		[SequenceReference]
		public readonly string[] StandSequences = { "stand" };

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithInfantryBody(init, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image, init.GetFacing());
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), StandSequences.First()));

			if (IsPlayerPalette)
				p = init.WorldRenderer.Palette(Palette + init.Get<OwnerInit>().InternalName);
			else if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	public class WithInfantryBody : ConditionalTrait<WithInfantryBodyInfo>, ITick, INotifyAttack, INotifyIdle
	{
		readonly IMove move;
		protected readonly Animation DefaultAnimation;

		bool dirty;
		string idleSequence;
		int idleDelay;
		protected AnimationState state;
		IRenderInfantrySequenceModifier rsm;

		bool IsModifyingSequence => rsm != null && rsm.IsModifyingSequence;
		bool wasModifying;

		// Allow subclasses to override the info that we use for rendering
		protected virtual WithInfantryBodyInfo GetDisplayInfo()
		{
			return Info;
		}

		public WithInfantryBody(ActorInitializer init, WithInfantryBodyInfo info)
			: base(info)
		{
			var self = init.Self;
			var rs = self.Trait<RenderSprites>();

			DefaultAnimation = new Animation(init.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
			rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled), info.Palette, info.IsPlayerPalette);
			PlayStandAnimation(self);

			move = init.Self.Trait<IMove>();
		}

		protected override void Created(Actor self)
		{
			rsm = self.TraitOrDefault<IRenderInfantrySequenceModifier>();
			var info = GetDisplayInfo();
			idleDelay = self.World.SharedRandom.Next(info.MinIdleDelay, info.MaxIdleDelay);

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
			return GetDisplayInfo().IdleSequences.Length > 0 && !IsModifyingSequence;
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

		protected virtual void Attacking(Actor self, Armament a, Barrel barrel)
		{
			var info = GetDisplayInfo();
			var sequence = info.DefaultAttackSequence;

			if (info.AttackSequences.TryGetValue(a.Info.Name, out var sequences) && sequences.Length > 0)
			{
				sequence = sequences[0];

				// Find the sequence corresponding to this barrel/burst.
				if (barrel != null && sequences.Length > 1)
					for (var i = 0; i < sequences.Length; i++)
						if (a.Barrels[i] == barrel)
							sequence = sequences[i];
			}

			if (!string.IsNullOrEmpty(sequence) && DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, sequence)))
			{
				state = AnimationState.Attacking;
				DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, sequence), () => PlayStandAnimation(self));
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
			// HACK: The FrameEndTask makes sure that this runs after Tick(), preventing that from
			// overriding the animation when an infantry unit stops to attack
			self.World.AddFrameEndTask(_ => Attacking(self, a, barrel));
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel) { }

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

			if ((state != AnimationState.Moving || dirty) && move.CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
			{
				state = AnimationState.Moving;
				DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, GetDisplayInfo().MoveSequence));
			}
			else if (((state == AnimationState.Moving || dirty) && !move.CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
				|| ((state == AnimationState.Idle || state == AnimationState.IdleAnimating) && !self.IsIdle))
				PlayStandAnimation(self);

			dirty = false;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (!AllowIdleAnimation(self))
				return;

			if (state == AnimationState.Waiting)
			{
				state = AnimationState.Idle;
				var info = GetDisplayInfo();
				idleSequence = info.IdleSequences.Random(self.World.SharedRandom);
				idleDelay = self.World.SharedRandom.Next(info.MinIdleDelay, info.MaxIdleDelay);
			}
			else if (state == AnimationState.Idle && idleDelay > 0 && --idleDelay == 0)
			{
				state = AnimationState.IdleAnimating;
				DefaultAnimation.PlayThen(idleSequence, () => PlayStandAnimation(self));
			}
		}

		protected enum AnimationState
		{
			Idle,
			Attacking,
			Moving,
			Waiting,
			IdleAnimating
		}
	}
}
