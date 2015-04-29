#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays a custom animation if conditions are satisfied.")]
	public class WithDecorationInfo : UpgradableTraitInfo, ITraitInfo
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for this decoration (can be animated).")]
		public readonly string Sequence = null;

		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		public readonly string Palette = "chrome";

		[Desc("Pixel offset relative to the top-left point of the actor's bounds.")]
		public readonly int2 Offset = int2.Zero;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Visual scale of the image.")]
		public readonly float Scale = 1f;

		[Desc("Should this be visible to allied players?")]
		public readonly bool ShowToAllies = true;

		[Desc("Should this be visible to enemy players?")]
		public readonly bool ShowToEnemies = false;

		public virtual object Create(ActorInitializer init) { return new WithDecoration(init.Self, this); }
	}

	public class WithDecoration : UpgradableTrait<WithDecorationInfo>, IRender
	{
		readonly WithDecorationInfo info;
		readonly string image;
		readonly Animation anim;

		public WithDecoration(Actor self, WithDecorationInfo info)
			: base(info)
		{
			this.info = info;
			image = info.Image ?? self.Info.Name;
			anim = new Animation(self.World, image);
			anim.Paused = () => self.World.Paused;
			anim.PlayRepeating(info.Sequence);
		}

		public virtual bool ShouldRender(Actor self) { return true; }

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

			if (self.IsDead || !self.IsInWorld)
				yield break;

			if (anim == null)
				yield break;

			var allied = self.Owner.IsAlliedWith(self.World.RenderPlayer);

			if (!allied && !info.ShowToEnemies)
				yield break;

			if (allied && !info.ShowToAllies)
				yield break;

			if (!ShouldRender(self))
				yield break;

			if (self.World.FogObscures(self))
				yield break;

			var pxPos = wr.ScreenPxPosition(self.CenterPosition);
			var actorBounds = self.Bounds;
			actorBounds.Offset(pxPos.X, pxPos.Y);
			pxPos = new int2(actorBounds.Left, actorBounds.Top);

			var img = anim.Image;
			var imgSize = img.Size.ToInt2();
			pxPos = pxPos.WithX(pxPos.X + imgSize.X / 2).WithY(pxPos.Y + imgSize.Y / 2);

			pxPos += info.Offset;
			var renderPos = wr.Position(pxPos);

			anim.Tick();

			yield return new SpriteRenderable(img, renderPos, WVec.Zero, info.ZOffset, wr.Palette(info.Palette), info.Scale, true);
		}
	}
}
