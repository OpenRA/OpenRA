#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will play a fire overlay animation over its body and take damage over time.")]
	class BurnsInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Image = "fire";
		[SequenceReference("Image")] public readonly string Anim = "1";

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Amount of damage received per Interval ticks.")]
		public readonly int Damage = 1;

		[Desc("Delay between receiving damage.")]
		public readonly int Interval = 8;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly HashSet<string> DamageTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Burns(init.Self, this); }
	}

	class Burns : PausableConditionalTrait<BurnsInfo>, ITick, ISync
	{
		readonly BurnsInfo info;
		[Sync] int ticks;

		public Burns(Actor self, BurnsInfo info)
			: base(info)
		{
			this.info = info;

			var anim = new Animation(self.World, info.Image, () => 0, () => IsTraitPaused);
			anim.IsDecoration = true;
			var animWithOffset = new AnimationWithOffset(anim, () => WVec.Zero, () => IsTraitDisabled, p => RenderUtils.ZOffsetFromCenter(self, p, 1));
			self.Trait<RenderSprites>().Add(animWithOffset, info.Palette, info.IsPlayerPalette);
			anim.PlayRepeating(info.Anim);
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				self.InflictDamage(self, new Damage(info.Damage, Info.DamageTypes));
				ticks = info.Interval;
			}
		}
	}
}
