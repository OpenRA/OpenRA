#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("This actor has a death animation.")]
	public class WithDeathAnimationInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence prefix to play when this actor is killed by a warhead.")]
		[SequenceReference(null, true)] public readonly string DeathSequence = "die";

		[Desc("The palette used for `DeathSequence`.")]
		[PaletteReference("DeathPaletteIsPlayerPalette")] public readonly string DeathSequencePalette = "player";

		[Desc("Custom death animation palette is a player palette BaseName")]
		public readonly bool DeathPaletteIsPlayerPalette = true;

		[Desc("Should DeathType-specific sequences be used (sequence name = DeathSequence + DeathType).")]
		public readonly bool UseDeathTypeSuffix = true; // TODO: check the complete sequence with lint rules

		[Desc("Sequence to play when this actor is crushed.")]
		[SequenceReference] public readonly string CrushedSequence = null;

		[Desc("The palette used for `CrushedSequence`.")]
		[PaletteReference("CrushedPaletteIsPlayerPalette")] public readonly string CrushedSequencePalette = "effect";

		[Desc("Custom crushed animation palette is a player palette BaseName")]
		public readonly bool CrushedPaletteIsPlayerPalette = false;

		[FieldLoader.LoadUsing("LoadDeathTypes")]
		[Desc("Death animation to use for each damage type (defined on the warheads).",
			"Is only used if UseDeathTypeSuffix is `True`.")]
		public readonly Dictionary<string, string> DeathTypes = new Dictionary<string, string>();

		[Desc("Sequence to use when the actor is killed by some non-standard means (e.g. suicide).")]
		[SequenceReference] public readonly string FallbackSequence = null;

		public static object LoadDeathTypes(MiniYaml yaml)
		{
			var md = yaml.ToDictionary();

			return md.ContainsKey("DeathTypes")
				? md["DeathTypes"].ToDictionary(my => FieldLoader.GetValue<string>("(value)", my.Value))
				: new Dictionary<string, string>();
		}

		public object Create(ActorInitializer init) { return new WithDeathAnimation(init.Self, this); }
	}

	public class WithDeathAnimation : INotifyKilled, INotifyCrushed
	{
		public readonly WithDeathAnimationInfo Info;
		readonly RenderSprites rs;
		bool crushed;

		public WithDeathAnimation(Actor self, WithDeathAnimationInfo info)
		{
			Info = info;
			rs = self.Trait<RenderSprites>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Actors with Crushable trait will spawn CrushedSequence.
			if (crushed)
				return;

			var palette = Info.DeathSequencePalette;
			if (Info.DeathPaletteIsPlayerPalette)
				palette += self.Owner.InternalName;

			// Killed by some non-standard means
			if (e.Warhead == null || !(e.Warhead is DamageWarhead))
			{
				if (Info.FallbackSequence != null)
					SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), Info.FallbackSequence, palette);

				return;
			}

			var sequence = Info.DeathSequence;
			if (Info.UseDeathTypeSuffix)
			{
				var warhead = e.Warhead as DamageWarhead;
				var damageType = Info.DeathTypes.Keys.FirstOrDefault(warhead.DamageTypes.Contains);
				if (damageType == null)
					return;

				sequence += Info.DeathTypes[damageType];
			}

			SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), sequence, palette);
		}

		public void SpawnDeathAnimation(Actor self, WPos pos, string image, string sequence, string palette)
		{
			self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, image, sequence, palette)));
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			crushed = true;

			if (Info.CrushedSequence == null)
				return;

			var crushPalette = Info.CrushedSequencePalette;
			if (Info.CrushedPaletteIsPlayerPalette)
				crushPalette += self.Owner.InternalName;

			SpawnDeathAnimation(self, self.CenterPosition, rs.GetImage(self), Info.CrushedSequence, crushPalette);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }
	}
}
