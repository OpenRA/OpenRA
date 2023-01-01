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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays a custom UI overlay relative to the actor's mouseover bounds.")]
	public class WithDecorationInfo : WithDecorationBaseInfo
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[FieldLoader.Require]
		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence used for this decoration (can be animated).")]
		public readonly string Sequence = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		public readonly string Palette = "chrome";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithDecoration(init.Self, this); }
	}

	public class WithDecoration : WithDecorationBase<WithDecorationInfo>, ITick
	{
		protected Animation anim;
		readonly string image;

		public WithDecoration(Actor self, WithDecorationInfo info)
			: base(self, info)
		{
			image = info.Image ?? self.Info.Name;
			anim = new Animation(self.World, image, () => self.World.Paused);
			anim.PlayRepeating(info.Sequence);
		}

		protected virtual PaletteReference GetPalette(Actor self, WorldRenderer wr)
		{
			return wr.Palette(Info.Palette + (Info.IsPlayerPalette ? self.Owner.InternalName : ""));
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			if (anim == null)
				return Enumerable.Empty<IRenderable>();

			return new IRenderable[]
			{
				new UISpriteRenderable(anim.Image, self.CenterPosition, screenPos - (0.5f * anim.Image.Size.XY).ToInt2(), 0, GetPalette(self, wr))
			};
		}

		void ITick.Tick(Actor self) { anim.Tick(); }
	}
}
