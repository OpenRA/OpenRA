#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class IsometricSelectionDecorationsInfo : SelectionDecorationsBaseInfo, Requires<IsometricSelectableInfo>
	{
		public override object Create(ActorInitializer init) { return new IsometricSelectionDecorations(init.Self, this); }
	}

	public class IsometricSelectionDecorations : SelectionDecorationsBase, IRender
	{
		readonly IsometricSelectable selectable;

		public IsometricSelectionDecorations(Actor self, IsometricSelectionDecorationsInfo info)
			: base(info)
		{
			selectable = self.Trait<IsometricSelectable>();
		}

		protected override int2 GetDecorationPosition(Actor self, WorldRenderer wr, DecorationPosition pos)
		{
			var bounds = selectable.DecorationBounds(self, wr);
			switch (pos)
			{
				case DecorationPosition.TopLeft: return bounds.Vertices[1];
				case DecorationPosition.TopRight: return bounds.Vertices[5];
				case DecorationPosition.BottomLeft: return bounds.Vertices[2];
				case DecorationPosition.BottomRight: return bounds.Vertices[4];
				case DecorationPosition.Top: return new int2((bounds.Vertices[1].X + bounds.Vertices[5].X) / 2, bounds.Vertices[1].Y);
				default: return bounds.BoundingRect.TopLeft + new int2(bounds.BoundingRect.Size.Width / 2, bounds.BoundingRect.Size.Height / 2);
			}
		}

		protected override IEnumerable<IRenderable> RenderSelectionBox(Actor self, WorldRenderer wr, Color color)
		{
			var bounds = selectable.DecorationBounds(self, wr);
			yield return new IsometricSelectionBoxAnnotationRenderable(self, bounds, color);
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			if (!displayHealth && !displayExtra)
				yield break;

			var bounds = selectable.DecorationBounds(self, wr);
			yield return new IsometricSelectionBarsAnnotationRenderable(self, bounds, displayHealth, displayExtra);
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield return selectable.DecorationBounds(self, wr).BoundingRect;
		}
	}
}
