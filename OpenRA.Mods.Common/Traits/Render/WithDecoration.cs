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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum ReferencePoints
	{
		Top = 0,
		VCenter = 1,
		Bottom = 2,

		Left = 0 << 2,
		HCenter = 1 << 2,
		Right = 2 << 2,
	}

	[Desc("Displays a custom animation if conditions are satisfied.")]
	public class WithDecorationInfo : UpgradableTraitInfo
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for this decoration (can be animated).")]
		public readonly string Sequence = null;

		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		public readonly string Palette = "chrome";

		[Desc("Point in the actor's bounding box used as reference for offsetting the decoration image." +
			"Possible values are any combination of Top, VCenter, Bottom and Left, HCenter, Right separated by a comma.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("Pixel offset relative to the actor's bounding box' reference point.")]
		public readonly int2 Offset = int2.Zero;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Visual scale of the image.")]
		public readonly float Scale = 1f;

		[Desc("Should this be visible to allied players?")]
		public readonly bool ShowToAllies = true;

		[Desc("Should this be visible to enemy players?")]
		public readonly bool ShowToEnemies = false;

		public override object Create(ActorInitializer init) { return new WithDecoration(init.Self, this); }
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

		public void PlaySingleFrame(int frame)
		{
			anim.PlayFetchIndex(info.Sequence, () => frame);
		}

		public virtual bool ShouldRender(Actor self) { return true; }

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				return Enumerable.Empty<IRenderable>();

			if (self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			if (anim == null)
				return Enumerable.Empty<IRenderable>();

			var allied = self.Owner.IsAlliedWith(self.World.RenderPlayer);

			if (!allied && !info.ShowToEnemies)
				return Enumerable.Empty<IRenderable>();

			if (allied && !info.ShowToAllies)
				return Enumerable.Empty<IRenderable>();

			if (!ShouldRender(self))
				return Enumerable.Empty<IRenderable>();

			if (self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var pxPos = wr.ScreenPxPosition(self.CenterPosition);
			var actorBounds = self.Bounds;
			actorBounds.Offset(pxPos.X, pxPos.Y);

			var img = anim.Image;
			var imgSize = img.Size.ToInt2();

			switch (info.ReferencePoint & (ReferencePoints)3)
			{
				case ReferencePoints.Top:
					pxPos = pxPos.WithY(actorBounds.Top + imgSize.Y / 2);
					break;
				case ReferencePoints.VCenter:
					pxPos = pxPos.WithY((actorBounds.Top + actorBounds.Bottom) / 2);
					break;
				case ReferencePoints.Bottom:
					pxPos = pxPos.WithY(actorBounds.Bottom - imgSize.Y / 2);
					break;
			}

			switch (info.ReferencePoint & (ReferencePoints)(3 << 2))
			{
				case ReferencePoints.Left:
					pxPos = pxPos.WithX(actorBounds.Left + imgSize.X / 2);
					break;
				case ReferencePoints.HCenter:
					pxPos = pxPos.WithX((actorBounds.Left + actorBounds.Right) / 2);
					break;
				case ReferencePoints.Right:
					pxPos = pxPos.WithX(actorBounds.Right - imgSize.X / 2);
					break;
			}

			pxPos += info.Offset;

			// HACK: Because WorldRenderer.Position() does not care about terrain height at the location
			var renderPos = wr.ProjectedPosition(pxPos);
			renderPos = new WPos(renderPos.X, renderPos.Y + self.CenterPosition.Z, self.CenterPosition.Z);

			anim.Tick();

			return new IRenderable[] { new SpriteRenderable(img, renderPos, WVec.Zero, info.ZOffset, wr.Palette(info.Palette), info.Scale, true) };
		}
	}
}
