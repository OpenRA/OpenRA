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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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
		public readonly Dictionary<string, int> DeathTypes = new Dictionary<string, int>();

		public static object LoadDeathTypes(MiniYaml yaml)
		{
			var md = yaml.ToDictionary();

			return md.ContainsKey("DeathTypes")
				? md["DeathTypes"].ToDictionary(my => FieldLoader.GetValue<int>("(value)", my.Value))
				: new Dictionary<string, int>();
		}

		public object Create(ActorInitializer init) { return new WithDeathAnimation(init.Self, this); }
	}

	public class WithDeathAnimation : INotifyKilled
	{
		public readonly WithDeathAnimationInfo Info;
		readonly RenderSprites rs;

		public WithDeathAnimation(Actor self, WithDeathAnimationInfo info)
		{
			Info = info;
			rs = self.Trait<RenderSprites>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means. This includes being crushed
			// by a vehicle (Actors with Crushable trait will spawn CrushedSequence instead).
			if (e.Warhead == null || !(e.Warhead is DamageWarhead))
				return;

			var sequence = Info.DeathSequence;
			if (Info.UseDeathTypeSuffix)
			{
				var warhead = e.Warhead as DamageWarhead;
				var damageType = Info.DeathTypes.Keys.FirstOrDefault(warhead.DamageTypes.Contains);
				if (damageType == null)
					return;

				sequence += Info.DeathTypes[damageType];
			}

			var palette = Info.DeathSequencePalette;
			if (Info.DeathPaletteIsPlayerPalette)
				palette += self.Owner.InternalName;

			SpawnDeathAnimation(self, sequence, palette);
		}

		public void SpawnDeathAnimation(Actor self, string sequence, string palette)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Disposed)
					w.Add(new Corpse(w, self.CenterPosition, rs.GetImage(self), sequence, palette));
			});
		}
	}
}
