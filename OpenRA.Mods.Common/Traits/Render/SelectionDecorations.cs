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
	public class SelectionDecorationsInfo : SelectionDecorationsBaseInfo, Requires<InteractableInfo>
	{
		public override object Create(ActorInitializer init) { return new SelectionDecorations(init.Self, this); }
	}

	public class SelectionDecorations : SelectionDecorationsBase, IRender
	{
		readonly Interactable interactable;

		public SelectionDecorations(Actor self, SelectionDecorationsInfo info)
			: base(info)
		{
			interactable = self.Trait<Interactable>();
		}

		protected override int2 GetDecorationPosition(Actor self, WorldRenderer wr, DecorationPosition pos)
		{
			var bounds = interactable.DecorationBounds(self, wr);
			switch (pos)
			{
				case DecorationPosition.TopLeft: return bounds.TopLeft;
				case DecorationPosition.TopRight: return bounds.TopRight;
				case DecorationPosition.BottomLeft: return bounds.BottomLeft;
				case DecorationPosition.BottomRight: return bounds.BottomRight;
				case DecorationPosition.Top: return new int2(bounds.Left + bounds.Size.Width / 2, bounds.Top);
				default: return bounds.TopLeft + new int2(bounds.Size.Width / 2, bounds.Size.Height / 2);
			}
		}

		protected override IEnumerable<IRenderable> RenderSelectionBox(Actor self, WorldRenderer wr, Color color)
		{
			var bounds = interactable.DecorationBounds(self, wr);
			yield return new SelectionBoxAnnotationRenderable(self, bounds, color);
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			// Don't render the selection bars for non-selectable actors
			if (!(interactable is Selectable) || (!displayHealth && !displayExtra))
				yield break;

			var bounds = interactable.DecorationBounds(self, wr);
			yield return new SelectionBarsAnnotationRenderable(self, bounds, displayHealth, displayExtra);
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield return interactable.DecorationBounds(self, wr);
		}
	}
}
