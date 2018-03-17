#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Flags]
	public enum ReferencePoints
	{
		Center = 0,
		Top = 1,
		Bottom = 2,
		Left = 4,
		Right = 8,
	}

	[Desc("Displays a custom UI overlay relative to the actor's mouseover bounds.")]
	public class WithDecorationInfo : ConditionalTraitInfo, Requires<IDecorationBoundsInfo>
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for this decoration (can be animated).")]
		public readonly string Sequence = null;

		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "chrome";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Player stances who can view the decoration.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Should this be visible only when selected?")]
		public readonly bool RequiresSelection = false;

		public override object Create(ActorInitializer init) { return new WithDecoration(init.Self, this); }
	}

	public class WithDecoration : ConditionalTrait<WithDecorationInfo>, ITick, IRenderAboveShroud, IRenderAboveShroudWhenSelected
	{
		protected readonly Animation Anim;
		readonly IDecorationBounds[] decorationBounds;
		readonly string image;

		public WithDecoration(Actor self, WithDecorationInfo info)
			: base(info)
		{
			image = info.Image ?? self.Info.Name;
			Anim = new Animation(self.World, image, () => self.World.Paused);
			Anim.PlayRepeating(info.Sequence);
			decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();
		}

		protected virtual bool ShouldRender(Actor self)
		{
			if (self.World.RenderPlayer != null)
			{
				var stance = self.Owner.Stances[self.World.RenderPlayer];
				if (!Info.ValidStances.HasStance(stance))
					return false;
			}

			return true;
		}

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return !Info.RequiresSelection ? RenderInner(self, wr) : SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return Info.RequiresSelection ? RenderInner(self, wr) : SpriteRenderable.None;
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return true; } }
		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return true; } }

		IEnumerable<IRenderable> RenderInner(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || self.IsDead || !self.IsInWorld || Anim == null)
				return Enumerable.Empty<IRenderable>();

			if (!ShouldRender(self) || self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var bounds = decorationBounds.FirstNonEmptyBounds(self, wr);
			var halfSize = (0.5f * Anim.Image.Size.XY).ToInt2();

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = -halfSize;
			if (Info.ReferencePoint.HasFlag(ReferencePoints.Top))
			{
				boundsOffset -= new int2(0, bounds.Height / 2);
				sizeOffset += new int2(0, halfSize.Y);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
			{
				boundsOffset += new int2(0, bounds.Height / 2);
				sizeOffset -= new int2(0, halfSize.Y);
			}

			if (Info.ReferencePoint.HasFlag(ReferencePoints.Left))
			{
				boundsOffset -= new int2(bounds.Width / 2, 0);
				sizeOffset += new int2(halfSize.X, 0);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Right))
			{
				boundsOffset += new int2(bounds.Width / 2, 0);
				sizeOffset -= new int2(halfSize.X, 0);
			}

			var pxPos = wr.Viewport.WorldToViewPx(boundsOffset) + sizeOffset;
			return new IRenderable[]
			{
				new UISpriteRenderable(Anim.Image, self.CenterPosition, pxPos, Info.ZOffset, wr.Palette(Info.Palette + (Info.IsPlayerPalette ? self.Owner.InternalName : "")), 1f)
			};
		}

		void ITick.Tick(Actor self) { Anim.Tick(); }
	}
}
