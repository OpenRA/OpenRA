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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays a text overlay relative to the selection box.")]
	public class WithTextDecorationInfo : UpgradableTraitInfo
	{
		[FieldLoader.Require] [Translate] public readonly string Text = null;

		public readonly string Font = "TinyBold";

		[Desc("Display in this color when not using the player color.")]
		public readonly Color Color = Color.White;

		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Player stances who can view the decoration.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Should this be visible only when selected?")]
		public readonly bool RequiresSelection = false;

		public override object Create(ActorInitializer init) { return new WithTextDecoration(init.Self, this); }
	}

	public class WithTextDecoration : UpgradableTrait<WithTextDecorationInfo>, IRender, IPostRenderSelection, INotifyCapture
	{
		readonly Actor self;
		readonly SpriteFont font;

		Color color;

		public WithTextDecoration(Actor self, WithTextDecorationInfo info)
			: base(info)
		{
			this.self = self;

			if (!Game.Renderer.Fonts.TryGetValue(info.Font, out font))
				throw new YamlException("Could not find font '{0}'".F(info.Font));

			color = Info.UsePlayerColor ? self.Owner.Color.RGB : Info.Color;
		}

		public virtual bool ShouldRender(Actor self) { return true; }

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return !Info.RequiresSelection ? RenderInner(self, wr) : Enumerable.Empty<IRenderable>();
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			return Info.RequiresSelection ? RenderInner(self, wr) : Enumerable.Empty<IRenderable>();
		}

		IEnumerable<IRenderable> RenderInner(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			if (self.World.RenderPlayer != null)
			{
				var stance = self.Owner.Stances[self.World.RenderPlayer];
				if (!Info.ValidStances.HasStance(stance))
					return Enumerable.Empty<IRenderable>();
			}

			if (!ShouldRender(self) || self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var bounds = self.VisualBounds;
			var halfSize = font.Measure(Info.Text) / 2;

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = new int2();
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

			var screenPos = wr.ScreenPxPosition(self.CenterPosition) + boundsOffset + sizeOffset;
			return new IRenderable[] { new TextRenderable(font, wr.ProjectedPosition(screenPos), Info.ZOffset, color, Info.Text) };
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (Info.UsePlayerColor)
				color = newOwner.Color.RGB;
		}
	}
}
