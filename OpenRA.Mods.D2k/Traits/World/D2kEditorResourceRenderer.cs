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

using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	using ClearSides = D2kResourceRenderer.ClearSides;

	[Desc("Used to render spice with round borders.")]
	public class D2kEditorResourceRendererInfo : EditorResourceRendererInfo
	{
		public override object Create(ActorInitializer init) { return new D2kEditorResourceRenderer(init.Self, this); }
	}

	public class D2kEditorResourceRenderer : EditorResourceRenderer
	{
		readonly Actor self;

		public D2kEditorResourceRenderer(Actor self, D2kEditorResourceRendererInfo info)
			: base(self, info)
		{
			this.self = self;
		}

		protected override void UpdateRenderedSprite(CPos cell, RendererCellContents content)
		{
			var density = content.Density;
			if (density > 0)
			{
				var type = content.Type;

				// The call chain for this method (that starts with AddDirtyCell()) guarantees
				// that the new content type would still be suitable for this renderer,
				// but that is a bit too fragile to rely on in case the code starts changing.
				if (!Info.RenderTypes.Contains(type.Info.Type))
					return;

				int index;
				var clear = FindClearSides(type, cell);
				if (clear == ClearSides.None)
				{
					var sprites = D2kResourceRenderer.Variants[content.Variant];
					var frame = density > type.Info.MaxDensity / 2 ? 1 : 0;
					UpdateSpriteLayers(cell, type.Variants.First().Value, sprites[frame], type.Palette);
				}
				else if (D2kResourceRenderer.SpriteMap.TryGetValue(clear, out index))
					UpdateSpriteLayers(cell, type.Variants.First().Value, index, type.Palette);
				else
					UpdateSpriteLayers(cell, null, 0, type.Palette);
			}
			else
				UpdateSpriteLayers(cell, null, 0, null);
		}

		protected override string ChooseRandomVariant(ResourceType t)
		{
			return D2kResourceRenderer.Variants.Keys.Random(Game.CosmeticRandom);
		}

		bool CellContains(CPos c, ResourceType t)
		{
			return self.World.Map.Contains(c) && ResourceLayer.GetResourceType(c) == t;
		}

		ClearSides FindClearSides(ResourceType t, CPos p)
		{
			var ret = ClearSides.None;
			if (!CellContains(p + new CVec(0, -1), t))
				ret |= ClearSides.Top | ClearSides.TopLeft | ClearSides.TopRight;

			if (!CellContains(p + new CVec(-1, 0), t))
				ret |= ClearSides.Left | ClearSides.TopLeft | ClearSides.BottomLeft;

			if (!CellContains(p + new CVec(1, 0), t))
				ret |= ClearSides.Right | ClearSides.TopRight | ClearSides.BottomRight;

			if (!CellContains(p + new CVec(0, 1), t))
				ret |= ClearSides.Bottom | ClearSides.BottomLeft | ClearSides.BottomRight;

			if (!CellContains(p + new CVec(-1, -1), t))
				ret |= ClearSides.TopLeft;

			if (!CellContains(p + new CVec(1, -1), t))
				ret |= ClearSides.TopRight;

			if (!CellContains(p + new CVec(-1, 1), t))
				ret |= ClearSides.BottomLeft;

			if (!CellContains(p + new CVec(1, 1), t))
				ret |= ClearSides.BottomRight;

			return ret;
		}
	}
}
