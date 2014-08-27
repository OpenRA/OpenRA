#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("This actor has a death animation.")]
	public class WithDeathAnimationInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		[Desc("Sequence to play when this actor is killed by a warhead.")]
		public readonly string DeathSequence = "die";
		public readonly string DeathSequencePalette = "player";
		[Desc("Custom death animation palette is a player palette BaseName")]
		public readonly bool DeathPaletteIsPlayerPalette = true;
		[Desc("Should DeathType-specific sequences be used (sequence name = DeathSequence + DeathType).")]
		public readonly bool UseDeathTypeSuffix = true;
		[Desc("Sequence to play when this actor is crushed.")]
		public readonly string CrushedSequence = "die-crushed";
		public readonly string CrushedSequencePalette = "effect";
		[Desc("Custom crushed animation palette is a player palette BaseName")]
		public readonly bool CrushedPaletteIsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDeathAnimation(init.self, this); }
	}

	public class WithDeathAnimation : INotifyKilled
	{
		public readonly WithDeathAnimationInfo Info;
		readonly RenderSimple renderSimple;

		public WithDeathAnimation(Actor self, WithDeathAnimationInfo info)
		{
			Info = info;
			renderSimple = self.Trait<RenderSimple>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means. This includes being crushed
			// by a vehicle (Actors with Crushable trait will spawn CrushedSequence instead).
			if (e.Warhead == null)
				return;

			var sequence = Info.DeathSequence;
			if (Info.UseDeathTypeSuffix)
				sequence += e.Warhead.DeathType;

			var palette = Info.DeathSequencePalette;
			if (Info.DeathPaletteIsPlayerPalette)
				palette += self.Owner.InternalName;

			SpawnDeathAnimation(self, sequence, palette);
		}

		public void SpawnDeathAnimation(Actor self, string sequence, string palette)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Destroyed)
					w.Add(new Corpse(w, self.CenterPosition, renderSimple.GetImage(self), sequence, palette));
			});
		}
	}
}
