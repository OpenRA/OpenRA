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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("This actor has a death animation.")]
	public class WithDeathAnimationInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference(prefix: true)]
		[Desc("Sequence prefix to play when this actor is killed by a warhead.")]
		public readonly string DeathSequence = "die";

		[PaletteReference(nameof(DeathPaletteIsPlayerPalette))]
		[Desc("The palette used for `DeathSequence`.")]
		public readonly string DeathSequencePalette = "player";

		[Desc("Custom death animation palette is a player palette BaseName")]
		public readonly bool DeathPaletteIsPlayerPalette = true;

		[Desc("Should DeathType-specific sequences be used (sequence name = DeathSequence + DeathType).")]
		public readonly bool UseDeathTypeSuffix = true; // TODO: check the complete sequence with lint rules

		[SequenceReference]
		[Desc("Sequence to play when this actor is crushed.")]
		public readonly string CrushedSequence = null;

		[PaletteReference(nameof(CrushedPaletteIsPlayerPalette))]
		[Desc("The palette used for `CrushedSequence`.")]
		public readonly string CrushedSequencePalette = "effect";

		[Desc("Custom crushed animation palette is a player palette BaseName")]
		public readonly bool CrushedPaletteIsPlayerPalette = false;

		[Desc("Death animations to use for each damage type (defined on the warheads).",
			"Is only used if UseDeathTypeSuffix is `True`.")]
		public readonly Dictionary<string, string[]> DeathTypes = new Dictionary<string, string[]>();

		[SequenceReference]
		[Desc("Sequence to use when the actor is killed by some non-standard means (e.g. suicide).")]
		public readonly string FallbackSequence = null;

		[Desc("Delay the spawn of the death animation by this many ticks.")]
		public readonly int Delay = 0;

		public override object Create(ActorInitializer init) { return new WithDeathAnimation(init.Self, this); }
	}

	public class WithDeathAnimation : ConditionalTrait<WithDeathAnimationInfo>, INotifyKilled, INotifyCrushed
	{
		readonly RenderSprites rs;
		bool crushed;

		public WithDeathAnimation(Actor self, WithDeathAnimationInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Actors with Crushable trait will spawn CrushedSequence.
			if (crushed || IsTraitDisabled)
				return;

			var palette = Info.DeathSequencePalette;
			if (Info.DeathPaletteIsPlayerPalette)
				palette += self.Owner.InternalName;

			// Killed by some non-standard means
			if (e.Damage.DamageTypes.IsEmpty)
			{
				if (Info.FallbackSequence != null)
					SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), Info.FallbackSequence, palette, Info.Delay);

				return;
			}

			var sequence = Info.DeathSequence;
			if (Info.UseDeathTypeSuffix)
			{
				var damageType = Info.DeathTypes.Keys.FirstOrDefault(e.Damage.DamageTypes.Contains);
				if (damageType == null)
					return;

				sequence += Info.DeathTypes[damageType].Random(self.World.SharedRandom);
			}

			SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), sequence, palette, Info.Delay);
		}

		public void SpawnDeathAnimation(Actor self, WPos pos, string image, string sequence, string palette, int delay)
		{
			self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, image, sequence, palette, delay: delay)));
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			crushed = true;

			if (Info.CrushedSequence == null)
				return;

			var crushPalette = Info.CrushedSequencePalette;
			if (Info.CrushedPaletteIsPlayerPalette)
				crushPalette += self.Owner.InternalName;

			SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), Info.CrushedSequence, crushPalette, Info.Delay);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }
	}
}
